using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// TEMP test script — press S (keyboard) or B button (right controller) to cycle shape.
/// Or right-click the component header in Inspector → CycleShape.
/// Remove before shipping.
/// </summary>
public class ShapeTestDriver : MonoBehaviour
{
    [SerializeField] private PetriDish targetDish;

    private void Update()
    {
        var kb = Keyboard.current;
        if ((kb != null && kb.sKey.wasPressedThisFrame) || OVRInput.GetDown(OVRInput.Button.Two))
            CycleShape();

        if (kb != null && kb.pKey.wasPressedThisFrame)
            TriggerPulse();

        if (kb != null && kb.oKey.wasPressedThisFrame)
            TriggerPersistentPulse();
    }

    [ContextMenu("TriggerPulse")]
    public void TriggerPulse()
    {
        var virus = FindAnyObjectByType<NetworkGrabbableVirus>();
        if (virus == null) { Debug.Log("[ShapeTest] No NetworkGrabbableVirus found"); return; }
        virus.RPC_TriggerPulse();
        Debug.Log("[ShapeTest] Pulse triggered");
    }

    [ContextMenu("TriggerPersistentPulse")]
    public void TriggerPersistentPulse()
    {
        NetworkGrabbableVirus virus = null;

        if (targetDish != null && targetDish.IsOccupied && targetDish.SnappedVirus != null)
            virus = targetDish.SnappedVirus;

        if (virus == null) { Debug.Log("[ShapeTest] No virus in target dish"); return; }
        virus.RequestSetPulsatingOn();
        Debug.Log($"[ShapeTest] Persistent pulse triggered on {virus.name}");
    }

    private NetworkGrabbableVirus FindNearestVirusToHands()
    {
        NetworkGrabbableVirus best = null;
        float bestDist = float.MaxValue;

        var hands = FindObjectsByType<NetworkedHandSimple>(FindObjectsSortMode.None);

        foreach (var v in FindObjectsByType<NetworkGrabbableVirus>(FindObjectsSortMode.None))
        {
            foreach (var hand in hands)
            {
                if (hand.Object == null || !hand.Object.HasStateAuthority) continue;
                float d = Vector3.Distance(hand.transform.position, v.transform.position);
                if (d < bestDist) { bestDist = d; best = v; }
            }
        }

        // Fallback: any virus in the scene
        if (best == null)
            best = FindAnyObjectByType<NetworkGrabbableVirus>();

        return best;
    }

    [ContextMenu("CycleShape")]
    public void CycleShape()
    {
        var virus = FindAnyObjectByType<NetworkGrabbableVirus>();
        if (virus == null) { Debug.Log("[ShapeTest] No NetworkGrabbableVirus found"); return; }

        if (!virus.HasStateAuthority)
        {
            Debug.Log("[ShapeTest] No state authority — test only works as host/single player");
            return;
        }

        var cycler = virus.GetComponentInChildren<VirusShapeCycler>(true);
        int count = cycler?.ShapeCount ?? 2;
        int next = (virus.ShapeVariantIndex + 1) % count;
        virus.ShapeVariantIndex = next;
        Debug.Log($"[ShapeTest] ShapeVariantIndex → {next} (of {count})");
    }
}
