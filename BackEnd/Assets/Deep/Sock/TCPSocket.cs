using System;
using System.Net;
using System.Net.Sockets;

namespace Deep.Sock {
    // TODO(randomuserhi): https://web.archive.org/web/20160728022524/http://blog.dickinsons.co.za/tips/2015/06/01/Net-Sockets-and-You/
    //                     - better memory allocation strategy with ArraySegment<byte> to prevent fragmentation
    public class TCPSocket {
        private ArraySegment<byte> buffer;
        private Socket socket;

        public delegate void onreceive_delegate(int bytesReceived, EndPoint endpoint);
        public onreceive_delegate onreceive;

        public TCPSocket(ArraySegment<byte> buffer) {
            this.buffer = buffer;
        }

        private void Open() {

        }
    }
}
