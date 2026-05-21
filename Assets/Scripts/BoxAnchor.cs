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
    [SerializeField] private FormationManager formationManager;

    [Header("Optional — positions the ToolboxRoot at the box QR location")]
    [Tooltip("Drag ToolboxRoot here so handle detection aligns with the physical box.")]
    [SerializeField] private Transform toolboxRoot;

    [Header("Offset from QR face to formation spawn point (QR local space)")]
    [Tooltip("Z = inward from door face (use negative), X = left/right, Y = up/down. Applied in QR local space so it works regardless of box orientation.")]
    [SerializeField] private Vector3 exampleBoxOffset = new Vector3(0f, 0.10f, -0.20f);

    public static Vector3 BoxQRPosition { get; private set; }
    public static bool BoxFound { get; private set; } = false;

    private bool _detected = false;

    void Start()
    {
        if (MRUK.Instance == null)
        {
            Debug.LogWarning("[BoxAnchor] MRUK.Instance is null — box QR detection will not work.");
            return;
        }
        MRUK.Instance.SceneSettings.TrackableAdded.AddListener(OnTrackableAdded);
    }

    void OnTrackableAdded(MRUKTrackable trackable)
    {
        if (_detected) return;
        if (trackable.TrackableType != OVRAnchor.TrackableType.QRCode) return;
        if (trackable.MarkerPayloadString != qrPayload) return;

        _detected = true;
        BoxQRPosition = trackable.transform.position;
        BoxFound = true;

        // Apply offset in QR local space so the spawn point is correct regardless of
        // which direction the box faces or where on the door the QR is placed.
        Vector3 interiorWorldPos = trackable.transform.position + (trackable.transform.rotation * exampleBoxOffset);

        Debug.Log($"[BoxAnchor] Box QR detected at {BoxQRPosition}, interior target: {interiorWorldPos}");

        // Move ToolboxRoot to box position so handle anchors align with physical box
        if (toolboxRoot != null)
            toolboxRoot.position = BoxQRPosition;

        // Find master runner and spawn ExampleFormation
        NetworkRunner masterRunner = FindMasterRunner();
        if (masterRunner == null)
        {
            Debug.LogWarning("[BoxAnchor] Box QR detected but no master runner yet — formation will not spawn.");
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
