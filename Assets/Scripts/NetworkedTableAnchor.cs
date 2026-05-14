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

    private Vector3 _pendingPosition;
    private Quaternion _pendingRotation;
    private bool _hasPending;

    public override void Spawned()
    {
        if (Runner.IsSharedModeMasterClient && !Object.HasStateAuthority)
            Object.RequestStateAuthority();
    }

    private void Update()
    {
        if (!_hasPending) return;
        if (Object == null || !Object.HasStateAuthority) return;

        PlaceNow(_pendingPosition, _pendingRotation);
        _hasPending = false;
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
    /// Only the master (state authority) commits — others store pending and discard it.
    /// </summary>
    public void RequestPlacement(Vector3 position, Quaternion rotation)
    {
        if (Object != null && Object.HasStateAuthority)
        {
            PlaceNow(position, rotation);
        }
        else
        {
            _pendingPosition = position;
            _pendingRotation = rotation;
            _hasPending = true;
        }
    }

    private void PlaceNow(Vector3 position, Quaternion rotation)
    {
        SyncedPosition = position;
        SyncedRotation = rotation;
        IsPlaced = true;
        Debug.Log($"[NetworkedTableAnchor] Master placed table at {position}");
    }
}
