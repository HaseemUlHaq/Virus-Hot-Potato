using Fusion;
using UnityEngine;
using Meta.XR.MRUtilityKit;

// Detects the physical box QR code and tells FormationManager to spawn the ExampleFormation inside.
// QR only needs to be detected once at startup — the door can open/move after that.
public class BoxAnchor : MonoBehaviour
{
    [Header("QR")]
    [Tooltip("Must match the text encoded in the printed QR code on the box.")]
    [SerializeField] private string qrPayload = "virus_toolbox";

    [Header("Wiring")]
    [SerializeField] private NetworkedBoxAnchor networkedBoxAnchor;
    [SerializeField] private FormationManager formationManager;

    [Header("Offsets from QR face (all in QR local space)")]
    [Tooltip("Where to place ToolboxRoot relative to the QR. Z negative = into the box. Tune this so the virtual box aligns with the physical box.")]
    [SerializeField] private Vector3 toolboxRootOffset = new Vector3(0f, 0f, 0f);
    [Tooltip("Where to spawn the ExampleFormation relative to the QR. Z negative = into box, Y = up/down, X = left/right.")]
    [SerializeField] private Vector3 exampleBoxOffset = new Vector3(0f, 0.10f, -0.20f);

    [Header("Testing")]
    [Tooltip("Skip the colocation gate — use in solo / Meta Quest Link testing. Disable before shipping.")]
    [SerializeField] private bool skipColocationGate = false;

    public static Vector3 BoxQRPosition { get; private set; }
    public static bool BoxFound { get; private set; } = false;

    private bool _detected = false;
    private bool _colocationReady = false;
    private bool _qrDetected = false;
    private Vector3 _pendingQrPosition;
    private Quaternion _pendingQrRotation;

    void Start()
    {
        if (MRUK.Instance == null)
        {
            Debug.LogWarning("[BoxAnchor] MRUK.Instance is null — box QR detection will not work.");
            return;
        }
        MRUK.Instance.SceneSettings.TrackableAdded.AddListener(OnTrackableAdded);

        if (skipColocationGate)
            OnColocationReady();
    }

    // Wire this to ColocationController → ColocationReadyCallbacks in the Inspector,
    // same as TableAnchor.OnColocationReady.
    public void OnColocationReady()
    {
        Debug.Log("[BoxAnchor] Colocation ready.");
        _colocationReady = true;

        if (_qrDetected)
        {
            Debug.Log("[BoxAnchor] Applying QR placement that was pending colocation.");
            ApplyPlacement(_pendingQrPosition, _pendingQrRotation);
        }
    }

    void OnTrackableAdded(MRUKTrackable trackable)
    {
        if (_detected) return;
        if (trackable.TrackableType != OVRAnchor.TrackableType.QRCode) return;
        if (trackable.MarkerPayloadString != qrPayload) return;

        Debug.Log($"[BoxAnchor] Box QR detected at {trackable.transform.position}");

        if (_colocationReady)
        {
            ApplyPlacement(trackable.transform.position, trackable.transform.rotation);
        }
        else
        {
            Debug.Log("[BoxAnchor] Colocation not ready yet — holding QR placement.");
            _pendingQrPosition = trackable.transform.position;
            _pendingQrRotation = trackable.transform.rotation;
            _qrDetected = true;
        }
    }

    private void ApplyPlacement(Vector3 qrPosition, Quaternion qrRotation)
    {
        _detected = true;
        BoxQRPosition = qrPosition;
        BoxFound = true;

        // Sync ToolboxRoot position to all clients via NetworkedBoxAnchor
        Vector3 boxRootWorldPos = qrPosition + (qrRotation * toolboxRootOffset);
        if (networkedBoxAnchor != null)
            networkedBoxAnchor.RequestPlacement(boxRootWorldPos);

        // Compute interior world position using QR local space so offset is correct
        // regardless of which direction the box faces or where the QR sits on the door.
        Vector3 interiorWorldPos = qrPosition + (qrRotation * exampleBoxOffset);
        Debug.Log($"[BoxAnchor] QR:{qrPosition} → interior:{interiorWorldPos}");

        NetworkRunner masterRunner = FindMasterRunner();
        if (masterRunner == null)
        {
            Debug.LogWarning("[BoxAnchor] No master runner yet — formation will not spawn.");
            return;
        }

        if (formationManager != null)
            formationManager.TrySpawnExampleFormation(masterRunner, interiorWorldPos);
        else
            Debug.LogWarning("[BoxAnchor] FormationManager not assigned.");
    }

    private NetworkRunner FindMasterRunner()
    {
        foreach (var runner in NetworkRunner.Instances)
        {
            if (runner != null && runner.IsRunning && runner.IsSharedModeMasterClient)
                return runner;
        }
        return null;
    }
}
