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
        public void TakeAllScreenshots( ChromeSession session, string targetId, int launchId, int resourceId )
        {
            foreach ( var screenSize in ScreenSizesStorage.ScreenSizes )
            {
                int width = screenSize.Key;
                int height = screenSize.Value;

                long windowID = session.Browser.GetWindowForTarget( new BaristaLabs.ChromeDevTools.Runtime.Browser.GetWindowForTargetCommand()
                {
                    TargetId = targetId
                } ).Result.WindowId;

                session.Browser.SetWindowBounds( new BaristaLabs.ChromeDevTools.Runtime.Browser.SetWindowBoundsCommand()
                {
                    WindowId = windowID,
                    Bounds = new BaristaLabs.ChromeDevTools.Runtime.Browser.Bounds()
                    {
                        Height = height,
                        Width = width
                    }
                } );

                string workingDirectory = Directory.GetCurrentDirectory();
                string path = $"{ workingDirectory }\\Screenshots\\{ launchId }\\{ resourceId }";

                //ensure that directory for resource is created
                Directory.CreateDirectory( path );

                Directory.CreateDirectory( $"{ path }\\chrome" );

                //wait a second to ensure page is fully loaded
                Thread.Sleep( 1000 );
                byte[] screenshot = session.Page.CaptureScreenshot( new BaristaLabs.ChromeDevTools.Runtime.Page.CaptureScreenshotCommand() )
                    .Result.Data;
                using ( BinaryWriter writer = new BinaryWriter(File.Open( $"{ path }\\chrome\\{ width }x{ height }.png", FileMode.Create) ) )
                {
                    writer.Write( screenshot );
                }
            }

        }
    }
}
