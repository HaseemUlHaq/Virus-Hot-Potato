using Fusion;
using TMPro;
using UnityEngine;
using LogType = UnityEngine.LogType;
using System.Collections.Generic;
using System.Text;

public class VirusDebugSceneManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private TMP_Text logText;

    [Header("Virus Debug")]
    [SerializeField] private NetworkGrabbableVirus virus;
    [SerializeField] private float virusSearchIntervalSeconds = 1f;

    [Header("Power roles")]
    [SerializeField] private PowerRoleSession powerRoleSession;
    [SerializeField] private float powerSessionSearchIntervalSeconds = 1f;

    [Header("Log Settings")]
    [SerializeField] private bool captureDebugLogs = true;
    [SerializeField] private int maxLogLines = 10;
    [SerializeField] private bool showLogTimestamp = true;

    private float _nextSearchTime;
    private float _nextPowerSessionSearchTime;
    private readonly Queue<string> _logQueue = new Queue<string>();

    private void Awake()
    {
        EnsureDebugCanvasEnabled();
    }

    private void OnEnable()
    {
        EnsureDebugCanvasEnabled();
        if (captureDebugLogs)
            Application.logMessageReceived += HandleLog;
    }

    private void EnsureDebugCanvasEnabled()
    {
        TMP_Text anyText = debugText != null ? debugText : logText;
        if (anyText == null)
            return;

        Canvas canvas = anyText.canvas;
        if (canvas == null)
            return;

        if (!canvas.gameObject.activeSelf)
            canvas.gameObject.SetActive(true);
        if (!canvas.enabled)
            canvas.enabled = true;
    }

    private void OnDisable()
    {
        if (captureDebugLogs)
            Application.logMessageReceived -= HandleLog;
    }

    private void Update()
    {
        UpdateVirusDebug();
    }

    private void UpdateVirusDebug()
    {
        if (debugText == null)
            return;

        if (virus == null && Time.time >= _nextSearchTime)
        {
            virus = FindFirstObjectByType<NetworkGrabbableVirus>();
            _nextSearchTime = Time.time + virusSearchIntervalSeconds;
        }

        if (powerRoleSession == null && Time.time >= _nextPowerSessionSearchTime)
        {
            powerRoleSession = FindFirstObjectByType<PowerRoleSession>(FindObjectsInactive.Include);
            _nextPowerSessionSearchTime = Time.time + powerSessionSearchIntervalSeconds;
        }

        var sb = new StringBuilder(1024);

        AppendPowerRoleDebug(sb);

        if (virus == null)
        {
            sb.AppendLine("Virus: waiting for spawned virus...");
            debugText.text = sb.ToString();
            return;
        }

        NetworkRunner runner = virus.Runner;
        if (runner == null)
        {
            sb.AppendLine("Virus: runner not ready.");
            debugText.text = sb.ToString();
            return;
        }

        float remainingSeconds = virus.GetRemainingSeconds();
        string holderText = virus.DebugHasHolder ? virus.DebugCurrentHolder.ToString() : "None";
        string lastTouchedText = virus.DebugLastTouchedPlayer.ToString();
        string eliminatedText = virus.DebugHasElimination ? virus.EliminatedPlayer.ToString() : "None";
        string authorityText = virus.HasStateAuthority ? "Yes" : "No";

        sb.AppendLine("--- Virus ---");
        sb.AppendLine($"Fuse Started: {virus.DebugFuseStarted} | Remaining: {remainingSeconds:0.00}s");
        sb.AppendLine($"Current Holder: {holderText} | Last Touched: {lastTouchedText}");
        sb.AppendLine($"Round Resolved: {virus.DebugRoundResolved}");
        sb.AppendLine($"Eliminated Player: {eliminatedText}");
        sb.AppendLine($"State Authority: {authorityText} | Local Player: {runner.LocalPlayer}");
        sb.AppendLine($"Material idx: {virus.MaterialIndex} | Scale: {virus.VirusScale:0.00} | Pulse: {virus.IsPulsating}");
        if (virus.TryGetComponent(out GrabFreeTransformerNetworkBridge bridge) &&
            bridge.TryGetComponent(out Oculus.Interaction.GrabFreeTransformer gft))
            sb.AppendLine($"GrabFreeTransformer.enabled (local): {gft.enabled}");

        debugText.text = sb.ToString();
    }

    private void AppendPowerRoleDebug(StringBuilder sb)
    {
        sb.AppendLine("--- Power roles ---");
        if (powerRoleSession == null)
        {
            sb.AppendLine("Session: not found (waiting…)");
            sb.AppendLine("Hint: master spawns PowerRoleSession; assign prefab on VirusSpawner.");
            sb.AppendLine();
            return;
        }

        if (!powerRoleSession.Object.IsValid)
        {
            sb.AppendLine("Session: invalid NetworkObject");
            sb.AppendLine();
            return;
        }

        NetworkRunner runner = powerRoleSession.Runner;
        if (runner == null)
        {
            sb.AppendLine("Session: no runner");
            sb.AppendLine();
            return;
        }

        var lp = runner.LocalPlayer;
        sb.AppendLine($"Current PlayerId: {lp.PlayerId}  (LocalPlayer: {lp})");
        sb.AppendLine($"Session OK | StateAuth: {powerRoleSession.Object.HasStateAuthority} | ActivePlayers: {powerRoleSession.ActivePlayerCount}");
        sb.AppendLine($"debugAllowAllPowers: {powerRoleSession.DebugAllowAllPowersWhenUnassigned}");
        sb.AppendLine($"Pulse slot: {FormatPlayer(powerRoleSession.PulsePowerPlayer)} | connected: {powerRoleSession.IsPlayerStillConnected(powerRoleSession.PulsePowerPlayer)}");
        sb.AppendLine($"Shape slot: {FormatPlayer(powerRoleSession.ShapeVariantPlayer)} | connected: {powerRoleSession.IsPlayerStillConnected(powerRoleSession.ShapeVariantPlayer)}");
        sb.AppendLine($"Color slot: {FormatPlayer(powerRoleSession.ColorPowerPlayer)} | connected: {powerRoleSession.IsPlayerStillConnected(powerRoleSession.ColorPowerPlayer)}");
        sb.AppendLine($"Scale slot: {FormatPlayer(powerRoleSession.ScalePowerPlayer)} | connected: {powerRoleSession.IsPlayerStillConnected(powerRoleSession.ScalePowerPlayer)}");

        bool canColor = powerRoleSession.IsColorPlayer(lp);
        bool canScale = powerRoleSession.IsScalePlayer(lp);
        bool canPulse = powerRoleSession.IsPulsePlayer(lp);
        sb.AppendLine($"Local {lp} | canColor: {canColor} | canScale: {canScale} | canPulse: {canPulse}");

        if (powerRoleSession.TryGetLocalPowerKind(out char k))
            sb.AppendLine($"Local power label: {(k == '*' ? "ALL (debug)" : k.ToString())}");
        else
        {
            sb.AppendLine("Local power: none");
            if (powerRoleSession.ColorPowerPlayer != PlayerRef.None &&
                powerRoleSession.ScalePowerPlayer != PlayerRef.None &&
                powerRoleSession.PulsePowerPlayer != PlayerRef.None &&
                powerRoleSession.ColorPowerPlayer != lp &&
                powerRoleSession.ScalePowerPlayer != lp &&
                powerRoleSession.PulsePowerPlayer != lp)
                sb.AppendLine("Hint: 4th+ joiner — no slot for you.");
        }

        sb.AppendLine();
    }

    private static string FormatPlayer(PlayerRef p)
    {
        if (p == PlayerRef.None)
            return "None";
        return $"{p} (id {p.PlayerId})";
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (logText == null)
            return;

        string prefix = showLogTimestamp ? $"[{Time.time:F1}] " : "";

        string coloredLog = type switch
        {
            LogType.Error => $"<color=red>{prefix}ERROR: {logString}</color>",
            LogType.Warning => $"<color=yellow>{prefix}WARN: {logString}</color>",
            LogType.Assert => $"<color=orange>{prefix}ASSERT: {logString}</color>",
            LogType.Exception => $"<color=red>{prefix}EXCEPTION: {logString}</color>",
            _ => $"<color=white>{prefix}{logString}</color>"
        };

        _logQueue.Enqueue(coloredLog);

        while (_logQueue.Count > maxLogLines)
            _logQueue.Dequeue();

        logText.text = string.Join("\n", _logQueue);
    }
}
