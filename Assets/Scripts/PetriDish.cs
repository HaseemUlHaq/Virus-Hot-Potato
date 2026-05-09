using UnityEngine;
using Fusion;

public class PetriDish : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int playerIndex = 1;
    [SerializeField] private float virusHoverHeight = 0.06f;

    [Header("Visuals")]
    [SerializeField] private Renderer dishRenderer;
    [SerializeField] private Color emptyColor = new Color(0.3f, 0.8f, 0.3f, 0.4f);
    [SerializeField] private Color occupiedColor = new Color(0.8f, 0.2f, 0.2f, 0.6f);
    [SerializeField] private Color nearbyColor = new Color(1f, 0.8f, 0.2f, 0.5f);

    // State
    private GameObject _snappedVirus = null;
    private bool _isOccupied = false;

    // Cached components
    private Rigidbody _virusRb;
    private NetworkGrabbableVirus _virusGrab;
    private NetworkTransform _virusNetTransform;

    // Material instance — prevents affecting all dishes
    private Material _materialInstance;

    public bool IsOccupied => _isOccupied;
    public GameObject SnappedVirus => _snappedVirus;

    // ─── Lifecycle ────────────────────────────────────────────────────────

    private void Start()
    {
        // Create material instance so color changes don't affect other dishes
        if (dishRenderer != null)
            _materialInstance = dishRenderer.material;

        UpdateDishVisual(emptyColor);
    }

    private void Update()
    {
        if (_isOccupied && _snappedVirus != null)
        {
            // Keep virus locked above dish center
            _snappedVirus.transform.position = transform.position +
                                               Vector3.up * virusHoverHeight;
        }
    }

    // ─── Trigger Detection ────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (_isOccupied) return;
        if (!IsVirus(other.gameObject)) return;

        UpdateDishVisual(nearbyColor);
        UnityEngine.Debug.Log("Virus nearby dish " + playerIndex);
    }

    private void OnTriggerStay(Collider other)
    {
        if (_isOccupied) return;
        if (!IsVirus(other.gameObject)) return;

        // GetComponentInParent handles virus collider being on child object
        NetworkGrabbableVirus grab = other.GetComponentInParent<NetworkGrabbableVirus>();
        Rigidbody rb = other.GetComponentInParent<Rigidbody>();

        if (grab == null || rb == null) return;

        // Snap only when released and moving slowly
        bool isBeingGrabbed = grab.IsBeingGrabbed;
        float speed = rb.linearVelocity.magnitude;

        if (!isBeingGrabbed && speed < 0.5f)
        {
            // Get the root virus GameObject
            GameObject virusRoot = grab.gameObject;
            SnapVirus(virusRoot);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_isOccupied) return;
        if (!IsVirus(other.gameObject)) return;

        UpdateDishVisual(emptyColor);
        UnityEngine.Debug.Log("Virus left dish " + playerIndex);
    }

    // ─── Snap / Release ───────────────────────────────────────────────────

    private void SnapVirus(GameObject virus)
    {
        _snappedVirus = virus;
        _isOccupied = true;

        _virusRb = virus.GetComponent<Rigidbody>();
        _virusGrab = virus.GetComponent<NetworkGrabbableVirus>();
        _virusNetTransform = virus.GetComponent<NetworkTransform>();

        if (_virusRb != null)
        {
            _virusRb.isKinematic = true;
            _virusRb.linearVelocity = Vector3.zero;
            _virusRb.angularVelocity = Vector3.zero;
        }

        if (_virusNetTransform != null)
            _virusNetTransform.enabled = false;

        virus.transform.position = transform.position + Vector3.up * virusHoverHeight;
        virus.transform.rotation = Quaternion.identity;

        UpdateDishVisual(occupiedColor);
        UnityEngine.Debug.Log("Virus SNAPPED to dish " + playerIndex);
    }

    public void ReleaseVirus()
    {
        if (_snappedVirus == null) return;

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
        _virusGrab = null;
        _virusNetTransform = null;

        UpdateDishVisual(emptyColor);
        UnityEngine.Debug.Log("Virus RELEASED from dish " + playerIndex);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private bool IsVirus(GameObject obj)
    {
        // Check parent chain in case collider is on a child object
        return obj.GetComponentInParent<NetworkGrabbableVirus>() != null;
    }

    private void UpdateDishVisual(Color color)
    {
        if (_materialInstance != null)
            _materialInstance.color = color;
    }
}