#nullable enable
using Deep.Net;
using System.Net;
using System.Text;
using UnityEngine;

public class Test : MonoBehaviour {
    private TCPClient? client = null;

    // Start is called before the first frame update
    void Start() {
        /*byte[] buffer = new byte[1024];
        UDPClient client = new UDPClient(buffer);
        client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 54892)).Wait();

        _ = client.Send(new byte[4] { 1, 2, 3, 4 });*/

        byte[] buffer = new byte[1024];
        client = new TCPClient(buffer);
        client.onReceive += (bytesReceived, _) => {
            Debug.Log(Encoding.ASCII.GetString(buffer, 0, bytesReceived));
        };

        client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 54892)).Wait();

        //_ = client.Send(new byte[4] { 1, 2, 3, 4 });
    }

    private void OnApplicationQuit() {
        Debug.Log("Disposing");
        client?.Dispose();
    }
}
