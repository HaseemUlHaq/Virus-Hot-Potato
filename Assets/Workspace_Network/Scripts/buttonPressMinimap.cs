using UnityEngine;
using TMPro;
using System.Collections;

public class buttonPressMinimap : MonoBehaviour
{
    public float pressDistance = 0.02f;
    public float recoilDuration = 0.2f;

    public TextMeshProUGUI debugText;

    private Vector3 startLocalPos;
    private bool isPressed = false;

    void Start()
    {
        // Store the ORIGINAL LOCAL position of the parent
        startLocalPos = transform.localPosition;
    }
    
    public void OnButtonPressed()
    {
        if (isPressed) return;

        isPressed = true;

        if (debugText != null)
            debugText.text = "Pressed: " + pressDistance;

        // Move the parent along LOCAL Z (your button press direction)
        transform.localPosition = startLocalPos + new Vector3(0, -pressDistance, 0);
    }

    public void OnButtonReleased()
    {
        if (!isPressed) return;

        isPressed = false;

        if (debugText != null)
            debugText.text = "Released";

        StopAllCoroutines();
        StartCoroutine(Recoil());
    }

    IEnumerator Recoil()
    {
        float t = 0f;
        Vector3 from = transform.localPosition;

        while (t < recoilDuration)
        {
            t += Time.deltaTime;

            float normalized = t / recoilDuration;

            // Smooth ease-out
            float eased = 1f - Mathf.Pow(1f - normalized, 3f);

            transform.localPosition = Vector3.Lerp(from, startLocalPos, eased);

            yield return null;
        }

        transform.localPosition = startLocalPos;
    }
}