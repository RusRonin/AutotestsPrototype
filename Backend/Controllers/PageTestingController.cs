using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BaristaLabs.ChromeDevTools.Runtime;
using Core.Chrome;
using Core.Screenshot;
using Core.Awaiters;
using Microsoft.AspNetCore.Mvc;
using Core;
using Core.HtmlStorage;
using Core.RemoteDebuggerPortManager;
using Core.LogExtractor;
using Microsoft.Extensions.Logging;

namespace Backend.Controllers
{
    [Route( "api/[controller]" )]
    [ApiController]
    public class PageTestingController : ControllerBase, IScreenshotRequester, IDomAwaitingTester, 
        IScriptAwaitingTester, IPageLoadedAwaitingTester
    {
        private readonly ChromeProcess _chromeProcess;
        private readonly IHtmlStorage _htmlStorage;
        private readonly IScreenshotTaker _screenshotTaker;
        private readonly IRemoteDebuggerPortManager _remoteDebuggerPortManager;
        private readonly IDomLoadedAwaiter _domAwaiter;
        private readonly IScriptExecutionCompletedAwaiter _scriptAwaiter;
        private readonly IPageLoadedAwaiter _pageAwaiter;
        private readonly ILogExtractor _logExtractor;
        private readonly IChromeFrameManager _frameManager;
        private readonly ILogger<PageTestingController> _logger;
        private int ChromeRemoteDebuggerPort { get; set; }
        private bool ScreenshotsAreReady { get; set; }
        private bool DomIsReady { get; set; }
        private bool ScriptExecutionCompleted { get; set; }
        private bool PageIsReady { get; set; }

        public PageTestingController( ChromeProcess chromeProcess, IHtmlStorage htmlStorage, IScreenshotTaker screenshotTaker,
            IRemoteDebuggerPortManager remoteDebuggerPortManager, IDomLoadedAwaiter domAwaiter, 
            IScriptExecutionCompletedAwaiter scriptAwaiter, IPageLoadedAwaiter pageAwaiter,
            ILogExtractor logExtractor, IChromeFrameManager frameManager, ILogger<PageTestingController> logger )
        {
            _chromeProcess = chromeProcess;
            _htmlStorage = htmlStorage;
            _screenshotTaker = screenshotTaker;
            _remoteDebuggerPortManager = remoteDebuggerPortManager;
            _domAwaiter = domAwaiter;
            _scriptAwaiter = scriptAwaiter;
            _pageAwaiter = pageAwaiter;
            _logExtractor = logExtractor;
            _frameManager = frameManager;
            _logger = logger;
        }

        [HttpGet( "{base64Url}" )]
        public async Task<IActionResult> Get( [FromRoute] string base64Url, [FromQuery] int launchId )
        {
            byte[] base64EncodedBytes = System.Convert.FromBase64String( base64Url );
            string url = System.Text.Encoding.UTF8.GetString( base64EncodedBytes );
            _logger.LogInformation( $"Received testing request for {url} with id {launchId}" );

            ChromeRemoteDebuggerPort = _remoteDebuggerPortManager.AllocateRDPort();

            //as whole site crawling is not implemented, and we use only file system storage,
            //we have single resource with constant resourceId
            int resourceId = 1;

            TestingResult testingResult = new TestingResult();

            _chromeProcess.StartChrome( ChromeRemoteDebuggerPort );

            List<ChromeBrowserFrame> pageFrames = _frameManager.GetFramesByType( ChromeRemoteDebuggerPort, "page" );
            if ( pageFrames.Count == 0 )
            {
                _logger.LogError( $"Launch {launchId}: unable to launch Chrome correctly" );
                //what response code should we return if we can't launch chrome properly => can't test page?
                //400 for now, should be changed
                return BadRequest();
            }

            ChromeBrowserFrame workingFrame = pageFrames[ 0 ];

            _logger.LogInformation( $"Successfully started Chrome for launch {launchId}" );

            using ( var session = new ChromeSession( workingFrame.WebSocketDebuggerUrl ) )
            {
                long windowID = ( await session.Browser.GetWindowForTarget( new BaristaLabs.ChromeDevTools.Runtime.Browser.GetWindowForTargetCommand()
                {
                    TargetId = workingFrame.Id
                } ) ).WindowId;

                await session.Browser.SetWindowBounds( new BaristaLabs.ChromeDevTools.Runtime.Browser.SetWindowBoundsCommand()
                {
                    WindowId = windowID,
                    Bounds = new BaristaLabs.ChromeDevTools.Runtime.Browser.Bounds
                    {
                        WindowState = BaristaLabs.ChromeDevTools.Runtime.Browser.WindowState.Minimized
                    }
                } );

                await session.Page.Navigate( new BaristaLabs.ChromeDevTools.Runtime.Page.NavigateCommand()
                {
                    Url = url
                } );

                _logger.LogInformation( $"Launch {launchId}: awaiting page response loading" );

                PageIsReady = false;

                new Thread( delegate () { _pageAwaiter.Await( this, session ); } ).Start();

                while ( !PageIsReady )
                {
                    Thread.Sleep( 100 );
                }

                _logger.LogInformation( $"Launch {launchId}: awaiting DOM loading" );

                DomIsReady = false;
                ScriptExecutionCompleted = false;

                new Thread( delegate () { _domAwaiter.Await( this, session ); } ).Start();

                while ( !DomIsReady )
                {
                    Thread.Sleep( 100 );
                }


                _logger.LogInformation( $"Launch {launchId}: awaiting scripts" );

                Thread.Sleep( 10000 );

                _logger.LogInformation( $"Launch {launchId}: loading completed. Retrieving DOM" );

                var document = await session.DOM.GetDocument( new BaristaLabs.ChromeDevTools.Runtime.DOM.GetDocumentCommand()
                {
                    Depth = -1
                } );

                _logger.LogInformation( $"Launch {launchId}: saving HTML" );

                //DOM.GetOuterHTML won't work properly if used before DOM.GetDocument due to protocol limitation
                string html = session.DOM.GetOuterHTML( new BaristaLabs.ChromeDevTools.Runtime.DOM.GetOuterHTMLCommand
                {
                    NodeId = 1
                } ).Result.OuterHTML;

                _htmlStorage.SaveHtml( launchId, resourceId, html );

                _logger.LogInformation( $"Launch {launchId}: extracting links" );

                List<BaristaLabs.ChromeDevTools.Runtime.DOM.Node> tags = new List<BaristaLabs.ChromeDevTools.Runtime.DOM.Node>();
                tags.AddRange( document.Root.Children );
                while ( tags.Count > 0 )
                {
                    BaristaLabs.ChromeDevTools.Runtime.DOM.Node tag = tags[ 0 ];
                    tags.RemoveAt( 0 );
                    if ( tag.Children != null )
                    {
                        tags.AddRange( tag.Children );
                    }
                    if ( tag.NodeName.ToLower().Equals( "a" ) )
                    {
                        int hrefPosition = Array.IndexOf( tag.Attributes, "href" );
                        testingResult.Links.Add( tag.Attributes[ hrefPosition + 1 ] );
                    }
                }

                _logger.LogInformation( $"Launch {launchId}: extracting logs" );

                testingResult.Logs = await _logExtractor.Extract( session, ChromeRemoteDebuggerPort );

                _logger.LogInformation( $"Launch {launchId}: enqueuing for screenshots" );

                ScreenshotsAreReady = false;
                _screenshotTaker.TakeAllScreenshots( this, session, workingFrame.Id, launchId, resourceId );
                while ( !ScreenshotsAreReady )
                {
                    Thread.Sleep( 100 );
                }

                _logger.LogInformation( $"Launch {launchId}: testing completed, stopping Chrome" );

                _chromeProcess.StopChrome();
            }

            _remoteDebuggerPortManager.FreeRDPort( ChromeRemoteDebuggerPort );

            _logger.LogInformation( $"Launch {launchId}: Chrome stopped, returning testing results for {url}" );

            return Ok( testingResult );
        }

        [NonAction]
        public void NotifyScreenshotsAreReady()
        {
            ScreenshotsAreReady = true;
        }

        [NonAction]
        void IDomAwaitingTester.NotifyReady()
        {
            DomIsReady = true;
        }

        [NonAction]
        void IScriptAwaitingTester.NotifyReady()
        {
            ScriptExecutionCompleted = true;
        }

        [NonAction]
        void IPageLoadedAwaitingTester.NotifyReady()
        {
            PageIsReady = true;
        }
    }
}
