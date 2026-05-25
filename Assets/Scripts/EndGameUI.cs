using System.Collections;
using UnityEngine;

public class EndGameUI : MonoBehaviour
{
    [SerializeField] private float animDuration = 1.2f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip appearClip;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        StartCoroutine(AnimateIn());
    }

    private IEnumerator AnimateIn()
    {
        if (audioSource != null && appearClip != null)
            audioSource.PlayOneShot(appearClip);

        float t = 0f;
        while (t < animDuration)
        {
            t += Time.deltaTime;
            float p = t / animDuration;

            canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, p);

            // slight overshoot on scale for punch feel
            float scale = p < 0.8f
                ? Mathf.SmoothStep(0f, 1.1f, p / 0.8f)
                : Mathf.Lerp(1.1f, 1f, (p - 0.8f) / 0.2f);

            transform.localScale = Vector3.one * scale;
            yield return null;
        }

        canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one;
    }
}
