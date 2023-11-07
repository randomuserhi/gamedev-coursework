using System;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;

namespace Deep.Sock
{
    public class UDPSocket
    {
        private byte[] buffer;
        private EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
        private Socket socket;

        public delegate void onreceive_delegate(int bytesReceived, EndPoint endpoint);
        public onreceive_delegate onreceive;

        public UDPSocket(byte[] buffer)
        {
            this.buffer = buffer;
        }

        public void Open()
        {
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

        public void Bind(IPEndPoint address)
        {
            socket.Bind(address);
            BeginReceive();
        }

        public async Task Connect(IPAddress address, int port)
        {
            await socket.ConnectAsync(address, port);
            BeginReceive();
        }

        public void Disconnect()
        {
            socket.Dispose();
            socket = null;
        }

        public void Dispose()
        {
            socket.Dispose();
            socket = null;
        }

        private void BeginReceive()
        {
            socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endPoint, ReceiveCallback, null);
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            onreceive(socket.EndReceiveFrom(result, ref endPoint), endPoint);
            socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endPoint, ReceiveCallback, null);
        }

        public void Send(byte[] data)
        {
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, null, null);
        }

        public void SendTo(byte[] data, IPEndPoint destination)
        {
            socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, destination, null, null);
        }
    }
}
