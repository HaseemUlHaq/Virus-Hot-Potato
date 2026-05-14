using System.Collections;
using UnityEngine;

/// <summary>
/// Lives in the SCENE (not on the virus prefab) so Meta gesture components
/// can have their HandRef scene references assigned in the Inspector.
///
/// Setup:
///   1. Create a scene GameObject "VirusGestureDetectors".
///   2. Add this script to it (or to a child "VirusGestureRouter").
///   3. In each ActiveStateUnityEventWrapper:
///        Left swipe  → VirusGestureRouter.OnLeftSwipe()
///        Right swipe → VirusGestureRouter.OnRightSwipe()
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
        if (_virus == null)
        {
            Debug.LogWarning("[VirusGestureRouter] OnLeftSwipe: no virus found yet.");
            return;
        }

        EnsureStateAuthority();
        _virus.CycleMaterialPrevious();
    }

    // Called by RIGHT hand swipe ActiveStateUnityEventWrapper → When Activated
    public void OnRightSwipe()
    {
        if (_virus == null)
        {
            Debug.LogWarning("[VirusGestureRouter] OnRightSwipe: no virus found yet.");
            return;
        }

        EnsureStateAuthority();
        _virus.CycleMaterialNext();
    }

    // Request state authority if this client doesn't already have it.
    // CycleMaterialNext/Previous both guard on HasStateAuthority, so without
    // this the swipe would silently no-op when the virus is held by another player.
    private void EnsureStateAuthority()
    {
        if (_virus.Object != null && !_virus.Object.HasStateAuthority)
            _virus.Object.RequestStateAuthority();
    }
}
