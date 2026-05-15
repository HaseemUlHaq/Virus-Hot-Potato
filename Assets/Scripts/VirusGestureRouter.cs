using System.Collections;
using Oculus.Interaction.Input;
using UnityEngine;

/// <summary>
/// Lives in the SCENE on VirusGestureDetectors.
/// Receives swipe events from HandSwipeGestureLeft/Right (scene-level ActiveStateUnityEventWrapper)
/// and forwards them to the spawned virus — but only when the swiping hand is within
/// maxSwipeDistance of the virus (proximity gate).
/// </summary>
public class VirusGestureRouter : MonoBehaviour
{
    // ─── Proximity Gate ───────────────────────────────────────────────────
    [Header("Proximity Gate")]
    [Tooltip("Max distance (metres) the hand must be from the virus centre for a swipe to register.")]
    [SerializeField] private float maxSwipeDistance = 0.35f;

    // ─── Hand References (auto-found if left empty) ───────────────────────
    [Header("Hand References (auto-found if empty)")]
    [SerializeField] private Hand leftHand;
    [SerializeField] private Hand rightHand;

    private NetworkGrabbableVirus _virus;

    private void OnEnable()
    {
        StartCoroutine(FindVirusRoutine());
        TryFindHands();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _virus = null;
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

    private IEnumerator FindVirusRoutine()
    {
        while (true)
        {
            _virus = FindFirstObjectByType<NetworkGrabbableVirus>();
            if (_virus != null)
            {
                Debug.Log("[VirusGestureRouter] Found virus.");
                yield break;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    // Called by LEFT hand swipe ActiveStateUnityEventWrapper → When Activated
    public void OnLeftSwipe()
    {
        Debug.Log("[VirusGestureRouter] OnLeftSwipe called");
        if (_virus == null) { Debug.LogWarning("[VirusGestureRouter] No virus found"); return; }
        if (!IsHandNearVirus(leftHand, Handedness.Left)) return;
        StartCoroutine(CycleWithAuthority(false));
    }

    // Called by RIGHT hand swipe ActiveStateUnityEventWrapper → When Activated
    public void OnRightSwipe()
    {
        Debug.Log("[VirusGestureRouter] OnRightSwipe called");
        if (_virus == null) { Debug.LogWarning("[VirusGestureRouter] No virus found"); return; }
        if (!IsHandNearVirus(rightHand, Handedness.Right)) return;
        StartCoroutine(CycleWithAuthority(true));
    }

    // Returns true when the hand is close enough to the virus (or when hand tracking
    // is unavailable, so the gate is transparent in that edge case).
    private bool IsHandNearVirus(Hand hand, Handedness side)
    {
        // Lazy-find in case hands weren't ready during OnEnable
        if (hand == null)
        {
            TryFindHands();
            hand = side == Handedness.Left ? leftHand : rightHand;
        }

        if (hand == null)
        {
            Debug.LogWarning($"[VirusGestureRouter] {side} Hand reference missing — swipe proximity check skipped");
            return true; // fail-open so the game still works without hand tracking setup
        }

        if (!hand.GetRootPose(out Pose pose))
        {
            Debug.LogWarning($"[VirusGestureRouter] {side} hand pose unavailable — swipe proximity check skipped");
            return true;
        }

        float dist = Vector3.Distance(pose.position, _virus.transform.position);
        bool near = dist <= maxSwipeDistance;

        if (!near)
            Debug.Log($"[VirusGestureRouter] {side} hand {dist:F2}m from virus (max {maxSwipeDistance}m) — swipe ignored");

        return near;
    }

    private IEnumerator CycleWithAuthority(bool next)
    {
        if (_virus.Object == null) yield break;

        if (!_virus.Object.HasStateAuthority)
        {
            Debug.Log("[VirusGestureRouter] Requesting state authority...");
            _virus.Object.RequestStateAuthority();

            float timeout = 1f;
            while (!_virus.Object.HasStateAuthority && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        if (!_virus.Object.HasStateAuthority)
        {
            Debug.LogWarning("[VirusGestureRouter] Could not get state authority — cycle skipped");
            yield break;
        }

        Debug.Log($"[VirusGestureRouter] Cycling {(next ? "next" : "previous")}");
        if (next) _virus.CycleMaterialNext();
        else _virus.CycleMaterialPrevious();
    }
}
