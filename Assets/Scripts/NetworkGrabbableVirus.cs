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

    [Header("Material Cycling")]
    [SerializeField] private VirusSwipeCycler swipeCycler;

    [Header("Shape Cycling")]
    [SerializeField] private VirusShapeCycler shapeCycler;

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

    [Networked, OnChangedRender(nameof(OnMaterialIndexChanged))]
    public int MaterialIndex { get; set; } = 1;

    [Networked, OnChangedRender(nameof(OnVirusPulsateChanged))]
    public NetworkBool IsPulsating { get; set; } = false;

    [Networked, OnChangedRender(nameof(OnShapeVariantChanged))]
    public int ShapeVariantIndex { get; set; } = 0;

    // ─── Local State ──────────────────────────────────────────────────────
    public bool IsBeingGrabbed { get; private set; } = false;
    private bool _pendingGrab = false;
    private bool _pendingRelease = false;
    private Handedness _pendingHandedness = Handedness.Left;

    // ─── Ghost Hand Snap ──────────────────────────────────────────────────
    [Networked] private NetworkBool _holderUsesLeftHand { get; set; }
    private NetworkedHandSimple _cachedHolderHand;
    private PlayerRef _cachedHolderHandFor = PlayerRef.None;
    private Coroutine _deferredReleaseAuthorityRoutine;
    private Coroutine _stopPulsateRoutine;
    private bool _networkTransformEnabled = true;
    public bool _petriDishDisablesNetworkTransform;

    [Header("Collaborative MR (Petri sync)")]
    [Tooltip(
        "When snapped, reposition this virus each LateUpdate onto each player's local dish hover pose. "+
        "Prevents MR collocation drift from showing the virus seated on dish for host but floating/in-hand for peers.")]
    [SerializeField]
    private bool bindVisualToPetriSnapOnAllPeers = true;

    // ─── Pulse Scale Memory ───────────────────────────────────────────────
    private float _preBeforeScale = 1f;

    // ─── Pulsate Animation State ──────────────────────────────────────────
    private float _pulsateTime = 0f;
    private const float PULSATE_SPEED = 2f;
    private const float PULSATE_AMOUNT = 0.2f;

    // ─── Handedness Detection ─────────────────────────────────────────────
    private readonly Dictionary<int, Handedness> _grabHandByInteractorId = new();
    private int _lastGrabInteractorId = -1;

    // ─── PetriDish Cache ──────────────────────────────────────────────────
    private PetriDish[] _cachedDishes;
    private float _lastDishCacheTime;

    private PowerRoleSession _powerRoleSession;

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

        if (swipeCycler == null)
            swipeCycler = GetComponent<VirusSwipeCycler>();

        ApplyRigidbodyMassIfConfigured();

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
            VirusScale = 1.0f;
            MaterialIndex = 1;
            IsPulsating = false;
            ShapeVariantIndex = 0;
        }

        // Apply initial visual state on all clients — [OnChangedRender] won't fire if the
        // networked value matches its default, so we push it explicitly once at spawn.
        transform.localScale = Vector3.one * VirusScale;
        if (swipeCycler != null)
            swipeCycler.SetMaterialIndex(MaterialIndex);
        if (shapeCycler != null)
            shapeCycler.SetShapeIndex(ShapeVariantIndex);

        _grabbable.WhenPointerEventRaised += OnPointerEvent;
        RefreshPowerRoleSessionReference();
    }

    /// <summary>Refreshes the Petri lookup used by simulation and visuals.</summary>
    private void EnsurePetriDishesCached()
    {
        if (_cachedDishes == null || Time.time - _lastDishCacheTime > 1f)
        {
            _cachedDishes = FindObjectsByType<PetriDish>(FindObjectsSortMode.None);
            _lastDishCacheTime = Time.time;
        }
    }

    private void RefreshPowerRoleSessionReference()
    {
        _powerRoleSession = PowerRoleSession.Instance;
        if (_powerRoleSession == null)
            _powerRoleSession = FindFirstObjectByType<PowerRoleSession>(FindObjectsInactive.Include);
    }

    public void SetPetriDishSnapNetworkTransformDisabled(bool petriDishHoldsTransformOff)
    {
        _petriDishDisablesNetworkTransform = petriDishHoldsTransformOff;
    }

    private void Update()
    {
        if (_networkTransform == null) return;
        if (_petriDishDisablesNetworkTransform) return;

        bool wantEnabled = !(IsBeingGrabbed && !Object.HasStateAuthority);
        if (wantEnabled != _networkTransformEnabled)
        {
            _networkTransformEnabled = wantEnabled;
            _networkTransform.enabled = wantEnabled;
        }
    }

    // ─── Network Tick ─────────────────────────────────────────────────────

    public override void FixedUpdateNetwork()
    {
        EnsurePetriDishesCached();

        // Apply grab/release on authority before physics so CurrentHolder matches this tick.
        if (_pendingGrab && Object.HasStateAuthority)
        {
            _pendingGrab = false;
            CurrentHolder = Runner.LocalPlayer;
            _lastTouchedPlayer = Runner.LocalPlayer;
            _holderUsesLeftHand = (_pendingHandedness == Handedness.Left);

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

        PetriDish myDish = null;
        if (_cachedDishes != null)
        {
            foreach (var dish in _cachedDishes)
            {
                if (dish != null && dish.SnappedVirus == this)
                {
                    myDish = dish;
                    break;
                }
            }
        }

        if (myDish != null && myDish.IsOccupied)
        {
            if (_rb != null && !_rb.isKinematic)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
                _rb.useGravity = false;
            }

            if (Object.HasStateAuthority)
            {
                transform.position = myDish.GetHoverPosition();
                transform.rotation = Quaternion.identity;
            }
        }
        else
        {
            // Not in dish: on proxies only, freeze RB while someone holds so NetworkTransform
            // pose is not fought by local physics (fixes remote jitter). Do not force kinematic
            // on state authority — Interaction SDK must own RB during hover/grab on the holder.
            if (CurrentHolder != PlayerRef.None && !Object.HasStateAuthority)
            {
                if (_rb != null && !_rb.isKinematic)
                {
                    _rb.linearVelocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                    _rb.isKinematic = true;
                    _rb.useGravity = false;
                }
            }
            else if (_rb != null && _rb.isKinematic && CurrentHolder == PlayerRef.None)
            {
                _rb.isKinematic = false;
                _rb.useGravity = true;
            }
        }

        // Handle fuse timer
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

    /// <summary>
    /// Keeps snapped viruses on the dish hover point (world space). Called from LateUpdate so formation
    /// rotation in Render runs first — no parenting, scale, or NetworkTransform changes.
    /// </summary>
    /// <returns>True if this virus is snapped and pose was updated.</returns>
    private bool ApplyPetriSnapFollowPose()
    {
        if (!bindVisualToPetriSnapOnAllPeers || Object == null || !Object.IsValid)
            return false;

        if (CurrentHolder != PlayerRef.None)
            return false;

        EnsurePetriDishesCached();

        if (_cachedDishes == null) return false;

        foreach (PetriDish dish in _cachedDishes)
        {
            if (dish == null || dish.Object == null || !dish.Object.IsValid) continue;
            if (!dish.IsOccupied || dish.SnappedVirus != this) continue;

            transform.SetPositionAndRotation(dish.GetHoverPosition(), Quaternion.identity);
            return true;
        }

        return false;
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
        _pendingHandedness = handedness;

        if (Object != null && !Object.HasStateAuthority)
            Object.RequestStateAuthority();

        _lastGrabInteractorId = interactorId;
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

    // ─── Material Cycling System ──────────────────────────────────────────

    private void RefreshVirusSwipeMaterial()
    {
        if (_lastGrabInteractorId < 0) return;

        if (!_grabHandByInteractorId.TryGetValue(_lastGrabInteractorId, out Handedness h))
            return;

        RefreshPowerRoleSessionReference();
        if (_powerRoleSession != null && !_powerRoleSession.IsColorPlayer(Runner.LocalPlayer))
            return;

        if (Object != null && Object.HasStateAuthority)
        {
            if (h == Handedness.Left)
                MaterialIndex = (MaterialIndex - 1 + 10) % 10;
            else
                MaterialIndex = (MaterialIndex + 1) % 10;
        }
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

    // ─── Networked Visual Property Callbacks ────────────────────────────────

    private void OnVirusScaleChanged()
    {
        if (!IsPulsating)
            transform.localScale = Vector3.one * VirusScale;
    }

    private void OnMaterialIndexChanged()
    {
        if (swipeCycler != null)
            swipeCycler.SetMaterialIndex(MaterialIndex);
    }

    private void OnVirusPulsateChanged()
    {
        _pulsateTime = 0f;
        if (!IsPulsating)
            transform.localScale = Vector3.one * VirusScale;
    }

    private void OnShapeVariantChanged()
    {
        if (shapeCycler != null)
            shapeCycler.SetShapeIndex(ShapeVariantIndex);
        if (swipeCycler != null)
            swipeCycler.RefreshAfterShapeChange();
    }

    // ─── Public API ───────────────────────────────────────────────────────

    public void SetVirusScale(float newScale)
    {
        if (Object != null && Object.HasStateAuthority)
        {
            RefreshPowerRoleSessionReference();
            if (_powerRoleSession != null && !_powerRoleSession.IsScalePlayer(Runner.LocalPlayer))
                return;
            VirusScale = Mathf.Clamp(newScale, 0.5f, 3.0f);
        }
    }

    public void CycleMaterialNext()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            RefreshPowerRoleSessionReference();
            if (_powerRoleSession != null && !_powerRoleSession.IsColorPlayer(Runner.LocalPlayer))
                return;
            MaterialIndex = (MaterialIndex + 1) % 10;
        }
    }

    public void CycleMaterialPrevious()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            RefreshPowerRoleSessionReference();
            if (_powerRoleSession != null && !_powerRoleSession.IsColorPlayer(Runner.LocalPlayer))
                return;
            MaterialIndex = (MaterialIndex - 1 + 10) % 10;
        }
    }

    public void TogglePulsate()
    {
        if (Object != null && Object.HasStateAuthority)
            IsPulsating = !IsPulsating;
    }

    public void RequestTogglePulseFromTangible()
    {
        RPC_RequestTogglePulse();
    }

    /// <summary>Gestures must use this path (RPC to authority); validates color role via <paramref name="info"/>.Source.</summary>
    public void RequestCycleMaterialFromGesture(bool nextMaterial)
    {
        RPC_RequestCycleMaterial(nextMaterial);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestCycleMaterial(bool nextMaterial, RpcInfo info = default)
    {
        RefreshPowerRoleSessionReference();
        if (_powerRoleSession != null && !_powerRoleSession.IsColorPlayer(info.Source))
            return;

        if (nextMaterial)
            MaterialIndex = (MaterialIndex + 1) % 10;
        else
            MaterialIndex = (MaterialIndex - 1 + 10) % 10;
    }

    public void RequestCycleShapeFromGesture(bool next)
    {
        RPC_RequestCycleShape(next);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestCycleShape(bool next, RpcInfo info = default)
    {
        RefreshPowerRoleSessionReference();
        if (_powerRoleSession != null && !_powerRoleSession.IsShapePlayer(info.Source))
            return;

        if (shapeCycler == null) return;
        int count = shapeCycler.ShapeCount;
        if (count <= 1) return;
        ShapeVariantIndex = next
            ? (ShapeVariantIndex + 1) % count
            : (ShapeVariantIndex - 1 + count) % count;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestTogglePulse(RpcInfo info = default)
    {
        RefreshPowerRoleSessionReference();
        if (_powerRoleSession == null || !_powerRoleSession.IsPulsePlayer(info.Source))
            return;
        TogglePulsate();
    }

    // ─── NEW: UDP Button Pulse RPC ────────────────────────────────────────

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TriggerPulse()
    {
        _preBeforeScale = VirusScale;
        IsPulsating = true;
        if (_stopPulsateRoutine != null) StopCoroutine(_stopPulsateRoutine);
        _stopPulsateRoutine = StartCoroutine(StopPulsateNetwork());
    }

    IEnumerator StopPulsateNetwork()
    {
        yield return new WaitForSeconds(1f);
        _stopPulsateRoutine = null;
        IsPulsating = false;
        VirusScale = _preBeforeScale;
    }

    // ─── Ghost Hand Snap ──────────────────────────────────────────────────

    private void LateUpdate()
    {
        if (Runner == null || Object == null || !Object.IsValid) return;

        if (ApplyPetriSnapFollowPose())
            return;

        if (CurrentHolder == PlayerRef.None || Object.HasStateAuthority) return;
        if (_petriDishDisablesNetworkTransform) return;

        if (_cachedHolderHandFor != CurrentHolder)
        {
            _cachedHolderHandFor = CurrentHolder;
            _cachedHolderHand = null;
            foreach (var hand in FindObjectsByType<NetworkedHandSimple>(FindObjectsSortMode.None))
            {
                if (hand.Object == null || !hand.Object.IsValid) continue;
                if (hand.Object.StateAuthority != CurrentHolder) continue;
                if (hand.IsLeftHand != _holderUsesLeftHand) continue;
                _cachedHolderHand = hand;
                break;
            }
        }

        if (_cachedHolderHand != null && _cachedHolderHand.Object != null && _cachedHolderHand.Object.IsValid)
            transform.position = _cachedHolderHand.GetPalmPosition();
    }

    // ─── Render Loop ─────────────────────────────────────────────────────

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

        if (_stopPulsateRoutine != null)
        {
            StopCoroutine(_stopPulsateRoutine);
            _stopPulsateRoutine = null;
        }

        if (_grabbable != null)
            _grabbable.WhenPointerEventRaised -= OnPointerEvent;

        _grabHandByInteractorId.Clear();
        _lastGrabInteractorId = -1;
    }
}