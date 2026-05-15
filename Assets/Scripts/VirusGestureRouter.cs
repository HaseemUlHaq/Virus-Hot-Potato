using System.Collections;
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

    private void OnDisable()
    {
        StopAllCoroutines();
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
        StartCoroutine(CycleWithAuthority(false, virus));
    }

    // Called by RIGHT hand swipe ActiveStateUnityEventWrapper → When Activated
    public void OnRightSwipe()
    {
        Debug.Log("[VirusGestureRouter] OnRightSwipe called");
        TryFindHands();
        NetworkGrabbableVirus virus = ResolveVirusForHand(rightHand, Handedness.Right);
        if (virus == null) { Debug.LogWarning("[VirusGestureRouter] No virus in range"); return; }
        StartCoroutine(CycleWithAuthority(true, virus));
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
            float d = Vector3.Distance(fromPosition, v.transform.position);
            if (d <= bestDist)
            {
                bestDist = d;
                best = v;
            }
        }

        return best;
    }

    private IEnumerator CycleWithAuthority(bool next, NetworkGrabbableVirus virus)
    {
        if (virus == null || virus.Object == null) yield break;

        if (!virus.Object.HasStateAuthority)
        {
            Debug.Log("[VirusGestureRouter] Requesting state authority...");
            virus.Object.RequestStateAuthority();

            float timeout = 1f;
            while (!virus.Object.HasStateAuthority && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        if (!virus.Object.HasStateAuthority)
        {
            Debug.LogWarning("[VirusGestureRouter] Could not get state authority — cycle skipped");
            yield break;
        }

        Debug.Log($"[VirusGestureRouter] Cycling {(next ? "next" : "previous")} on {virus.name}");
        if (next) virus.CycleMaterialNext();
        else virus.CycleMaterialPrevious();
    }
}
