using UnityEngine;

// Place on an empty GameObject centered on the front wall of the physical box.
// Forward (Z) axis must point outward from the wall (away from the box interior).
// Triggers when a hand comes within the wall's flat detection zone.
public class BoxFrontWallTrigger : MonoBehaviour
{
    [SerializeField] private FormationManager formationManager;

    [Header("Detection Zone")]
    [Tooltip("How close to the wall plane to trigger (depth, in metres).")]
    [SerializeField] private float planeDepth = 0.10f;
    [Tooltip("How far from the wall centre the hand can be laterally (width/height of detection zone).")]
    [SerializeField] private float lateralRadius = 0.30f;

    [Header("Hands")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("Timing")]
    [SerializeField] private float spawnDelay = 5f;

    private bool _triggered;

    private void Start()
    {
        AutoFindHands();
    }

    private void Update()
    {
        if (_triggered) return;
        if (leftHand == null && rightHand == null) { AutoFindHands(); return; }

        if (IsHandNear())
        {
            _triggered = true;
            Debug.Log($"[BoxTrigger] Hand detected at wall — spawning work area in {spawnDelay}s.");
            StartCoroutine(SpawnAfterDelay());
        }
    }

    private System.Collections.IEnumerator SpawnAfterDelay()
    {
        yield return new WaitForSeconds(spawnDelay);
        formationManager?.SpawnWorkArea();
    }

    private bool IsHandNear()
    {
        return IsNearWallPlane(leftHand) || IsNearWallPlane(rightHand);
    }

    private bool IsNearWallPlane(Transform hand)
    {
        if (hand == null) return false;

        Vector3 toHand = hand.position - transform.position;

        // How far from the wall surface (along the wall normal)
        float depth = Mathf.Abs(Vector3.Dot(toHand, transform.forward));
        if (depth > planeDepth) return false;

        // How far from the wall centre laterally (along the wall surface)
        float lateral = Vector3.ProjectOnPlane(toHand, transform.forward).magnitude;
        return lateral < lateralRadius;
    }

    private void AutoFindHands()
    {
        var hands = FindObjectsByType<NetworkedHandSimple>(FindObjectsSortMode.None);
        foreach (var hand in hands)
        {
            if (!hand.Object.HasStateAuthority) continue;
            if (hand.IsLeftHand && leftHand == null) leftHand = hand.transform;
            else if (!hand.IsLeftHand && rightHand == null) rightHand = hand.transform;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, new Vector3(lateralRadius * 2f, lateralRadius * 2f, planeDepth * 2f));
    }
}
