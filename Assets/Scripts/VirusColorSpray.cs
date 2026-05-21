using UnityEngine;

// Attach to the spray bottle. Wire WaterSpray.WhenSpray → OnSpray().
// Raycasts from the nozzle and cycles the material on any NetworkGrabbableVirus it hits.
public class VirusColorSpray : MonoBehaviour
{
    [SerializeField] private Transform nozzle;
    [SerializeField] private float range = 2f;

    // Wired to WaterSpray.WhenSpray UnityEvent in the Inspector
    public void OnSpray()
    {
        if (nozzle == null) { Debug.LogWarning("[ColorSpray] Nozzle not assigned!"); return; }

        Debug.Log($"[ColorSpray] Spraying from {nozzle.position} dir {nozzle.forward}");

        if (Physics.Raycast(nozzle.position, nozzle.forward, out RaycastHit hit, range))
        {
            Debug.Log($"[ColorSpray] Hit: {hit.collider.name}");
            NetworkGrabbableVirus virus = hit.collider.GetComponentInParent<NetworkGrabbableVirus>();
            if (virus != null)
            {
                Debug.Log($"[ColorSpray] Virus hit — cycling material");
                virus.RPC_RequestCycleMaterial(true);
            }
            else
            {
                Debug.Log($"[ColorSpray] Hit {hit.collider.name} but no virus found");
            }
        }
        else
        {
            Debug.Log("[ColorSpray] Raycast missed — nothing in range");
        }
    }
}
