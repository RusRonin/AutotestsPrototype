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
        private readonly ILogger<RemoteDebuggerPortManager> _logger;

        public RemoteDebuggerPortManager( ILogger<RemoteDebuggerPortManager> logger )
        {
            _logger = logger;
            logger.LogInformation( "PortManager created" );
        }

        public int AllocateRDPort()
        {
            // we have about 800 possible ports. even if we run N instances at a time, we have only N/800 chance of collision
            // N^2/640000 of double collision, N^3/(5*10^9) of triple collision and so on
            // if we won't allocate port after 5 attempts, is's surely an error in program,
            // as chance of 5x collision is EXTREMELY low

            int port = new Random().Next( lowestPortNumber, highestPortNumber );
            // now we generated first attempt's port
            int attempts = 1;
            while ( ( attempts < 5 ) && unavailablePorts.Contains( port )  )
            {
                _logger.LogWarning( "Unsuccessful allocation. Retrying..." );
                attempts++;
                port = new Random().Next( lowestPortNumber, highestPortNumber );
            }
            
            if ( attempts >= 5 )
            {
                _logger.LogError( "Port allocation failed" );
                throw new Exception( "Too many attempts, unknown error" );
            }

            _logger.LogInformation( $"Allocated port: {port}" );
            unavailablePorts.Add( port );
            return port;
        }

        public void BanRDPort( int port )
        {
            _logger.LogInformation( $"Banned port: {port}" );
            unavailablePorts.Add( port );
        }

        public void FreeRDPort( int port )
        {
            _logger.LogInformation( $"Port {port} is now free" );
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
