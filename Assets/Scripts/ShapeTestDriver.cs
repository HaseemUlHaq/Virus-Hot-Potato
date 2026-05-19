using UnityEngine;

/// <summary>
/// TEMP test script — press S (keyboard) or B button (right controller) to cycle shape.
/// Or right-click the component header in Inspector → CycleShape.
/// Remove before shipping.
/// </summary>
public class ShapeTestDriver : MonoBehaviour
{
    private void Update()
    {
        bool triggered = Input.GetKeyDown(KeyCode.S)
            || OVRInput.GetDown(OVRInput.Button.Two);
        if (!triggered) return;
        CycleShape();
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
