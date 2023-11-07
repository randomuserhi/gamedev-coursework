using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Net;
using System.Net.Sockets;

namespace Deep.Sock
{
    public class TCPSocket
    {
        private byte[] buffer;
        private EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
        private Socket socket;

        public delegate void onreceive_delegate(int bytesReceived, EndPoint endpoint);
        public onreceive_delegate onreceive;

        public TCPSocket(byte[] buffer)
        {
            this.buffer = buffer;
        }
    }
}
