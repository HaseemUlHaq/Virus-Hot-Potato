using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using TMPro;
using UnityEngine;

/// <summary>
/// Floating world-space status panel. Follows the player's head at a fixed offset
/// and shows the current connection + QR scan phase. Hides itself once the table
/// is placed and the required number of players are connected.
///
/// Setup:
///   1. Create a World Space Canvas child of this GameObject.
///   2. Add a TMP_Text child for the status message and assign it to statusText.
///   3. Assign networkedTableAnchor from the scene.
///   4. Set requiredPlayerCount (default 2).
///   5. Place this GameObject anywhere — it will follow Camera.main each frame.
/// </summary>
public class SessionStatusHUD : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("References")]
    [SerializeField] private NetworkedTableAnchor networkedTableAnchor;
    [SerializeField] private TMP_Text statusText;

    [Header("Settings")]
    [SerializeField] private int requiredPlayerCount = 2;
    [Tooltip("Distance in front of the player's head.")]
    [SerializeField] private float forwardDistance = 1.2f;
    [Tooltip("How far below eye level the panel sits.")]
    [SerializeField] private float verticalOffset = -0.25f;
    [Tooltip("Seconds to show the 'Ready!' message before hiding.")]
    [SerializeField] private float readyDisplayDuration = 2f;

    // ── State ────────────────────────────────────────────────────────────────
    private int _playerCount;
    private bool _connected;
    private bool _hidden;
    private float _readyTimer = -1f;

    private readonly List<NetworkRunner> _runners = new List<NetworkRunner>();
    private Transform _cameraTransform;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        _cameraTransform = Camera.main?.transform;
        RegisterToActiveRunners();
    }

    private void Update()
    {
        if (_hidden) return;

        FollowHead();
        RefreshText();
        CheckReady();
    }

    private void OnDestroy()
    {
        foreach (var r in _runners)
            if (r != null) r.RemoveCallbacks(this);
    }

    // ── Head follow ───────────────────────────────────────────────────────────

    private void FollowHead()
    {
        if (_cameraTransform == null)
        {
            _cameraTransform = Camera.main?.transform;
            return;
        }

        Vector3 flat = _cameraTransform.forward;
        flat.y = 0f;
        if (flat == Vector3.zero) flat = Vector3.forward;
        flat.Normalize();

        transform.position = _cameraTransform.position
            + flat * forwardDistance
            + Vector3.up * verticalOffset;

        transform.rotation = Quaternion.LookRotation(flat);
    }

    // ── Status text ───────────────────────────────────────────────────────────

    private void RefreshText()
    {
        if (statusText == null) return;

        bool tablePlaced = networkedTableAnchor != null && networkedTableAnchor.IsTablePlaced;
        bool localQrFound = TableAnchor.TableFound;
        bool enoughPlayers = _playerCount >= requiredPlayerCount;

        if (!_connected)
        {
            statusText.text = "Connecting to session…";
            return;
        }

        if (!enoughPlayers)
        {
            statusText.text = $"Waiting for players ({_playerCount}/{requiredPlayerCount})…";
            return;
        }

        if (!localQrFound)
        {
            statusText.text = "Point at the QR code on the table";
            return;
        }

        if (!tablePlaced)
        {
            statusText.text = "Aligning with other player…";
            return;
        }

        // All conditions met — show ready message then hide
        statusText.text = "Ready!";
        if (_readyTimer < 0f)
            _readyTimer = Time.time + readyDisplayDuration;
    }

    private void CheckReady()
    {
        if (_readyTimer > 0f && Time.time >= _readyTimer)
            HideHUD();
    }

    private void HideHUD()
    {
        _hidden = true;
        gameObject.SetActive(false);
    }

    // ── Runner registration ───────────────────────────────────────────────────

    private void RegisterToActiveRunners()
    {
        var found = FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None);
        foreach (var r in found)
        {
            if (r == null || _runners.Contains(r)) continue;
            r.AddCallbacks(this);
            _runners.Add(r);
            if (r.IsRunning)
            {
                _connected = true;
                _playerCount = r.ActivePlayers.Count();
            }
        }
    }

    // ── INetworkRunnerCallbacks ───────────────────────────────────────────────

    public void OnConnectedToServer(NetworkRunner runner)
    {
        _connected = true;
        if (!_runners.Contains(runner)) { runner.AddCallbacks(this); _runners.Add(runner); }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        _playerCount = runner.ActivePlayers.Count();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        _playerCount = runner.ActivePlayers.Count();
        // Re-show HUD if a player drops mid-game
        if (_playerCount < requiredPlayerCount && _hidden)
        {
            _hidden = false;
            _readyTimer = -1f;
            gameObject.SetActive(true);
        }
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        _connected = false;
        _playerCount = 0;
        if (_hidden) { _hidden = false; _readyTimer = -1f; gameObject.SetActive(true); }
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        _connected = false;
        _playerCount = 0;
    }

    // ── Unused callbacks ──────────────────────────────────────────────────────
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
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
