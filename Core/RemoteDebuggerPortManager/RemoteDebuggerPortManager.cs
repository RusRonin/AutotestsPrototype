using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Core.RemoteDebuggerPortManager
{
    public class RemoteDebuggerPortManager : IRemoteDebuggerPortManager
    {
        private readonly List<int> unavailablePorts = new List<int>();
        private readonly int lowestPortNumber = 9222;
        private readonly int highestPortNumber = 10000;

        public RemoteDebuggerPortManager( ILogger<RemoteDebuggerPortManager> logger )
        {

        }

        public int AllocateRDPort()
        {
            // we have about 800 possible ports. even if we run 10 instances at a time, we have only 1/80 chance of collision
            // 1/6400 of double collision, 1/(5*10^6) of triple collision and so on
            // if we won't allocate port after 5 attempts, is's surely an error in program,
            // as chance of 5x collision is EXTREMELY low

            int port = new Random().Next( lowestPortNumber, highestPortNumber );
            // now we generated first attempt's port
            int attempts = 1;
            while ( ( attempts < 5 ) && unavailablePorts.Contains( port )  )
            {
                attempts++;
                port = new Random().Next( lowestPortNumber, highestPortNumber );
            }
            
            if ( attempts >= 5 )
            {
                throw new Exception( "Too many attempts, unknown error" );
            }

            unavailablePorts.Add( port );
            return port;
        }

        public void BanRDPort( int port )
        {
            unavailablePorts.Add( port );
        }

        public void FreeRDPort( int port )
        {
            if ( unavailablePorts.Contains( port ) )
            {
                unavailablePorts.Remove( port );
            }
        }

        public bool IsRDPortAvailable( int port )
        {
            return !unavailablePorts.Contains( port );
        }
    }
}
