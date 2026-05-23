using System.Collections.Generic;
using Fusion;
using UnityEngine;

// Root of the placeholder formation. Checks every tick if all slots are correctly filled and fires OnIsCompleteChanged when the puzzle is solved.
[DefaultExecutionOrder(-50)]
public class PlaceholderFormation : NetworkBehaviour
{
    [System.Serializable]
    public struct SlotConnection
    {
        [Tooltip("First slot (0 = Slots[0] in Inspector, 1 = Slots[1], …).")]
        public int slotA;
        [Tooltip("Second slot to link. Line shows when both slots are correctly filled.")]
        public int slotB;
    }

    [Header("Formation")]
    [SerializeField] private Transform formationRoot;

    [Header("Slots (children of FormationRoot, same order as VirusFormationData.slots)")]
    [SerializeField] private PlaceholderSlot[] slots;

    [Header("Formation Data (assign in prefab — applied on all clients in Spawned)")]
    [SerializeField] private VirusFormationData preassignedFormationData;

    [Header("Connection Lines")]
    [Tooltip("Pairs to link (e.g. 0↔1, 1↔3). Uses this list when not empty; otherwise VirusFormationData.connectionIndices; else 0–1–2–3 chain.")]
    [SerializeField] private SlotConnection[] slotConnections;
    [SerializeField] private float connectionLineWidth = 0.012f;
    [SerializeField] private Color connectionLineColor = new Color(0x8A / 255f, 0xF2 / 255f, 0xAA / 255f, 1f);

    [Header("Rotation")]
    [SerializeField] private float rotationSensitivity = 1f;
    [SerializeField] private float autoRotateSpeed = 8f;

    [Header("Rotation Handle Visual")]
    [Tooltip("Mesh renderer on HandleVisual (RotationGrabHandle). Auto-found by name if unset.")]
    public Renderer handleVisualRenderer;
    [Tooltip("Handle tint when all placeholder slots are correctly filled.")]
    public Color completeHandleColor = new Color(0x8A / 255f, 0xF2 / 255f, 0xAA / 255f, 1f);

    private static readonly int HandleColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Material _connectionLineMaterial;

    private MaterialPropertyBlock _handleMpb;
    private Color _defaultHandleColor;
    private bool _defaultHandleColorCached;

    private struct ConnectionEdge
    {
        public int SlotA;
        public int SlotB;
        public LineRenderer Line;
    }

    private ConnectionEdge[] _connectionEdges;
    private Transform _connectionLinesRoot;

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

        EnsureHandleRenderer();
        ApplyHandleVisualColor();
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

        if (IsComplete)
            _rotationY += autoRotateSpeed * Runner.DeltaTime;
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

