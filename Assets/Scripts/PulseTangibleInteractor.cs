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
        if (virus == null)
            virus = FindFirstObjectByType<NetworkGrabbableVirus>();
        if (virus != null)
            virus.RequestTogglePulseFromTangible();
    }
}
