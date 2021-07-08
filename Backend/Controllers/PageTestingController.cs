using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BaristaLabs.ChromeDevTools.Runtime;
using Newtonsoft.Json;
using Core.Chrome;
using Core.Screenshot;
using Core.Awaiters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Core;
using Core.HtmlStorage;
using Core.RemoteDebuggerPortManager;

namespace Backend.Controllers
{
    [Route( "api/[controller]" )]
    [ApiController]
    public class PageTestingController : ControllerBase, IScreenshotRequester, IDomAwaitingTester, IScriptAwaitingTester, IPageLoadedAwaitingTester
    {
        private readonly ChromeProcess _chromeProcess;
        private readonly IHtmlStorage _htmlStorage;
        private readonly IScreenshotTaker _screenshotTaker;
        private readonly IRemoteDebuggerPortManager _remoteDebuggerPortManager;
        private readonly IDomLoadedAwaiter _domAwaiter;
        private readonly IScriptExecutionCompletedAwaiter _scriptAwaiter;
        private readonly IPageLoadedAwaiter _pageAwaiter;
        private int ChromeRemoteDebuggerPort { get; set; }
        private bool ScreenshotsAreReady { get; set; }
        private bool DomIsReady { get; set; }
        private bool ScriptExecutionCompleted { get; set; }
        private bool PageIsReady { get; set; }

        public PageTestingController( ChromeProcess chromeProcess, IHtmlStorage htmlStorage, IScreenshotTaker screenshotTaker,
            IRemoteDebuggerPortManager remoteDebuggerPortManager, IDomLoadedAwaiter domAwaiter, 
            IScriptExecutionCompletedAwaiter scriptAwaiter, IPageLoadedAwaiter pageAwaiter )
        {
            _chromeProcess = chromeProcess;
            _htmlStorage = htmlStorage;
            _screenshotTaker = screenshotTaker;
            _remoteDebuggerPortManager = remoteDebuggerPortManager;
            _domAwaiter = domAwaiter;
            _scriptAwaiter = scriptAwaiter;
            _pageAwaiter = pageAwaiter;
        }

        [HttpGet( "{base64Url}" )]
        public async Task<IActionResult> Get( [FromRoute] string base64Url, [FromQuery] int launchId )
        {
            ChromeRemoteDebuggerPort = _remoteDebuggerPortManager.AllocateRDPort();

            //as whole site crawling is not implemented, and we use only file system storage,
            //we have single resource with constant resourceId
            int resourceId = 1;

            TestingResult testingResult = new TestingResult();

            byte[] base64EncodedBytes = System.Convert.FromBase64String( base64Url );
            string url = System.Text.Encoding.UTF8.GetString( base64EncodedBytes );

            _chromeProcess.StartChrome( ChromeRemoteDebuggerPort );

            HttpWebRequest request = WebRequest.CreateHttp( $"http://localhost:{ChromeRemoteDebuggerPort}/json" );
            request.Method = WebRequestMethods.Http.Get;
            HttpWebResponse response = ( HttpWebResponse )request.GetResponse();
            ChromeBrowserFrame[] frames;
            using ( var reader = new StreamReader( response.GetResponseStream() ) )
            {
                frames = JsonConvert.DeserializeObject<ChromeBrowserFrame[]>( reader.ReadToEnd() );
            }

            ChromeBrowserFrame workingFrame = null;
            foreach ( var frame in frames )
            {
                if ( frame.Type.Equals( "page" ) )
                {
                    workingFrame = frame;
                    break;
                }
            }

            if ( workingFrame == null )
            {
                //what response code should we return if we can't launch chrome properly => can't test page?
                //400 for now, should be changed
                return BadRequest();
            }

            using ( var session = new ChromeSession( workingFrame.WebSocketDebuggerUrl ) )
            {
                await session.Page.Navigate( new BaristaLabs.ChromeDevTools.Runtime.Page.NavigateCommand()
                {
                    Url = url
                } );

                PageIsReady = false;

                new Thread( delegate () { _pageAwaiter.Await( this, session ); } ).Start();

                while ( !PageIsReady )
                {
                    Thread.Sleep( 100 );
                }

                DomIsReady = false;
                ScriptExecutionCompleted = false;

                new Thread( delegate () { _domAwaiter.Await( this, session ); } ).Start();

                while ( !DomIsReady )
                {
                    Thread.Sleep( 100 );
                }

                Thread.Sleep( 10000 );

                //if page is formed by scripts, we have to wait to ensure its' html is fully created
                //Thread.Sleep( 30000 );

                var document = await session.DOM.GetDocument( new BaristaLabs.ChromeDevTools.Runtime.DOM.GetDocumentCommand()
                {
                    Depth = -1
                } );

                //DOM.GetOuterHTML won't work properly if used before DOM.GetDocument due to protocol limitation
                string html = session.DOM.GetOuterHTML( new BaristaLabs.ChromeDevTools.Runtime.DOM.GetOuterHTMLCommand
                {
                    NodeId = 1
                } ).Result.OuterHTML;

                _htmlStorage.SaveHtml( launchId, resourceId, html );

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

                BaristaLabs.ChromeDevTools.Runtime.Log.LogAdapter logAdapter = new BaristaLabs.ChromeDevTools.Runtime.Log.LogAdapter( session );
                logAdapter.SubscribeToEntryAddedEvent( log =>
                {
                    testingResult.Logs.Add( log.Entry.Text );
                } );
                await logAdapter.Enable( new BaristaLabs.ChromeDevTools.Runtime.Log.EnableCommand() );

                BaristaLabs.ChromeDevTools.Runtime.Runtime.RuntimeAdapter runtimeAdapter = new BaristaLabs.ChromeDevTools.Runtime.Runtime.RuntimeAdapter( session );
                runtimeAdapter.SubscribeToExceptionThrownEvent( exception =>
                {
                    testingResult.Logs.Add( exception.ExceptionDetails.Exception.Description );
                } );
                await runtimeAdapter.Enable( new BaristaLabs.ChromeDevTools.Runtime.Runtime.EnableCommand() );

                testingResult.Logs.AddRange( await GetIframesLogs( ChromeRemoteDebuggerPort ) );

                ScreenshotsAreReady = false;
                _screenshotTaker.TakeAllScreenshots( this, session, workingFrame.Id, launchId, resourceId );
                while ( !ScreenshotsAreReady )
                {
                    Thread.Sleep( 100 );
                }
                _chromeProcess.StopChrome();
            }

            _remoteDebuggerPortManager.FreeRDPort( ChromeRemoteDebuggerPort );

            return Ok(testingResult);
        }

