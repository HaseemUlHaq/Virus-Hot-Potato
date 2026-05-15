using System.Linq;
using UnityEngine;

/// <summary>
/// Cycles through material variants on the virus when triggered by
/// Meta's Interaction SDK gesture system (via ActiveStateUnityEventWrapper).
/// 
/// Setup:
///   1. Add this script to the virus prefab.
///   2. Assign the 10 material variants in the inspector (ordered Virus 1 … Virus 10).
///   3. Set Default Material Index to the index of Virus 2 (i.e. 1 if zero-indexed).
///   4. Assign the MeshRenderer, or enable Apply To Child Renderers for skinned meshes.
///   5. In your Select object's ActiveStateUnityEventWrapper, wire the
///      "When Activated" event to call VirusSwipeCycler.CycleNext.
/// </summary>
public class VirusSwipeCycler : MonoBehaviour
{
    [Header("Materials")]
    [Tooltip("All material variants in order (e.g. Virus 1, Virus 2, … Virus 10).")]
    [SerializeField] private Material[] materialVariants;

    [Tooltip("Index into materialVariants for the default material (Virus 2 = index 1).")]
    [SerializeField] private int defaultMaterialIndex = 1;

    [Header("Renderer")]
    [Tooltip("Single MeshRenderer for simple virus meshes. Ignored when Apply To Child Renderers is on.")]
    [SerializeField] private MeshRenderer targetRenderer;

    [Tooltip("Apply material variants to all Mesh/Skinned renderers under this object (e.g. Virus 3).")]
    [SerializeField] private bool applyToChildRenderers;

    private Renderer[] _targetRenderers = System.Array.Empty<Renderer>();
    private int _currentIndex;

    private void Awake()
    {
        CacheTargetRenderers();
        _currentIndex = materialVariants != null && materialVariants.Length > 0
            ? Mathf.Clamp(defaultMaterialIndex, 0, materialVariants.Length - 1)
            : defaultMaterialIndex;
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

    private void CacheTargetRenderers()
    {
        if (applyToChildRenderers)
        {
            _targetRenderers = GetComponentsInChildren<Renderer>(true)
                .Where(r => r != null && r.gameObject != gameObject)
                .ToArray();
            return;
        }

        if (targetRenderer == null)
            targetRenderer = GetComponent<MeshRenderer>();

        _targetRenderers = targetRenderer != null
            ? new Renderer[] { targetRenderer }
            : System.Array.Empty<Renderer>();
    }

    private void ApplyCurrentMaterial()
    {
        if (_targetRenderers == null || _targetRenderers.Length == 0
            || materialVariants == null || materialVariants.Length == 0)
            return;

        Material mat = materialVariants[_currentIndex];
        if (mat == null)
            return;

        foreach (Renderer renderer in _targetRenderers)
        {
            if (renderer != null)
                renderer.material = mat;
        }
    }
}