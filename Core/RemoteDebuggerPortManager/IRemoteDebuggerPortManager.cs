using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.RemoteDebuggerPortManager
{
    public interface IRemoteDebuggerPortManager
    {
        public int AllocateRDPort();
        public void FreeRDPort( int port );
        public bool IsRDPortAvailable( int port );
        public void BanRDPort( int port );
    }
}
