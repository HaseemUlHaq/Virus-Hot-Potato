using UnityEngine;

/// <summary>
/// Wire a tangible / Interaction SDK event to the networked virus pulse RPC.
/// Assign the virus reference or leave empty to resolve at runtime.
/// </summary>
public class PulseTangibleInteractor : MonoBehaviour
{
    [SerializeField] private NetworkGrabbableVirus virus;

    public void OnPulseTangibleActivated()
    {
        if (virus != null)
        {
            virus.RequestTogglePulseFromTangible();
            return;
        }

        foreach (var v in FindObjectsByType<NetworkGrabbableVirus>(FindObjectsSortMode.None))
        {
            if (v != null)
                v.RequestTogglePulseFromTangible();
        }
    }
}
