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
    [SerializeField] private PowerKind[] powerAssignmentOrder = { PowerKind.Pulse, PowerKind.Shape, PowerKind.Color, PowerKind.Scale };

    [Header("Assignment timing")]
    [Tooltip("Wait this long after join before assigning a power, so PC spectators can RPC_RegisterSpectator first.")]
    [SerializeField] private float powerAssignDelaySeconds = 2.5f;

    [Header("Ranked seat map (recommended for lab)")]
    [Tooltip("When enabled, non-spectators are sorted by PlayerId; lowest gets Pulse, then Shape, Color, Scale. Solo testers always get Pulse even if PlayerId is not 1.")]
    [SerializeField] private bool useFixedPlayerIdSlots = true;

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
        PlayerRef p = TryFindPlayerById(playerId);
        if (p == PlayerRef.None)
            Debug.LogWarning($"[PowerRoleSession] Player {playerId} not found.");
        return p;
    }

    private PlayerRef TryFindPlayerById(int playerId)
    {
        if (Runner == null)
            return PlayerRef.None;
        foreach (var p in Runner.ActivePlayers)
            if (p.PlayerId == playerId)
                return p;
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
            if (SlotContainsPlayer(p) || IsSpectator(p))
                continue;
            StartCoroutine(DelayedAssignPlayerRoutine(p));
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

    /// <summary>True when this player holds a gameplay power (not a facilitator PC client).</summary>
    public bool HasAssignedPowerSlot(PlayerRef player) => SlotContainsPlayer(player);

    /// <summary>State authority only: mark a joiner as non-playing spectator.</summary>
    public void RegisterSpectatorOnAuthority(PlayerRef player)
    {
        if (!Object.HasStateAuthority || player == PlayerRef.None)
            return;

        SpectatorPlayerIdMask |= 1 << player.PlayerId;
        ClearPowerSlotsForPlayer(player);
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
        ClearPowerSlotsForPlayer(info.Source);
        Debug.Log($"[PowerRoleSession] Registered spectator PlayerId {info.Source.PlayerId}.");
        LogAssignments();
    }

    private void ClearPowerSlotsForPlayer(PlayerRef player)
    {
        if (player == PlayerRef.None)
            return;

        bool changed = false;
        if (ColorPowerPlayer == player) { ColorPowerPlayer = PlayerRef.None; changed = true; }
        if (ScalePowerPlayer == player) { ScalePowerPlayer = PlayerRef.None; changed = true; }
        if (ShapeVariantPlayer == player) { ShapeVariantPlayer = PlayerRef.None; changed = true; }
        if (PulsePowerPlayer == player) { PulsePowerPlayer = PlayerRef.None; changed = true; }

        if (changed)
        {
            Debug.Log($"[PowerRoleSession] Cleared gameplay power slots for spectator {player} (PlayerId {player.PlayerId}).");
            TryFillOpenPowerSlots();
        }
    }

    /// <summary>After a spectator releases a slot, re-apply seat assignments.</summary>
    private void TryFillOpenPowerSlots()
    {
        if (!Object.HasStateAuthority || Runner == null)
            return;

        if (useFixedPlayerIdSlots)
            ApplyFixedPlayerIdAssignments();
        else
        {
            foreach (var p in ToSortedList(Runner.ActivePlayers))
            {
                if (p == PlayerRef.None || IsSpectator(p) || SlotContainsPlayer(p))
                    continue;
                TryAssignPlayerToNextSlot(p);
            }
        }
    }

    /// <summary>
    /// Sorted non-spectators by PlayerId → powerAssignmentOrder slots (Pulse first).
    /// One headset in the room always gets Pulse regardless of whether Fusion assigned PlayerId 1 or 2.
    /// </summary>
    private void ApplyFixedPlayerIdAssignments()
    {
        if (!Object.HasStateAuthority || Runner == null)
            return;

        var players = new List<PlayerRef>();
        foreach (var p in ToSortedList(Runner.ActivePlayers))
        {
            if (p != PlayerRef.None && !IsSpectator(p))
                players.Add(p);
        }

        if (players.Count == 0)
            return;

        PulsePowerPlayer = PlayerRef.None;
        ShapeVariantPlayer = PlayerRef.None;
        ColorPowerPlayer = PlayerRef.None;
        ScalePowerPlayer = PlayerRef.None;

        int slotCount = Mathf.Min(powerAssignmentOrder.Length, players.Count);
        for (int i = 0; i < slotCount; i++)
            AssignPowerToPlayer(players[i], powerAssignmentOrder[i]);

        Debug.Log($"[PowerRoleSession] Ranked assignment for {players.Count} gameplay player(s): " +
                  string.Join(", ", players.ConvertAll(p => $"PlayerId {p.PlayerId}")));
        LogAssignments();
    }

    private void AssignPowerToPlayer(PlayerRef player, PowerKind kind)
    {
        switch (kind)
        {
            case PowerKind.Pulse:
                PulsePowerPlayer = player;
                break;
            case PowerKind.Shape:
                ShapeVariantPlayer = player;
                break;
            case PowerKind.Color:
                ColorPowerPlayer = player;
                break;
            case PowerKind.Scale:
                ScalePowerPlayer = player;
                break;
        }
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
                    ColorPowerPlayer = player;
                    Debug.Log($"[PowerRoleSession] Assigned Color → {player} (PlayerId {player.PlayerId})");
                    LogAssignments();
                    return;
                case PowerKind.Scale when ScalePowerPlayer == PlayerRef.None:
                    ScalePowerPlayer = player;
                    Debug.Log($"[PowerRoleSession] Assigned Scale → {player} (PlayerId {player.PlayerId})");
                    LogAssignments();
                    return;
                case PowerKind.Shape when ShapeVariantPlayer == PlayerRef.None:
                    ShapeVariantPlayer = player;
                    Debug.Log($"[PowerRoleSession] Assigned Shape → {player} (PlayerId {player.PlayerId})");
                    LogAssignments();
                    return;
                case PowerKind.Pulse when PulsePowerPlayer == PlayerRef.None:
                    PulsePowerPlayer = player;
                    Debug.Log($"[PowerRoleSession] Assigned Pulse → {player} (PlayerId {player.PlayerId})");
                    LogAssignments();
                    return;
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

    private int CountGameplayPlayers()
    {
        if (Runner == null)
            return 0;
        int n = 0;
        foreach (var p in Runner.ActivePlayers)
            if (p != PlayerRef.None && !IsSpectator(p))
                n++;
        return n;
    }

    private IEnumerator DelayedAssignPlayerRoutine(PlayerRef player)
    {
        float delay = CountGameplayPlayers() <= 1 ? 0.5f : powerAssignDelaySeconds;
        float deadline = Time.time + delay;
        while (Time.time < deadline)
        {
            if (!Object.IsValid || !Object.HasStateAuthority)
                yield break;
            if (player == PlayerRef.None || SlotContainsPlayer(player))
                yield break;
            if (IsSpectator(player))
                yield break;
            yield return null;
        }

        if (!Object.IsValid || !Object.HasStateAuthority)
            yield break;
        if (player == PlayerRef.None || SlotContainsPlayer(player))
            yield break;
        if (IsSpectator(player))
            yield break;

        if (useFixedPlayerIdSlots)
            ApplyFixedPlayerIdAssignments();
        else
            TryAssignPlayerToNextSlot(player);
    }

    private void LogAssignments()
    {
        Debug.Log($"[PowerRoleSession] Assignments — Pulse:{PulsePowerPlayer} Shape:{ShapeVariantPlayer} Color:{ColorPowerPlayer} Scale:{ScalePowerPlayer}");
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
