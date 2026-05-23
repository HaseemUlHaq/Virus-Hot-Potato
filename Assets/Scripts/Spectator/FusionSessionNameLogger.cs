#if !VIRUS_SPECTATOR
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Logs the Fusion session name on Quest after Meta matchmaking connects.
/// Use this name in the PC spectator client until Quest uses a fixed room name.
/// </summary>
public class FusionSessionNameLogger : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;
    private bool _logged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateIfNeeded()
    {
        if (FindFirstObjectByType<FusionSessionNameLogger>() != null)
            return;

        var go = new GameObject(nameof(FusionSessionNameLogger));
        go.AddComponent<FusionSessionNameLogger>();
    }

    private void Start()
    {
        TryRegisterRunner();
    }

    private void Update()
    {
        if (!_logged)
            TryRegisterRunner();
    }

    private void TryRegisterRunner()
    {
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null || !runner.IsRunning)
            return;

        if (_runner == runner)
            return;

        if (_runner != null)
            _runner.RemoveCallbacks(this);

        _runner = runner;
        _runner.AddCallbacks(this);
        LogSessionName(_runner);
    }

    private void LogSessionName(NetworkRunner runner)
    {
        if (_logged || runner == null || !runner.IsRunning)
            return;

        string name = runner.SessionInfo.Name;
        if (string.IsNullOrEmpty(name))
            return;

        _logged = true;
        Debug.Log(
            $"[FusionSessionNameLogger] Quest room: '{name}' (lobby '{SpectatorSessionConfig.CustomLobbyName}'). " +
            "PC spectator auto-joins from session list; set PlayerPrefs SpectatorSessionName to force this room.");
    }

    public void OnConnectedToServer(NetworkRunner runner) => LogSessionName(runner);
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) => LogSessionName(runner);
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) => _logged = false;
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) => _logged = false;

    private void OnDestroy()
    {
        if (_runner != null)
            _runner.RemoveCallbacks(this);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
#endif
