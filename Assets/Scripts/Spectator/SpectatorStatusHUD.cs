#if VIRUS_SPECTATOR
using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;

/// <summary>
/// Screen-space status overlay for the PC spectator client.
/// </summary>
public class SpectatorStatusHUD : MonoBehaviour
{
    private TMP_Text _statusText;
    private NetworkRunner _runner;
    private NetworkedTableAnchor _tableAnchor;
    private FormationManager _formationManager;
    private PowerRoleSession _powerRoleSession;

    private void Awake()
    {
        BuildCanvas();
    }

    private void Update()
    {
        RefreshReferences();
        RefreshText();
    }

    private void BuildCanvas()
    {
        var canvasGo = new GameObject("SpectatorHUDCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var textGo = new GameObject("StatusText");
        textGo.transform.SetParent(canvasGo.transform, false);

        var rect = textGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(16f, -16f);
        rect.sizeDelta = new Vector2(560f, 280f);

        _statusText = textGo.AddComponent<TextMeshProUGUI>();
        _statusText.fontSize = 20f;
        _statusText.color = Color.white;
        _statusText.alignment = TextAlignmentOptions.TopLeft;
        _statusText.textWrappingMode = TextWrappingModes.Normal;

        if (TMP_Settings.defaultFontAsset != null)
            _statusText.font = TMP_Settings.defaultFontAsset;
    }

    private void RefreshReferences()
    {
        if (_runner == null || !_runner.IsRunning)
            _runner = FindFirstObjectByType<NetworkRunner>();

        if (_tableAnchor == null)
            _tableAnchor = FindFirstObjectByType<NetworkedTableAnchor>(FindObjectsInactive.Include);

        if (_formationManager == null)
            _formationManager = FindFirstObjectByType<FormationManager>(FindObjectsInactive.Include);

        if (_powerRoleSession == null)
            _powerRoleSession = PowerRoleSession.Instance;
    }

    private void RefreshText()
    {
        if (_statusText == null)
            return;

        bool connected = _runner != null && _runner.IsRunning;
        int totalPlayers = connected ? _runner.ActivePlayers.Count() : 0;
        int participantCount = connected ? CountParticipants() : 0;
        string sessionLabel = connected && _runner.SessionInfo.IsValid
            ? _runner.SessionInfo.Name
            : $"(waiting — lobby {SpectatorSessionConfig.CustomLobbyName})";

        bool tableSpawned = SpectatorTableAnchorQueries.IsNetworkSpawned(_tableAnchor);
        SpectatorTableAnchorQueries.TryGetPlacedState(
            _tableAnchor, out bool tablePlaced, out int placementVersion, out _);

        bool workAreaSpawned = _formationManager != null && _formationManager.WorkAreaWasSpawned;

        string aloneHint = connected && totalPlayers <= 1
            ? $"\n(Alone in room — start Quest first; PC browses lobby '{SpectatorSessionConfig.CustomLobbyName}')"
            : string.Empty;

        _statusText.text =
            "PC Spectator\n" +
            $"Session: {sessionLabel}\n" +
            $"Connection: {(connected ? "Connected" : "Connecting…")}\n" +
            $"Players: {participantCount} playing / {totalPlayers} connected{aloneHint}\n" +
            $"Network table spawned: {(tableSpawned ? "Yes" : "No")}\n" +
            $"Table placed: {(tablePlaced ? "Yes" : "No")}\n" +
            $"Work area spawned: {(workAreaSpawned ? "Yes" : "No")}\n" +
            $"Placement version: {placementVersion}\n" +
            "\nWASD move | RMB look | Q/E up/down | Shift fast | F focus table | Esc unlock cursor" +
            "\nHold R ~2s round reset (facilitator)" +
            FormatRoundResetStatus();
    }

    private static string FormatRoundResetStatus()
    {
        if (SpectatorRoundResetInput.HoldProgress > 0f)
            return $"\nRound reset: holding R… {SpectatorRoundResetInput.HoldProgress * 100f:F0}%";

        if (!string.IsNullOrEmpty(SpectatorRoundResetInput.BlockReason))
            return $"\nRound reset (R): {SpectatorRoundResetInput.BlockReason}";

        return "\nRound reset: hold R ~2s when connected";
    }

    private int CountParticipants()
    {
        if (_runner == null)
            return 0;

        int count = 0;
        foreach (PlayerRef player in _runner.ActivePlayers)
        {
            if (player == PlayerRef.None)
                continue;
            if (_powerRoleSession != null && _powerRoleSession.IsSpectator(player))
                continue;
            count++;
        }

        return count;
    }
}
#endif
