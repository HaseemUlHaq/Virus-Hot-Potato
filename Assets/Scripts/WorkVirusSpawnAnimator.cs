using System.Collections;
using Fusion;
using UnityEngine;

// Add to work virus prefab. On spawn, sets default material and animates the virus
// dropping down from above into its slot position. No scale changes.
public class WorkVirusSpawnAnimator : NetworkBehaviour
{
    [SerializeField] private float animDuration = 0.5f;
    [SerializeField] private float spawnHeightOffset = 0.3f;
    [SerializeField] private int defaultMaterialIndex = 5;

    public override void Spawned()
    {
        base.Spawned();
        if (!Object.HasStateAuthority) return;

        var virus = GetComponent<NetworkGrabbableVirus>();
        if (virus != null)
            virus.MaterialIndex = defaultMaterialIndex;

        StartCoroutine(AnimateIn());
    }

    private IEnumerator AnimateIn()
    {
        Vector3 targetPos = transform.position;
        Vector3 startPos = targetPos + Vector3.up * spawnHeightOffset;
        transform.position = startPos;

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            if (GetComponent<NetworkGrabbableVirus>() is { IsBeingGrabbed: true }) yield break;
            elapsed += Runner.DeltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            transform.position = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        transform.position = targetPos;
    }
}
