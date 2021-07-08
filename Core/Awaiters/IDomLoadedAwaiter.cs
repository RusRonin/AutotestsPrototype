using BaristaLabs.ChromeDevTools.Runtime;

namespace Core.Awaiters
{
    public interface IDomLoadedAwaiter
    {
        public void Await( IDomAwaitingTester awaitingTester, ChromeSession session );
    }
}
