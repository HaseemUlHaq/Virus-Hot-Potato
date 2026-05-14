using System.Collections;
using UnityEngine;

/// <summary>
/// Lives in the SCENE on VirusGestureDetectors.
/// Receives swipe events from HandSwipeGestureLeft/Right (scene-level ActiveStateUnityEventWrapper)
/// and forwards them to the spawned virus.
/// </summary>
public class VirusGestureRouter : MonoBehaviour
{
    private NetworkGrabbableVirus _virus;

    private void OnEnable()
    {
        StartCoroutine(FindVirusRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _virus = null;
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
        StartCoroutine(CycleWithAuthority(false));
    }

    // Called by RIGHT hand swipe ActiveStateUnityEventWrapper → When Activated
    public void OnRightSwipe()
    {
        Debug.Log("[VirusGestureRouter] OnRightSwipe called");
        if (_virus == null) { Debug.LogWarning("[VirusGestureRouter] No virus found"); return; }
        StartCoroutine(CycleWithAuthority(true));
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
