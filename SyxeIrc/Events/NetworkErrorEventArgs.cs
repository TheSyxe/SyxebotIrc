using System;
using System.Net.Sockets;

namespace SyxeIrc.Events
{
    public class NetworkErrorEventArgs : EventArgs
    {
        public SocketError SocketError { get; set; }

        public NetworkErrorEventArgs(SocketError socketError)
        {
            SocketError = socketError;
        }
    }
}
