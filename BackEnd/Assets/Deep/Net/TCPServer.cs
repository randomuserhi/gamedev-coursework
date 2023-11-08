#nullable enable
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Deep.Net {

    public class TCPServer {
        private ArraySegment<byte> buffer;
        private Socket? socket;

        public delegate void onAccept_delegate(EndPoint endpoint);
        public onAccept_delegate? onAccept;

        private ConcurrentDictionary<EndPoint, Socket> acceptedConnections = new ConcurrentDictionary<EndPoint, Socket>();

        public TCPServer(ArraySegment<byte> buffer) {
            this.buffer = buffer;
        }

        private void Open() {
            if (socket != null) Dispose();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
        }

        public EndPoint Bind(EndPoint remoteEP, int backlog = 5) {
            Open();
            socket!.Bind(remoteEP);
            socket!.Listen(backlog);
            _ = Listen(); // NOTE(randomuserhi): Start listen loop, not sure if `Bind` should automatically start the listen loop
            return socket.LocalEndPoint!;
        }

        private async Task Listen() {
            if (socket == null) return;

            Socket incomingConnection = await socket.AcceptAsync().ConfigureAwait(false);

            EndPoint? remoteEndPoint = incomingConnection.RemoteEndPoint;
            if (remoteEndPoint != null) {
                acceptedConnections.AddOrUpdate(remoteEndPoint, incomingConnection, (key, old) => { incomingConnection.Dispose(); return old; });
                onAccept?.Invoke(remoteEndPoint);
            } else {
                incomingConnection.Dispose();
            }

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
