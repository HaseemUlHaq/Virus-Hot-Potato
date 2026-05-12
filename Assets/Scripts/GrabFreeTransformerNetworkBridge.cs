using UnityEngine;
using Fusion;
using Oculus.Interaction;

/// This syncs with Fusion's network tick rate (60hz) for smoother updates

[RequireComponent(typeof(NetworkGrabbableVirus))]
public class GrabFreeTransformerNetworkBridge : NetworkBehaviour
{
    [Header("Meta SDK Component")]
    [SerializeField] private GrabFreeTransformer grabFreeTransformer;
    [SerializeField] private Grabbable grabbable;

    [Header("Settings")]
    [Tooltip("Minimum scale change to trigger network update (prevents spam)")]
    [SerializeField] private float scaleChangedThreshold = 0.01f;

    private NetworkGrabbableVirus _networkVirus;
    private Vector3 _lastSyncedScale = Vector3.one;

    void Start()
    {
        _networkVirus = GetComponent<NetworkGrabbableVirus>();

        if (grabFreeTransformer == null)
        {
            grabFreeTransformer = GetComponent<GrabFreeTransformer>();
        }

        if (grabbable == null)
        {
            grabbable = GetComponent<Grabbable>();
        }

        _lastSyncedScale = transform.localScale;
    }

    // Use FixedUpdateNetwork instead of Update - syncs with Fusion tick rate!
    public override void FixedUpdateNetwork()
    {
        // Only sync when we have authority
        if (_networkVirus == null || !_networkVirus.Object.HasStateAuthority)
            return;

        // Check if scale changed significantly
        Vector3 currentScale = transform.localScale;
        float scaleDifference = Vector3.Distance(currentScale, _lastSyncedScale);

        if (scaleDifference > scaleChangedThreshold)
        {
            // Scale changed! Sync to network
            float averageScale = (currentScale.x + currentScale.y + currentScale.z) / 3f;

            if (_networkVirus != null)
            {
                _networkVirus.SetVirusScale(averageScale);
            }

            _lastSyncedScale = currentScale;
        }
    }
}