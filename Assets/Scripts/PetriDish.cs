using UnityEngine;
using Fusion;

public class PetriDish : NetworkBehaviour
{
    private static PetriDish[] s_dishesCache;
    private static float s_dishesCacheTime;

    [SerializeField] private float virusHoverHeight = 0.08f;
    [SerializeField] private float snapDelay = 0.25f;
    [SerializeField] private float snapOverlapRadius = 0.4f;
    [SerializeField] private Renderer dishRenderer;

    // SIMPLE: Just track "is virus in dish" - nothing else
    [Networked] public NetworkGrabbableVirus SnappedVirus { get; set; }
    [Networked] public NetworkBool IsOccupied { get; set; }

    private float _snapDwellStart = -1f;

    public Vector3 GetHoverPosition()
    {
        Vector3 center = dishRenderer != null ? dishRenderer.bounds.center : transform.position;
        return center + Vector3.up * virusHoverHeight;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // Release if grabbed
        // Release if grabbed - USE CurrentHolder instead!
        if (IsOccupied && SnappedVirus != null && SnappedVirus.CurrentHolder != PlayerRef.None)
        {
            SnappedVirus = null;
            IsOccupied = false;
            return;
        }

        // Try to snap nearby virus (closest candidate; supports multiple viruses in the scene)
        if (!IsOccupied)
        {
            NetworkGrabbableVirus virus = FindBestVirusForSnap();
            if (virus != null)
            {
                Vector3 center = GetHoverPosition();
                float dist = GetDistanceVirusToSnapPoint(virus, center);

                if (dist <= snapOverlapRadius)
                {
                    if (_snapDwellStart < 0f)
                        _snapDwellStart = (float)Runner.SimulationTime;

                    if (Runner.SimulationTime - _snapDwellStart >= snapDelay)
                    {
                        SnappedVirus = virus;
                        IsOccupied = true;
                        _snapDwellStart = -1f;
                    }
                }
                else
                {
                    _snapDwellStart = -1f;
                }
            }
            else
            {
                _snapDwellStart = -1f;
            }
        }
    }

    private static PetriDish[] GetCachedDishes()
    {
        if (s_dishesCache == null || Time.time - s_dishesCacheTime > 1f)
        {
            s_dishesCache = FindObjectsByType<PetriDish>(FindObjectsSortMode.None);
            s_dishesCacheTime = Time.time;
        }
        return s_dishesCache ?? System.Array.Empty<PetriDish>();
    }

    /// <summary>
    /// Distance from snap target to virus: use collider if present so offset meshes /
    /// child-heavy prefabs still overlap the dish correctly.
    /// </summary>
    private static float GetDistanceVirusToSnapPoint(NetworkGrabbableVirus virus, Vector3 snapPoint)
    {
        if (virus == null) return float.MaxValue;
        Collider col = virus.GetComponent<Collider>();
        if (col != null && col.enabled)
        {
            Vector3 closest = col.ClosestPoint(snapPoint);
            return Vector3.Distance(closest, snapPoint);
        }
        return Vector3.Distance(virus.transform.position, snapPoint);
    }

    /// <summary>True if another petri dish already holds this virus.</summary>
    private bool IsVirusHeldElsewhere(NetworkGrabbableVirus virus)
    {
        if (virus == null) return true;
        foreach (var dish in GetCachedDishes())
        {
            if (dish == null || ReferenceEquals(dish, this)) continue;
            if (!dish) continue;
            if (dish.IsOccupied && dish.SnappedVirus == virus)
                return true;
        }
        return false;
    }

    private NetworkGrabbableVirus FindBestVirusForSnap()
    {
        Vector3 center = GetHoverPosition();
        NetworkGrabbableVirus best = null;
        float bestDist = float.MaxValue;

        foreach (var virus in FindObjectsByType<NetworkGrabbableVirus>(FindObjectsSortMode.None))
        {
            if (virus == null) continue;
            // Avoid excluding valid networked viruses — IsValid can be false on some peers/ticks in Shared mode.
            if (virus.Object == null) continue;
            if (virus.IsBeingGrabbed) continue;
            if (IsVirusHeldElsewhere(virus)) continue;

            float dist = GetDistanceVirusToSnapPoint(virus, center);
            if (dist <= snapOverlapRadius && dist < bestDist)
            {
                bestDist = dist;
                best = virus;
            }
        }

        return best;
    }
}