using Fusion;
using TMPro;
using UnityEngine;
using LogType = UnityEngine.LogType;
using System.Collections.Generic;

public class VirusDebugSceneManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private TMP_Text logText; // NEW: For Debug.Log messages

    [Header("Virus Debug")]
    [SerializeField] private NetworkGrabbableVirus virus;
    [SerializeField] private float virusSearchIntervalSeconds = 1f;

    [Header("Log Settings")]
    [SerializeField] private bool captureDebugLogs = true;
    [SerializeField] private int maxLogLines = 10;
    [SerializeField] private bool showLogTimestamp = true;

    private float _nextSearchTime;
    private Queue<string> _logQueue = new Queue<string>();

    private void OnEnable()
    {
        if (captureDebugLogs)
            Application.logMessageReceived += HandleLog;
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

        if (virus == null)
        {
            debugText.text = "Virus Debug\nWaiting for spawned virus...";
            return;
        }

        NetworkRunner runner = virus.Runner;
        if (runner == null)
        {
            debugText.text = "Virus Debug\nVirus found, waiting for runner...";
            return;
        }

        float remainingSeconds = virus.GetRemainingSeconds();
        string holderText = virus.DebugHasHolder ? virus.DebugCurrentHolder.ToString() : "None";
        string lastTouchedText = virus.DebugLastTouchedPlayer.ToString();
        string eliminatedText = virus.DebugHasElimination ? virus.EliminatedPlayer.ToString() : "None";
        string authorityText = virus.HasStateAuthority ? "Yes" : "No";

        debugText.text =
            $"Fuse Started: {virus.DebugFuseStarted} | Remaining: {remainingSeconds:0.00}s\n" +
            $"Current Holder: {holderText} | Last Touched: {lastTouchedText}\n" +
            $"Round Resolved: {virus.DebugRoundResolved}\n" +
            $"Eliminated Player: {eliminatedText}\n" +
            $"State Authority: {authorityText} | Local Player: {runner.LocalPlayer}";
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