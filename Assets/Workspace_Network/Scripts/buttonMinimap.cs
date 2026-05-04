using UnityEngine;
using System.Collections;

public class buttonMinimap : MonoBehaviour
{
    public bool isDisabled = true;
    public GameObject pinMap; 
    public GameObject cover; 

    [Header("Animation Settings")]
    public float liftHeight = 0.001f;
    public float duration = 0.5f;

    private Vector3 startPos;
    private CanvasGroup canvasGroup;

    /*
    void Awake()
    {
        if (cover != null)
        {
            startPos = cover.transform.localPosition;

            // Add CanvasGroup if not present (for fade)
            canvasGroup = cover.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = cover.AddComponent<CanvasGroup>();
        }
    }
    */
    
    /*
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) //Temporaty for testing
        {
            openCover();
        }
    }
    */

    public void openCover()
    {
        if (cover != null)
            StartCoroutine(OpenCoverAnim());
    }

    IEnumerator OpenCoverAnim()
    {
        float time = 0f;

        Vector3 currentStart = cover.transform.localPosition;
        Vector3 targetPos = currentStart + cover.transform.up * liftHeight;

        Vector3 startScale = cover.transform.localScale;

        // --- LIFT ---
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            float ease = Mathf.SmoothStep(0f, 1f, t);

            cover.transform.localPosition = Vector3.Lerp(currentStart, targetPos, ease);

            yield return null;
        }

        cover.transform.localPosition = targetPos;

        // --- SHRINK ---
        float shrinkTime = 0f;
        float shrinkDuration = 0.35f;

        while (shrinkTime < shrinkDuration)
        {
            shrinkTime += Time.deltaTime;
            float t = shrinkTime / shrinkDuration;

            float ease = t * t;

            cover.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, ease);

            yield return null;
        }

        cover.transform.localScale = Vector3.zero;

        cover.SetActive(false);
    }

    public void activateButton(bool _status)
    {
        isDisabled = _status;
    }

    public void ShowPin()
    {
        if (pinMap != null)
            pinMap.SetActive(true);
    }

    public void HidePin()
    {
        if (pinMap != null)
            pinMap.SetActive(false);
    }
    
}