using Fusion;
using UnityEngine;

// The locked reference formation players look at each round. Rotation is synced across all peers so everyone sees the same angle.
public class ExampleVirusFormation : NetworkBehaviour
{
    [Header("Formation")]
    [SerializeField] private Transform formationRoot;
    [SerializeField] private LockedVirusDisplay[] displayViruses;
    [SerializeField] private LineRenderer[] connectionLines;

    [Header("Formation Data (assign in prefab — applied on all clients in Spawned)")]
    [SerializeField] private VirusFormationData preassignedFormationData;

    [Header("Rotation")]
    [SerializeField] private float rotationSensitivity = 90f;

    [Networked] private float _rotationY { get; set; }

    public override void Spawned()
    {
        base.Spawned();
        if (preassignedFormationData != null)
            ApplyFormationData(preassignedFormationData);
    }

    public override void Render()
    {
        base.Render();
        if (formationRoot != null)
            formationRoot.localRotation = Quaternion.Euler(0f, _rotationY, 0f);

        RefreshConnectionLines();
    }

    /// <summary>Called by FormationManager after spawn.</summary>
    public void ApplyFormationData(VirusFormationData data)
    {
        if (data == null || displayViruses == null) return;

        for (int i = 0; i < displayViruses.Length && i < data.slots.Length; i++)
        {
            var config = data.slots[i];
            var display = displayViruses[i];
            if (display == null) continue;

            display.transform.localPosition = config.localPosition;
            display.transform.localEulerAngles = config.localEulerAngles;
            display.ApplyConfig(config.materialIndex, config.scale, config.isPulsating, config.shapeVariantIndex);
        }
    }

    public void RequestRotate(float deltaY)
    {
        RPC_Rotate(deltaY * rotationSensitivity);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_Rotate(float deltaDegrees)
    {
        _rotationY += deltaDegrees;
    }

    private void RefreshConnectionLines()
    {
        for (int i = 0; i < connectionLines.Length; i++)
        {
            var line = connectionLines[i];
            if (line == null || line.positionCount < 2) continue;
        }
    }
}
