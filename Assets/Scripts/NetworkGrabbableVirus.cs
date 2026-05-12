using Fusion;
using System.Collections;
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
    [Header("Fusion / throw")]
    [Tooltip("If enabled, state authority is released after you let go (with delay). Leaving this off keeps authority on your client so free-flight physics is not overwritten by another peer's NetworkTransform, which avoids mid-air snaps/stutter in shared mode.")]
    [SerializeField] private bool releaseStateAuthorityOnUnselect = false;
    [Tooltip("Only used when Release State Authority On Unselect is enabled.")]
    [SerializeField] private float releaseStateAuthorityDelaySeconds = 0.35f;

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

    // ─── Virus Visual Properties (Networked) ──────────────────────────────
    /// <summary>Scale multiplier - synced across all players (like car explode view!)</summary>
    [Networked, OnChangedRender(nameof(OnVirusScaleChanged))]
    public float VirusScale { get; set; } = 1.0f;

    /// <summary>Surface tint color - synced across all players</summary>
    [Networked, OnChangedRender(nameof(OnVirusColorChanged))]
    public Color VirusColor { get; set; } = Color.white;

    /// <summary>Pulsate animation enabled - synced across all players (EXACTLY like car explode!)</summary>
    [Networked, OnChangedRender(nameof(OnVirusPulsateChanged))]
    public NetworkBool IsPulsating { get; set; } = false;

    // ─── Local State ──────────────────────────────────────────────────────
    public bool IsBeingGrabbed { get; private set; } = false;
    private bool _pendingGrab = false;
    private bool _pendingRelease = false;
    private Coroutine _deferredReleaseAuthorityRoutine;

    /// <summary>
    /// While true, <see cref="PetriDish"/> owns NetworkTransform off — do not re-enable it from grab logic.
    /// </summary>
    public bool _petriDishDisablesNetworkTransform;

    // ─── Pulsate Animation State ──────────────────────────────────────────
    private float _pulsateTime = 0f;
    private const float PULSATE_SPEED = 2f;
    private const float PULSATE_AMOUNT = 0.2f;

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

            // Initialize virus visual properties
            VirusScale = 1.0f;
            VirusColor = _persistedVirusSurfaceColor;
            IsPulsating = false;
        }

        _grabbable.WhenPointerEventRaised += OnPointerEvent;
    }

    public void SetPetriDishSnapNetworkTransformDisabled(bool petriDishHoldsTransformOff)
    {
        _petriDishDisablesNetworkTransform = petriDishHoldsTransformOff;
    }

    private void Update()
    {
        if (_networkTransform == null) return;
        if (_petriDishDisablesNetworkTransform) return;

        // Until state authority reaches this client, Fusion reapplies the spawner pose every tick and the
        // Interaction SDK cannot pull the virus — it looks "stuck". Suppress NT only in that window.
        if (IsBeingGrabbed && !Object.HasStateAuthority)
            _networkTransform.enabled = false;
        else
            _networkTransform.enabled = true;
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

                if (_deferredReleaseAuthorityRoutine != null)
                {
                    StopCoroutine(_deferredReleaseAuthorityRoutine);
                    _deferredReleaseAuthorityRoutine = null;
                }

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

                if (releaseStateAuthorityOnUnselect)
                    ScheduleDeferredReleaseStateAuthority();
                break;

            case PointerEventType.Cancel:
                IsBeingGrabbed = false;
                _grabHandByInteractorId.Remove(evt.Identifier);
                if (_lastGrabInteractorId == evt.Identifier)
                    _lastGrabInteractorId = FirstInteractorIdOrMinusOne(_grabHandByInteractorId);

                RefreshVirusSwipeSurfaceColor();

                if (_grabHandByInteractorId.Count == 0)
                    RPC_NotifyRelease();

                if (releaseStateAuthorityOnUnselect)
                    ScheduleDeferredReleaseStateAuthority();
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

        Color targetColor = _persistedVirusSurfaceColor;

        if (_grabHandByInteractorId.Count == 0)
        {
            targetColor = _persistedVirusSurfaceColor;
        }
        else if (_grabHandByInteractorId.Count == 1)
        {
            foreach (Handedness h in _grabHandByInteractorId.Values)
            {
                targetColor = VirusSwipeColorForHandedness(h);
                _persistedVirusSurfaceColor = targetColor;
            }
        }
        else if (_lastGrabInteractorId >= 0 &&
            _grabHandByInteractorId.TryGetValue(_lastGrabInteractorId, out Handedness lastHand))
        {
            targetColor = VirusSwipeColorForHandedness(lastHand);
            _persistedVirusSurfaceColor = targetColor;
        }
        else
        {
            foreach (Handedness h in _grabHandByInteractorId.Values)
            {
                targetColor = VirusSwipeColorForHandedness(h);
                _persistedVirusSurfaceColor = targetColor;
                break;
            }
        }

        // ★★★ KEY CHANGE: Update NETWORKED property instead of just local visual! ★★★
        if (Object != null && Object.HasStateAuthority)
        {
            VirusColor = targetColor;  // This syncs to ALL players!
        }
        else
        {
            // If we don't have authority, just apply locally (will be overridden by network)
            ApplySurfaceColor(targetColor);
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

    private void ScheduleDeferredReleaseStateAuthority()
    {
        if (_deferredReleaseAuthorityRoutine != null)
        {
            StopCoroutine(_deferredReleaseAuthorityRoutine);
            _deferredReleaseAuthorityRoutine = null;
        }

        if (releaseStateAuthorityDelaySeconds <= 0f)
        {
            if (Object != null && Object.IsValid)
                Object.ReleaseStateAuthority();
            return;
        }

        _deferredReleaseAuthorityRoutine = StartCoroutine(DeferredReleaseStateAuthority());
    }

    private IEnumerator DeferredReleaseStateAuthority()
    {
        yield return new WaitForSeconds(releaseStateAuthorityDelaySeconds);
        _deferredReleaseAuthorityRoutine = null;
        if (Object != null && Object.IsValid)
            Object.ReleaseStateAuthority();
    }

    // ─── Networked Visual Property Callbacks (SAME PATTERN AS CAR EXPLODE VIEW!) ────

    /// <summary>
    /// Called on ALL clients when VirusScale changes.
    /// Same pattern as car's OnCatWalkObjectExplodeViewEnabled!
    /// </summary>
    private void OnVirusScaleChanged()
    {
        if (!IsPulsating)
        {
            transform.localScale = Vector3.one * VirusScale;
        }
        UnityEngine.Debug.Log($"Virus scale changed to {VirusScale} (synced across network!)");
    }

    /// <summary>
    /// Called on ALL clients when VirusColor changes.
    /// </summary>
    private void OnVirusColorChanged()
    {
        ApplySurfaceColor(VirusColor);
        _persistedVirusSurfaceColor = VirusColor;
        UnityEngine.Debug.Log($"Virus color changed to {VirusColor} (synced across network!)");
    }

    /// <summary>
    /// Called on ALL clients when IsPulsating changes.
    /// EXACTLY like car's OnCatWalkObjectExplodeViewEnabled!
    /// </summary>
    private void OnVirusPulsateChanged()
    {
        _pulsateTime = 0f;

        if (!IsPulsating)
        {
            // Reset to base scale when stopping
            transform.localScale = Vector3.one * VirusScale;
        }

        UnityEngine.Debug.Log($"Virus pulsate toggled to {IsPulsating} (synced across network!)");
    }

    /// <summary>
    /// Public methods to modify virus properties (with authority check!)
    /// Same pattern as car's ToggleExplodeView!
    /// </summary>
    public void SetVirusScale(float newScale)
    {
        if (Object != null && Object.HasStateAuthority)
        {
            VirusScale = Mathf.Clamp(newScale, 0.5f, 3.0f);
        }
    }

    public void SetVirusColor(Color newColor)
    {
        if (Object != null && Object.HasStateAuthority)
        {
            VirusColor = newColor;
        }
    }

    public void TogglePulsate()
    {
        // EXACTLY like car's ToggleExplodeView()!
        if (Object != null && Object.HasStateAuthority)
        {
            IsPulsating = !IsPulsating;
        }
    }

    // ─── Render Loop (for pulsate animation) ─────────────────────────────

    public override void Render()
    {
        base.Render();

        // Animate pulsation on ALL clients (like car animation!)
        if (IsPulsating)
        {
            _pulsateTime += Time.deltaTime * PULSATE_SPEED;
            float pulse = 1f + Mathf.Sin(_pulsateTime) * PULSATE_AMOUNT;
            transform.localScale = Vector3.one * VirusScale * pulse;
        }
    }

    // ─── Cleanup ──────────────────────────────────────────────────────────

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_deferredReleaseAuthorityRoutine != null)
        {
            StopCoroutine(_deferredReleaseAuthorityRoutine);
            _deferredReleaseAuthorityRoutine = null;
        }

        if (_grabbable != null)
            _grabbable.WhenPointerEventRaised -= OnPointerEvent;

        _grabHandByInteractorId.Clear();
        _lastGrabInteractorId = -1;
        ApplySurfaceColor(_persistedVirusSurfaceColor);
    }
}