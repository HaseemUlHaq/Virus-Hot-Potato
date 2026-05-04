using UnityEngine;

public class pinMapFloatAnim : MonoBehaviour
{
    public float amplitude = 0.005f; // smaller movement
    public float speed = 2f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float yOffset = (Mathf.Sin(Time.time * speed) + 1f) / 2f * amplitude;
        transform.position = startPos + new Vector3(0, yOffset, 0);
    }
}