using Fusion;
using Fusion.Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shared Fusion runner setup for PC spectator session start.
/// </summary>
public static class SpectatorFusionUtil
{
    public static void EnsureRunnerComponents(NetworkRunner runner)
    {
        if (runner == null)
            return;

        if (runner.GetComponent<INetworkSceneManager>() == null)
            runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        if (runner.GetComponent<INetworkObjectProvider>() == null)
            runner.gameObject.AddComponent<NetworkObjectProviderDefault>();
    }

    public static StartGameArgs CreateStartGameArgs(NetworkRunner runner, string sessionName)
    {
        EnsureRunnerComponents(runner);

        var appSettings = new FusionAppSettings();
        PhotonAppSettings.Global.AppSettings.CopyTo(appSettings);

        var sceneManager = runner.GetComponent<INetworkSceneManager>();
        var sceneInfo = new NetworkSceneInfo();
        if (sceneManager is NetworkSceneManagerDefault defaultSceneManager)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneRef sceneRef = defaultSceneManager.GetSceneRef(activeScene.path);
            if (sceneRef.IsValid)
                sceneInfo.AddSceneRef(sceneRef);
        }

        return new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
            CustomLobbyName = SpectatorSessionConfig.CustomLobbyName,
            PlayerCount = SpectatorSessionConfig.MaxPeers,
            CustomPhotonAppSettings = appSettings,
            SceneManager = sceneManager,
            Scene = sceneInfo,
        };
    }
}
