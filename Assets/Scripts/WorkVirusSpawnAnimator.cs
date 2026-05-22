using System.Collections;
using Fusion;
using UnityEngine;

// Add to work virus prefab. On spawn, animates VirusScale from 0 to 1 with a bounce.
// State authority drives the networked value — all clients see the animation.
public class WorkVirusSpawnAnimator : NetworkBehaviour
{
    [SerializeField] private float animDuration = 0.5f;
    [SerializeField] private int defaultMaterialIndex = 5;

    public override void Spawned()
    {
        base.Spawned();
        if (!Object.HasStateAuthority) return;

        var virus = GetComponent<NetworkGrabbableVirus>();
        if (virus == null) return;

        virus.MaterialIndex = defaultMaterialIndex;
        virus.VirusScale = 0f;
        StartCoroutine(AnimateIn(virus));
    }

    private IEnumerator AnimateIn(NetworkGrabbableVirus virus)
    {
        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Runner.DeltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            // Overshoot bounce: goes slightly past 1 then settles
            float scale = t < 0.8f
                ? Mathf.SmoothStep(0f, 1.15f, t / 0.8f)
                : Mathf.Lerp(1.15f, 1f, (t - 0.8f) / 0.2f);
            virus.VirusScale = scale;
            yield return null;
        }
        virus.VirusScale = 1f;
    }
}
