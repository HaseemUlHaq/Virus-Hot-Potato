using UnityEngine;
using Meta.XR.MRUtilityKit;

public class TableAnchor : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private GameObject virtualTable;
    [SerializeField] private VirusSpawner virusSpawner;

    [Header("QR Code Settings")]
    [SerializeField] private string qrCodePayload = "virus_puck_1";

    [Header("Tune these in Inspector")]
    [SerializeField] private float yRotationOffset = 0f;

    public static Vector3 TableSurfacePosition { get; private set; }
    public static bool TableFound { get; private set; } = false;

    void Start()
    {
        if (MRUK.Instance == null)
        {
            Debug.LogError("TableAnchor: MRUK.Instance is null!");
            return;
        }

        // Enable QR code tracking
        var config = MRUK.Instance.SceneSettings.TrackerConfiguration;
        config.QRCodeTrackingEnabled = true;
        MRUK.Instance.SceneSettings.TrackerConfiguration = config;

        // Listen for QR codes
        MRUK.Instance.SceneSettings.TrackableAdded.AddListener(OnTrackableAdded);
    }

    void OnTrackableAdded(MRUKTrackable trackable)
    {
        // Only care about QR codes
        if (trackable.TrackableType != OVRAnchor.TrackableType.QRCode)
            return;

        // Check if this is our table QR code
        if (trackable.MarkerPayloadString != qrCodePayload)
        {
            Debug.Log($"Ignoring QR code: {trackable.MarkerPayloadString}");
            return;
        }

        Debug.Log($"Table QR code found: {qrCodePayload}");

        // Snap virtual table to QR code position
        virtualTable.transform.position = trackable.transform.position;

        // Apply rotation
        Quaternion baseRotation = trackable.transform.rotation * Quaternion.Euler(90, 0, 0);
        virtualTable.transform.rotation = baseRotation * Quaternion.Euler(0, yRotationOffset, 0);

        // Store for other scripts
        TableSurfacePosition = trackable.transform.position;
        TableFound = true;

        // Tell virus spawner
        if (virusSpawner != null)
            virusSpawner.SetTablePosition(trackable.transform.position);
    }
}