        [NonAction]
        public async Task<List<string>> GetIframesLogs( int chromeRemoteDebuggingPort )
        {
            List<string> logs = new List<string>();

            HttpWebRequest request = WebRequest.CreateHttp( $"http://localhost:{ChromeRemoteDebuggerPort}/json" );
            request.Method = WebRequestMethods.Http.Get;
            HttpWebResponse response = ( HttpWebResponse )request.GetResponse();
            ChromeBrowserFrame[] frames;
            using ( var reader = new StreamReader( response.GetResponseStream() ) )
            {
                frames = JsonConvert.DeserializeObject<ChromeBrowserFrame[]>( reader.ReadToEnd() );
            }

            foreach ( var frame in frames )
            {
                if ( frame.Type.Equals( "iframe" ) )
                {
                    using ( var session = new ChromeSession( frame.WebSocketDebuggerUrl ) )
                    {

                        BaristaLabs.ChromeDevTools.Runtime.Log.LogAdapter logAdapter = new BaristaLabs.ChromeDevTools.Runtime.Log.LogAdapter( session );
                        logAdapter.SubscribeToEntryAddedEvent( log =>
                        {
                            logs.Add( log.Entry.Text );
                        } );
                        await logAdapter.Enable( new BaristaLabs.ChromeDevTools.Runtime.Log.EnableCommand() );

                        BaristaLabs.ChromeDevTools.Runtime.Runtime.RuntimeAdapter runtimeAdapter = new BaristaLabs.ChromeDevTools.Runtime.Runtime.RuntimeAdapter( session );
                        runtimeAdapter.SubscribeToExceptionThrownEvent( exception =>
                        {
                            logs.Add( $"{ exception.ExceptionDetails.Text ?? ""  } " +
                                $"{ exception.ExceptionDetails.Exception.Value ?? "" } " +
                                $"{ exception.ExceptionDetails.Exception.Description ?? "" }" );
                        } );
                        await runtimeAdapter.Enable( new BaristaLabs.ChromeDevTools.Runtime.Runtime.EnableCommand() );
                        //Thread.Sleep( 1000 );
                        //Thread.Sleep( 1 );
                    }
                }
            }

            return logs;
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
