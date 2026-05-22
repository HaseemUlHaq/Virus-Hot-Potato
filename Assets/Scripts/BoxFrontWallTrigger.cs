using UnityEngine;

// Place on an empty GameObject positioned at the front wall of the physical box.
// Detects when a hand is within radius and spawns the work area.
public class BoxFrontWallTrigger : MonoBehaviour
{
    [SerializeField] private FormationManager formationManager;
    [SerializeField] private float detectionRadius = 0.12f;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

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
            Debug.Log($"[BoxTrigger] Hand detected — spawning work area in {spawnDelay}s.");
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
        if (leftHand != null && Vector3.Distance(leftHand.position, transform.position) < detectionRadius) return true;
        if (rightHand != null && Vector3.Distance(rightHand.position, transform.position) < detectionRadius) return true;
        return false;
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
}
