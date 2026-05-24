using Fusion;
using UnityEngine;

// Breath / button UDP → pulse only the virus snapped in the assigned petri dish (PetriDish_p3).
// Any connected player may trigger; no PowerRoleSession / PlayerId check.
public class BreathSensorVirusIntegration : MonoBehaviour
{
    [SerializeField] private PetriDish targetDish;

    private NetworkRunner _runner;

    void Start()
    {
        _runner = FindFirstObjectByType<NetworkRunner>();
        if (_runner == null)
            Debug.LogWarning("[BreathIntegration] NetworkRunner not found at Start - will retry on blow.");
        else
            LogLocalPlayerId("Start");
    }

    private void LogLocalPlayerId(string context)
    {
        if (_runner == null)
            _runner = FindFirstObjectByType<NetworkRunner>();

        if (_runner == null)
        {
            Debug.Log($"[BreathIntegration] ({context}) Current PlayerId: n/a (no NetworkRunner)");
            return;
        }

        Debug.Log($"[BreathIntegration] ({context}) Current PlayerId: {_runner.LocalPlayer.PlayerId}");
    }

    void Update()
    {
        if (BreathSensorHandler.triggerBlow)
        {
            BreathSensorHandler.triggerBlow = false;
            LogLocalPlayerId("BLOW");
            Debug.Log($"[BreathIntegration] Processing BLOW (target dish: {(targetDish != null ? targetDish.name : "none")})");
            if (TryGetTargetVirus(out NetworkGrabbableVirus virus, "BLOW"))
            {
                Debug.Log($"[BreathIntegration] ✓ BLOW → {virus.name}");
                virus.RequestSetPulsatingOn();
            }
        }

        if (BreathSensorHandler.triggerButtonPressed)
        {
            BreathSensorHandler.triggerButtonPressed = false;
            LogLocalPlayerId("BUTTON_PRESSED");
            Debug.Log($"[BreathIntegration] Processing BUTTON_PRESSED (target dish: {(targetDish != null ? targetDish.name : "none")})");
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

        if (targetDish == null)
        {
            Debug.LogWarning("[BreathIntegration] No target dish assigned!");
            return false;
        }

        if (!targetDish.IsOccupied || targetDish.SnappedVirus == null)
        {
            Debug.LogWarning($"[BreathIntegration] {eventLabel} — no virus snapped in {targetDish.name}.");
            return false;
        }

        virus = targetDish.SnappedVirus;
        return true;
    }
}
