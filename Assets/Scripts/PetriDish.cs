using UnityEngine;
using Fusion;

public class PetriDish : NetworkBehaviour
{
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

        // Try to snap nearby virus
        if (!IsOccupied)
        {
            NetworkGrabbableVirus virus = FindFirstObjectByType<NetworkGrabbableVirus>();
            if (virus != null && !virus.IsBeingGrabbed)
            {
                Vector3 center = GetHoverPosition();
                float dist = Vector3.Distance(virus.transform.position, center);

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
        }
    }
}