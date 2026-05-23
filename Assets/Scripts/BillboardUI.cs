using UnityEngine;

// Faces the nearest player each frame.
// Checks Camera.main (local player) + all objects tagged "PlayerHead" (networked remote heads).
// Tag your Networked-PlayerHead prefab as "PlayerHead" in Unity for this to work with multiple players.
public class BillboardUI : MonoBehaviour
{
    private void LateUpdate()
    {
        Vector3 nearestPos;
        if (!TryGetNearestPlayerPosition(out nearestPos)) return;

        transform.rotation = Quaternion.LookRotation(transform.position - nearestPos);
    }

    private bool TryGetNearestPlayerPosition(out Vector3 position)
    {
        position = Vector3.zero;
        float bestDist = float.MaxValue;
        bool found = false;

        // Local player
        Camera cam = Camera.main;
        if (cam != null)
        {
            float d = Vector3.Distance(transform.position, cam.transform.position);
            if (d < bestDist) { bestDist = d; position = cam.transform.position; found = true; }
        }

        // Remote networked heads
        foreach (var go in GameObject.FindGameObjectsWithTag("PlayerHead"))
        {
            float d = Vector3.Distance(transform.position, go.transform.position);
            if (d < bestDist) { bestDist = d; position = go.transform.position; found = true; }
        }

        return found;
    }
}
