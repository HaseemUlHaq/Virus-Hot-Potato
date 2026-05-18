using Fusion;
using UnityEngine;

// Root of the placeholder formation. Checks every tick if all slots are correctly filled and fires OnIsCompleteChanged when the puzzle is solved.
public class PlaceholderFormation : NetworkBehaviour
{
    [Header("Formation")]
    [SerializeField] private Transform formationRoot;

    [Header("Slots (children of FormationRoot, same order as VirusFormationData.slots)")]
    [SerializeField] private PlaceholderSlot[] slots;

    [Header("Formation Data (assign in prefab — applied on all clients in Spawned)")]
    [SerializeField] private VirusFormationData preassignedFormationData;

    [Header("Connection Lines (optional visual links between slots)")]
    [SerializeField] private LineRenderer[] connectionLines;

    [Header("Rotation")]
    [SerializeField] private float rotationSensitivity = 1f;

    [Networked] private float _rotationY { get; set; }

    [Networked, OnChangedRender(nameof(OnIsCompleteChanged))]
    public NetworkBool IsComplete { get; private set; }

    public Transform FormationRoot => formationRoot;
    public float RotationSensitivity => rotationSensitivity;

    public override void Spawned()
    {
        base.Spawned();
        if (preassignedFormationData != null)
            ConfigureSlots(preassignedFormationData);
    }

    public override void FixedUpdateNetwork()
    {
        ApplyRotation();

        if (!Object.HasStateAuthority) return;

        bool complete = slots != null && slots.Length > 0;
        foreach (var slot in slots)
        {
            if (slot == null || !slot.IsFilledCorrectly)
            {
                complete = false;
                break;
            }
        }

        IsComplete = complete;
    }

    public override void Render()
    {
        base.Render();
        ApplyRotation();
        RefreshConnectionLines();
    }

    /// <summary>Normalized input (e.g. degrees / sensitivity). Synced for all players.</summary>
    public void RequestRotate(float normalizedDelta)
    {
        if (!Object || !Object.IsValid) return;
        RPC_Rotate(normalizedDelta * rotationSensitivity);
    }

    /// <summary>Degrees to add to networked Y rotation.</summary>
    public void AddRotationDegrees(float deltaDegrees)
    {
        if (!Object || !Object.IsValid) return;
        RPC_Rotate(deltaDegrees);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_Rotate(float deltaDegrees)
    {
        _rotationY += deltaDegrees;
    }

    /// <summary>Called by FormationManager after spawn.</summary>
    public void ConfigureSlots(VirusFormationData data)
    {
        if (data == null || slots == null) return;
        for (int i = 0; i < slots.Length && i < data.slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].ConfigureFromSlot(data.slots[i]);
        }
    }

    public int SlotCount => slots != null ? slots.Length : 0;
    public PlaceholderSlot GetSlot(int index) => (slots != null && index < slots.Length) ? slots[index] : null;

    private void ApplyRotation()
    {
        Transform root = formationRoot != null ? formationRoot : transform;
        root.localRotation = Quaternion.Euler(0f, _rotationY, 0f);
    }

    private void OnIsCompleteChanged()
    {
        if (IsComplete)
            Debug.Log("[PlaceholderFormation] All slots correctly filled — formation complete!");
    }

    private void RefreshConnectionLines()
    {
        if (connectionLines == null) return;
        for (int i = 0; i < connectionLines.Length; i++)
        {
            if (connectionLines[i] == null) continue;
        }
    }
}
