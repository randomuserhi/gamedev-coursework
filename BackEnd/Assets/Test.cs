using Deep.Net;
using System.Net;
using UnityEngine;

public class Test : MonoBehaviour {
    // Start is called before the first frame update
    void Start() {
        /*byte[] buffer = new byte[1024];
        UDPClient client = new UDPClient(buffer);
        client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 54892)).Wait();

        _ = client.Send(new byte[4] { 1, 2, 3, 4 });*/

        byte[] buffer = new byte[1024];
        TCPClient client = new TCPClient(buffer);
        client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 54892)).Wait();

        //_ = client.Send(new byte[4] { 1, 2, 3, 4 });
    }
}
