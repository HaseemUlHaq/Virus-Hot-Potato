using System.Collections.Generic;

using Fusion;

using Oculus.Interaction;

using TMPro;

using UnityEngine;



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



    [Header("Virus appearance (material)")]

    [SerializeField] private MeshRenderer virusColorMeshRenderer;

    [SerializeField]

    [Tooltip("Shader color property to tint (Virus 2.mat uses _Color_2 for the main body; use _Color_3_Overlay for the accent slot). Swipe colors use the same property via VirusSwipeColorFromGestures on the Gestures object.")]

    private string virusSwipeTintShaderProperty = "_Color_2";



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



    private readonly HashSet<int> _selectingPointerIds = new HashSet<int>();

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

        _grabbable.WhenPointerEventRaised += OnPointerEvent;

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotTo, false);



        _virusMeshRenderer = virusColorMeshRenderer != null ? virusColorMeshRenderer : GetComponent<MeshRenderer>();

        _virusColorMpb = new MaterialPropertyBlock();

        _virusSwipeTintPropertyId = Shader.PropertyToID(string.IsNullOrEmpty(virusSwipeTintShaderProperty)

            ? "_Color_2"

            : virusSwipeTintShaderProperty.Trim());

        CachePersistedVirusColorFromMaterial();

        ApplySurfaceColor(_persistedVirusSurfaceColor);



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

                _selectingPointerIds.Add(evt.Identifier);

                RPC_NotifyGrab(Runner.LocalPlayer);

                break;



            case PointerEventType.Unselect:

            case PointerEventType.Cancel:

                _selectingPointerIds.Remove(evt.Identifier);

                if (_selectingPointerIds.Count == 0)

                    RPC_NotifyRelease();

                break;

        }

    }



    public void ApplyPersistedVirusSurfaceColor(Color color)

    {

        _persistedVirusSurfaceColor = color;

        ApplySurfaceColor(color);

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

        _selectingPointerIds.Clear();

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


