using UnityEngine;

// Attach to any world-space UI or label that should always face the player camera.
public class BillboardUI : MonoBehaviour
{
    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
