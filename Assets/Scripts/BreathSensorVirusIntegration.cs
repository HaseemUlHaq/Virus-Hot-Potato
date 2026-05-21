using Fusion;
using UnityEngine;

public class BreathSensorVirusIntegration : MonoBehaviour
{
    private NetworkRunner _runner;
    private NetworkGrabbableVirus _lastHeldVirus;

    void Start()
    {
        _runner = FindFirstObjectByType<NetworkRunner>();
        if (_runner == null)
            Debug.LogWarning("[BreathIntegration] NetworkRunner not found at Start - will retry on blow.");
        else
            Debug.Log("[BreathIntegration] ✓ Found NetworkRunner");
    }

    void Update()
    {
        if (!BreathSensorHandler.triggerBlow)
            return;

        BreathSensorHandler.triggerBlow = false;

        if (_runner == null)
            _runner = FindFirstObjectByType<NetworkRunner>();

        if (_runner == null)
        {
            Debug.LogWarning("[BreathIntegration] BLOW received but NetworkRunner not found!");
            return;
        }

        // Find the virus currently held by this local player
        NetworkGrabbableVirus target = null;
        foreach (var v in FindObjectsByType<NetworkGrabbableVirus>(FindObjectsSortMode.None))
        {
            if (v.CurrentHolder == _runner.LocalPlayer)
            {
                target = v;
                _lastHeldVirus = v;
                break;
            }
        }

        // Fallback: pulse the last virus this player held
        if (target == null)
            target = _lastHeldVirus;

        if (target != null)
        {
            Debug.Log($"[BreathIntegration] ✓ BLOW → {target.name} (held:{target.CurrentHolder == _runner.LocalPlayer})");
            target.RequestSpikeBurstFromTangible();
        }
        else
        {
            Debug.LogWarning("[BreathIntegration] BLOW received but no virus held or previously held by local player!");
        }
    }
}
