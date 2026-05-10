using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class UDPTest : MonoBehaviour
{
    [Header("Virus")]
    public Renderer virusRenderer;
    public Transform virusTransform;

    UdpClient udpClient;
    Thread receiveThread;
    volatile string latestMessage = "";

    void Start()
    {
        udpClient = new UdpClient(5006);
        receiveThread = new Thread(() => {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 5006);
            while (true) {
                byte[] data = udpClient.Receive(ref ep);
                latestMessage = Encoding.UTF8.GetString(data);
            }
        });
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log("Listening on 5006");
    }

    void Update()
    {
        if (latestMessage == "") return;

        string msg = latestMessage;
        latestMessage = "";

        // Format will be "hue:0.5,scale:0.3"
        string[] parts = msg.Split(',');
        foreach (string part in parts)
        {
            string[] kv = part.Split(':');
            if (kv.Length != 2) continue;

            if (kv[0] == "hue" && float.TryParse(kv[1],
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out float hue))
            {
                Color c = Color.HSVToRGB(hue, 1f, 1f);
                if (virusRenderer != null)
                {
                    virusRenderer.material.color = c;
                    virusRenderer.material.SetColor("_Color", c);
                    virusRenderer.material.SetColor("_BaseColor", c);
                    virusRenderer.material.SetColor("_EmissionColor", c * 2f);
                }
                Debug.Log("Hue: " + hue);
            }

            if (kv[0] == "scale" && float.TryParse(kv[1],
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out float scale))
            {
                float s = Mathf.Lerp(0.5f, 3f, scale);
                if (virusTransform != null)
                    virusTransform.localScale = new Vector3(s, s, s);
                Debug.Log("Scale: " + s);
            }
        }
    }

    void OnApplicationQuit() { udpClient?.Close(); }
}