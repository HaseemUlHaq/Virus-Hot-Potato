using Fusion;
using UnityEngine;

// A snap slot inside the placeholder formation. Extends PetriDish (inherits all snap logic) and checks if the snapped virus matches the required properties.
public class PlaceholderSlot : PetriDish
{
    [Header("Required Virus Properties")]
    [Tooltip("Set by FormationManager at runtime from VirusFormationData.")]
    public int RequiredMaterialIndex;
    [Range(0.05f, 3.0f)] public float RequiredScale = 1f;
    public bool RequiredIsPulsating;
    public int RequiredShapeVariantIndex;

    [Header("Validation")]
    [SerializeField] private float scaleTolerance = 0.3f;

    [Header("Visual Feedback")]
    [SerializeField] private Renderer slotRenderer;
    [SerializeField] private Material emptyMaterial;
    [SerializeField] private Material correctMaterial;
    [SerializeField] private Material wrongMaterial;

    [Networked] public NetworkBool IsFilledCorrectly { get; private set; }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!Object.HasStateAuthority) return;

        IsFilledCorrectly = IsOccupied && SnappedVirus != null && ValidateVirus(SnappedVirus);
    }

    public override void Render()
    {
        base.Render();
        UpdateSlotVisual();
    }

    public void ConfigureFromSlot(VirusFormationData.SlotConfig config)
    {
        RequiredMaterialIndex = config.materialIndex;
        RequiredScale = NetworkGrabbableVirus.QuantizeScale(config.scale);
        RequiredIsPulsating = config.isPulsating;
        RequiredShapeVariantIndex = config.shapeVariantIndex;
    }

    private bool ValidateVirus(NetworkGrabbableVirus virus)
    {
        if (virus.MaterialIndex != RequiredMaterialIndex) return false;
        if (Mathf.Abs(virus.VirusScale - RequiredScale) > scaleTolerance) return false;
        if ((bool)virus.IsPulsating != RequiredIsPulsating) return false;
        if (virus.ShapeVariantIndex != RequiredShapeVariantIndex) return false;
        return true;
    }

    private void UpdateSlotVisual()
    {
        if (slotRenderer == null) return;

        Material target = emptyMaterial;
        if (IsOccupied)
            target = IsFilledCorrectly ? correctMaterial : wrongMaterial;

        if (slotRenderer.sharedMaterial != target)
            slotRenderer.sharedMaterial = target;
    }
}
