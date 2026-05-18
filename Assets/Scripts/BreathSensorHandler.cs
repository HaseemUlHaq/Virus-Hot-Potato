using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class BreathSensorHandler : MonoBehaviour
{
    public int listenPort = 5006;

    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;
    private static BreathSensorHandler instance;

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
    }

    void Update()
    {
        // triggerBlow is set on main thread via UnityMainThreadDispatcher
        // nothing needed here
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
            Debug.Log($"[BREATH] ✓ Listening on port {listenPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[BREATH] ✗ Failed to start listener: {e.Message}");
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
                Debug.Log($"[BREATH] GOT: '{message}'");
                if (message == "BLOW")
                {
                    UnityMainThreadDispatcher.Enqueue(() =>
                    {
                        Debug.Log("[BREATH] Setting triggerBlow = true on main thread");
                        triggerBlow = true;
                    });
                }
                else if (message == "BUTTON_PRESSED")
                {
                    UnityMainThreadDispatcher.Enqueue(() =>
                    {
                        Debug.Log("[BREATH] Forwarding BUTTON_PRESSED to UDPReceiver");
                        UDPReceiver.triggerPulse = true;
                    });
                }
            }
            catch (Exception e)
            {
                if (isRunning) Debug.LogError($"[BREATH] Error: {e.Message}");
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
