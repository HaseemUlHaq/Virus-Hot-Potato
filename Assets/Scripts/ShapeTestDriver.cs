using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// TEMP test script — press S (keyboard) or B button (right controller) to cycle shape.
/// Or right-click the component header in Inspector → CycleShape.
/// Remove before shipping.
/// </summary>
public class ShapeTestDriver : MonoBehaviour
{
    private void Update()
    {
        var kb = Keyboard.current;
        if ((kb != null && kb.sKey.wasPressedThisFrame) || OVRInput.GetDown(OVRInput.Button.Two))
            CycleShape();

        if (kb != null && kb.pKey.wasPressedThisFrame)
            TriggerPulse();
    }

    [ContextMenu("TriggerPulse")]
    public void TriggerPulse()
    {
        var virus = FindAnyObjectByType<NetworkGrabbableVirus>();
        if (virus == null) { Debug.Log("[ShapeTest] No NetworkGrabbableVirus found"); return; }
        virus.RPC_TriggerPulse();
        Debug.Log("[ShapeTest] Pulse triggered");
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
