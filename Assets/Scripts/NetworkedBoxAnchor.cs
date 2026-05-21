using Fusion;
using UnityEngine;

// Add to ToolboxRoot alongside a NetworkObject.
// Master client writes the QR-detected position; all clients read it in Render() every frame.
public class NetworkedBoxAnchor : NetworkBehaviour
{
    [Networked] private Vector3 SyncedPosition { get; set; }
    [Networked] private NetworkBool IsPlaced { get; set; }

    public bool IsBoxPlaced => IsPlaced;

    public override void Spawned()
    {
        if (Runner.IsSharedModeMasterClient && !Object.HasStateAuthority)
            Object.RequestStateAuthority();
    }

    public override void Render()
    {
        if (IsPlaced)
            transform.position = SyncedPosition;
    }

    public void RequestPlacement(Vector3 position)
    {
        if (Object == null || !Object.IsValid) return;

        if (Object.HasStateAuthority)
            PlaceNow(position);
        else
            RPC_RequestPlacement(position);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestPlacement(Vector3 position, RpcInfo info = default)
    {
        PlaceNow(position);
    }

    private void PlaceNow(Vector3 position)
    {
        SyncedPosition = position;
        IsPlaced = true;
        Debug.Log($"[NetworkedBoxAnchor] Box anchor placed at {position}");
    }
}
