using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BaristaLabs.ChromeDevTools.Runtime;

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

        public ChromeScreenshotTaker()
        {
            new Thread( delegate () { ProcessQueue(); } ).Start();
        }

        private void TakeScreenshotsForRequester( EnqueuedScreenshotRequester requester )
        {
            foreach ( var screenSize in ScreenSizesStorage.ScreenSizes )
            {
                int width = screenSize.Key;
                int height = screenSize.Value;

                long windowID = requester.ChromeSession.Browser.GetWindowForTarget( new BaristaLabs.ChromeDevTools.Runtime.Browser.GetWindowForTargetCommand()
                {
                    TargetId = requester.TargetId
                } ).Result.WindowId;

                requester.ChromeSession.Browser.SetWindowBounds( new BaristaLabs.ChromeDevTools.Runtime.Browser.SetWindowBoundsCommand()
                {
                    WindowId = windowID,
                    Bounds = new BaristaLabs.ChromeDevTools.Runtime.Browser.Bounds()
                    {
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
                int attempts = 0;
                byte[] screenshot;
                while ( true )
                {
                    try
                    {
                        screenshot = requester.ChromeSession.Page.CaptureScreenshot( new BaristaLabs.ChromeDevTools.Runtime.Page.CaptureScreenshotCommand() )
                            .Result.Data;
                    }
                    catch ( InvalidOperationException )
                    {
                        if (attempts >= 5)
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

            requester.ScreenshotRequester.NotifyScreenshotsAreReady();
        }

        public void TakeAllScreenshots( IScreenshotRequester requester, ChromeSession session, string targetId, int launchId, int resourceId )
        {
            Requesters.Enqueue( new EnqueuedScreenshotRequester()
            {
                ScreenshotRequester = requester,
                ChromeSession = session,
                TargetId = targetId,
                LaunchId = launchId,
                ResourceId = resourceId
            } );
        }

        private void ProcessQueue()
        {
            while ( true )
            {
                if ( Requesters.Count > 0 )
                {
                    EnqueuedScreenshotRequester requester = Requesters.Dequeue();
                    TakeScreenshotsForRequester( requester );
                    requester.ScreenshotRequester.NotifyScreenshotsAreReady();
                }
                else
                {
                    Thread.Sleep( 100 );
                }
            }
        }
    }
}
