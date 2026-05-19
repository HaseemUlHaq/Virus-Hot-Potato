using UnityEngine;

public class BreathSensorVirusIntegration : MonoBehaviour
{
    private NetworkGrabbableVirus _virus;

    void Start()
    {
        _virus = FindFirstObjectByType<NetworkGrabbableVirus>();
        if (_virus == null)
            Debug.LogWarning("[BreathIntegration] NetworkGrabbableVirus not found at Start - will retry on blow.");
        else
            Debug.Log("[BreathIntegration] ✓ Found NetworkGrabbableVirus");
    }

    void Update()
    {
        if (!BreathSensorHandler.triggerBlow)
            return;

        BreathSensorHandler.triggerBlow = false;

        if (_virus == null)
            _virus = FindFirstObjectByType<NetworkGrabbableVirus>();

        if (_virus != null)
        {
            Debug.Log("[BreathIntegration] ✓ BLOW - calling RequestSpikeBurstFromTangible");
            _virus.RequestSpikeBurstFromTangible();
        }
        else
        {
            Debug.LogWarning("[BreathIntegration] BLOW received but virus not found!");
        }
    }
}
