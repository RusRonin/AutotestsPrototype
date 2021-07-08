using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BaristaLabs.ChromeDevTools.Runtime;

namespace Core.Awaiters
{
    public class ScriptExecutionCompletedAwaiter : IScriptExecutionCompletedAwaiter
    {
        private const int TIMEOUT_LENGTH = 3000;

        private int RunningExecutionContexts { get; set; } = 0;
        private bool HasTimeoutPassed { get; set; } = false;

        //when script execution begins, ExecutionContextCreatedEvent is fired
        //when script execution ends, ExecutionContextDestroyedEvent is fired
        //so we can count how many scripts are being executed right now
        //if no scripts are being executed, and were not started/finished in some time,
        //so all scripts have already finished their work
        public void Await( IScriptAwaitingTester awaitingTester, ChromeSession session )
        {
            BaristaLabs.ChromeDevTools.Runtime.Runtime.RuntimeAdapter adapter = new BaristaLabs.ChromeDevTools.Runtime.Runtime.RuntimeAdapter( session );
            adapter.SubscribeToExecutionContextCreatedEvent( e =>
            {
                HasTimeoutPassed = false;
                RunningExecutionContexts++;
            } );
            adapter.SubscribeToExecutionContextDestroyedEvent( e =>
            {
                HasTimeoutPassed = false;
                RunningExecutionContexts--;
            } );
            adapter.SubscribeToExceptionThrownEvent( e =>
            {
                HasTimeoutPassed = false;
                RunningExecutionContexts--;
            } );
            adapter.Enable( new BaristaLabs.ChromeDevTools.Runtime.Runtime.EnableCommand() );

            while ( !HasTimeoutPassed || ( RunningExecutionContexts > 0 ) )
            {
                if ( RunningExecutionContexts == 0 )
                {
                    HasTimeoutPassed = true;
                }
                Thread.Sleep( TIMEOUT_LENGTH );
            }

            adapter.Disable( new BaristaLabs.ChromeDevTools.Runtime.Runtime.DisableCommand() );

            awaitingTester.NotifyReady();
        }
    }
}
