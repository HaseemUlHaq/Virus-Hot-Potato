/// <summary>
/// Shared session settings for Quest gameplay and the Windows PC spectator client.
/// Custom lobby must match Meta Custom Matchmaking <c>lobbyName</c> on Network (e.g. virus5).
/// </summary>
public static class SpectatorSessionConfig
{
    /// <summary>Photon custom lobby — must match Quest Network building block lobbyName.</summary>
    public const string CustomLobbyName = "virus5";

    public const int MaxPeers = 5;

    /// <summary>PlayerPrefs override: join this Fusion session name directly (skips session-list pick).</summary>
    public const string SessionNamePlayerPrefsKey = "SpectatorSessionName";

    public static bool TryGetSessionNameOverride(out string sessionName)
    {
        sessionName = UnityEngine.PlayerPrefs.GetString(SessionNamePlayerPrefsKey, string.Empty).Trim();
        return !string.IsNullOrWhiteSpace(sessionName);
    }
}
