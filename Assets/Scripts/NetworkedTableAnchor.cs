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
    /// Left-hand long pinch (or any client) requests a round reset on the shared-mode master.
    /// Keeps table placement and colocation; despawns and respawns puzzle entities.
    /// </summary>
    public void RequestRoundReset()
    {
        if (Object == null || !Object.IsValid)
            return;

        if (Object.HasStateAuthority)
            ExecuteRoundReset();
        else
            RPC_RequestRoundReset();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestRoundReset(RpcInfo info = default)
    {
        ExecuteRoundReset();
    }

    private static void ExecuteRoundReset()
    {
        VirusSpawner spawner = FindFirstObjectByType<VirusSpawner>(FindObjectsInactive.Include);
        if (spawner == null)
        {
            Debug.LogWarning("[NetworkedTableAnchor] Round reset requested but no VirusSpawner found.");
            return;
        }

        spawner.RestartRoundAtCurrentTable();
    }
}
