using Deep.Sock;
using System.Net;
using System.Threading.Tasks;
using UnityEngine; // Temporary for logging

namespace Deep.Net {
    public class UDPChannel {
        private byte[] buffer;
        private UDPSocket socket;

        public UDPChannel(int bufferSize) {
            buffer = new byte[bufferSize];
            socket = new UDPSocket(buffer);
            socket.onreceive += OnReceive;
        }

        public void OnReceive(int numBytes, EndPoint endPoint) {
            int index = 0;
            string message = BitHelper.ReadASCIIString(buffer, ref index);
            Debug.Log($"Received {numBytes} bytes: {message}");
        }

        public void Bind(IPEndPoint address) => socket.Bind(address);
        public async Task Connect(IPAddress address, int port) => await socket.Connect(address, port);
        public void Disconnect() => socket.Disconnect();
        public void Dispose() => socket.Dispose();
    }
}
