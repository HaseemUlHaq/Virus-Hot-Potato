using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirusSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Assign in Inspector")]
    public NetworkObject VirusPrefab;

    // Internal state
    private bool _virusSpawned = false;
    private bool _positionReady = false;
    private Vector3 _spawnPosition = Vector3.zero;

    private readonly List<NetworkRunner> _registeredRunners = new List<NetworkRunner>();
    private Coroutine _runnerDiscoveryRoutine;

    // ─── Lifecycle ────────────────────────────────────────────────────────

    private void OnEnable()
    {
        RegisterToActiveRunners();
        _runnerDiscoveryRoutine = StartCoroutine(DiscoverRunnersRoutine());
    }

    private void OnDisable()
    {
        if (_runnerDiscoveryRoutine != null)
            StopCoroutine(_runnerDiscoveryRoutine);
        UnregisterFromRunners();
    }

    // ─── Public API ───────────────────────────────────────────────────────

    /// Called by TableAnchor when table is found
    /// Sets spawn position to center of table surface
    public void SetTablePosition(Vector3 tablePosition)
    {
        // 0.15m above table surface
        _spawnPosition = tablePosition + Vector3.up * 0.15f;
        _positionReady = true;
        UnityEngine.Debug.Log("Table position received. Virus will spawn at: " + _spawnPosition);
        TrySpawnVirus();
    }

    /// Called by PuckTracker when QR is detected
    /// Overrides spawn position with QR horizontal position
    /// but keeps table surface Y so virus is always on the table
    public void SetSpawnPosition(Vector3 position)
    {
        _spawnPosition = position;
        _positionReady = true;
        UnityEngine.Debug.Log("QR spawn position received: " + _spawnPosition);
        TrySpawnVirus();
    }

    /// Call this at the start of each new round
    public void ResetForNewRound()
    {
        _virusSpawned = false;
        _positionReady = false;
        UnityEngine.Debug.Log("VirusSpawner reset for new round");
    }

    // ─── Fusion Callbacks ─────────────────────────────────────────────────

    /// When a player joins, try to spawn in case table was found before Fusion connected
    //public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    //{
    //    TrySpawnVirus(runner);
    //}
    /// When a player joins, try to spawn in case table was found before Fusion connected
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsSharedModeMasterClient) return;
        TrySpawnVirus();
    }

    // ─── Runner Management ────────────────────────────────────────────────

    private IEnumerator DiscoverRunnersRoutine()
    {
        while (true)
        {
            RegisterToActiveRunners();
            yield return new WaitForSeconds(1f);
        }
    }

    private void RegisterToActiveRunners()
    {
        NetworkRunner[] runners = FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None);
        foreach (NetworkRunner runner in runners)
        {
            if (runner == null || _registeredRunners.Contains(runner))
                continue;
            runner.AddCallbacks(this);
            _registeredRunners.Add(runner);
            TrySpawnVirus(runner);
        }
    }

    private void UnregisterFromRunners()
    {
        foreach (NetworkRunner runner in _registeredRunners)
        {
            if (runner != null)
                runner.RemoveCallbacks(this);
        }
        _registeredRunners.Clear();
    }

     // ─── Spawn Logic ──────────────────────────────────────────────────────
    private static bool HasVirusSpawnAuthority(NetworkRunner runner)
    {
        if (runner == null)
            return false;
        switch (runner.GameMode)
        {
            case GameMode.Single:
                return true;
            case GameMode.Shared:
                return runner.IsSharedModeMasterClient;
            default:
                return runner.IsServer;
        }
    }
    /// <summary>Retry spawn using any registered runner that is allowed to spawn.</summary>
    private void TrySpawnVirus()
    {
        foreach (var runner in _registeredRunners)
            TrySpawnVirus(runner);
    }
    /// <summary>Attempt spawn on this runner (registration / Fusion callbacks).</summary>
    private void TrySpawnVirus(NetworkRunner runner)
    {
        if (_virusSpawned)
            return;
        if (VirusPrefab == null || runner == null || !runner.IsRunning || !runner.CanSpawn)
            return;
        if (!_positionReady)
            return;
        if (!HasVirusSpawnAuthority(runner))
            return;
        NetworkObject spawned = runner.Spawn(VirusPrefab, _spawnPosition, Quaternion.identity);
        if (spawned != null)
        {
            _virusSpawned = true;
            UnityEngine.Debug.Log("Virus spawned at: " + _spawnPosition);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        _virusSpawned = false;
    }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        TrySpawnVirus(runner);
    }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}