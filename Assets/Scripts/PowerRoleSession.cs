using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// Networked session: first three joiners get Power A (color), B (scale), C (pulse) in order.
/// Slots are never cleared/remapped on leave (fixed assignment for the session).
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PowerRoleSession : NetworkBehaviour, INetworkRunnerCallbacks
{
    public static PowerRoleSession Instance { get; private set; }

    [Header("Debug")]
    [Tooltip("When enabled on this client, local gate checks act as if this client has all three powers.")]
    [SerializeField] private bool debugAllowAllPowersWhenUnassigned;

    [Networked] public PlayerRef ColorPowerPlayer { get; set; }
    [Networked] public PlayerRef ScalePowerPlayer { get; set; }
    [Networked] public PlayerRef PulsePowerPlayer { get; set; }

    private bool _reconciledOnce;
    private bool _loggedFourthPlayerFull;

    public bool DebugAllowAllPowersWhenUnassigned => debugAllowAllPowersWhenUnassigned;

    public override void Spawned()
    {
        Instance = this;
        if (Runner != null)
            Runner.AddCallbacks(this);
        if (Object.HasStateAuthority)
            ReconcileMissedJoinersOnce();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Runner != null)
            Runner.RemoveCallbacks(this);
        if (Instance == this)
            Instance = null;
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority && !_reconciledOnce)
            ReconcileMissedJoinersOnce();
    }

    private void ReconcileMissedJoinersOnce()
    {
        if (!Object.HasStateAuthority || Runner == null)
            return;

        var active = ToSortedList(Runner.ActivePlayers);
        foreach (var p in active)
        {
            if (p == PlayerRef.None)
                continue;
            if (SlotContainsPlayer(p))
                continue;
            TryAssignPlayerToNextSlot(p);
        }

        _reconciledOnce = true;
    }

    private static List<PlayerRef> ToSortedList(IEnumerable<PlayerRef> players)
    {
        var list = players.ToList();
        list.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));
        return list;
    }

    private bool SlotContainsPlayer(PlayerRef p)
    {
        return ColorPowerPlayer == p || ScalePowerPlayer == p || PulsePowerPlayer == p;
    }

    private void TryAssignPlayerToNextSlot(PlayerRef player)
    {
        if (ColorPowerPlayer == PlayerRef.None)
        {
            ColorPowerPlayer = player;
            return;
        }

        if (ScalePowerPlayer == PlayerRef.None)
        {
            ScalePowerPlayer = player;
            return;
        }

        if (PulsePowerPlayer == PlayerRef.None)
        {
            PulsePowerPlayer = player;
            return;
        }

        if (!_loggedFourthPlayerFull)
        {
            _loggedFourthPlayerFull = true;
            Debug.Log("[PowerRoleSession] All power slots full — further joiners get no power role.");
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!Object.IsValid || !Object.HasStateAuthority)
            return;
        if (player == PlayerRef.None)
            return;
        if (SlotContainsPlayer(player))
            return;
        TryAssignPlayerToNextSlot(player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // Intentionally no-op: keep fixed PlayerRef slots for the session.
    }

    public bool IsPlayerStillConnected(PlayerRef p)
    {
        if (p == PlayerRef.None || Runner == null)
            return false;
        foreach (var ap in Runner.ActivePlayers)
        {
            if (ap == p)
                return true;
        }

        return false;
    }

    public bool IsColorPlayer(PlayerRef p)
    {
        if (debugAllowAllPowersWhenUnassigned)
            return true;
        return p != PlayerRef.None && ColorPowerPlayer == p;
    }

    public bool IsScalePlayer(PlayerRef p)
    {
        if (debugAllowAllPowersWhenUnassigned)
            return true;
        return p != PlayerRef.None && ScalePowerPlayer == p;
    }

    public bool IsPulsePlayer(PlayerRef p)
    {
        if (debugAllowAllPowersWhenUnassigned)
            return true;
        return p != PlayerRef.None && PulsePowerPlayer == p;
    }

    /// <summary>Which power the local player has, if any.</summary>
    public bool TryGetLocalPowerKind(out char kind)
    {
        kind = '-';
        if (Runner == null)
            return false;
        var lp = Runner.LocalPlayer;
        if (debugAllowAllPowersWhenUnassigned)
        {
            kind = '*';
            return true;
        }

        if (ColorPowerPlayer == lp)
        {
            kind = 'A';
            return true;
        }

        if (ScalePowerPlayer == lp)
        {
            kind = 'B';
            return true;
        }

        if (PulsePowerPlayer == lp)
        {
            kind = 'C';
            return true;
        }

        return false;
    }

    public int ActivePlayerCount => Runner != null ? Runner.ActivePlayers.Count() : 0;

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
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
