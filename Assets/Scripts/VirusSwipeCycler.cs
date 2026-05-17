using System.Linq;
using UnityEngine;

[System.Serializable]
public struct VirusColorTheme
{
    [ColorUsage(true, true)] public Color bodyColor;
    [ColorUsage(true, true)] public Color spikeColor;
    [ColorUsage(true, true)] public Color veinGlowColor;
    [Range(0f, 10f)] public float glowIntensity;
}

/// <summary>
/// Drives the virus's RGB-mask shader by injecting per-theme colors into a
/// cached MaterialPropertyBlock. No material instances are created, so GPU
/// batching stays intact on the MR headset.
///
/// Setup:
///   1. Add this script to the virus prefab root.
///   2. Each shape child needs its own Material (Virus.shadergraph) with its
///      own ColorMask texture. Colors are injected at runtime — do not bake
///      color variants into separate materials.
///   3. Enable "Apply To Child Renderers" (finds all shape renderers including
///      inactive ones managed by VirusShapeCycler).
///   4. Click "Auto-Populate Themes from Virus Materials" in the Inspector to
///      fill the 10 Color Theme entries from Assets/Viruses_FREE/Material/.
///   5. NetworkGrabbableVirus.OnMaterialIndexChanged drives this via SetMaterialIndex.
/// </summary>
public class VirusSwipeCycler : MonoBehaviour
{
    // ── Shader property IDs (cached once; avoids per-call string hashing) ──
    private static readonly int PropBody      = Shader.PropertyToID("_Color_1");
    private static readonly int PropSpike     = Shader.PropertyToID("_Color_2");
    private static readonly int PropVein      = Shader.PropertyToID("_Color_3_Overlay");
    private static readonly int PropGlowScale = Shader.PropertyToID("_Color3_Scale");

    [Header("Color Themes")]
    [Tooltip("10 visual variants. Index 0 = Virus 1, index 9 = Virus 10.")]
    [SerializeField] private VirusColorTheme[] colorThemes;

    [Tooltip("Which theme to show on first enable.")]
    [SerializeField] private int defaultThemeIndex = 1;

    [Header("Renderer")]
    [Tooltip("The single MeshRenderer for simple virus meshes. Ignored when Apply To Child Renderers is on.")]
    [SerializeField] private MeshRenderer targetRenderer;

    [Tooltip("Apply to every Renderer found in children (includeInactive=true). Use when VirusShapeCycler manages multiple shape GameObjects.")]
    [SerializeField] private bool applyToChildRenderers;

    // ── Private state ───────────────────────────────────────────────────────
    private MaterialPropertyBlock _block;
    private Renderer[] _allRenderers = System.Array.Empty<Renderer>();
    private int _currentIndex;

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        _block = new MaterialPropertyBlock();
        CacheAllRenderers();
        _currentIndex = colorThemes != null && colorThemes.Length > 0
            ? Mathf.Clamp(defaultThemeIndex, 0, colorThemes.Length - 1)
            : 0;
    }

    private void OnEnable() => ApplyTheme(_currentIndex);

    // ── Public API — wire these to gesture events or NetworkGrabbableVirus ──

    [ContextMenu("Cycle Next Theme")]
    public void CycleNext()
    {
        if (colorThemes == null || colorThemes.Length == 0) return;
        _currentIndex = (_currentIndex + 1) % colorThemes.Length;
        ApplyTheme(_currentIndex);
    }

    [ContextMenu("Cycle Previous Theme")]
    public void CyclePrevious()
    {
        if (colorThemes == null || colorThemes.Length == 0) return;
        _currentIndex = (_currentIndex - 1 + colorThemes.Length) % colorThemes.Length;
        ApplyTheme(_currentIndex);
    }

    public int CurrentMaterialIndex => _currentIndex;

    public void SetMaterialIndex(int index)
    {
        if (colorThemes == null || colorThemes.Length == 0) return;
        _currentIndex = Mathf.Clamp(index, 0, colorThemes.Length - 1);
        ApplyTheme(_currentIndex);
    }

    public void ResetToDefault()
    {
        SetMaterialIndex(colorThemes != null && colorThemes.Length > 0
            ? Mathf.Clamp(defaultThemeIndex, 0, colorThemes.Length - 1)
            : 0);
    }

    /// <summary>
    /// Call this from NetworkGrabbableVirus.OnShapeVariantChanged after
    /// VirusShapeCycler.SetShapeIndex so the newly-activated renderer is
    /// guaranteed to carry the current theme. See note in CacheAllRenderers.
    /// </summary>
    public void RefreshAfterShapeChange() => ApplyTheme(_currentIndex);

    // ── Internal ─────────────────────────────────────────────────────────────

    private void CacheAllRenderers()
    {
        if (applyToChildRenderers)
        {
            // includeInactive = true: VirusShapeCycler disables all but the
            // active shape variant. We write the MPB to every renderer up-front
            // so the correct theme is already applied the instant a variant is
            // made visible — no extra call required at shape-swap time.
            _allRenderers = GetComponentsInChildren<Renderer>(true)
                .Where(r => r != null)
                .ToArray();
        }
        else
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<MeshRenderer>();

            _allRenderers = targetRenderer != null
                ? new Renderer[] { targetRenderer }
                : System.Array.Empty<Renderer>();
        }
    }

    private void ApplyTheme(int index)
    {
        if (_allRenderers == null || _allRenderers.Length == 0) return;
        if (colorThemes == null || index < 0 || index >= colorThemes.Length) return;

        VirusColorTheme theme = colorThemes[index];
        _block.SetColor(PropBody,      theme.bodyColor);
        _block.SetColor(PropSpike,     theme.spikeColor);
        _block.SetColor(PropVein,      theme.veinGlowColor);
        _block.SetFloat(PropGlowScale, theme.glowIntensity);

        foreach (Renderer r in _allRenderers)
        {
            if (r != null)
                r.SetPropertyBlock(_block);
        }
    }
}
