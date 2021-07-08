using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaristaLabs.ChromeDevTools.Runtime;

namespace Core.Awaiters
{
    public interface IPageLoadedAwaiter
    {
        public void Await( IPageLoadedAwaitingTester awaitingTester, ChromeSession session );
    }
}
