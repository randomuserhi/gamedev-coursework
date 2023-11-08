#nullable enable

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Deep.Net {

    public class TCPClient {
        private ArraySegment<byte> buffer;
        private Socket? socket;

        public delegate void onreceive_delegate(int bytesReceived, EndPoint endpoint);
        public onreceive_delegate? onreceive;

        public TCPClient(ArraySegment<byte> buffer) {
            this.buffer = buffer;
        }

        private void Open() {
            if (socket != null) Dispose();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
        }

        public async Task Connect(IPEndPoint endPoint) {
            Open();
            await socket.ConnectAsync(endPoint);
            _ = Listen(); // NOTE(randomuserhi): Start listen loop, not sure if `Connect` should automatically start the listen loop
        }

        private async Task Listen() {
            if (socket == null) return;

            int receivedBytes = await socket.ReceiveAsync(buffer, SocketFlags.None);
            onreceive?.Invoke(receivedBytes, socket.RemoteEndPoint);

            _ = Listen(); // Start new listen task => async loop
        }

        public void Disconnect() {
            Dispose();
        }

        public void Dispose() {
            if (socket == null) return;

            socket.Dispose();
            socket = null;
        }
    }
}
