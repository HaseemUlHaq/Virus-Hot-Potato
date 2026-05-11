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

    private bool _virusSpawned = false;
    private bool _positionReady = false;
    private Vector3 _spawnPosition = Vector3.zero;

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

    public void SetTablePosition(Vector3 tablePosition)
    {
        _spawnPosition = tablePosition + Vector3.up * 0.15f;
        _positionReady = true;
        UnityEngine.Debug.Log("Table position set. Spawn at: " + _spawnPosition);
        TrySpawnVirus();
    }

    public void SetSpawnPosition(Vector3 position)
    {
        _spawnPosition = position;
        _positionReady = true;
        UnityEngine.Debug.Log("QR spawn position set: " + _spawnPosition);
        TrySpawnVirus();
    }

    public void ResetForNewRound()
    {
        _virusSpawned = false;
        _positionReady = false;
        UnityEngine.Debug.Log("VirusSpawner reset");
    }

    private void TrySpawnVirus()
    {
        if (_virusSpawned) return;
        if (!_positionReady) return;
        if (VirusPrefab == null)
        {
            UnityEngine.Debug.LogWarning("Virus prefab not assigned");
            return;
        }

        NetworkRunner masterRunner = null;
        foreach (var runner in _registeredRunners)
        {
            if (runner != null &&
                runner.IsRunning &&
                runner.IsSharedModeMasterClient)
            {
                masterRunner = runner;
                break;
            }
        }

        if (masterRunner == null)
        {
            UnityEngine.Debug.Log("No master runner yet — will retry on player join");
            return;
        }

        NetworkObject spawned = masterRunner.Spawn(
            VirusPrefab,
            _spawnPosition,
            Quaternion.identity
        );

        if (spawned != null)
        {
            _virusSpawned = true;
            UnityEngine.Debug.Log("Virus spawned at: " + _spawnPosition);
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsSharedModeMasterClient) return;
        TrySpawnVirus();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        _virusSpawned = false;
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

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
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
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}