using UnityEngine;

/// <summary>
/// Cycles through material variants on the virus when triggered by
/// Meta's Interaction SDK gesture system (via ActiveStateUnityEventWrapper).
/// 
/// Setup:
///   1. Add this script to the virus prefab.
///   2. Assign the 10 material variants in the inspector (ordered Virus 1 … Virus 10).
///   3. Set Default Material Index to the index of Virus 2 (i.e. 1 if zero-indexed).
///   4. Assign the MeshRenderer (or leave empty to auto-find on this GameObject).
///   5. In your Select object's ActiveStateUnityEventWrapper, wire the
///      "When Activated" event to call VirusSwipeCycler.CycleNext.
/// 
/// Networking note:
///   This script is visual-only and does not touch any networked state.
///   Your colleague can add [Networked] material index sync later if needed.
/// </summary>
public class VirusSwipeCycler : MonoBehaviour
{
    [Header("Materials")]
    [Tooltip("All material variants in order (e.g. Virus 1, Virus 2, … Virus 10).")]
    [SerializeField] private Material[] materialVariants;

    [Tooltip("Index into materialVariants for the default material (Virus 2 = index 1).")]
    [SerializeField] private int defaultMaterialIndex = 1;

    [Header("Renderer")]
    [Tooltip("The MeshRenderer whose material will be swapped. Auto-found if left empty.")]
    [SerializeField] private MeshRenderer targetRenderer;

    private int _currentIndex;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<MeshRenderer>();

        _currentIndex = Mathf.Clamp(defaultMaterialIndex, 0, materialVariants.Length - 1);
    }

    private void OnEnable()
    {
        ApplyCurrentMaterial();
    }

    // ── Wire these to ActiveStateUnityEventWrapper ──────────────────────

    public void CycleNext()
    {
        if (materialVariants == null || materialVariants.Length == 0) return;

        _currentIndex = (_currentIndex + 1) % materialVariants.Length;
        ApplyCurrentMaterial();
        Debug.Log($"[VirusSwipeCycler] Cycled NEXT → {_currentIndex} ({materialVariants[_currentIndex].name})");
    }

    public void CyclePrevious()
    {
        if (materialVariants == null || materialVariants.Length == 0) return;

        _currentIndex = (_currentIndex - 1 + materialVariants.Length) % materialVariants.Length;
        ApplyCurrentMaterial();
        Debug.Log($"[VirusSwipeCycler] Cycled PREVIOUS → {_currentIndex} ({materialVariants[_currentIndex].name})");
    }

    // ── Public API for networking ───────────────────────────────────────

    public int CurrentMaterialIndex => _currentIndex;

    public void SetMaterialIndex(int index)
    {
        if (materialVariants == null || materialVariants.Length == 0) return;
        _currentIndex = Mathf.Clamp(index, 0, materialVariants.Length - 1);
        ApplyCurrentMaterial();
    }

    public void ResetToDefault()
    {
        _currentIndex = Mathf.Clamp(defaultMaterialIndex, 0, materialVariants.Length - 1);
        ApplyCurrentMaterial();
    }

    // ── Internal ────────────────────────────────────────────────────────

    private void ApplyCurrentMaterial()
    {
        if (targetRenderer == null || materialVariants == null || materialVariants.Length == 0)
            return;

        Material mat = materialVariants[_currentIndex];
        if (mat != null)
            targetRenderer.material = mat;
    }
}