using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace PoplarCloud
{
     
    internal class AsyncConnectState
    {
        public AsyncConnectState(Socket socket,object state)
        {
            this.ConnectSocket = socket;
            this.State = state;
        }
        public Socket ConnectSocket { get; private set; }
        public object State { get; private set; }
    }
}
