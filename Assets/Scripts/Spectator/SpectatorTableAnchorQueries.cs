using Fusion;
using UnityEngine;

/// <summary>
/// Safe reads for <see cref="NetworkedTableAnchor"/> on the PC spectator client
/// (scene objects are not spawned until Fusion StartGame completes).
/// </summary>
public static class SpectatorTableAnchorQueries
{
    public static bool IsNetworkSpawned(NetworkedTableAnchor anchor)
    {
        if (anchor == null)
            return false;

        NetworkObject networkObject = anchor.Object;
        return networkObject != null && networkObject.IsValid;
    }

    public static bool TryGetPlacedState(
        NetworkedTableAnchor anchor,
        out bool isPlaced,
        out int placementVersion,
        out Vector3 surfacePosition)
    {
        isPlaced = false;
        placementVersion = 0;
        surfacePosition = default;

        if (!IsNetworkSpawned(anchor))
            return false;

        isPlaced = anchor.IsTablePlaced;
        placementVersion = anchor.CurrentPlacementVersion;
        if (isPlaced)
            surfacePosition = anchor.PlacedSurfacePosition;

        return true;
    }
}
