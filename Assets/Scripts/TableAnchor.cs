using UnityEngine;
using Meta.XR.MRUtilityKit;

public class TableAnchor : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private NetworkedTableAnchor networkedTable;
    [SerializeField] private VirusSpawner virusSpawner;

    [Header("QR Code Settings")]
    [SerializeField] private string qrCodePayload = "virus_puck_1";

    [Header("Tune these in Inspector")]
    [SerializeField] private float yRotationOffset = 0f;

    public static Vector3 TableSurfacePosition { get; private set; }
    public static bool TableFound { get; private set; } = false;

    // ─── Colocation-gate state ────────────────────────────────────────────
    private bool _colocationReady = false;
    private bool _qrDetected = false;
    private Vector3 _pendingQrPosition;
    private Quaternion _pendingQrRotation;

    void Start()
    {
        if (MRUK.Instance == null)
        {
            Debug.LogError("TableAnchor: MRUK.Instance is null!");
            return;
        }

        var config = MRUK.Instance.SceneSettings.TrackerConfiguration;
        config.QRCodeTrackingEnabled = true;
        MRUK.Instance.SceneSettings.TrackerConfiguration = config;

        MRUK.Instance.SceneSettings.TrackableAdded.AddListener(OnTrackableAdded);
    }

    // ─── Called by ColocationController.ColocationReadyCallbacks ─────────

    /// <summary>
    /// Wire this to ColocationController → ColocationReadyCallbacks in the Inspector.
    /// Until this fires, any QR detection is held and applied as soon as it does.
    /// </summary>
    public void OnColocationReady()
    {
        Debug.Log("[TableAnchor] Colocation ready.");
        _colocationReady = true;

        // If the QR code was already detected before colocation finished, apply it now.
        if (_qrDetected)
        {
            Debug.Log("[TableAnchor] Applying QR placement that was pending colocation.");
            ApplyPlacement(_pendingQrPosition, _pendingQrRotation);
        }
    }

    // ─── QR Detection ─────────────────────────────────────────────────────

    void OnTrackableAdded(MRUKTrackable trackable)
    {
        if (trackable.TrackableType != OVRAnchor.TrackableType.QRCode)
            return;

        if (trackable.MarkerPayloadString != qrCodePayload)
        {
            Debug.Log($"[TableAnchor] Ignoring QR code: {trackable.MarkerPayloadString}");
            return;
        }

        Debug.Log($"[TableAnchor] QR code detected: {qrCodePayload}");

        Quaternion baseRotation = trackable.transform.rotation * Quaternion.Euler(90, 0, 0);
        Quaternion finalRotation = baseRotation * Quaternion.Euler(0, yRotationOffset, 0);

        if (_colocationReady)
        {
            // Coordinate space is already aligned — place immediately.
            ApplyPlacement(trackable.transform.position, finalRotation);
        }
        else
        {
            // Colocation not done yet — store and wait for OnColocationReady().
            Debug.Log("[TableAnchor] Colocation not ready yet — holding QR placement.");
            _pendingQrPosition = trackable.transform.position;
            _pendingQrRotation = finalRotation;
            _qrDetected = true;
        }
    }

    private void ApplyPlacement(Vector3 position, Quaternion rotation)
    {
        if (networkedTable != null)
            networkedTable.RequestPlacement(position, rotation);

        TableSurfacePosition = position;
        TableFound = true;

        if (virusSpawner != null)
            virusSpawner.SetTablePosition(position);

        Debug.Log($"[TableAnchor] Table placed at {position}");
    }
}
