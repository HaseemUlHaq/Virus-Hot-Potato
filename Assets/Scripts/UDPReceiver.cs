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
    public static bool triggerBlow = false;

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
        if (triggerBlow)
        {
            triggerBlow = false;
            TrySpikeBurst();
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
        // Find by component - works regardless of object name or clone suffix
        NetworkGrabbableVirus netVirus =
            FindFirstObjectByType<NetworkGrabbableVirus>();

        Debug.Log("[UDP] Virus found: " + (netVirus != null));

        if (netVirus != null)
        {
            Debug.Log("[UDP] ✓ Sending RPC_TriggerPulse to all headsets!");
            netVirus.RPC_TriggerPulse();
        }
        else
        {
            Debug.LogWarning("[UDP] NetworkGrabbableVirus not found in scene!");
        }
    }

    void TrySpikeBurst()
    {
        NetworkGrabbableVirus netVirus = FindFirstObjectByType<NetworkGrabbableVirus>();
        Debug.Log("[UDP] Virus found for SpikeBurst: " + (netVirus != null));
        if (netVirus != null)
        {
            Debug.Log("[UDP] ✓ Sending RPC_TriggerSpikeBurst to all headsets!");
            netVirus.RPC_TriggerPulse();
        }
        else
        {
            Debug.LogWarning("[UDP] NetworkGrabbableVirus not found in scene!");
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
                else if (message == "BLOW")
                {
                    Debug.Log("[UDP] Setting triggerBlow = true");
                    triggerBlow = true;
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