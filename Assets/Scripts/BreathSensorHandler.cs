using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class BreathSensorHandler : MonoBehaviour
{
    public int listenPort = 5006;

    private UdpClient _udpClient;
    private Thread _receiveThread;
    private bool _isRunning = false;
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

    void StartListening()
    {
        try
        {
            _udpClient = new UdpClient(listenPort);
            _isRunning = true;
            _receiveThread = new Thread(ReceiveData) { IsBackground = true };
            _receiveThread.Start();
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
        while (_isRunning)
        {
            try
            {
                byte[] data = _udpClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data).Trim();
                Debug.Log($"[BREATH] GOT: '{message}'");
                if (message == "BLOW")
                {
                    UnityMainThreadDispatcher.Enqueue(() =>
                    {
                        Debug.Log("[BREATH] Setting triggerBlow = true");
                        triggerBlow = true;
                    });
                }
            }
            catch (Exception e)
            {
                if (_isRunning) Debug.LogError($"[BREATH] Error: {e.Message}");
            }
        }
    }

    void OnDestroy() => Shutdown();
    void OnApplicationQuit() => Shutdown();

    void Shutdown()
    {
        _isRunning = false;
        _udpClient?.Close();
    }
}
