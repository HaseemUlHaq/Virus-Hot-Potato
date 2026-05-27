using Fusion;
using UnityEngine;

/// <summary>
/// Add to TableRoot alongside a NetworkObject.
/// Master client writes the QR-detected position to networked properties.
/// All clients (including late joiners) read the position in Render() every frame —
/// the correct Fusion pattern for visual transforms that don't need physics prediction.
/// </summary>
public class NetworkedTableAnchor : NetworkBehaviour
{
    [Header("Round reset")]
    [Tooltip("Fusion PlayerId that may trigger left-hand long-pinch round reset. Set to 0 to allow any player.")]
    [SerializeField] private int roundResetPlayerId = 1;

    public int RoundResetPlayerId => roundResetPlayerId;

    [Networked] private Vector3 SyncedPosition { get; set; }
    [Networked] private Quaternion SyncedRotation { get; set; }
    [Networked] private NetworkBool IsPlaced { get; set; }
    [Networked] private int PlacementVersion { get; set; }

    public bool IsTablePlaced => IsPlaced;
    public Quaternion PlacedRotation => SyncedRotation;
    public Vector3 PlacedSurfacePosition => SyncedPosition;
    /// <summary>Increments whenever state authority commits a new placement; spawners use this to resync after round reset.</summary>
    public int CurrentPlacementVersion => PlacementVersion;

    public override void Spawned()
    {
        if (Runner.IsSharedModeMasterClient && !Object.HasStateAuthority)
            Object.RequestStateAuthority();
    }

    // Render() runs every visual frame on all clients.
    // Applying networked state here guarantees every client
    // (including late joiners who missed the initial change event) stays in sync.
    public override void Render()
    {
        if (IsPlaced)
        {
            transform.position = SyncedPosition;
            transform.rotation = SyncedRotation;
        }
    }

    /// <summary>
    /// Called by TableAnchor when QR code is detected on any client.
    /// State authority commits immediately; everyone else forwards via RPC.
    /// </summary>
    public void RequestPlacement(Vector3 position, Quaternion rotation)
    {
        if (Object == null || !Object.IsValid)
            return;

        if (Object.HasStateAuthority)
            PlaceNow(position, rotation);
        else
            RPC_RequestPlacement(position, rotation);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestPlacement(Vector3 position, Quaternion rotation, RpcInfo info = default)
    {
        PlaceNow(position, rotation);
    }

    private void PlaceNow(Vector3 position, Quaternion rotation)
    {
        SyncedPosition = position;
        SyncedRotation = rotation;
        IsPlaced = true;
        PlacementVersion++;
        Debug.Log($"[NetworkedTableAnchor] Table placed at {position} (version {PlacementVersion})");
    }

    /// <summary>
    /// Player 1 only: left-hand long pinch requests a round reset on the shared-mode master.
    /// Keeps table placement and colocation; despawns and respawns puzzle entities.
    /// </summary>
    public void RequestRoundReset()
    {
        if (Object == null || !Object.IsValid || Runner == null)
            return;

        if (!CanPlayerRequestRoundReset(Runner.LocalPlayer))
            return;

        if (Object.HasStateAuthority)
            ExecuteRoundReset();
        else
            RPC_RequestRoundReset();
    }

    public bool CanPlayerRequestRoundReset(PlayerRef player)
    {
        if (player == PlayerRef.None)
            return false;
        if (roundResetPlayerId <= 0)
            return true;
        return player.PlayerId == roundResetPlayerId;
    }

    public bool CanLocalPlayerRequestRoundReset()
    {
        NetworkRunner runner = Runner != null && Runner.IsRunning ? Runner : FindActiveRunner();
        return runner != null && CanPlayerRequestRoundReset(runner.LocalPlayer);
    }

    /// <summary>
    /// PC spectator facilitator: same round reset as player 1 pinch, without changing Quest permissions.
    /// </summary>
    public void RequestSpectatorRoundReset()
    {
        if (Object == null || !Object.IsValid || Runner == null)
            return;

        if (!CanLocalSpectatorRequestRoundReset())
            return;

        if (Object.HasStateAuthority)
            ExecuteRoundResetAsSpectator(Runner.LocalPlayer);
        else
            RPC_RequestSpectatorRoundReset();
    }

    public bool CanSpectatorRequestRoundReset(PlayerRef player)
    {
        if (player == PlayerRef.None)
            return false;

        PowerRoleSession powerRoles = PowerRoleSession.Instance;
        if (powerRoles == null || !powerRoles.Object.IsValid)
            return false;

        if (powerRoles.HasAssignedPowerSlot(player))
            return false;

        return powerRoles.IsSpectator(player);
    }

    public bool CanLocalSpectatorRequestRoundReset()
    {
        NetworkRunner runner = Runner != null && Runner.IsRunning ? Runner : FindActiveRunner();
        if (runner == null)
            return false;

        if (SpectatorSession.LocalIsSpectator)
            return true;

        return CanSpectatorRequestRoundReset(runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestRoundReset(RpcInfo info = default)
    {
        if (!CanPlayerRequestRoundReset(info.Source))
            return;

        ExecuteRoundReset();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestSpectatorRoundReset(RpcInfo info = default)
    {
        ExecuteRoundResetAsSpectator(info.Source);
    }

    private void ExecuteRoundResetAsSpectator(PlayerRef requester)
    {
        PowerRoleSession powerRoles = PowerRoleSession.Instance;
        if (powerRoles == null || !powerRoles.Object.IsValid)
        {
            Debug.LogWarning("[NetworkedTableAnchor] Spectator round reset ignored — no PowerRoleSession.");
            return;
        }

        if (requester == PlayerRef.None)
            return;

        if (powerRoles.HasAssignedPowerSlot(requester))
        {
            Debug.LogWarning(
                $"[NetworkedTableAnchor] Spectator round reset clearing mis-assigned gameplay power for PlayerId {requester.PlayerId}.");
        }

        powerRoles.RegisterSpectatorOnAuthority(requester);
        ExecuteRoundReset();
    }

    private static NetworkRunner FindActiveRunner()
    {
        foreach (NetworkRunner runner in FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None))
        {
            if (runner != null && runner.IsRunning)
                return runner;
        }

        return null;
    }

    private void ExecuteRoundReset()
    {
        VirusSpawner spawner = FindFirstObjectByType<VirusSpawner>(FindObjectsInactive.Include);
        if (spawner == null)
            Debug.LogWarning("[NetworkedTableAnchor] Round reset requested but no VirusSpawner found.");
        else
            spawner.RestartRoundAtCurrentTable();

        RPC_ResetEndGameVisuals();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ResetEndGameVisuals()
    {
        EndGameBottleReveal reveal = FindFirstObjectByType<EndGameBottleReveal>(FindObjectsInactive.Include);
        reveal?.ResetForNewRound();
    }
}
