using Oculus.Interaction.Input;
using UnityEngine;

/// <summary>
/// Lives in the SCENE on VirusGestureDetectors.
/// Receives swipe events from HandSwipeGestureLeft/Right (scene-level ActiveStateUnityEventWrapper)
/// and forwards them to the virus closest to the swiping hand (proximity gate).
/// </summary>
public class VirusGestureRouter : MonoBehaviour
{
    // ─── Proximity Gate ───────────────────────────────────────────────────
    [Header("Proximity Gate")]
    [Tooltip("Max distance (metres) the hand must be from a virus centre for a swipe to register.")]
    [SerializeField] private float maxSwipeDistance = 0.35f;

    // ─── Hand References (auto-found if left empty) ───────────────────────
    [Header("Hand References (auto-found if empty)")]
    [SerializeField] private Hand leftHand;
    [SerializeField] private Hand rightHand;

    private void OnEnable()
    {
        TryFindHands();
    }

    private void TryFindHands()
    {
        if (leftHand != null && rightHand != null) return;

        var hands = FindObjectsByType<Hand>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var h in hands)
        {
            if (h.Handedness == Handedness.Left && leftHand == null)
                leftHand = h;
            else if (h.Handedness == Handedness.Right && rightHand == null)
                rightHand = h;
        }
    }

    // Called by LEFT hand swipe ActiveStateUnityEventWrapper → When Activated
    public void OnLeftSwipe()
    {
        Debug.Log("[VirusGestureRouter] OnLeftSwipe called");
        TryFindHands();
        NetworkGrabbableVirus virus = ResolveVirusForHand(leftHand, Handedness.Left);
        if (virus == null) { Debug.LogWarning("[VirusGestureRouter] No virus in range"); return; }
        if (virus.Object == null || !virus.Object.IsValid) return;
        Debug.Log($"[VirusGestureRouter] Cycle previous via RPC on {virus.name}");
        virus.RequestCycleMaterialFromGesture(nextMaterial: false);
        virus.RequestCycleShapeFromGesture(next: false);
    }

    // Called by RIGHT hand swipe ActiveStateUnityEventWrapper → When Activated
    public void OnRightSwipe()
    {
        Debug.Log("[VirusGestureRouter] OnRightSwipe called");
        TryFindHands();
        NetworkGrabbableVirus virus = ResolveVirusForHand(rightHand, Handedness.Right);
        if (virus == null) { Debug.LogWarning("[VirusGestureRouter] No virus in range"); return; }
        if (virus.Object == null || !virus.Object.IsValid) return;
        Debug.Log($"[VirusGestureRouter] Cycle next via RPC on {virus.name}");
        virus.RequestCycleMaterialFromGesture(nextMaterial: true);
        virus.RequestCycleShapeFromGesture(next: true);
    }

    private NetworkGrabbableVirus ResolveVirusForHand(Hand hand, Handedness side)
    {
        if (hand != null && hand.GetRootPose(out Pose pose))
            return FindClosestVirus(pose.position, maxSwipeDistance);

        Debug.LogWarning($"[VirusGestureRouter] {side} hand pose unavailable — using first virus in scene (fallback)");
        return FindFirstObjectByType<NetworkGrabbableVirus>();
    }

    private static NetworkGrabbableVirus FindClosestVirus(Vector3 fromPosition, float maxDistance)
    {
        NetworkGrabbableVirus best = null;
        float bestDist = maxDistance;

        foreach (var v in FindObjectsByType<NetworkGrabbableVirus>(FindObjectsSortMode.None))
        {
            if (v == null) continue;
            float d = Vector3.Distance(v.transform.position, fromPosition);
            if (d <= bestDist)
            {
                bestDist = d;
                best = v;
            }
        }

        return best;
    }
}
