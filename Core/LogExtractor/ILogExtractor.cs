using System.Collections.Generic;
using System.Threading.Tasks;
using BaristaLabs.ChromeDevTools.Runtime;

namespace Core.LogExtractor
{
    public interface ILogExtractor
    {
        public Task<List<string>> Extract( ChromeSession session, int chromeRemoteDebuggerPort );
    }
}
