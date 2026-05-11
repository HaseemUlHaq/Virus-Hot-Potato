using UnityEngine;
using Meta.XR.MRUtilityKit;

public class PuckTracker : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private VirusSpawner virusSpawner;

    [Header("Height above puck surface")]
    [SerializeField] private float virusHeightOffset = 0.08f;

    private bool _positionSent = false;

    // Wire this to MRUK Trackable Added event in Inspector
    public void OnTrackableAdded(MRUKTrackable trackable)
    {
        // Only QR codes
        if (trackable.TrackableType != OVRAnchor.TrackableType.QRCode)
            return;

        // Only our specific puck
        if (trackable.MarkerPayloadString != "virus_puck_1")
            return;

        // Only once per round
        if (_positionSent)
            return;

        Vector3 spawnPos;

        if (TableAnchor.TableFound)
        {
            // Use QR horizontal position but table Y
            // Guarantees virus is on the table regardless of scan angle
            spawnPos = new Vector3(
                trackable.transform.position.x,
                TableAnchor.TableSurfacePosition.y + virusHeightOffset,
                trackable.transform.position.z
            );
        }
        else
        {
            // Fallback — table not found yet
            spawnPos = trackable.transform.position + Vector3.up * virusHeightOffset;
        }

        UnityEngine.Debug.Log("QR detected — spawn at: " + spawnPos);
        virusSpawner.SetSpawnPosition(spawnPos);
        _positionSent = true;
    }

    // Wire this to MRUK Trackable Removed event in Inspector
    public void OnTrackableRemoved(MRUKTrackable trackable)
    {
        // Do nothing — virus is already spawned and free
    }

    // Call this at start of each new round
    public void ResetForNewRound()
    {
        _positionSent = false;
        virusSpawner.ResetForNewRound();
    }
}