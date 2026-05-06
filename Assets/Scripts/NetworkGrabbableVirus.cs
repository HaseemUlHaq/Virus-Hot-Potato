using UnityEngine;
using Fusion;
using Oculus.Interaction;
using TMPro;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Grabbable))]
public class NetworkGrabbableVirus : NetworkBehaviour
{
    [SerializeField] private float fuseDurationSeconds = 10f;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private ParticleSystem explosionFxFallback;
    [SerializeField] private TextMeshProUGUI eliminationMessageText;

    [Networked] private TickTimer FuseTimer { get; set; }
    [Networked] private NetworkBool FuseStarted { get; set; }
    [Networked] public NetworkBool RoundResolved { get; private set; }
    [Networked] private NetworkBool HasElimination { get; set; }
    [Networked] public PlayerRef EliminatedPlayer { get; private set; }
    [Networked] private NetworkBool HasHolder { get; set; }
    [Networked] private PlayerRef CurrentHolder { get; set; }
    [Networked] private NetworkBool HasLastTouchedPlayer { get; set; }
    [Networked] private PlayerRef LastTouchedPlayer { get; set; }

    public bool DebugFuseStarted => FuseStarted;
    public bool DebugHasHolder => HasHolder;
    public bool DebugHasElimination => HasElimination;
    public PlayerRef DebugCurrentHolder => CurrentHolder;
    public PlayerRef DebugLastTouchedPlayer => LastTouchedPlayer;

    private Grabbable _grabbable;
    private Rigidbody _rb;
    private ChangeDetector _changeDetector;
    private bool _localEliminationAnnounced;
    private bool _grabDisabledLocally;
    private bool _localExplosionPlayed;

    public override void Spawned()
    {
        _grabbable = GetComponent<Grabbable>();
        _rb = GetComponent<Rigidbody>();
        _grabbable.WhenPointerEventRaised += OnPointerEvent;
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotTo, false);

        if (HasStateAuthority)
        {
            FuseTimer = TickTimer.CreateFromSeconds(Runner, fuseDurationSeconds);
            FuseStarted = true;
            RoundResolved = false;
            HasHolder = false;
            HasElimination = false;
            HasLastTouchedPlayer = false;
        }

        if (RoundResolved)
            ApplyRoundEndedLocal();
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority && !FuseStarted)
        {
            FuseTimer = TickTimer.CreateFromSeconds(Runner, fuseDurationSeconds);
            FuseStarted = true;
            RoundResolved = false;
            HasHolder = false;
            HasElimination = false;
            HasLastTouchedPlayer = false;
        }

        if (RoundResolved)
        {
            ApplyRoundEndedLocal();
            return;
        }

        if (HasStateAuthority && FuseStarted && FuseTimer.Expired(Runner))
            ResolveRound();
    }

    // Phase 1: last touched player loses if virus is in flight at fuse end.
    private void ResolveRound()
    {
        RoundResolved = true;
        if (HasHolder)
        {
            HasElimination = true;
            EliminatedPlayer = CurrentHolder;
        }
        else if (HasLastTouchedPlayer)
        {
            HasElimination = true;
            EliminatedPlayer = LastTouchedPlayer;
        }
        else
        {
            HasElimination = false;
        }

        Debug.Log($"[HotPotato] Fuse expired. Holder={CurrentHolder}, LastTouched={LastTouchedPlayer}, HasHolder={HasHolder}");

        if (_rb != null)
        {
            if (!_rb.isKinematic)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            _rb.isKinematic = true;
        }
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change != nameof(RoundResolved) || !RoundResolved)
                continue;
        }

        // Do not rely on a single change-detection frame for one-shot visuals.
        if (RoundResolved && !_localExplosionPlayed)
        {
            _localExplosionPlayed = true;
            if (explosionPrefab != null)
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            else if (explosionFxFallback != null)
                explosionFxFallback.Play();
        }

        if (!RoundResolved || !HasElimination || _localEliminationAnnounced)
            return;

        if (EliminatedPlayer != Runner.LocalPlayer)
            return;

        _localEliminationAnnounced = true;
        Debug.Log("[HotPotato] You are eliminated.");
        if (eliminationMessageText != null)
        {
            eliminationMessageText.gameObject.SetActive(true);
            eliminationMessageText.text = "You are out!";
        }
    }

    public float GetRemainingSeconds()
    {
        if (!FuseStarted || RoundResolved)
            return 0f;

        float remaining = FuseTimer.RemainingTime(Runner).GetValueOrDefault(0f);
        return Mathf.Max(remaining, 0f);
    }

    private void ApplyRoundEndedLocal()
    {
        if (_grabDisabledLocally)
            return;
        _grabDisabledLocally = true;
        if (_grabbable != null)
            _grabbable.enabled = false;
    }

    private void OnPointerEvent(PointerEvent evt)
    {
        if (RoundResolved)
            return;

        switch (evt.Type)
        {
            case PointerEventType.Select:
                RPC_NotifyGrab(Runner.LocalPlayer);
                break;

            case PointerEventType.Unselect:
                RPC_NotifyRelease();
                break;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_NotifyGrab(PlayerRef player)
    {
        if (RoundResolved)
            return;
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

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_grabbable != null)
            _grabbable.WhenPointerEventRaised -= OnPointerEvent;
    }
}
