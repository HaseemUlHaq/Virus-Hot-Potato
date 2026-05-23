using Fusion;
using UnityEngine;

public class BreathSensorVirusIntegration : MonoBehaviour
{
    private NetworkRunner _runner;
    private PowerRoleSession _powerRoleSession;
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
        if (_runner == null)
        {
            _runner = FindFirstObjectByType<NetworkRunner>();
            return;
        }

        // Keep _lastHeldVirus up to date every frame
        foreach (var v in FindObjectsByType<NetworkGrabbableVirus>(FindObjectsSortMode.None))
        {
            if (v.CurrentHolder == _runner.LocalPlayer)
            {
                _lastHeldVirus = v;
                break;
            }
        }

        if (!BreathSensorHandler.triggerBlow)
            return;

        BreathSensorHandler.triggerBlow = false;

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

        NetworkGrabbableVirus target = _lastHeldVirus;

        if (target != null)
        {
            Debug.Log($"[BreathIntegration] ✓ BLOW → {target.name}");
            target.RequestSetPulsatingOn();
        }
        else
        {
            Debug.LogWarning("[BreathIntegration] BLOW received but no virus held or previously held by local player!");
        }
    }
}
