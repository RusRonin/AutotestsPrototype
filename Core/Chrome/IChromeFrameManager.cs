using System.Collections.Generic;

namespace Core.Chrome
{
    public interface IChromeFrameManager
    {
        public List<ChromeBrowserFrame> GetFrames( int chromeRemoteDebuggerPort );
        public List<ChromeBrowserFrame> GetFramesByType( int chromeRemoteDebuggerPort, string type );
    }
}
