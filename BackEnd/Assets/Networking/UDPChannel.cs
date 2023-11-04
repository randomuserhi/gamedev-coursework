using System;
using System.Net;

using Deep.Sock;

using UnityEngine; // Temporary for logging

namespace Deep.Net
{
    public class UDPChannel
    {
        private byte[] buffer;
        private EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
        private UDPSocket socket;

        public UDPChannel(int bufferSize)
        {
            buffer = new byte[bufferSize];
            socket = new UDPSocket(buffer, OnReceive);
        }

        public void OnReceive(IAsyncResult result)
        {
            int numBytes = socket.EndReceiveFrom(result, ref endPoint);
            int index = 0;
            string message = BitHelper.ReadASCIIString(buffer, ref index);
            Debug.Log($"Received {numBytes} bytes: {message}");
        }

        public void Open() => socket.Open();
        public void Bind(IPEndPoint address) => socket.Bind(address);
        public void Connect(IPAddress address, int port) => socket.Connect(address, port);
        public void Disconnect() => socket.Disconnect();
        public void Dispose() => socket.Dispose();
    }
}
