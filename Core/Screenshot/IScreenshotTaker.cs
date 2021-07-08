using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaristaLabs.ChromeDevTools.Runtime;

namespace Core.Screenshot
{
    public interface IScreenshotTaker
    {
        public void TakeAllScreenshots( IScreenshotRequester requester, ChromeSession session, string targetId, int launchId, int resourceId );
    }
}
