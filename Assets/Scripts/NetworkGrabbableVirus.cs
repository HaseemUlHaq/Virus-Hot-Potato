using System.Collections.Generic;

using Fusion;
using Oculus.Interaction;
using System.Linq;

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

    // ─── Networked State ──────────────────────────────────────────────────
    [Networked] public PlayerRef CurrentHolder { get; private set; } = PlayerRef.None;
    [Networked] private PlayerRef _lastTouchedPlayer { get; set; } = PlayerRef.None;
    [Networked] private PlayerRef _eliminatedPlayer { get; set; } = PlayerRef.None;
    [Networked] private bool _roundResolved { get; set; } = false;
    [Networked] private TickTimer _fuseTimer { get; set; }

    // ─── Local State ──────────────────────────────────────────────────────
    public bool IsBeingGrabbed { get; private set; } = false;
    private bool _pendingGrab = false;
    private bool _pendingRelease = false;

    // ─── Debug Properties ─────────────────────────────────────────────────
    public float GetRemainingSeconds()
    {
        if (!_fuseTimer.IsRunning) return fuseDurationSeconds;
        return _fuseTimer.RemainingTime(Runner) ?? 0f;
    }
    [SerializeField] private TextMeshProUGUI eliminationMessageText;



    [Header("Rigidbody")]

    [Tooltip("0 = keep prefab mass. Set > 0 to set mass every round (fixes locked / ignored prefab values).")]

    [SerializeField] private float rigidbodyMass = 0f;



    [Header("Round loop")]

    [SerializeField] private float pauseAfterExplosionSeconds = 2f;



    [Header("Physics after first grab")]

    [Tooltip("While spawn-locked: kinematic + no gravity + FreezeAll. After first grab, when released: dynamic with this gravity. Bounciness comes from a PhysicMaterial on the collider.")]

    [SerializeField] private bool useGravityWhenFree = true;



    [Header("Virus color after swipe (local, persists)")]

    [Tooltip("Virus surface color after a left-hand grab/swipe. When you let go, the virus stays this color until the other hand swipes.")]

    [SerializeField] private Color virusColorAfterLeftSwipe = new Color(0.25f, 0.55f, 1f, 1f);

    [Tooltip("Virus surface color after a right-hand grab/swipe. When you let go, the virus stays this color until the other hand swipes.")]

    [SerializeField] private Color virusColorAfterRightSwipe = new Color(1f, 0.35f, 0.25f, 1f);

    [SerializeField] private MeshRenderer virusColorMeshRenderer;

    [SerializeField]

    [Tooltip("Shader color property to tint (Virus 2.mat uses _Color_2 for the main body; use _Color_3_Overlay for the accent slot).")]

    private string virusSwipeTintShaderProperty = "_Color_2";



    [Networked] private TickTimer FuseTimer { get; set; }

    [Networked] private NetworkBool WaitingBetweenRoundRestart { get; set; }

    [Networked] private float NextRoundAtSimulationTime { get; set; }



    [Networked] private NetworkBool FuseStarted { get; set; }

    [Networked] private NetworkBool RoundResolved { get; set; }

    [Networked] private NetworkBool HasElimination { get; set; }

    [Networked] private NetworkBool HasHolder { get; set; }

    [Networked] private NetworkBool HasLastTouchedPlayer { get; set; }

    [Networked] private PlayerRef LastTouchedPlayer { get; set; }

    [Networked] private NetworkBool SpawnRestUnlocked { get; set; }

    public bool DebugHasHolder => CurrentHolder != PlayerRef.None;
    public PlayerRef DebugCurrentHolder => CurrentHolder;
    public PlayerRef DebugLastTouchedPlayer => _lastTouchedPlayer;
    public bool DebugHasElimination => _eliminatedPlayer != PlayerRef.None;
    public PlayerRef EliminatedPlayer => _eliminatedPlayer;
    public bool DebugFuseStarted => _fuseTimer.IsRunning;
    public bool DebugRoundResolved => _roundResolved;

    // ─── Lifecycle ────────────────────────────────────────────────────────

    private readonly Dictionary<int, Handedness> _grabHandByInteractorId = new Dictionary<int, Handedness>();

    private int _lastGrabInteractorId = -1;

    private MeshRenderer _virusMeshRenderer;

    private MaterialPropertyBlock _virusColorMpb;

    private Color _persistedVirusSurfaceColor = Color.white;

    private int _virusSwipeTintPropertyId;

    private static readonly int ShaderPropBaseColor = Shader.PropertyToID("_BaseColor");

    private static readonly int ShaderPropColor = Shader.PropertyToID("_Color");

    private static readonly int ShaderPropColor2Fallback = Shader.PropertyToID("_Color_2");



    public override void Spawned()
    {
        _grabbable = GetComponent<Grabbable>();
        _rb = GetComponent<Rigidbody>();
        _networkTransform = GetComponent<NetworkTransform>();

        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
        }

        _grabbable.WhenPointerEventRaised += OnPointerEvent;

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotTo, false);



        _virusMeshRenderer = virusColorMeshRenderer != null ? virusColorMeshRenderer : GetComponent<MeshRenderer>();

        _virusColorMpb = new MaterialPropertyBlock();

        _virusSwipeTintPropertyId = Shader.PropertyToID(string.IsNullOrEmpty(virusSwipeTintShaderProperty)

            ? "_Color_2"

            : virusSwipeTintShaderProperty.Trim());

        CachePersistedVirusColorFromMaterial();

        RefreshVirusSwipeSurfaceColor();



        ApplyRigidbodyMassIfConfigured();

        ApplyRestingSpawnLock();



        if (HasStateAuthority)

        {

            WaitingBetweenRoundRestart = false;

            FuseTimer = TickTimer.CreateFromSeconds(Runner, fuseDurationSeconds);

            FuseStarted = true;

            RoundResolved = false;

            HasHolder = false;

            HasElimination = false;

            HasLastTouchedPlayer = false;

            SpawnRestUnlocked = false;

        }



        SyncGrabbableForRoundState();

        if (RoundResolved)

            FreezeRigidbodyExplodedPose();

    }

    // ─── Network Tick — runs when we have authority ────────────────────────

    public override void FixedUpdateNetwork()
    {
        // Handle pending grab — now we definitely have authority
        if (_pendingGrab && Object.HasStateAuthority)
        {
            _pendingGrab = false;
            CurrentHolder = Runner.LocalPlayer;
            _lastTouchedPlayer = Runner.LocalPlayer;

            if (!_fuseTimer.IsRunning && !_roundResolved)
            {
                _fuseTimer = TickTimer.CreateFromSeconds(Runner, fuseDurationSeconds);
                UnityEngine.Debug.Log("Fuse started!");
            }
        }

        // Handle pending release
        if (_pendingRelease && Object.HasStateAuthority)
        {
            _pendingRelease = false;
            CurrentHolder = PlayerRef.None;
        }

        // Check fuse expiry
        if (!Object.HasStateAuthority) return;
        if (_roundResolved) return;
        if (!_fuseTimer.IsRunning) return;

        if (_fuseTimer.Expired(Runner))
        {
            _eliminatedPlayer = CurrentHolder != PlayerRef.None
                ? CurrentHolder
                : _lastTouchedPlayer;

            _roundResolved = true;
            _fuseTimer = TickTimer.None;

            UnityEngine.Debug.Log("BOOM! Eliminated: " + _eliminatedPlayer);
        }
    }

    private void ApplyRestingSpawnLock()
    {
        if (_rb == null)

            return;

        if (!_rb.isKinematic)

        {

            _rb.linearVelocity = Vector3.zero;

            _rb.angularVelocity = Vector3.zero;

        }

        _rb.isKinematic = true;

        _rb.useGravity = false;

        _rb.constraints = RigidbodyConstraints.FreezeAll;

    }

    private void ApplyRigidbodyMassIfConfigured()

    {

        if (_rb == null || rigidbodyMass <= 0f)

            return;

        _rb.mass = rigidbodyMass;

    }

    private void FreezeRigidbodyExplodedPose()
    {

        if (_rb == null)

            return;

        if (!_rb.isKinematic)

        {

            _rb.linearVelocity = Vector3.zero;

            _rb.angularVelocity = Vector3.zero;

        }

        _rb.isKinematic = true;

        _rb.useGravity = false;

        _rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    // ─── Grab Events ──────────────────────────────────────────────────────

    private void SyncGrabbableForRoundState()
    {
        if (_grabbable == null)

            return;

        _grabbable.enabled = !RoundResolved;

    }

    private void OnPointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
                IsBeingGrabbed = true;

                // Release from petri dish
                PetriDish dish = FindObjectsByType<PetriDish>(FindObjectsSortMode.None)
                    .FirstOrDefault(d => d.SnappedVirus == gameObject);
                if (dish != null)
                    dish.ReleaseVirus();
                if (TryGetHandednessFromPointerEvent(evt, out Handedness selectHand))

                {

                    _grabHandByInteractorId[evt.Identifier] = selectHand;

                    _lastGrabInteractorId = evt.Identifier;

                }

                RefreshVirusSwipeSurfaceColor();

                RPC_NotifyGrab(Runner.LocalPlayer);

                // Request authority — state will be set in FixedUpdateNetwork
                // once authority is confirmed
                Object.RequestStateAuthority();
                _pendingGrab = true;
                _pendingRelease = false;

                if (_networkTransform != null)
                    _networkTransform.enabled = false;

                if (_rb != null)
                {
                    _rb.isKinematic = false;
                    _rb.useGravity = false;
                }
                break;

            case PointerEventType.Unselect:
                IsBeingGrabbed = false;
                _pendingRelease = true;
                _pendingGrab = false;

                if (_rb != null)
                {
                    _rb.isKinematic = false;
                    _rb.useGravity = true;
                }

                if (_networkTransform != null)
                    _networkTransform.enabled = true;

                Object.ReleaseStateAuthority();
                break;

            case PointerEventType.Cancel:

                _grabHandByInteractorId.Remove(evt.Identifier);

                if (_lastGrabInteractorId == evt.Identifier)

                    _lastGrabInteractorId = FirstInteractorIdOrMinusOne(_grabHandByInteractorId);

                RefreshVirusSwipeSurfaceColor();

                if (_grabHandByInteractorId.Count == 0)

                    RPC_NotifyRelease();

                if (_networkTransform != null)
                    _networkTransform.enabled = true;

                Object.ReleaseStateAuthority();
                break;
        }
    }

    // ─── Public API ───────────────────────────────────────────────────────

    public void ResetForNewRound()
    {
        if (!Object.HasStateAuthority) return;
        CurrentHolder = PlayerRef.None;
        _lastTouchedPlayer = PlayerRef.None;
        _eliminatedPlayer = PlayerRef.None;
        _roundResolved = false;
        _fuseTimer = TickTimer.None;
        _pendingGrab = false;
        _pendingRelease = false;

        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.linearVelocity = Vector3.zero;
        }
    }

    public void SetFuseDuration(float seconds)
    {
        fuseDurationSeconds = seconds;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]

    private void RPC_NotifyGrab(PlayerRef player)
    {
        if (RoundResolved)

            return;

        SpawnRestUnlocked = true;

        HasHolder = true;

        CurrentHolder = player;

        HasLastTouchedPlayer = true;

        LastTouchedPlayer = player;
    }



    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]

    private void RPC_NotifyRelease()
    {
        if (RoundResolved)

            return;

        HasHolder = false;
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



    private static bool TryReadTintFromMaterial(Material sharedMat, int propertyId, out Color color)

    {

        color = default;

        if (!sharedMat.HasProperty(propertyId))

            return false;

        color = sharedMat.GetColor(propertyId);

        return true;

    }



    private static bool TryGetHandednessFromPointerEvent(PointerEvent evt, out Handedness handedness)

    {

        handedness = default;

        object data = evt.Data;

        if (data == null)

            return false;

        if (TryHandednessFromKnownInteractors(data, out handedness))

            return true;

        Transform root = null;

        if (data is Component dataComp)

            root = dataComp.transform;

        else if (data is GameObject dataGo)

            root = dataGo.transform;

        if (root == null)

            return false;

        return TryHandednessFromInteractorHierarchy(root, out handedness);

    }



    private static bool TryHandednessFromKnownInteractors(object data, out Handedness handedness)

    {

        handedness = default;

        if (data is HandGrabInteractor handGrab && handGrab.Hand != null)

        {

            handedness = handGrab.Hand.Handedness;

            return true;

        }

        if (data is DistanceHandGrabInteractor distanceGrab && distanceGrab.Hand != null)

        {

            handedness = distanceGrab.Hand.Handedness;

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

                {

                    handedness = hgi.Hand.Handedness;

                    return true;

                }

                if (mb is DistanceHandGrabInteractor dgi && dgi.Hand != null)

                {

                    handedness = dgi.Hand.Handedness;

                    return true;

                }

                if (mb is IHand iHand)

                {

                    handedness = iHand.Handedness;

                    return true;

                }

            }

            t = t.parent;

        }



        foreach (HandGrabInteractor hgi in root.GetComponentsInChildren<HandGrabInteractor>(true))

        {

            if (hgi.Hand != null)

            {

                handedness = hgi.Hand.Handedness;

                return true;

            }

        }



        foreach (DistanceHandGrabInteractor dgi in root.GetComponentsInChildren<DistanceHandGrabInteractor>(true))

        {

            if (dgi.Hand != null)

            {

                handedness = dgi.Hand.Handedness;

                return true;

            }

        }



        foreach (Component comp in root.GetComponentsInChildren<Component>(true))

        {

            if (comp is IHand iHand)

            {

                handedness = iHand.Handedness;

                return true;

            }

        }



        return false;

    }



    private static int FirstInteractorIdOrMinusOne(Dictionary<int, Handedness> map)

    {

        foreach (int key in map.Keys)

            return key;

        return -1;

    }



    private Color VirusSwipeColorForHandedness(Handedness handedness)

    {

        return handedness == Handedness.Left ? virusColorAfterLeftSwipe : virusColorAfterRightSwipe;

    }



    private void RefreshVirusSwipeSurfaceColor()

    {

        if (_virusMeshRenderer == null)

            return;

        if (_grabHandByInteractorId.Count == 0)

        {

            ApplySurfaceColor(_persistedVirusSurfaceColor);

            return;

        }

        Handedness handForColor;

        if (_grabHandByInteractorId.Count == 1)

        {

            foreach (Handedness h in _grabHandByInteractorId.Values)

            {

                handForColor = h;

                Color swipeColor = VirusSwipeColorForHandedness(handForColor);

                _persistedVirusSurfaceColor = swipeColor;

                ApplySurfaceColor(swipeColor);

                return;

            }

        }

        if (_lastGrabInteractorId >= 0 && _grabHandByInteractorId.TryGetValue(_lastGrabInteractorId, out Handedness lastHand))

        {

            handForColor = lastHand;

            Color swipeColor = VirusSwipeColorForHandedness(handForColor);

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

        if (_virusMeshRenderer == null)

            return;

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
}