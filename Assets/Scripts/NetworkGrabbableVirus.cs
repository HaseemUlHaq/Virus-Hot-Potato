using UnityEngine;
using Fusion;
using Oculus.Interaction;
using System.Linq;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Grabbable))]
public class NetworkGrabbableVirus : NetworkBehaviour
{
    // ─── Components ───────────────────────────────────────────────────────
    private Grabbable _grabbable;
    private Rigidbody _rb;
    private NetworkTransform _networkTransform;

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

        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
        }

        _grabbable.WhenPointerEventRaised += OnPointerEvent;
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

    // ─── Grab Events ──────────────────────────────────────────────────────

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

    // ─── Cleanup ──────────────────────────────────────────────────────────

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_grabbable != null)
            _grabbable.WhenPointerEventRaised -= OnPointerEvent;
    }
}