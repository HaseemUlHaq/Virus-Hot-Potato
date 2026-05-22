using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns networked session pieces on the shared-mode master. Master syncs spawn pose from replicated
/// <see cref="NetworkedTableAnchor"/> (QR from any peer).
///
/// Phase 1 device regression: non-master scans QR first (table + spawn); master scans QR; color-power swipe
/// changes materials; grab/throw; UDP pulse on <see cref="NetworkGrabbableVirus.RPC_TriggerPulse"/>.
/// </summary>
public class VirusSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Table / anchor (Fusion)")]
    [Tooltip("Used on the master client to mirror QR placement replicated from NetworkedTableAnchor. Assign the scene anchor.")]
    [SerializeField] private NetworkedTableAnchor networkedTableAnchor;
    /// <summary>Last PlacementVersion consumed from anchor; cleared on round reset so respawn still runs when table stays placed.</summary>
    private int _lastAppliedPlacementVersion = -1;

    [Header("Formation")]
    [SerializeField] private FormationManager formationManager;

    [Header("Assign in Inspector")]
    public NetworkObject VirusPrefab;
    [Tooltip("Optional second virus (e.g. alternate mesh). Spawned beside the primary using Second Virus Spawn Offset.")]
    public NetworkObject SecondVirusPrefab;
    [Tooltip("Applied on top of table spawn position for the second virus only.")]
    [SerializeField] private Vector3 secondVirusSpawnOffset = new Vector3(0.2f, 0f, 0f);
    [Tooltip("Spawned once by shared-mode master so power roles exist before or with first player.")]
    public NetworkObject PowerRoleSessionPrefab;

    private bool _primarySpawned;
    private bool _secondarySpawned;
    private bool _powerRoleSessionSpawned = false;
    private bool _positionReady = false;
    private Vector3 _spawnPosition = Vector3.zero;
    private NetworkObject _primaryInstance;
    private NetworkObject _secondaryInstance;

    private readonly List<NetworkRunner> _registeredRunners = new List<NetworkRunner>();
    private Coroutine _runnerDiscoveryRoutine;

    private void Update()
    {
        TrySyncSpawnFromNetworkedTableAnchor();
    }

    /// <summary>
    /// Master only: derive virus spawn pose from replicated table anchor after any client placed via RPC.
    /// </summary>
    private void TrySyncSpawnFromNetworkedTableAnchor()
    {
        if (networkedTableAnchor == null) return;

        NetworkObject anchorObj = networkedTableAnchor.Object;
        if (anchorObj == null || !anchorObj.IsValid) return;
        if (!networkedTableAnchor.IsTablePlaced) return;

        NetworkRunner masterRunner = null;
        foreach (NetworkRunner runner in _registeredRunners)
        {
            if (runner != null && runner.IsRunning && runner.IsSharedModeMasterClient)
            {
                masterRunner = runner;
                break;
            }
        }
        if (masterRunner == null) return;

        int version = networkedTableAnchor.CurrentPlacementVersion;
        if (version == _lastAppliedPlacementVersion) return;

        Vector3 surfacePosition = networkedTableAnchor.PlacedSurfacePosition;
        _spawnPosition = surfacePosition + Vector3.up * 0.15f;
        _positionReady = true;
        _lastAppliedPlacementVersion = version;
        UnityEngine.Debug.Log("[VirusSpawner] Spawn pose synced from NetworkedTableAnchor at: " + _spawnPosition);
        TrySpawnPowerRoleSession();
        TrySpawnViruses();
    }

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
        TrySpawnPowerRoleSession();
        TrySpawnViruses();
    }

    public void SetSpawnPosition(Vector3 position)
    {
        _spawnPosition = position;
        _positionReady = true;
        UnityEngine.Debug.Log("QR spawn position set: " + _spawnPosition);
        TrySpawnPowerRoleSession();
        TrySpawnViruses();
    }

    public void ResetForNewRound()
    {
        _primarySpawned = false;
        _secondarySpawned = false;
        _positionReady = false;
        _powerRoleSessionSpawned = false;
        _lastAppliedPlacementVersion = -1;
        _primaryInstance = null;
        _secondaryInstance = null;
        formationManager?.ResetForNewRound();
        UnityEngine.Debug.Log("VirusSpawner reset");
    }

    /// <summary>
    /// Despawn puzzle entities and respawn at the current table anchor. Keeps colocation, anchor pose,
    /// and <see cref="PowerRoleSession"/>; does not re-scan QR.
    /// </summary>
    public void RestartRoundAtCurrentTable()
    {
        NetworkRunner masterRunner = GetMasterRunner();
        if (masterRunner == null || !masterRunner.IsSharedModeMasterClient)
            return;

        EnsureSpawnPoseFromAnchor();

        if (!_positionReady)
        {
            Debug.LogWarning("[VirusSpawner] Round restart skipped — table anchor not placed yet.");
            return;
        }

        bool respawnWorkArea = formationManager != null && formationManager.WorkAreaWasSpawned;

        DespawnLegacyViruses(masterRunner);
        formationManager?.DespawnRoundEntities(masterRunner);
        formationManager?.ResetForNewRound();

        _primarySpawned = false;
        _secondarySpawned = false;
        _primaryInstance = null;
        _secondaryInstance = null;

        TrySpawnViruses();
        if (respawnWorkArea)
            formationManager?.SpawnWorkArea();

        Debug.Log("[VirusSpawner] Round restarted at " + _spawnPosition);
    }

    private bool AllRequestedVirusesSpawned()
    {
        bool wantsPrimary = VirusPrefab != null;
        bool wantsSecondary = SecondVirusPrefab != null;
        return (!wantsPrimary || _primarySpawned) && (!wantsSecondary || _secondarySpawned);
    }

    private void TrySpawnPowerRoleSession()
    {
        if (_powerRoleSessionSpawned)
            return;
        if (PowerRoleSessionPrefab == null)
            return;
        if (FindFirstObjectByType<PowerRoleSession>(FindObjectsInactive.Include) != null)
        {
            _powerRoleSessionSpawned = true;
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
            return;

        NetworkObject spawned = masterRunner.Spawn(
            PowerRoleSessionPrefab,
            Vector3.zero,
            Quaternion.identity
        );

        if (spawned != null)
        {
            _powerRoleSessionSpawned = true;
            UnityEngine.Debug.Log("PowerRoleSession spawned.");
        }
    }

    private void TrySpawnViruses()
    {
        if (!_positionReady) return;

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

        TrySpawnPowerRoleSession();

        if (VirusPrefab != null && !_primarySpawned)
        {
            NetworkObject spawned = masterRunner.Spawn(
                VirusPrefab,
                _spawnPosition,
                Quaternion.identity
            );
            if (spawned != null)
            {
                _primarySpawned = true;
                _primaryInstance = spawned;
                UnityEngine.Debug.Log("Primary virus spawned at: " + _spawnPosition);
            }
        }

        if (SecondVirusPrefab != null && !_secondarySpawned)
        {
            Vector3 p2 = _spawnPosition + secondVirusSpawnOffset;
            NetworkObject spawned2 = masterRunner.Spawn(
                SecondVirusPrefab,
                p2,
                Quaternion.identity
            );
            if (spawned2 != null)
            {
                _secondarySpawned = true;
                _secondaryInstance = spawned2;
                UnityEngine.Debug.Log("Second virus spawned at: " + p2);
            }
        }

        formationManager?.TrySpawnFormations(masterRunner, _spawnPosition);
    }

    private void EnsureSpawnPoseFromAnchor()
    {
        if (networkedTableAnchor == null || !networkedTableAnchor.IsTablePlaced)
            return;

        _spawnPosition = networkedTableAnchor.PlacedSurfacePosition + Vector3.up * 0.15f;
        _positionReady = true;
        _lastAppliedPlacementVersion = networkedTableAnchor.CurrentPlacementVersion;
    }

    private void DespawnLegacyViruses(NetworkRunner runner)
    {
        if (runner == null) return;

        if (_primaryInstance != null && _primaryInstance.IsValid)
            runner.Despawn(_primaryInstance);
        if (_secondaryInstance != null && _secondaryInstance.IsValid)
            runner.Despawn(_secondaryInstance);
    }

    private NetworkRunner GetMasterRunner()
    {
        foreach (NetworkRunner runner in _registeredRunners)
        {
            if (runner != null && runner.IsRunning && runner.IsSharedModeMasterClient)
                return runner;
        }

        NetworkRunner[] runners = FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None);
        foreach (NetworkRunner runner in runners)
        {
            if (runner != null && runner.IsRunning && runner.IsSharedModeMasterClient)
                return runner;
        }

        return null;
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsSharedModeMasterClient) return;
        TrySpawnPowerRoleSession();
        TrySpawnViruses();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        _primarySpawned = false;
        _secondarySpawned = false;
        _powerRoleSessionSpawned = false;
        _lastAppliedPlacementVersion = -1;
    }

    private IEnumerator DiscoverRunnersRoutine()
    {
        while (true)
        {
            RegisterToActiveRunners();
            // Stop once we have a master runner and all viruses are spawned — OnPlayerJoined handles the rest.
            bool hasMaster = false;
            foreach (var r in _registeredRunners)
            {
                if (r != null && r.IsRunning && r.IsSharedModeMasterClient) { hasMaster = true; break; }
            }
            if (hasMaster && AllRequestedVirusesSpawned() && (formationManager == null || formationManager.HasSpawned))
                yield break;
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
            TrySpawnPowerRoleSession();
            TrySpawnViruses();
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