using BaristaLabs.ChromeDevTools.Runtime;

namespace Core.Awaiters
{
    public interface IPageLoadedAwaiter
    {
        public void Await( IPageLoadedAwaitingTester awaitingTester, ChromeSession session );
    }
}
