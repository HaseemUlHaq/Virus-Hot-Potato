using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// Networked session: joiners are assigned powers in the order defined by powerAssignmentOrder.
/// Slots are never cleared/remapped on leave (fixed assignment for the session).
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PowerRoleSession : NetworkBehaviour, INetworkRunnerCallbacks
{
    public static PowerRoleSession Instance { get; private set; }

    public enum PowerKind { Color, Scale, Shape, Pulse }

    [Header("Power Assignment Order")]
    [Tooltip("Which power each joiner receives, in order. 1st joiner = slot 0, 2nd = slot 1, etc.")]
    [SerializeField] private PowerKind[] powerAssignmentOrder = { PowerKind.Color, PowerKind.Scale, PowerKind.Shape, PowerKind.Pulse };

    [Header("Debug")]
    [Tooltip("When enabled on this client, local gate checks act as if this client has all three powers.")]
    [SerializeField] private bool debugAllowAllPowersWhenUnassigned;

    [Header("Manual Power Assignment (Play Mode)")]
    [Tooltip("Enter the PlayerId shown in console as [Player:N]. 0 = skip. Then right-click → Apply Manual Assignments.")]
    [SerializeField] private int manualColorPlayerId = 0;
    [SerializeField] private int manualScalePlayerId = 0;
    [SerializeField] private int manualShapePlayerId = 0;
    [SerializeField] private int manualPulsePlayerId = 0;

    [ContextMenu("Apply Manual Assignments")]
    public void ApplyManualAssignments()
    {
        if (Runner == null || !Object.HasStateAuthority)
        {
            Debug.LogWarning("[PowerRoleSession] Must have state authority to apply assignments.");
            return;
        }
        if (manualColorPlayerId > 0)  ColorPowerPlayer  = FindPlayerById(manualColorPlayerId);
        if (manualScalePlayerId > 0)  ScalePowerPlayer  = FindPlayerById(manualScalePlayerId);
        if (manualShapePlayerId > 0)  ShapeVariantPlayer = FindPlayerById(manualShapePlayerId);
        if (manualPulsePlayerId > 0)  PulsePowerPlayer  = FindPlayerById(manualPulsePlayerId);
        Debug.Log($"[PowerRoleSession] Assignments — Color:{ColorPowerPlayer} Scale:{ScalePowerPlayer} Shape:{ShapeVariantPlayer} Pulse:{PulsePowerPlayer}");
    }

    private PlayerRef FindPlayerById(int playerId)
    {
        foreach (var p in Runner.ActivePlayers)
            if (p.PlayerId == playerId) return p;
        Debug.LogWarning($"[PowerRoleSession] Player {playerId} not found.");
        return PlayerRef.None;
    }

    [Networked] public PlayerRef ColorPowerPlayer { get; set; }
    [Networked] public PlayerRef ScalePowerPlayer { get; set; }
    [Networked] public PlayerRef ShapeVariantPlayer { get; set; }
    [Networked] public PlayerRef PulsePowerPlayer { get; set; }

    /// <summary>Bitmask of Fusion PlayerId values registered as non-playing spectators (e.g. PC client).</summary>
    [Networked] public int SpectatorPlayerIdMask { get; set; }

    private bool _reconciledOnce;
    private bool _loggedFifthPlayerFull;

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
        return ColorPowerPlayer == p || ScalePowerPlayer == p
            || ShapeVariantPlayer == p || PulsePowerPlayer == p;
    }

    public bool IsSpectator(PlayerRef player)
    {
        if (player == PlayerRef.None)
            return false;
        int bit = 1 << player.PlayerId;
        return (SpectatorPlayerIdMask & bit) != 0;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RegisterSpectator(RpcInfo info = default)
    {
        if (info.Source == PlayerRef.None)
            return;
        SpectatorPlayerIdMask |= 1 << info.Source.PlayerId;
        Debug.Log($"[PowerRoleSession] Registered spectator PlayerId {info.Source.PlayerId}.");
    }

    private void TryAssignPlayerToNextSlot(PlayerRef player)
    {
        if (IsSpectator(player))
            return;

        foreach (var kind in powerAssignmentOrder)
        {
            switch (kind)
            {
                case PowerKind.Color when ColorPowerPlayer == PlayerRef.None:
                    ColorPowerPlayer = player; return;
                case PowerKind.Scale when ScalePowerPlayer == PlayerRef.None:
                    ScalePowerPlayer = player; return;
                case PowerKind.Shape when ShapeVariantPlayer == PlayerRef.None:
                    ShapeVariantPlayer = player; return;
                case PowerKind.Pulse when PulsePowerPlayer == PlayerRef.None:
                    PulsePowerPlayer = player; return;
            }
        }

        if (!_loggedFifthPlayerFull)
        {
            _loggedFifthPlayerFull = true;
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
        StartCoroutine(DelayedAssignPlayerRoutine(player));
    }

    private IEnumerator DelayedAssignPlayerRoutine(PlayerRef player)
    {
        // Allow spectator clients time to RPC_RegisterSpectator before power assignment.
        yield return new WaitForSeconds(1f);
        if (!Object.IsValid || !Object.HasStateAuthority)
            yield break;
        if (player == PlayerRef.None || SlotContainsPlayer(player))
            yield break;
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

    public bool IsShapePlayer(PlayerRef p)
    {
        if (debugAllowAllPowersWhenUnassigned)
            return true;
        return p != PlayerRef.None && ShapeVariantPlayer == p;
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

        if (ColorPowerPlayer == lp)  { kind = 'A'; return true; }
        if (ScalePowerPlayer == lp)  { kind = 'B'; return true; }
        if (ShapeVariantPlayer == lp){ kind = 'C'; return true; }
        if (PulsePowerPlayer == lp)  { kind = 'D'; return true; }

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
