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
    public static float HoldProgress { get; private set; }
    public static string BlockReason { get; private set; } = string.Empty;

    [SerializeField] private float holdDurationSeconds = 2.05f;
    [SerializeField] private float resetCooldownSeconds = 3f;
    [SerializeField] private float blockedLogIntervalSeconds = 2f;

    private NetworkRunner _runner;
    private NetworkedTableAnchor _tableAnchor;
    private PowerRoleSession _powerRoleSession;
    private EndGameBottleReveal _endGameBottleReveal;
    private float _holdTimer;
    private float _cooldownTimer;
    private float _lastBlockedLogTime;

    private void Update()
    {
        RefreshReferences();

        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
            HoldProgress = 0f;
            return;
        }

        string blockReason = GetBlockReason();
        BlockReason = blockReason;

        if (!string.IsNullOrEmpty(blockReason))
        {
            HoldProgress = 0f;
            _holdTimer = 0f;
            LogBlockedPeriodically(blockReason);
            return;
        }

        if (Keyboard.current == null || !Keyboard.current.rKey.isPressed)
        {
            HoldProgress = 0f;
            _holdTimer = 0f;
            return;
        }

        _holdTimer += Time.deltaTime;
        HoldProgress = Mathf.Clamp01(_holdTimer / holdDurationSeconds);
        if (_holdTimer < holdDurationSeconds)
            return;

        _holdTimer = 0f;
        HoldProgress = 0f;
        _cooldownTimer = resetCooldownSeconds;

        _powerRoleSession?.RPC_RegisterSpectator();
        if (_endGameBottleReveal == null)
            _endGameBottleReveal = FindFirstObjectByType<EndGameBottleReveal>(FindObjectsInactive.Include);
        _endGameBottleReveal?.ResetForNewRound();
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

        if (_endGameBottleReveal == null)
            _endGameBottleReveal = FindFirstObjectByType<EndGameBottleReveal>(FindObjectsInactive.Include);
    }

    private string GetBlockReason()
    {
        if (!SpectatorSession.LocalIsSpectator)
            return "Not spectator client";

        if (_runner == null || !_runner.IsRunning)
            return "Not connected to Fusion";

        if (_tableAnchor == null)
            return "No NetworkedTableAnchor in scene";

        if (!SpectatorTableAnchorQueries.IsNetworkSpawned(_tableAnchor))
            return "Table not network-spawned yet";

        if (_tableAnchor != null && !_tableAnchor.CanLocalSpectatorRequestRoundReset())
            return "Round reset not allowed for local player";

        return string.Empty;
    }

    private void LogBlockedPeriodically(string reason)
    {
        if (Keyboard.current == null || !Keyboard.current.rKey.isPressed)
            return;

        if (Time.unscaledTime - _lastBlockedLogTime < blockedLogIntervalSeconds)
            return;

        _lastBlockedLogTime = Time.unscaledTime;
        Debug.LogWarning($"[SpectatorRoundReset] Hold R blocked: {reason}");
    }
}
#endif
