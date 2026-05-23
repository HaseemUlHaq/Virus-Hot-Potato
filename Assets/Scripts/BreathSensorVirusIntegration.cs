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

        // Gate: only the pulse player can trigger
        if (_powerRoleSession == null)
            _powerRoleSession = PowerRoleSession.Instance
                ?? FindFirstObjectByType<PowerRoleSession>(FindObjectsInactive.Include);

        bool debugAll = _powerRoleSession != null && _powerRoleSession.DebugAllowAllPowersWhenUnassigned;
        if (!debugAll && (_powerRoleSession == null || !_powerRoleSession.IsPulsePlayer(_runner.LocalPlayer)))
        {
            Debug.Log("[BreathIntegration] BLOW received but local player does not have Pulse role — ignored.");
            return;
        }

        if (targetDish == null)
        {
            Debug.LogWarning("[BreathIntegration] No target dish assigned!");
            return;
        }

        if (!targetDish.IsOccupied || targetDish.SnappedVirus == null)
        {
            Debug.LogWarning("[BreathIntegration] BLOW received but no virus in the target dish.");
            return;
        }

        Debug.Log($"[BreathIntegration] ✓ BLOW → {targetDish.SnappedVirus.name}");
        targetDish.SnappedVirus.RequestSetPulsatingOn();
    }
}