        RebuildConnectionLines(data);
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
        ApplyHandleVisualColor();
        if (IsComplete)
            Debug.Log("[PlaceholderFormation] All slots correctly filled — formation complete!");
    }

    private void EnsureHandleRenderer()
    {
        if (handleVisualRenderer != null)
            return;

        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].gameObject.name == "HandleVisual")
            {
                handleVisualRenderer = renderers[i];
                return;
            }
        }
    }

    private void CacheDefaultHandleColor()
    {
        if (_defaultHandleColorCached || handleVisualRenderer == null)
            return;

        Material mat = handleVisualRenderer.sharedMaterial;
        _defaultHandleColor = mat != null && mat.HasProperty(HandleColorId)
            ? mat.GetColor(HandleColorId)
            : Color.white;
        _defaultHandleColorCached = true;
    }

    private void ApplyHandleVisualColor()
    {
        if (handleVisualRenderer == null)
            return;

        CacheDefaultHandleColor();
        _handleMpb ??= new MaterialPropertyBlock();
        _handleMpb.SetColor(HandleColorId, IsComplete ? completeHandleColor : _defaultHandleColor);
        handleVisualRenderer.SetPropertyBlock(_handleMpb);
    }

    private void RebuildConnectionLines(VirusFormationData data)
    {
        ClearConnectionLineObjects();

        if (slots == null || slots.Length < 2)
            return;

        List<(int a, int b)> edgePairs = CollectConnectionEdges(data);
        if (edgePairs.Count == 0)
            return;

        Transform root = formationRoot != null ? formationRoot : transform;
        var containerGo = new GameObject("ConnectionLines");
        _connectionLinesRoot = containerGo.transform;
        _connectionLinesRoot.SetParent(root, false);
        _connectionLinesRoot.localPosition = Vector3.zero;
        _connectionLinesRoot.localRotation = Quaternion.identity;
        _connectionLinesRoot.localScale = Vector3.one;

        _connectionEdges = new ConnectionEdge[edgePairs.Count];
        for (int i = 0; i < edgePairs.Count; i++)
        {
            (int a, int b) = edgePairs[i];
            var lineGo = new GameObject($"Edge_{a}_{b}");
            lineGo.transform.SetParent(_connectionLinesRoot, false);

            var line = lineGo.AddComponent<LineRenderer>();
            SetupConnectionLineRenderer(line);

            _connectionEdges[i] = new ConnectionEdge { SlotA = a, SlotB = b, Line = line };
        }

        RefreshConnectionLines();
    }

    private List<(int a, int b)> CollectConnectionEdges(VirusFormationData data)
    {
        var set = new HashSet<long>();
        var list = new List<(int, int)>();
        int slotCount = slots != null ? slots.Length : 0;
        if (slotCount < 2)
            return list;

        if (slotConnections != null && slotConnections.Length > 0)
        {
            foreach (SlotConnection conn in slotConnections)
                TryAddEdge(conn.slotA, conn.slotB, slotCount, set, list);
            return list;
        }

        if (data != null && data.slots != null)
        {
            for (int i = 0; i < data.slots.Length && i < slotCount; i++)
            {
                int[] indices = data.slots[i].connectionIndices;
                if (indices == null || indices.Length == 0)
                    continue;

                foreach (int j in indices)
                    TryAddEdge(i, j, slotCount, set, list);
            }
        }

        if (list.Count == 0)
        {
            for (int i = 0; i < slotCount - 1; i++)
                TryAddEdge(i, i + 1, slotCount, set, list);
        }

        return list;
    }

    private static void TryAddEdge(int indexA, int indexB, int slotCount, HashSet<long> set, List<(int, int)> list)
    {
        if (indexA == indexB || indexA < 0 || indexB < 0 || indexA >= slotCount || indexB >= slotCount)
            return;

        int a = Mathf.Min(indexA, indexB);
        int b = Mathf.Max(indexA, indexB);
        long key = ((long)a << 32) | (uint)b;
        if (set.Add(key))
            list.Add((a, b));
    }

    private void SetupConnectionLineRenderer(LineRenderer line)
    {
        Material mat = GetConnectionLineMaterial();

        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = connectionLineWidth;
        line.endWidth = connectionLineWidth;
        line.numCornerVertices = 4;
        line.numCapVertices = 4;
        line.startColor = connectionLineColor;
        line.endColor = connectionLineColor;
        line.enabled = false;
        line.sharedMaterial = mat;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
    }

    private Material GetConnectionLineMaterial()
    {
        if (_connectionLineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                return null;

            _connectionLineMaterial = new Material(shader);
        }

        ApplyConnectionLineColorToMaterial(_connectionLineMaterial, connectionLineColor);
        return _connectionLineMaterial;
    }

    private static void ApplyConnectionLineColorToMaterial(Material mat, Color color)
    {
        if (mat == null)
            return;

        if (mat.HasProperty(BaseColorId))
            mat.SetColor(BaseColorId, color);
        if (mat.HasProperty(ColorId))
            mat.SetColor(ColorId, color);
    }

    private void RefreshConnectionLines()
    {
        if (_connectionEdges == null || slots == null)
            return;

        for (int i = 0; i < _connectionEdges.Length; i++)
        {
            ConnectionEdge edge = _connectionEdges[i];
            if (edge.Line == null)
                continue;

            if (edge.SlotA < 0 || edge.SlotA >= slots.Length || edge.SlotB < 0 || edge.SlotB >= slots.Length)
            {
                edge.Line.enabled = false;
                continue;
            }

            PlaceholderSlot slotA = slots[edge.SlotA];
            PlaceholderSlot slotB = slots[edge.SlotB];
            if (slotA == null || slotB == null)
            {
                edge.Line.enabled = false;
                continue;
            }

            bool show = slotA.IsFilledCorrectly && slotB.IsFilledCorrectly;
            edge.Line.enabled = show;
            if (!show)
                continue;

            edge.Line.SetPosition(0, slotA.GetHoverPosition());
            edge.Line.SetPosition(1, slotB.GetHoverPosition());
        }
    }

    private void ClearConnectionLineObjects()
    {
        _connectionEdges = null;

        if (_connectionLineMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(_connectionLineMaterial);
            else
                DestroyImmediate(_connectionLineMaterial);
            _connectionLineMaterial = null;
        }

        if (_connectionLinesRoot != null)
        {
            if (Application.isPlaying)
                Destroy(_connectionLinesRoot.gameObject);
            else
                DestroyImmediate(_connectionLinesRoot.gameObject);
            _connectionLinesRoot = null;
        }

        Transform root = formationRoot != null ? formationRoot : transform;
        Transform leftover = root.Find("ConnectionLines");
        if (leftover != null)
        {
            if (Application.isPlaying)
                Destroy(leftover.gameObject);
            else
                DestroyImmediate(leftover.gameObject);
        }
    }

    private void OnDestroy()
    {
        ClearConnectionLineObjects();
    }
}
