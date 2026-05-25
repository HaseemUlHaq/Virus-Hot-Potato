using Fusion;
using TMPro;
using UnityEngine;

// Snap slot in the placeholder formation. Green when the snapped virus matches any virus in the round formation (any slot).
public class PlaceholderSlot : PetriDish
{
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

    private PlaceholderFormation _formation;
    private bool _wasOccupied;

    public float ScaleTolerance => scaleTolerance;

    public void BindFormation(PlaceholderFormation formation)
    {
        _formation = formation;
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!Object.HasStateAuthority) return;

        IsFilledCorrectly = IsOccupied && SnappedVirus != null && VirusMatchesFormation(SnappedVirus);
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

    public bool VirusMatchesFormation(NetworkGrabbableVirus virus)
    {
        if (_formation == null)
            _formation = GetComponentInParent<PlaceholderFormation>();

        return _formation != null && _formation.VirusMatchesFormation(virus, scaleTolerance);
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
            wrongPlacementText.text = "Not in formation";
    }
}
