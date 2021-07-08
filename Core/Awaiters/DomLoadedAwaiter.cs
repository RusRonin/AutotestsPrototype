using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BaristaLabs.ChromeDevTools.Runtime;

namespace Core.Awaiters
{
    public class DomLoadedAwaiter : IDomLoadedAwaiter
    {
        private const int TIMEOUT_LENGTH = 2000;

        private bool DomChanged { get; set; } = false;
        private bool HasTimeoutPassed { get; set; } = false;

        public void Await( IDomAwaitingTester awaitingTester, ChromeSession session )
        {
            BaristaLabs.ChromeDevTools.Runtime.DOM.DOMAdapter adapter = new BaristaLabs.ChromeDevTools.Runtime.DOM.DOMAdapter( session );

            adapter.SubscribeToAttributeModifiedEvent( e => EventHappened() );
            adapter.SubscribeToAttributeRemovedEvent( e => EventHappened() );
            adapter.SubscribeToChildNodeCountUpdatedEvent( e => EventHappened() );
            adapter.SubscribeToDocumentUpdatedEvent( e => EventHappened() );
            adapter.SubscribeToPseudoElementAddedEvent( e => EventHappened() );
            adapter.SubscribeToPseudoElementRemovedEvent( e => EventHappened() );

            adapter.Enable( new BaristaLabs.ChromeDevTools.Runtime.DOM.EnableCommand() );
            
            while ( !HasTimeoutPassed || DomChanged )
            {
                Thread.Sleep( TIMEOUT_LENGTH );
                if ( !DomChanged )
                {
                    HasTimeoutPassed = true;
                }
                DomChanged = false;
            }

            adapter.Disable( new BaristaLabs.ChromeDevTools.Runtime.DOM.DisableCommand() );

            awaitingTester.NotifyReady();
        }

        private void EventHappened()
        {
            DomChanged = true;
            HasTimeoutPassed = false;
        }
    }
}
