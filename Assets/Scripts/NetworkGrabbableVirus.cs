using Fusion;
using System.Collections.Generic;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Grabbable))]
public class NetworkGrabbableVirus : NetworkBehaviour
{
    // ─── Components ───────────────────────────────────────────────────────
    private Grabbable _grabbable;
    private Rigidbody _rb;
    private NetworkTransform _networkTransform;
    private ChangeDetector _changeDetector;

    // ─── Settings ─────────────────────────────────────────────────────────
    [Header("Fuse Settings")]
    [SerializeField] private float fuseDurationSeconds = 10f;

    [Header("Physics")]
    [SerializeField] private float rigidbodyMass = 0f;

    [Header("Virus Color — hand swipe")]
    [SerializeField] private Color virusColorAfterLeftSwipe = new Color(0.25f, 0.55f, 1f, 1f);
    [SerializeField] private Color virusColorAfterRightSwipe = new Color(1f, 0.35f, 0.25f, 1f);
    [SerializeField] private MeshRenderer virusColorMeshRenderer;
    [SerializeField] private string virusSwipeTintShaderProperty = "_Color_2";

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI eliminationMessageText;

    // ─── Networked State ──────────────────────────────────────────────────
    [Networked] public PlayerRef CurrentHolder { get; private set; } = PlayerRef.None;
    [Networked] private PlayerRef _lastTouchedPlayer { get; set; } = PlayerRef.None;
    [Networked] private PlayerRef _eliminatedPlayer { get; set; } = PlayerRef.None;
    [Networked] private bool _roundResolved { get; set; } = false;
    [Networked] private TickTimer _fuseTimer { get; set; }
    [Networked] private NetworkBool SpawnRestUnlocked { get; set; }
    [Networked] private NetworkBool RoundResolved { get; set; }
    [Networked] private NetworkBool HasElimination { get; set; }
    [Networked] private NetworkBool FuseStarted { get; set; }

    // ─── Local State ──────────────────────────────────────────────────────
    public bool IsBeingGrabbed { get; private set; } = false;
    private bool _pendingGrab = false;
    private bool _pendingRelease = false;

    // ─── Color system ─────────────────────────────────────────────────────
    private readonly Dictionary<int, Handedness> _grabHandByInteractorId = new();
    private int _lastGrabInteractorId = -1;
    private MeshRenderer _virusMeshRenderer;
    private MaterialPropertyBlock _virusColorMpb;
    private Color _persistedVirusSurfaceColor = Color.white;
    private int _virusSwipeTintPropertyId;

    private static readonly int ShaderPropBaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ShaderPropColor = Shader.PropertyToID("_Color");
    private static readonly int ShaderPropColor2Fallback = Shader.PropertyToID("_Color_2");

    // ─── Debug Properties ─────────────────────────────────────────────────
    public float GetRemainingSeconds()
    {
        if (Runner == null) return fuseDurationSeconds;
        if (!_fuseTimer.IsRunning) return fuseDurationSeconds;
        return _fuseTimer.RemainingTime(Runner) ?? 0f;
    }

    public bool DebugHasHolder => CurrentHolder != PlayerRef.None;
    public PlayerRef DebugCurrentHolder => CurrentHolder;
    public PlayerRef DebugLastTouchedPlayer => _lastTouchedPlayer;
    public bool DebugHasElimination => _eliminatedPlayer != PlayerRef.None;
    public PlayerRef EliminatedPlayer => _eliminatedPlayer;
    public bool DebugFuseStarted => _fuseTimer.IsRunning;
    public bool DebugRoundResolved => _roundResolved;

    // ─── Lifecycle ────────────────────────────────────────────────────────

    public override void Spawned()
    {
        _grabbable = GetComponent<Grabbable>();
        _rb = GetComponent<Rigidbody>();
        _networkTransform = GetComponent<NetworkTransform>();
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotTo, false);

