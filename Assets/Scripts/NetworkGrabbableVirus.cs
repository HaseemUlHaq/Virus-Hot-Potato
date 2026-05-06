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



    [Header("Rigidbody")]

    [Tooltip("0 = keep prefab mass. Set > 0 to set mass every round (fixes locked / ignored prefab values).")]

    [SerializeField] private float rigidbodyMass = 0f;



    [Header("Round loop")]

    [SerializeField] private float pauseAfterExplosionSeconds = 2f;



    [Header("Physics after first grab")]

    [Tooltip("While spawn-locked: kinematic + no gravity + FreezeAll. After first grab, when released: dynamic with this gravity. Bounciness comes from a PhysicMaterial on the collider.")]

    [SerializeField] private bool useGravityWhenFree = true;



    [Networked] private TickTimer FuseTimer { get; set; }

    [Networked] private NetworkBool WaitingBetweenRoundRestart { get; set; }

    [Networked] private float NextRoundAtSimulationTime { get; set; }



    [Networked] private NetworkBool FuseStarted { get; set; }

    [Networked] private NetworkBool RoundResolved { get; set; }

    [Networked] private NetworkBool HasElimination { get; set; }

    [Networked] public PlayerRef EliminatedPlayer { get; set; }

    [Networked] private NetworkBool HasHolder { get; set; }

    [Networked] private PlayerRef CurrentHolder { get; set; }

    [Networked] private NetworkBool HasLastTouchedPlayer { get; set; }

    [Networked] private PlayerRef LastTouchedPlayer { get; set; }

    [Networked] private NetworkBool SpawnRestUnlocked { get; set; }



    public bool DebugFuseStarted => FuseStarted;

    public bool DebugHasHolder => HasHolder;

    public bool DebugHasElimination => HasElimination;

    public bool DebugRoundResolved => RoundResolved;

    public PlayerRef DebugCurrentHolder => CurrentHolder;

    public PlayerRef DebugLastTouchedPlayer => LastTouchedPlayer;



    private Grabbable _grabbable;

    private Rigidbody _rb;

    private ChangeDetector _changeDetector;

    private bool _localEliminationAnnounced;

    private bool _localExplosionPlayed;

    private bool _wasRoundResolved;



    public override void Spawned()

    {

        _grabbable = GetComponent<Grabbable>();

        _rb = GetComponent<Rigidbody>();

        _grabbable.WhenPointerEventRaised += OnPointerEvent;

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotTo, false);



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



    public override void FixedUpdateNetwork()

    {

        if (HasStateAuthority && !FuseStarted && !RoundResolved)

        {

            FuseTimer = TickTimer.CreateFromSeconds(Runner, fuseDurationSeconds);

            FuseStarted = true;

            RoundResolved = false;

            WaitingBetweenRoundRestart = false;

            HasHolder = false;

            HasElimination = false;

            HasLastTouchedPlayer = false;

            SpawnRestUnlocked = false;

        }



        if (RoundResolved)

        {

            SyncGrabbableForRoundState();



            if (HasStateAuthority

                && WaitingBetweenRoundRestart

                && (float)Runner.SimulationTime >= NextRoundAtSimulationTime)

            {

                BeginNextRound();

            }



            return;

        }



        if (HasStateAuthority && FuseStarted && FuseTimer.Expired(Runner))

            ResolveRound();



        SyncRestingRigidbodyLocks();

        SyncGrabbableForRoundState();

    }



    public override void Render()

    {

        if (_wasRoundResolved && !RoundResolved)

        {

            _localExplosionPlayed = false;

            _localEliminationAnnounced = false;

            if (eliminationMessageText != null)

                eliminationMessageText.gameObject.SetActive(false);

        }



        _wasRoundResolved = RoundResolved;



        foreach (var change in _changeDetector.DetectChanges(this))

        {

            if (change != nameof(RoundResolved) || !RoundResolved)

                continue;

        }



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



        WaitingBetweenRoundRestart = true;

        NextRoundAtSimulationTime = (float)Runner.SimulationTime + pauseAfterExplosionSeconds;



        Debug.Log($"[HotPotato] Fuse expired. Holder={CurrentHolder}, LastTouched={LastTouchedPlayer}, HasHolder={HasHolder}");



        FreezeRigidbodyExplodedPose();

    }



    private void BeginNextRound()

    {

        RoundResolved = false;

        WaitingBetweenRoundRestart = false;

        HasElimination = false;

        EliminatedPlayer = default;

        HasHolder = false;

        HasLastTouchedPlayer = false;

        SpawnRestUnlocked = false;

        FuseTimer = TickTimer.CreateFromSeconds(Runner, fuseDurationSeconds);

        FuseStarted = true;

        ApplyRigidbodyMassIfConfigured();

        ApplyRestingSpawnLock();

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



    private void ApplyRigidbodyMassIfConfigured()

    {

        if (_rb == null || rigidbodyMass <= 0f)

            return;

        _rb.mass = rigidbodyMass;

    }



    private void SyncGrabbableForRoundState()

    {

        if (_grabbable == null)

            return;

        _grabbable.enabled = !RoundResolved;

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



    private void SyncRestingRigidbodyLocks()

    {

        if (_rb == null)

            return;

        if (!SpawnRestUnlocked)

        {

            ApplyRestingSpawnLock();

            return;

        }



        if (HasHolder)

            return;

        if (!Object.HasStateAuthority)

            return;



        ApplyFreeFlightPhysics();

    }



    private void ApplyFreeFlightPhysics()

    {

        if (_rb == null)

            return;

        _rb.constraints = RigidbodyConstraints.None;

        _rb.isKinematic = false;

        _rb.useGravity = useGravityWhenFree;

        if (_rb.collisionDetectionMode == CollisionDetectionMode.Discrete)

            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

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



    public override void Despawned(NetworkRunner runner, bool hasState)

    {

        if (_grabbable != null)

            _grabbable.WhenPointerEventRaised -= OnPointerEvent;

    }

}


