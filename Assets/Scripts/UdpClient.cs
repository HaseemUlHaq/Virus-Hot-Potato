using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

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
        receiveThread = new Thread(() =>
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 5006);
            while (true)
            {
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

        // Examples: "hue:0.5,scale:0.3" or "r:0.2,g:0.8,b:0.4,scale:0.3"
        float? hue = null;
        float? r = null, g = null, b = null;
        float? scaleVal = null;

        foreach (string part in msg.Split(','))
        {
            string[] kv = part.Split(':');
            if (kv.Length != 2) continue;

            string key = kv[0].Trim();
            string value = kv[1].Trim();
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                continue;

            switch (key)
            {
                case "hue":
                    hue = f;
                    break;
                case "r":
                case "red":
                    r = f;
                    break;
                case "g":
                case "green":
                    g = f;
                    break;
                case "b":
                case "blue":
                    b = f;
                    break;
                case "scale":
                    scaleVal = f;
                    break;
            }
        }

        if (virusRenderer != null)
        {
            if (r.HasValue && g.HasValue && b.HasValue)
            {
                var c = new Color(r.Value, g.Value, b.Value);
                ApplyVirusMaterialColor(virusRenderer.material, c);
                Debug.Log($"Virus color (RGB): {c}");
            }
            else if (hue.HasValue)
            {
                Color c = Color.HSVToRGB(Mathf.Repeat(hue.Value, 1f), 1f, 1f);
                ApplyVirusMaterialColor(virusRenderer.material, c);
                Debug.Log("Hue: " + hue.Value);
            }
        }

        if (scaleVal.HasValue && virusTransform != null)
        {
            float s = Mathf.Lerp(0.5f, 3f, scaleVal.Value);
            virusTransform.localScale = new Vector3(s, s, s);
            Debug.Log("Scale: " + s);
        }
    }

    /// <summary>
    /// Virus_Difficult uses Shader Graph materials with _Color_1 / _Color_2 / _Color_3_Overlay (not _BaseColor).
    /// Falls back to common Lit properties when those are absent.
    /// </summary>
    static void ApplyVirusMaterialColor(Material mat, Color c)
    {
        if (mat == null) return;

        if (mat.HasProperty("_Color_3_Overlay"))
        {
            Color c1 = Color.Lerp(Color.black, c, 0.45f);
            Color c2 = Color.Lerp(Color.black, c, 0.85f);
            var c3 = new Color(c.r, c.g, c.b, 1f);
            mat.SetColor("_Color_1", c1);
            mat.SetColor("_Color_2", c2);
            mat.SetColor("_Color_3_Overlay", c3);
            return;
        }

        mat.color = c;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", c);
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", c * 2f);
    }

    void OnApplicationQuit()
    {
        udpClient?.Close();
    }
}
