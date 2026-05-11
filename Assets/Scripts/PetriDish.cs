using UnityEngine;
using Fusion;

public class PetriDish : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int playerIndex = 1;
    [SerializeField] private float virusHoverHeight = 0.08f;
    [SerializeField] private float snapSpeedThreshold = 0.3f;
    [SerializeField] private float snapDelay = 0.3f;

    [Header("Visuals")]
    [SerializeField] private Renderer dishRenderer;
    [SerializeField] private Color emptyColor = new Color(0.3f, 0.8f, 0.3f, 1f);
    [SerializeField] private Color occupiedColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color nearbyColor = new Color(1f, 0.8f, 0.2f, 1f);

    private GameObject _snappedVirus = null;
    private bool _isOccupied = false;
    private Rigidbody _virusRb;
    private NetworkTransform _virusNetTransform;
    private Material _matInstance;

    // Snap delay — prevents immediate snap on release
    private float _virusEnteredTime = -1f;
    private bool _virusInsideTrigger = false;

    public bool IsOccupied => _isOccupied;
    public GameObject SnappedVirus => _snappedVirus;

    private void Start()
    {
        if (dishRenderer != null)
        {
            // Force unique material instance
            _matInstance = new Material(dishRenderer.material);
            dishRenderer.material = _matInstance;
            UnityEngine.Debug.Log("PetriDish " + playerIndex + " material instance created");
        }
        else
        {
            UnityEngine.Debug.LogWarning("PetriDish " + playerIndex + " has no renderer!");
        }

        SetColor(emptyColor);
    }

    private void Update()
    {
        // Keep virus hovering above dish
        if (_isOccupied && _snappedVirus != null)
        {
            _snappedVirus.transform.position = transform.position +
                                               Vector3.up * virusHoverHeight;
            _snappedVirus.transform.rotation = Quaternion.identity;
        }

        // Delayed snap check — only snap after virus has been
        // inside trigger for snapDelay seconds AND is not grabbed
        if (!_isOccupied && _virusInsideTrigger && _virusEnteredTime > 0f)
        {
            float timeInside = Time.time - _virusEnteredTime;
            if (timeInside >= snapDelay)
            {
                TrySnap();
            }
        }
    }

    private void TrySnap()
    {
        if (_isOccupied) return;

        // Only check objects currently inside our trigger
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            0.08f  // small fixed radius — only snaps when very close to dish center
        );

        foreach (var hit in hits)
        {
            NetworkGrabbableVirus grab = hit.GetComponentInParent<NetworkGrabbableVirus>();
            if (grab == null) continue;

            Rigidbody rb = grab.GetComponent<Rigidbody>();
            if (rb == null) continue;

            if (!grab.IsBeingGrabbed && rb.linearVelocity.magnitude < snapSpeedThreshold)
            {
                SnapVirus(grab.gameObject);
                return;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isOccupied) return;
        if (!IsVirus(other.gameObject)) return;

        _virusInsideTrigger = true;
        _virusEnteredTime = Time.time;
        SetColor(nearbyColor);
        UnityEngine.Debug.Log("Virus entered dish " + playerIndex);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_isOccupied) return;
        if (!IsVirus(other.gameObject)) return;

        _virusInsideTrigger = false;
        _virusEnteredTime = -1f;
        SetColor(emptyColor);
        UnityEngine.Debug.Log("Virus left dish " + playerIndex);
    }

    private void SnapVirus(GameObject virus)
    {
        _snappedVirus = virus;
        _isOccupied = true;
        _virusInsideTrigger = false;
        _virusEnteredTime = -1f;

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

        virus.transform.position = transform.position + Vector3.up * virusHoverHeight;
        virus.transform.rotation = Quaternion.identity;

        SetColor(occupiedColor);
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
        _virusNetTransform = null;
        _virusInsideTrigger = false;
        _virusEnteredTime = -1f;

        SetColor(emptyColor);
        UnityEngine.Debug.Log("Virus RELEASED from dish " + playerIndex);
    }

    private bool IsVirus(GameObject obj)
    {
        return obj.GetComponentInParent<NetworkGrabbableVirus>() != null;
    }

    private void SetColor(Color color)
    {
        if (_matInstance != null)
        {
            _matInstance.color = color;
            UnityEngine.Debug.Log("PetriDish " + playerIndex + " color set to " + color);
        }
    }
}