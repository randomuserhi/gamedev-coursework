using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Deep.Sock {
    // TODO(randomuserhi): https://web.archive.org/web/20160728022524/http://blog.dickinsons.co.za/tips/2015/06/01/Net-Sockets-and-You/
    //                     - better memory allocation strategy with ArraySegment<byte> to prevent fragmentation
    public class UDPSocket {
        private ArraySegment<byte> buffer;
        private EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private Socket socket;

        public delegate void onreceive_delegate(int bytesReceived, EndPoint endpoint);
        public onreceive_delegate onreceive;

        public UDPSocket(ArraySegment<byte> buffer) {
            this.buffer = buffer;
        }

        private void Open() {
            if (socket != null) socket.Dispose();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);

            //https://stackoverflow.com/questions/38191968/c-sharp-udp-an-existing-connection-was-forcibly-closed-by-the-remote-host
            socket.IOControl(
                (IOControlCode)(-1744830452),
                new byte[] { 0, 0, 0, 0 },
                null
            );
        }

        public void Bind(IPEndPoint address) {
            Open();
            socket.Bind(address);
            _ = Listen(); // NOTE(randomuserhi): Start listen loop, not sure if `Bind` should automatically start the listen loop
        }

        public async Task Connect(IPAddress address, int port) {
            Open();
            await socket.ConnectAsync(address, port);
            _ = Listen(); // NOTE(randomuserhi): Start listen loop, not sure if `Connect` should automatically start the listen loop
        }

        public void Disconnect() {
            socket.Dispose();
            socket = null;
        }

        public void Dispose() {
            socket.Dispose();
            socket = null;
        }

        private async Task Listen() {
            // NOTE(randomuserhi): remote end point passed in here is the endpoint we expect data to be from.
            //                     by default we expect any, hence `remoteEndPoint = new IPEndPoint(IPAddress.Any, 0)`
            SocketReceiveFromResult result = await socket.ReceiveFromAsync(buffer, SocketFlags.None, remoteEndPoint);
            onreceive(result.ReceivedBytes, result.RemoteEndPoint);

            _ = Listen(); // Start new listen task => async loop
        }

        public async Task<int> Send(byte[] data) {
            return await socket.SendAsync(data, SocketFlags.None);
        }

        public async Task<int> SendTo(byte[] data, IPEndPoint destination) {
            return await socket.SendToAsync(data, SocketFlags.None, destination);
        }
    }
}
