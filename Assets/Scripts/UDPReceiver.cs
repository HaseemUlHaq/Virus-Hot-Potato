using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UDPReceiver : MonoBehaviour
{
    public int listenPort = 5006;

    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;
    private static UDPReceiver instance;

    public static bool triggerPulse = false;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartListening();
        StartCoroutine(PulseChecker());
    }

    void Update()
    {
        if (triggerPulse)
        {
            triggerPulse = false;
            TryPulse();
        }
    }

    IEnumerator PulseChecker()
    {
        Debug.Log("[UDP] PulseChecker started!");
        while (true)
        {
            if (triggerPulse)
            {
                triggerPulse = false;
                TryPulse();
            }
            yield return null;
        }
    }

    void TryPulse()
    {
        var viruses = FindObjectsByType<NetworkGrabbableVirus>(FindObjectsSortMode.None);
        Debug.Log($"[UDP] Viruses found: {viruses.Length}");

        if (viruses.Length == 0)
        {
            Debug.LogWarning("[UDP] NetworkGrabbableVirus not found in scene!");
            return;
        }

        foreach (var netVirus in viruses)
        {
            if (netVirus != null)
            {
                Debug.Log($"[UDP] ✓ RPC_TriggerPulse → {netVirus.name}");
                netVirus.RPC_TriggerPulse();
            }
        }
    }

    void StartListening()
    {
        try
        {
            udpClient = new UdpClient(listenPort);
            isRunning = true;
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            Debug.Log($"[UDP] ✓ Listening on port {listenPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[UDP] ✗ Failed: {e.Message}");
        }
    }

    void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data).Trim();
                Debug.Log($"[UDP] GOT: '{message}'");
                if (message == "BUTTON_PRESSED")
                {
                    Debug.Log("[UDP] Setting triggerPulse = true");
                    triggerPulse = true;
                }
            }
            catch (Exception e)
            {
                if (isRunning) Debug.LogError($"[UDP] Error: {e.Message}");
            }
        }
    }

    void OnDestroy() => Shutdown();
    void OnApplicationQuit() => Shutdown();

    void Shutdown()
    {
        isRunning = false;
        udpClient?.Close();
        receiveThread?.Abort();
    }
}