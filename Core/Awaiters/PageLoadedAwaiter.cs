using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BaristaLabs.ChromeDevTools.Runtime;

namespace Core.Awaiters
{
    public class PageLoadedAwaiter : IPageLoadedAwaiter
    {
        private const int TIMEOUT_LENGTH = 500;

        private bool PageLoaded { get; set; } = false;

        public void Await( IPageLoadedAwaitingTester awaitingTester, ChromeSession session )
        {
            BaristaLabs.ChromeDevTools.Runtime.Network.NetworkAdapter adapter = 
                new BaristaLabs.ChromeDevTools.Runtime.Network.NetworkAdapter( session );

            adapter.SubscribeToLoadingFinishedEvent( e => PageLoaded = true );

            adapter.Enable( new BaristaLabs.ChromeDevTools.Runtime.Network.EnableCommand() );

            while ( !PageLoaded )
            {
                Thread.Sleep( TIMEOUT_LENGTH );
            }

            adapter.Disable( new BaristaLabs.ChromeDevTools.Runtime.Network.DisableCommand() );

            awaitingTester.NotifyReady();
        }
    }
}
