using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BaristaLabs.ChromeDevTools.Runtime;
using Microsoft.Extensions.Logging;

namespace Core.Screenshot
{
    public class ChromeScreenshotTaker : IScreenshotTaker
    {

        private class EnqueuedScreenshotRequester
        {
            public IScreenshotRequester ScreenshotRequester { get; set; }
            public ChromeSession ChromeSession { get; set; }
            public string TargetId { get; set; }
            public int LaunchId { get; set; }
            public int ResourceId { get; set; }
        }

        private Queue<EnqueuedScreenshotRequester> Requesters { get; set; } = new Queue<EnqueuedScreenshotRequester>();
        private readonly ILogger<ChromeScreenshotTaker> _logger;

        public ChromeScreenshotTaker( ILogger<ChromeScreenshotTaker> logger )
        {
            _logger = logger;
            _logger.LogInformation( "Starting screenshot queue..." );
            new Thread( async delegate () { await ProcessQueue(); } ).Start();
        }

        private async Task TakeScreenshotsForRequester( EnqueuedScreenshotRequester requester )
        {
            _logger.LogInformation( "Taking screenshots" );
            foreach ( var screenSize in ScreenSizesStorage.ScreenSizes )
            {
                int width = screenSize.Key;
                int height = screenSize.Value;

                long windowID = ( await requester.ChromeSession.Browser.GetWindowForTarget( new BaristaLabs.ChromeDevTools.Runtime.Browser.GetWindowForTargetCommand()
                {
                    TargetId = requester.TargetId
                } ) ).WindowId;

                await requester.ChromeSession.Browser.SetWindowBounds( new BaristaLabs.ChromeDevTools.Runtime.Browser.SetWindowBoundsCommand()
                {
                    WindowId = windowID,
                    Bounds = new BaristaLabs.ChromeDevTools.Runtime.Browser.Bounds()
                    {
                        WindowState = BaristaLabs.ChromeDevTools.Runtime.Browser.WindowState.Normal,
                        Height = height,
                        Width = width
                    }
                } );

                string workingDirectory = Directory.GetCurrentDirectory();
                string path = $"{ workingDirectory }\\Screenshots\\{ requester.LaunchId }\\{ requester.ResourceId }";

                //ensure that directory for resource is created
                Directory.CreateDirectory( path );

                Directory.CreateDirectory( $"{ path }\\chrome" );

                //wait a second to ensure page is fully loaded
                Thread.Sleep( 1000 );
                int attempts = 1;
                byte[] screenshot;
                while ( true )
                {
                    try
                    {
                        _logger.LogInformation( $"Trying to get screenshot: attempt {attempts}" );
                        screenshot = ( await requester.ChromeSession.Page.CaptureScreenshot(
                            new BaristaLabs.ChromeDevTools.Runtime.Page.CaptureScreenshotCommand() ) ).Data;
                    }
                    catch ( InvalidOperationException )
                    {
                        if ( attempts > 5 )
                        {
                            throw new Exception();
                        }
                        attempts++;
                        continue;
                    }
                    break;
                }
                using ( BinaryWriter writer = new BinaryWriter( File.Open( $"{ path }\\chrome\\{ width }x{ height }.png", FileMode.Create ) ) )
                {
                    writer.Write( screenshot );
                }
            }

            _logger.LogInformation( "Screenshots ready" );

            _logger.LogInformation( $"Finished processing requester from launch {requester.LaunchId}" );

            requester.ScreenshotRequester.NotifyScreenshotsAreReady();
        }

        public void TakeAllScreenshots( IScreenshotRequester requester, ChromeSession session, string targetId, int launchId, int resourceId )
        {
            _logger.LogInformation( $"Enqueuing requester from launch {launchId}" );
            Requesters.Enqueue( new EnqueuedScreenshotRequester()
            {
                ScreenshotRequester = requester,
                ChromeSession = session,
                TargetId = targetId,
                LaunchId = launchId,
                ResourceId = resourceId
            } );
        }

        private async Task ProcessQueue()
        {
            while ( true )
            {
                if ( Requesters.Count > 0 )
                {
                    EnqueuedScreenshotRequester requester = Requesters.Dequeue();
                    _logger.LogInformation( $"Started processing requester from launch {requester.LaunchId}" );
                    await TakeScreenshotsForRequester( requester );
                }
                else
                {
                    Thread.Sleep( 100 );
                }
            }
        }
    }
}
