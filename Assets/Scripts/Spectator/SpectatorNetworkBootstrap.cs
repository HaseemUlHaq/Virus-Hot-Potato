#if VIRUS_SPECTATOR
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// PC spectator: joins the Quest Fusion session via Photon session list in the same custom lobby as Meta matchmaking.
/// </summary>
public class SpectatorNetworkBootstrap : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private float retryIntervalSeconds = 2f;
    [SerializeField] private float waitingForQuestLogIntervalSeconds = 5f;

    private NetworkRunner _runner;
    private bool _connecting;
    private bool _spectatorRegistered;
    private bool _joinedLobby;
    private bool _startGameRequested;
    private float _lastWaitingLogTime;

    private void Start()
    {
        StartCoroutine(ConnectRoutine());
    }

    private void Update()
    {
        if (_spectatorRegistered || _runner == null || !_runner.IsRunning)
            return;

        TryRegisterSpectator();
    }

    private IEnumerator ConnectRoutine()
    {
        yield return null;

        while (true)
        {
            if (_runner == null || (!_runner.IsRunning && !_connecting && !_startGameRequested))
                BeginConnect();

            if (_runner != null && _runner.IsRunning && !_spectatorRegistered)
                TryRegisterSpectator();

            yield return new WaitForSeconds(retryIntervalSeconds);
        }
    }

    private async void BeginConnect()
    {
        if (_connecting || _startGameRequested || _joinedLobby)
            return;

        _runner = FindFirstObjectByType<NetworkRunner>();
        if (_runner == null)
        {
            Debug.LogWarning("[SpectatorNetworkBootstrap] No NetworkRunner in scene.");
            return;
        }

        if (_runner.IsRunning)
        {
            _runner.AddCallbacks(this);
            TryRegisterSpectator();
            return;
        }

        _connecting = true;
        SpectatorFusionUtil.EnsureRunnerComponents(_runner);
        _runner.AddCallbacks(this);

        if (SpectatorSessionConfig.TryGetSessionNameOverride(out string overrideSession))
        {
            Debug.Log(
                $"[SpectatorNetworkBootstrap] PlayerPrefs override — joining session '{overrideSession}'.");
            await StartGameWithSessionName(overrideSession);
            _connecting = false;
            return;
        }

        try
        {
            await _runner.JoinSessionLobby(SessionLobby.Custom, SpectatorSessionConfig.CustomLobbyName);
            _joinedLobby = true;
            Debug.Log(
                $"[SpectatorNetworkBootstrap] Joined Photon lobby '{SpectatorSessionConfig.CustomLobbyName}'. " +
                "Waiting for Quest session list… (start Quest first)");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SpectatorNetworkBootstrap] JoinSessionLobby failed: {ex.Message}");
        }

        _connecting = false;
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (_startGameRequested || runner == null)
            return;

        LogSessionList(sessionList);

        string bestName = PickBestSessionName(sessionList);
        if (!string.IsNullOrEmpty(bestName))
        {
            Debug.Log($"[SpectatorNetworkBootstrap] Auto-joining Quest session '{bestName}'.");
            _ = StartGameWithSessionName(bestName);
            return;
        }

        if (Time.unscaledTime - _lastWaitingLogTime < waitingForQuestLogIntervalSeconds)
            return;

        _lastWaitingLogTime = Time.unscaledTime;
        Debug.LogWarning(
            $"[SpectatorNetworkBootstrap] No open Quest sessions in lobby '{SpectatorSessionConfig.CustomLobbyName}' yet. " +
            "Start the Quest build and wait for Meta matchmaking to connect, then launch PC spectator.");
    }

    private static void LogSessionList(List<SessionInfo> sessionList)
    {
        if (sessionList == null || sessionList.Count == 0)
        {
            Debug.Log($"[SpectatorNetworkBootstrap] Session list empty (lobby '{SpectatorSessionConfig.CustomLobbyName}').");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"[SpectatorNetworkBootstrap] Sessions in '{SpectatorSessionConfig.CustomLobbyName}':");
        foreach (SessionInfo session in sessionList)
        {
            if (!session.IsValid)
                continue;

            sb.AppendLine(
                $"  '{session.Name}' players={session.PlayerCount} open={session.IsOpen} visible={session.IsVisible}");
        }

        Debug.Log(sb.ToString());
    }

    private static string PickBestSessionName(List<SessionInfo> sessionList)
    {
        if (sessionList == null || sessionList.Count == 0)
            return null;

        SessionInfo best = null;
        foreach (SessionInfo session in sessionList)
        {
            if (!session.IsValid || !session.IsOpen)
                continue;
            if (session.PlayerCount <= 0)
                continue;

            if (best == null || session.PlayerCount > best.PlayerCount)
                best = session;
        }

        return best != null ? best.Name : null;
    }

    private async System.Threading.Tasks.Task StartGameWithSessionName(string sessionName)
    {
        if (_startGameRequested || _runner == null || string.IsNullOrEmpty(sessionName))
            return;

        _startGameRequested = true;

        StartGameResult result = await _runner.StartGame(
            SpectatorFusionUtil.CreateStartGameArgs(_runner, sessionName));

        if (!result.Ok)
        {
            Debug.LogWarning($"[SpectatorNetworkBootstrap] StartGame failed: {result.ShutdownReason}");
            _startGameRequested = false;
            return;
        }

        Debug.Log($"[SpectatorNetworkBootstrap] Joined session '{sessionName}'.");
        TryRegisterSpectator();
    }

    private void TryRegisterSpectator()
    {
        if (_runner == null || !_runner.IsRunning)
            return;

        PowerRoleSession powerRoles = PowerRoleSession.Instance;
        if (powerRoles == null || !powerRoles.Object.IsValid)
            return;

        powerRoles.RPC_RegisterSpectator();

        if (powerRoles.IsSpectator(_runner.LocalPlayer))
            _spectatorRegistered = true;
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer)
            TryRegisterSpectator();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        _spectatorRegistered = false;
        _startGameRequested = false;
        _joinedLobby = false;
    }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        _spectatorRegistered = false;
        _startGameRequested = false;
        _joinedLobby = false;
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
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
