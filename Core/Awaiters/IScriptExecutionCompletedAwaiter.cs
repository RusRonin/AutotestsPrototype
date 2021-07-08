using BaristaLabs.ChromeDevTools.Runtime;

namespace Core.Awaiters
{
    public interface IScriptExecutionCompletedAwaiter
    {
        public void Await( IScriptAwaitingTester awaitingTester, ChromeSession session );
    }
}
