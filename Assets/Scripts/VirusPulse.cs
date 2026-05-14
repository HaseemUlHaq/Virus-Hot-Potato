using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class VirusPulse : MonoBehaviour
{
    [Header("Target Virus Object")]
    public GameObject virusObject;

    [Range(1.05f, 1.5f)] public float pulseSize = 1.25f;
    [Range(0.05f, 0.2f)] public float attackTime = 0.08f;
    [Range(0.2f, 0.8f)]  public float releaseTime = 0.45f;

    private Vector3 originalScale;

    void Start()
    {
        if (virusObject == null)
            virusObject = this.gameObject;
        originalScale = virusObject.transform.localScale;
        Debug.Log("[VirusPulse] Ready | Scale: " + originalScale);
    }

    void Update()
    {
        try
        {
            if (Keyboard.current != null &&
                Keyboard.current.spaceKey.wasPressedThisFrame)
                TriggerPulse();
        }
        catch { }
    }

    public void TriggerPulse()
    {
        Debug.Log("[VirusPulse] TriggerPulse called!");
        StopAllCoroutines();
        StartCoroutine(PulseRoutine());
    }

    IEnumerator PulseRoutine()
    {
        float elapsed = 0f;
        Vector3 bigScale = originalScale * pulseSize;

        while (elapsed < attackTime)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / attackTime), 3f);
            virusObject.transform.localScale = Vector3.Lerp(originalScale, bigScale, t);
            yield return null;
        }

        virusObject.transform.localScale = bigScale;
        elapsed = 0f;

        while (elapsed < releaseTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / releaseTime);
            virusObject.transform.localScale = Vector3.Lerp(bigScale, originalScale, SpringEase(t));
            yield return null;
        }

        virusObject.transform.localScale = originalScale;
        Debug.Log("[VirusPulse] Done!");
    }

    float SpringEase(float t)
    {
        const float c4 = (2f * Mathf.PI) / 3f;
        if (t == 0) return 0;
        if (t == 1) return 1;
        return Mathf.Pow(2f, -8f * t) *
               Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }
}
