using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class VirusSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkObject VirusPrefab;
    [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 0.05f, 0f);

    private bool _virusSpawned;
    private readonly List<NetworkRunner> _registeredRunners = new List<NetworkRunner>();
    private Coroutine _runnerDiscoveryRoutine;

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

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        TrySpawnVirus(runner);
    }

    private void TrySpawnVirus(NetworkRunner runner)
    {
        if (_virusSpawned || VirusPrefab == null || runner == null || !runner.IsRunning || !runner.CanSpawn)
            return;

        if (!HasVirusSpawnAuthority(runner))
            return;

        NetworkObject spawned = runner.Spawn(VirusPrefab, spawnPosition, Quaternion.identity);
        if (spawned != null)
            _virusSpawned = true;
    }

    private static bool HasVirusSpawnAuthority(NetworkRunner runner)
    {
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
