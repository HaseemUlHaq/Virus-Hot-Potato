using Fusion;
using UnityEngine;

// Triggers persistent spike displacement on the virus sitting in the assigned petri dish.
// Assign the specific dish in the Inspector — only that dish's virus will be pulsed on blow.
public class BreathSensorVirusIntegration : MonoBehaviour
{
    [SerializeField] private PetriDish targetDish;

    private NetworkRunner _runner;
    private PowerRoleSession _powerRoleSession;

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
        if (BreathSensorHandler.triggerBlow)
        {
            BreathSensorHandler.triggerBlow = false;
            if (TryGetTargetVirus(out NetworkGrabbableVirus virus, "BLOW"))
            {
                Debug.Log($"[BreathIntegration] ✓ BLOW → {virus.name}");
                virus.RequestSetPulsatingOn();
            }
        }

        if (BreathSensorHandler.triggerButtonPressed)
        {
            BreathSensorHandler.triggerButtonPressed = false;
            if (TryGetTargetVirus(out NetworkGrabbableVirus virus, "BUTTON_PRESSED"))
            {
                Debug.Log($"[BreathIntegration] ✓ BUTTON_PRESSED → stop pulse on {virus.name}");
                virus.RequestSetPulsatingOff();
            }
        }
    }

    private bool TryGetTargetVirus(out NetworkGrabbableVirus virus, string eventLabel)
    {
        virus = null;

        if (_runner == null)
            _runner = FindFirstObjectByType<NetworkRunner>();

        if (_runner == null)
        {
            Debug.LogWarning($"[BreathIntegration] {eventLabel} received but NetworkRunner not found!");
            return false;
        }

        if (_powerRoleSession == null)
            _powerRoleSession = PowerRoleSession.Instance
                ?? FindFirstObjectByType<PowerRoleSession>(FindObjectsInactive.Include);

        bool debugAll = _powerRoleSession != null && _powerRoleSession.DebugAllowAllPowersWhenUnassigned;
        if (!debugAll && (_powerRoleSession == null || !_powerRoleSession.IsPulsePlayer(_runner.LocalPlayer)))
        {
            Debug.Log($"[BreathIntegration] {eventLabel} received but local player does not have Pulse role — ignored.");
            return false;
        }

        if (targetDish == null)
        {
            Debug.LogWarning("[BreathIntegration] No target dish assigned!");
            return false;
        }

        if (!targetDish.IsOccupied || targetDish.SnappedVirus == null)
        {
            Debug.LogWarning($"[BreathIntegration] {eventLabel} received but no virus in the target dish.");
            return false;
        }

        virus = targetDish.SnappedVirus;
        return true;
    }
}
