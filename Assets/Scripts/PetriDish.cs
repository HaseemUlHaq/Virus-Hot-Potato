using UnityEngine;
using Fusion;

public class PetriDish : MonoBehaviour
{
    private enum DishVisual
    {
        Empty,
        Nearby,
        Occupied
    }

    [Header("Settings")]
    [SerializeField] private int playerIndex = 1;
    [SerializeField] private float virusHoverHeight = 0.08f;
    [SerializeField] private float snapSpeedThreshold = 0.45f;
    [SerializeField] private float snapDelay = 0.25f;
    [Tooltip("World-space radius to detect a virus near the dish (yellow state). Uses FixedUpdate overlap — stable vs trigger flicker.")]
    [SerializeField] private float nearbyDetectionRadius = 0.55f;
    [Tooltip("World-space radius used when testing whether we can snap.")]
    [SerializeField] private float snapOverlapRadius = 0.4f;
    [Tooltip("Seconds with no virus in nearby radius before returning to green (reduces yellow flicker at the edge).")]
    [SerializeField] private float nearbyExitDebounce = 0.12f;

    [Header("Visuals")]
    [SerializeField] private Renderer dishRenderer;
    [SerializeField] private Color emptyColor = new Color(0.25f, 0.75f, 0.25f, 1f);
    [SerializeField] private Color occupiedColor = new Color(0.85f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color nearbyColor = new Color(1f, 0.82f, 0.15f, 1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private GameObject _snappedVirus;
    private bool _isOccupied;
    private Rigidbody _virusRb;
    private NetworkTransform _virusNetTransform;
    private Material _matInstance;

    private DishVisual _visual = DishVisual.Empty;
    private float _noNearbyTimer;
    private float _snapDwellStart = -1f;
    private NetworkGrabbableVirus _pendingAuthorityForSnap;
    private NetworkGrabbableVirus _cachedVirus;

    public bool IsOccupied => _isOccupied;
    public GameObject SnappedVirus => _snappedVirus;

    private void Start()
    {
        if (dishRenderer != null)
        {
            _matInstance = new Material(dishRenderer.material);
            dishRenderer.material = _matInstance;
        }

        ApplyVisual(DishVisual.Empty, force: true);
        RefreshVirusCache();
    }

    private void RefreshVirusCache()
    {
        if (_cachedVirus != null && _cachedVirus.gameObject != null)
            return;
        _cachedVirus = FindFirstObjectByType<NetworkGrabbableVirus>();
    }

    private void FixedUpdate()
    {
        if (_isOccupied)
        {
            if (_snappedVirus != null)
            {
                _snappedVirus.transform.position = SnapHoverWorldPosition();
                _snappedVirus.transform.rotation = Quaternion.identity;
            }

            return;
        }

        RefreshVirusCache();

        Vector3 center = SnapCenterWorld();
        float detectR = Mathf.Max(nearbyDetectionRadius, snapOverlapRadius);
        NetworkGrabbableVirus virus = null;
        if (!TryFindVirusInRadius(center, detectR, out virus) &&
            _cachedVirus != null &&
            Vector3.Distance(_cachedVirus.transform.position, center) <= detectR)
        {
            virus = _cachedVirus;
        }

        bool inArea = virus != null;

        if (inArea)
        {
            _noNearbyTimer = 0f;
            ApplyVisual(DishVisual.Nearby);

            float dist = Vector3.Distance(virus.transform.position, center);
            if (dist <= snapOverlapRadius)
            {
                if (_snapDwellStart < 0f)
                    _snapDwellStart = Time.time;

                if (Time.time - _snapDwellStart >= snapDelay)
                    TrySnapVirus(virus, center);
            }
            else
            {
                _snapDwellStart = -1f;
            }
        }
        else
        {
            _snapDwellStart = -1f;
            _pendingAuthorityForSnap = null;
            _noNearbyTimer += Time.fixedDeltaTime;
            if (_noNearbyTimer >= nearbyExitDebounce)
                ApplyVisual(DishVisual.Empty);
        }
    }

    private readonly Collider[] _overlapHits = new Collider[32];

    /// <summary>
    /// Fusion can host networked rigidbodies in the runner's <see cref="PhysicsScene"/> (not the default scene),
    /// especially in multi-peer / editor setups. <see cref="Physics.OverlapSphereNonAlloc"/> only hits the default scene.
    /// </summary>
    private bool TryFindVirusInRadius(Vector3 center, float radius, out NetworkGrabbableVirus virus)
    {
        virus = null;

        int n = Physics.OverlapSphereNonAlloc(center, radius, _overlapHits, ~0, QueryTriggerInteraction.Collide);
        if (TryPickVirusFromHits(n, out virus))
            return true;

        foreach (NetworkRunner runner in NetworkRunner.Instances)
        {
            if (runner == null || !runner.IsRunning)
                continue;

            PhysicsScene ps = runner.GetPhysicsScene();
            if (!ps.IsValid() || ps == Physics.defaultPhysicsScene)
                continue;

            n = ps.OverlapSphere(center, radius, _overlapHits, ~0, QueryTriggerInteraction.Collide);
            if (TryPickVirusFromHits(n, out virus))
                return true;
        }

        return false;
    }

    private bool TryPickVirusFromHits(int hitCount, out NetworkGrabbableVirus virus)
    {
        virus = null;
        for (int i = 0; i < hitCount; i++)
        {
            Collider c = _overlapHits[i];
            if (c == null) continue;
            virus = c.GetComponentInParent<NetworkGrabbableVirus>();
            if (virus != null) return true;
        }

        return false;
    }

    /// <summary>
    /// PetriDish logic often sits on a parent while the mesh is on an offset child — using only
    /// <see cref="Transform.position"/> misses the virus. Prefer the assigned renderer's bounds center.
    /// </summary>
    private Vector3 DishWorldCenter()
    {
        if (dishRenderer != null)
            return dishRenderer.bounds.center;
        return transform.position;
    }

    private Vector3 SnapCenterWorld() => DishWorldCenter() + Vector3.up * (virusHoverHeight * 0.5f);

    private void TrySnapVirus(NetworkGrabbableVirus grab, Vector3 center)
    {
        if (_isOccupied || grab == null) return;

        Rigidbody rb = grab.GetComponent<Rigidbody>();
        if (rb == null) return;

        NetworkObject netObj = grab.GetComponent<NetworkObject>();
        if (netObj != null && !netObj.HasStateAuthority)
        {
            if (_pendingAuthorityForSnap != grab)
            {
                _pendingAuthorityForSnap = grab;
                netObj.RequestStateAuthority();
            }

            return;
        }

        _pendingAuthorityForSnap = null;

        if (grab.IsBeingGrabbed)
            return;

        if (rb.linearVelocity.magnitude >= snapSpeedThreshold)
            return;

        if (Vector3.Distance(grab.transform.position, center) > snapOverlapRadius * 1.05f)
            return;

        SnapVirus(grab.gameObject);
    }

    private void SnapVirus(GameObject virus)
    {
        _snappedVirus = virus;
        _isOccupied = true;
        _snapDwellStart = -1f;
        _noNearbyTimer = 0f;
        _pendingAuthorityForSnap = null;

        _virusRb = virus.GetComponent<Rigidbody>();
        _virusNetTransform = virus.GetComponent<NetworkTransform>();

        if (_virusRb != null)
        {
            _virusRb.isKinematic = true;
            _virusRb.useGravity = false;
            _virusRb.linearVelocity = Vector3.zero;
            _virusRb.angularVelocity = Vector3.zero;
        }

        if (_virusNetTransform != null)
            _virusNetTransform.enabled = false;

        NetworkGrabbableVirus netVirus = virus.GetComponent<NetworkGrabbableVirus>();
        if (netVirus != null)
            netVirus.SetPetriDishSnapNetworkTransformDisabled(true);

        virus.transform.position = SnapHoverWorldPosition();
        virus.transform.rotation = Quaternion.identity;

        ApplyVisual(DishVisual.Occupied, force: true);
    }

    public void ReleaseVirus()
    {
        if (_snappedVirus == null) return;

        NetworkGrabbableVirus netVirus = _snappedVirus.GetComponent<NetworkGrabbableVirus>();
        if (netVirus != null)
            netVirus.SetPetriDishSnapNetworkTransformDisabled(false);

        if (_virusRb != null)
        {
            _virusRb.isKinematic = false;
            _virusRb.useGravity = true;
        }

        if (_virusNetTransform != null)
            _virusNetTransform.enabled = true;

        _snappedVirus = null;
        _isOccupied = false;
        _virusRb = null;
        _virusNetTransform = null;
        _snapDwellStart = -1f;
        _noNearbyTimer = 0f;

        ApplyVisual(DishVisual.Empty, force: true);
    }

    private void ApplyVisual(DishVisual state, bool force = false)
    {
        if (!force && _visual == state) return;
        _visual = state;

        Color c = state switch
        {
            DishVisual.Occupied => occupiedColor,
            DishVisual.Nearby => nearbyColor,
            _ => emptyColor,
        };

        if (_matInstance == null) return;

        if (_matInstance.HasProperty(BaseColorId))
            _matInstance.SetColor(BaseColorId, c);
        if (_matInstance.HasProperty(ColorId))
            _matInstance.SetColor(ColorId, c);

        _matInstance.color = c;
    }

    private Vector3 SnapHoverWorldPosition() => DishWorldCenter() + Vector3.up * virusHoverHeight;
}
