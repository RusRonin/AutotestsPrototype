using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;

namespace Core.Chrome
{
    public class ChromeFrameManager : IChromeFrameManager
    {
        public List<ChromeBrowserFrame> GetFrames( int chromeRemoteDebuggerPort )
        {
            HttpWebRequest request = WebRequest.CreateHttp( $"http://localhost:{chromeRemoteDebuggerPort}/json" );
            request.Method = WebRequestMethods.Http.Get;
            HttpWebResponse response = ( HttpWebResponse )request.GetResponse();
            ChromeBrowserFrame[] frames;
            using ( var reader = new StreamReader( response.GetResponseStream() ) )
            {
                frames = JsonConvert.DeserializeObject<ChromeBrowserFrame[]>( reader.ReadToEnd() );
            }
            return frames.ToList();
        }

        public List<ChromeBrowserFrame> GetFramesByType( int chromeRemoteDebuggerPort, string type )
        {
            List<ChromeBrowserFrame> frames = GetFrames( chromeRemoteDebuggerPort );
            return ( from frame in frames where frame.Type.Equals( type ) select frame ).ToList();
        }
    }
}
