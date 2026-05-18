using Fusion;
using UnityEngine;

// Root of the placeholder formation. Checks every tick if all slots are correctly filled and fires OnIsCompleteChanged when the puzzle is solved.
public class PlaceholderFormation : NetworkBehaviour
{
    [Header("Slots (children, same order as VirusFormationData.slots)")]
    [SerializeField] private PlaceholderSlot[] slots;

    [Header("Formation Data (assign in prefab — applied on all clients in Spawned)")]
    [SerializeField] private VirusFormationData preassignedFormationData;

    [Header("Connection Lines (optional visual links between slots)")]
    [SerializeField] private LineRenderer[] connectionLines;

    [Networked, OnChangedRender(nameof(OnIsCompleteChanged))]
    public NetworkBool IsComplete { get; private set; }

    public override void Spawned()
    {
        base.Spawned();
        if (preassignedFormationData != null)
            ConfigureSlots(preassignedFormationData);
    }

    public override void FixedUpdateNetwork()
    {
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
        RefreshConnectionLines();
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

    private void OnIsCompleteChanged()
    {
        if (IsComplete)
            Debug.Log("[PlaceholderFormation] All slots correctly filled — formation complete!");
        // TODO: hook into round manager / trigger victory VFX
    }

    private void RefreshConnectionLines()
    {
        for (int i = 0; i < connectionLines.Length; i++)
        {
            var line = connectionLines[i];
            if (line == null) continue;
        }
    }
}
