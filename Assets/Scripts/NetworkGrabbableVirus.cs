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
    [Networked, OnChangedRender(nameof(OnVirusScaleChanged))]
    public float VirusScale { get; set; } = 1.0f;

    [Networked, OnChangedRender(nameof(OnVirusColorChanged))]
    public Color VirusColor { get; set; } = Color.white;

    [Networked, OnChangedRender(nameof(OnVirusPulsateChanged))]
    public NetworkBool IsPulsating { get; set; } = false;

    // ─── Local State ──────────────────────────────────────────────────────
    public bool IsBeingGrabbed { get; private set; } = false;
    private bool _pendingGrab = false;
    private bool _pendingRelease = false;
    private Coroutine _deferredReleaseAuthorityRoutine;
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

        // SIMPLE: Just set physics directly on all clients
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

        if (IsBeingGrabbed && !Object.HasStateAuthority)
            _networkTransform.enabled = false;
        else
            _networkTransform.enabled = true;
    }

    // ─── Network Tick ─────────────────────────────────────────────────────

    public override void FixedUpdateNetwork()
    {
        // CHECK: Am I in a PetriDish?
        PetriDish[] dishes = FindObjectsByType<PetriDish>(FindObjectsSortMode.None);
        PetriDish myDish = null;

        foreach (var dish in dishes)
        {
            if (dish.SnappedVirus == this)
            {
                myDish = dish;
                break;
            }
        }

        // HANDLE PHYSICS based on dish state
        if (myDish != null && myDish.IsOccupied)
        {
            // I'm in a dish - disable physics and hover
            if (_rb != null && !_rb.isKinematic)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
                _rb.useGravity = false;
            }

            // Move to hover position
            if (Object.HasStateAuthority)
            {
                transform.position = myDish.GetHoverPosition();
                transform.rotation = Quaternion.identity;
            }
        }
        else
        {
            if (_rb != null && _rb.isKinematic && CurrentHolder == PlayerRef.None)  // ← Use CurrentHolder!
            {
                _rb.isKinematic = false;
                _rb.useGravity = true;
            }
        }

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

        if (Object.HasStateAuthority && _fuseTimer.IsRunning)
        {
            if (_fuseTimer.Expired(Runner))
            {
                _fuseTimer = TickTimer.None;
                _eliminatedPlayer = _lastTouchedPlayer;
                _roundResolved = true;
                RoundResolved = true;
                HasElimination = true;
                UnityEngine.Debug.Log($"Fuse exploded! Player {_eliminatedPlayer} eliminated!");
            }
        }

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(_eliminatedPlayer):
                    OnEliminatedPlayerChanged();
                    break;
            }
        }
    }

    // ─── Grab Events ──────────────────────────────────────────────────────

    private void OnPointerEvent(PointerEvent evt)
    {
        if (!Object || !Object.IsValid) return;

        if (!TryGetHandednessFromPointerEvent(evt, out Handedness handedness))
            handedness = Handedness.Left;

        int interactorId = evt.Identifier;

        switch (evt.Type)
        {
            case PointerEventType.Select:
                _grabHandByInteractorId[interactorId] = handedness;
                OnGrabStarted(interactorId, handedness);
                break;

            case PointerEventType.Unselect:
                if (_grabHandByInteractorId.ContainsKey(interactorId))
                    _grabHandByInteractorId.Remove(interactorId);
                OnGrabEnded();
                break;

            case PointerEventType.Hover:
            case PointerEventType.Unhover:
            case PointerEventType.Move:
            case PointerEventType.Cancel:
            default:
                break;
        }
    }

    private void OnGrabStarted(int interactorId, Handedness handedness)
    {
        IsBeingGrabbed = true;
        _pendingGrab = true;

        if (Object != null && !Object.HasStateAuthority)
            Object.RequestStateAuthority();

        _lastGrabInteractorId = interactorId;

        if (Object.HasStateAuthority)
            RefreshVirusSwipeSurfaceColor();
    }

    private void OnGrabEnded()
    {
        IsBeingGrabbed = false;
        _pendingRelease = true;

        if (releaseStateAuthorityOnUnselect && Object != null && Object.HasStateAuthority)
            ScheduleDeferredReleaseStateAuthority();

        _lastGrabInteractorId = -1;
    }

    // ─── Elimination UI ───────────────────────────────────────────────────

    private void OnEliminatedPlayerChanged()
    {
        if (_eliminatedPlayer == PlayerRef.None) return;
        if (eliminationMessageText == null) return;

        eliminationMessageText.text = $"Player {_eliminatedPlayer.PlayerId} eliminated!";
        eliminationMessageText.gameObject.SetActive(true);
    }

    // ─── Color System ─────────────────────────────────────────────────────

    private void CachePersistedVirusColorFromMaterial()
    {
        if (_virusMeshRenderer == null) return;
        Material mat = _virusMeshRenderer.sharedMaterial;
        if (mat == null) return;

        if (mat.HasProperty(_virusSwipeTintPropertyId))
        {
            _persistedVirusSurfaceColor = mat.GetColor(_virusSwipeTintPropertyId);
            return;
        }

        if (mat.HasProperty(ShaderPropBaseColor))
        {
            _persistedVirusSurfaceColor = mat.GetColor(ShaderPropBaseColor);
            return;
        }

        if (mat.HasProperty(ShaderPropColor))
            _persistedVirusSurfaceColor = mat.GetColor(ShaderPropColor);
    }

    private void RefreshVirusSwipeSurfaceColor()
    {
        if (_lastGrabInteractorId < 0)
        {
            ApplySurfaceColor(_persistedVirusSurfaceColor);
            return;
        }

        if (!_grabHandByInteractorId.TryGetValue(_lastGrabInteractorId, out Handedness h))
        {
            ApplySurfaceColor(_persistedVirusSurfaceColor);
            return;
        }

        Color targetColor = GetSwipeColorForHandedness(h);

        if (Object != null && Object.HasStateAuthority)
            VirusColor = targetColor;
    }

    private void ApplySurfaceColor(Color c)
    {
        if (_virusMeshRenderer == null || _virusColorMpb == null) return;

        if (_virusMeshRenderer.sharedMaterial.HasProperty(_virusSwipeTintPropertyId))
        {
            _virusColorMpb.SetColor(_virusSwipeTintPropertyId, c);
        }
        else if (_virusMeshRenderer.sharedMaterial.HasProperty(ShaderPropBaseColor))
        {
            _virusColorMpb.SetColor(ShaderPropBaseColor, c);
        }
        else if (_virusMeshRenderer.sharedMaterial.HasProperty(ShaderPropColor))
        {
            _virusColorMpb.SetColor(ShaderPropColor, c);
        }

        _virusMeshRenderer.SetPropertyBlock(_virusColorMpb);
    }

    private Color GetSwipeColorForHandedness(Handedness h) =>
        (h == Handedness.Left) ? virusColorAfterLeftSwipe : virusColorAfterRightSwipe;

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

    // ─── Networked Visual Property Callbacks ────

    private void OnVirusScaleChanged()
    {
        if (!IsPulsating)
        {
            transform.localScale = Vector3.one * VirusScale;
        }
        UnityEngine.Debug.Log($"Virus scale changed to {VirusScale}");
    }

    private void OnVirusColorChanged()
    {
        ApplySurfaceColor(VirusColor);
        _persistedVirusSurfaceColor = VirusColor;
        UnityEngine.Debug.Log($"Virus color changed to {VirusColor}");
    }

    private void OnVirusPulsateChanged()
    {
        _pulsateTime = 0f;

        if (!IsPulsating)
        {
            transform.localScale = Vector3.one * VirusScale;
        }

        UnityEngine.Debug.Log($"Virus pulsate toggled to {IsPulsating}");
    }

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
        if (Object != null && Object.HasStateAuthority)
        {
            IsPulsating = !IsPulsating;
        }
    }

    // ─── Render Loop (for pulsate animation) ─────────────────────────────

    public override void Render()
    {
        base.Render();

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