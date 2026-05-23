#if VIRUS_SPECTATOR
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PC spectator: hold R to request the same round reset as Quest player 1 left-hand pinch.
/// Quest pinch behaviour is unchanged; this is an additional facilitator control only.
/// </summary>
public class SpectatorRoundResetInput : MonoBehaviour
{
    [SerializeField] private float holdDurationSeconds = 2.05f;
    [SerializeField] private float resetCooldownSeconds = 3f;

    private NetworkRunner _runner;
    private NetworkedTableAnchor _tableAnchor;
    private PowerRoleSession _powerRoleSession;
    private float _holdTimer;
    private float _cooldownTimer;

    private void Update()
    {
        RefreshReferences();

        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
            return;
        }

        if (!CanRequestReset())
        {
            _holdTimer = 0f;
            return;
        }

        if (Keyboard.current == null || !Keyboard.current.rKey.isPressed)
        {
            _holdTimer = 0f;
            return;
        }

        _holdTimer += Time.deltaTime;
        if (_holdTimer < holdDurationSeconds)
            return;

        _holdTimer = 0f;
        _cooldownTimer = resetCooldownSeconds;
        _tableAnchor.RequestSpectatorRoundReset();
        Debug.Log($"[SpectatorRoundReset] Round reset requested (held R {holdDurationSeconds:F2}s)");
    }

    private void RefreshReferences()
    {
        if (_runner == null || !_runner.IsRunning)
            _runner = FindFirstObjectByType<NetworkRunner>();

        if (_tableAnchor == null)
            _tableAnchor = FindFirstObjectByType<NetworkedTableAnchor>(FindObjectsInactive.Include);

        if (_powerRoleSession == null)
            _powerRoleSession = PowerRoleSession.Instance;
    }

    private bool CanRequestReset()
    {
        if (_runner == null || !_runner.IsRunning)
            return false;

        if (_tableAnchor == null || !SpectatorTableAnchorQueries.IsNetworkSpawned(_tableAnchor))
            return false;

        if (_powerRoleSession == null || !_powerRoleSession.Object.IsValid)
            return false;

        if (!_powerRoleSession.IsSpectator(_runner.LocalPlayer))
            return false;

        return _tableAnchor.CanLocalSpectatorRequestRoundReset();
    }
}
#endif