        _virusMeshRenderer = virusColorMeshRenderer != null
            ? virusColorMeshRenderer
            : GetComponent<MeshRenderer>();
        _virusColorMpb = new MaterialPropertyBlock();
        _virusSwipeTintPropertyId = Shader.PropertyToID(
            string.IsNullOrEmpty(virusSwipeTintShaderProperty)
                ? "_Color_2"
                : virusSwipeTintShaderProperty.Trim());

        CachePersistedVirusColorFromMaterial();
        RefreshVirusSwipeSurfaceColor();
        ApplyRigidbodyMassIfConfigured();

        // Set physics once — never touch again
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        if (HasStateAuthority)
        {
            _fuseTimer = TickTimer.None;
            _roundResolved = false;
            RoundResolved = false;
            FuseStarted = false;
            HasElimination = false;
            SpawnRestUnlocked = false;
        }

        _grabbable.WhenPointerEventRaised += OnPointerEvent;
    }

    // ─── Network Tick ─────────────────────────────────────────────────────

    public override void FixedUpdateNetwork()
    {
        if (_pendingGrab && Object.HasStateAuthority)
        {
            _pendingGrab = false;
            CurrentHolder = Runner.LocalPlayer;
            _lastTouchedPlayer = Runner.LocalPlayer;

            if (!_fuseTimer.IsRunning && !_roundResolved)
            {
                _fuseTimer = TickTimer.CreateFromSeconds(Runner, fuseDurationSeconds);
                FuseStarted = true;
                UnityEngine.Debug.Log("Fuse started! " + fuseDurationSeconds + "s");
            }
        }

        if (_pendingRelease && Object.HasStateAuthority)
        {
            _pendingRelease = false;
            CurrentHolder = PlayerRef.None;
        }

        if (!Object.HasStateAuthority) return;
        if (_roundResolved) return;
        if (!_fuseTimer.IsRunning) return;

        if (_fuseTimer.Expired(Runner))
        {
            _eliminatedPlayer = CurrentHolder != PlayerRef.None
                ? CurrentHolder
                : _lastTouchedPlayer;

            _roundResolved = true;
            RoundResolved = true;
            HasElimination = true;
            _fuseTimer = TickTimer.None;

            if (_grabbable != null)
                _grabbable.enabled = false;

            UnityEngine.Debug.Log("BOOM! Eliminated: " + _eliminatedPlayer);
        }
    }

    // ─── Grab Events ──────────────────────────────────────────────────────

    private void OnPointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
                IsBeingGrabbed = true;

                // Release from petri dish if snapped
                PetriDish dish = FindObjectsByType<PetriDish>(FindObjectsSortMode.None)
                    .FirstOrDefault(d => d.SnappedVirus == gameObject);
                if (dish != null)
                    dish.ReleaseVirus();

                // Track which hand grabbed
                if (TryGetHandednessFromPointerEvent(evt, out Handedness selectHand))
                {
                    _grabHandByInteractorId[evt.Identifier] = selectHand;
                    _lastGrabInteractorId = evt.Identifier;
                }

                RefreshVirusSwipeSurfaceColor();
                RPC_NotifyGrab(Runner.LocalPlayer);
                Object.RequestStateAuthority();
                _pendingGrab = true;
                _pendingRelease = false;
                break;

            case PointerEventType.Unselect:
                IsBeingGrabbed = false;
                _pendingRelease = true;
                _pendingGrab = false;

                _grabHandByInteractorId.Remove(evt.Identifier);
                if (_lastGrabInteractorId == evt.Identifier)
                    _lastGrabInteractorId = FirstInteractorIdOrMinusOne(_grabHandByInteractorId);

                RefreshVirusSwipeSurfaceColor();

                if (_grabHandByInteractorId.Count == 0)
                    RPC_NotifyRelease();

                Object.ReleaseStateAuthority();
                break;

            case PointerEventType.Cancel:
                IsBeingGrabbed = false;
                _grabHandByInteractorId.Remove(evt.Identifier);
                if (_lastGrabInteractorId == evt.Identifier)
                    _lastGrabInteractorId = FirstInteractorIdOrMinusOne(_grabHandByInteractorId);

                RefreshVirusSwipeSurfaceColor();

                if (_grabHandByInteractorId.Count == 0)
                    RPC_NotifyRelease();

                Object.ReleaseStateAuthority();
                break;
        }
    }

    // ─── RPCs ─────────────────────────────────────────────────────────────

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_NotifyGrab(PlayerRef player)
    {
        if (_roundResolved) return;
        SpawnRestUnlocked = true;
        CurrentHolder = player;
        _lastTouchedPlayer = player;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_NotifyRelease()
    {
        if (_roundResolved) return;
        CurrentHolder = PlayerRef.None;
    }

    // ─── Public API ───────────────────────────────────────────────────────

    public void ResetForNewRound()
    {
        if (!Object.HasStateAuthority) return;

        CurrentHolder = PlayerRef.None;
        _lastTouchedPlayer = PlayerRef.None;
        _eliminatedPlayer = PlayerRef.None;
        _roundResolved = false;
        RoundResolved = false;
        FuseStarted = false;
        HasElimination = false;
        SpawnRestUnlocked = false;
        _fuseTimer = TickTimer.None;
        _pendingGrab = false;
        _pendingRelease = false;

        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        if (_grabbable != null)
            _grabbable.enabled = true;
    }

    public void SetFuseDuration(float seconds)
    {
        fuseDurationSeconds = seconds;
    }

    // ─── Color Helpers ────────────────────────────────────────────────────

    private void CachePersistedVirusColorFromMaterial()
    {
        if (_virusMeshRenderer == null || _virusMeshRenderer.sharedMaterial == null)
        {
            _persistedVirusSurfaceColor = Color.white;
            return;
        }

        Material sharedMat = _virusMeshRenderer.sharedMaterial;

        if (TryReadTintFromMaterial(sharedMat, _virusSwipeTintPropertyId, out Color configured))
            _persistedVirusSurfaceColor = configured;
        else if (TryReadTintFromMaterial(sharedMat, ShaderPropColor2Fallback, out configured))
            _persistedVirusSurfaceColor = configured;
        else if (sharedMat.HasProperty(ShaderPropBaseColor))
            _persistedVirusSurfaceColor = sharedMat.GetColor(ShaderPropBaseColor);
        else if (sharedMat.HasProperty(ShaderPropColor))
            _persistedVirusSurfaceColor = sharedMat.GetColor(ShaderPropColor);
        else
            _persistedVirusSurfaceColor = Color.white;
    }

    private static bool TryReadTintFromMaterial(Material mat, int propertyId, out Color color)
    {
        color = default;
        if (!mat.HasProperty(propertyId)) return false;
        color = mat.GetColor(propertyId);
        return true;
    }

    private void RefreshVirusSwipeSurfaceColor()
    {
        if (_virusMeshRenderer == null) return;

        if (_grabHandByInteractorId.Count == 0)
        {
            ApplySurfaceColor(_persistedVirusSurfaceColor);
            return;
        }

        if (_grabHandByInteractorId.Count == 1)
        {
            foreach (Handedness h in _grabHandByInteractorId.Values)
            {
                Color swipeColor = VirusSwipeColorForHandedness(h);
                _persistedVirusSurfaceColor = swipeColor;
                ApplySurfaceColor(swipeColor);
                return;
            }
        }

        if (_lastGrabInteractorId >= 0 &&
            _grabHandByInteractorId.TryGetValue(_lastGrabInteractorId, out Handedness lastHand))
        {
            Color swipeColor = VirusSwipeColorForHandedness(lastHand);
            _persistedVirusSurfaceColor = swipeColor;
            ApplySurfaceColor(swipeColor);
            return;
        }

        foreach (Handedness h in _grabHandByInteractorId.Values)
        {
            Color swipeColor = VirusSwipeColorForHandedness(h);
            _persistedVirusSurfaceColor = swipeColor;
            ApplySurfaceColor(swipeColor);
            return;
        }
    }

    private void ApplySurfaceColor(Color color)
    {
        if (_virusMeshRenderer == null) return;

        Material mat = _virusMeshRenderer.sharedMaterial;
        _virusMeshRenderer.GetPropertyBlock(_virusColorMpb);

        if (mat != null && mat.HasProperty(_virusSwipeTintPropertyId))
            _virusColorMpb.SetColor(_virusSwipeTintPropertyId, color);
        else if (mat != null && mat.HasProperty(ShaderPropColor2Fallback))
            _virusColorMpb.SetColor(ShaderPropColor2Fallback, color);
        else if (mat != null && mat.HasProperty(ShaderPropBaseColor))
            _virusColorMpb.SetColor(ShaderPropBaseColor, color);
        else if (mat != null && mat.HasProperty(ShaderPropColor))
            _virusColorMpb.SetColor(ShaderPropColor, color);
        else
            _virusColorMpb.SetColor(_virusSwipeTintPropertyId, color);

        _virusMeshRenderer.SetPropertyBlock(_virusColorMpb);
    }

    private Color VirusSwipeColorForHandedness(Handedness handedness)
    {
        return handedness == Handedness.Left
            ? virusColorAfterLeftSwipe
            : virusColorAfterRightSwipe;
    }

    // ─── Handedness Detection ─────────────────────────────────────────────

    private static bool TryGetHandednessFromPointerEvent(PointerEvent evt, out Handedness handedness)
    {
        handedness = default;
        object data = evt.Data;
        if (data == null) return false;

        if (TryHandednessFromKnownInteractors(data, out handedness)) return true;

        Transform root = null;
        if (data is Component dataComp) root = dataComp.transform;
        else if (data is GameObject dataGo) root = dataGo.transform;
        if (root == null) return false;

        return TryHandednessFromInteractorHierarchy(root, out handedness);
    }

    private static bool TryHandednessFromKnownInteractors(object data, out Handedness handedness)
    {
        handedness = default;
        if (data is HandGrabInteractor hgi && hgi.Hand != null)
        {
            handedness = hgi.Hand.Handedness;
            return true;
        }
        if (data is DistanceHandGrabInteractor dgi && dgi.Hand != null)
        {
            handedness = dgi.Hand.Handedness;
            return true;
        }
        return false;
    }

    private static bool TryHandednessFromInteractorHierarchy(Transform root, out Handedness handedness)
    {
        handedness = default;
        Transform t = root;
        for (int depth = 0; depth < 24 && t != null; depth++)
        {
            foreach (MonoBehaviour mb in t.GetComponents<MonoBehaviour>())
            {
                if (mb is HandGrabInteractor hgi && hgi.Hand != null)
                { handedness = hgi.Hand.Handedness; return true; }
                if (mb is DistanceHandGrabInteractor dgi && dgi.Hand != null)
                { handedness = dgi.Hand.Handedness; return true; }
                if (mb is IHand iHand)
                { handedness = iHand.Handedness; return true; }
            }
            t = t.parent;
        }

        foreach (var hgi in root.GetComponentsInChildren<HandGrabInteractor>(true))
            if (hgi.Hand != null) { handedness = hgi.Hand.Handedness; return true; }

        foreach (var dgi in root.GetComponentsInChildren<DistanceHandGrabInteractor>(true))
            if (dgi.Hand != null) { handedness = dgi.Hand.Handedness; return true; }

        foreach (Component comp in root.GetComponentsInChildren<Component>(true))
            if (comp is IHand iHand) { handedness = iHand.Handedness; return true; }

        return false;
    }

    private static int FirstInteractorIdOrMinusOne(Dictionary<int, Handedness> map)
    {
        foreach (int key in map.Keys) return key;
        return -1;
    }

    private void ApplyRigidbodyMassIfConfigured()
    {
        if (_rb == null || rigidbodyMass <= 0f) return;
        _rb.mass = rigidbodyMass;
    }

    // ─── Cleanup ──────────────────────────────────────────────────────────

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_grabbable != null)
            _grabbable.WhenPointerEventRaised -= OnPointerEvent;

        _grabHandByInteractorId.Clear();
        _lastGrabInteractorId = -1;
        ApplySurfaceColor(_persistedVirusSurfaceColor);
    }
}