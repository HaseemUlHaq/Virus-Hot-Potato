using Fusion;
using TMPro;
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

    [Header("Wrong Placement Popup")]
    [Tooltip("World-space UI GameObject that appears above the slot when placement is wrong.")]
    [SerializeField] private GameObject wrongPlacementPopup;
    [Tooltip("TextMeshPro text inside the popup that lists what is wrong.")]
    [SerializeField] private TextMeshProUGUI wrongPlacementText;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip correctClip;
    [SerializeField] private AudioClip wrongClip;

    [Networked] public NetworkBool IsFilledCorrectly { get; private set; }

    private bool _wasOccupied;

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

        if (IsOccupied && !_wasOccupied)
        {
            AudioClip clip = IsFilledCorrectly ? correctClip : wrongClip;
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }
        _wasOccupied = IsOccupied;

        UpdateWrongPlacementPopup();
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

    private void UpdateWrongPlacementPopup()
    {
        if (wrongPlacementPopup == null) return;

        if (!IsOccupied || IsFilledCorrectly || SnappedVirus == null)
        {
            wrongPlacementPopup.SetActive(false);
            return;
        }

        wrongPlacementPopup.SetActive(true);

        if (wrongPlacementText != null)
            wrongPlacementText.text = BuildWrongPlacementMessage(SnappedVirus);
    }

    private string BuildWrongPlacementMessage(NetworkGrabbableVirus virus)
    {
        var sb = new System.Text.StringBuilder();

        if (virus.MaterialIndex != RequiredMaterialIndex)
            sb.AppendLine("Wrong color");
        if (Mathf.Abs(virus.VirusScale - RequiredScale) > scaleTolerance)
            sb.AppendLine("Wrong size");
        if ((bool)virus.IsPulsating != RequiredIsPulsating)
            sb.AppendLine(RequiredIsPulsating ? "Should be pulsating" : "Should not be pulsating");
        if (virus.ShapeVariantIndex != RequiredShapeVariantIndex)
            sb.AppendLine("Wrong shape");

        return sb.ToString().TrimEnd();
    }
}
