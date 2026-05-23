#if VIRUS_SPECTATOR
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Windows spectator build: disables Quest MR / matchmaking / local gameplay and enables a desktop camera rig.
/// </summary>
[DefaultExecutionOrder(-500)]
public class SpectatorPlatformBootstrap : MonoBehaviour
{
    private static readonly string[] DisableObjectNames =
    {
        "[BuildingBlock] Colocation",
        "[BuildingBlock] MR Utility Kit",
        "[BuildingBlock] Custom Matchmaking",
        "[BuildingBlock] Local Matchmaking",
        "TableAnchorManager",
        "SessionStatusHUD",
        "OVRCameraRig",
        "XR Origin",
    };

#if !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateIfNeeded()
    {
        if (FindFirstObjectByType<SpectatorPlatformBootstrap>() != null)
            return;

        var root = new GameObject("SpectatorRoot");
        root.AddComponent<SpectatorPlatformBootstrap>();
        root.AddComponent<SpectatorNetworkBootstrap>();
        root.AddComponent<SpectatorStatusHUD>();
    }
#endif

    private void Awake()
    {
        SpectatorSession.LocalIsSpectator = true;

        foreach (string objectName in DisableObjectNames)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null)
                target.SetActive(false);
        }

        DisablePlayerSpawner();
        DisableRoundResetGesture();
        DisableXrCamerasAndListeners();
        EnsureSpectatorCamera();
    }

    private static void DisablePlayerSpawner()
    {
        PlayerSpawner spawner = FindFirstObjectByType<PlayerSpawner>(FindObjectsInactive.Include);
        if (spawner != null)
            spawner.enabled = false;
    }

    private static void DisableRoundResetGesture()
    {
        LeftHandRoundResetGesture gesture = FindFirstObjectByType<LeftHandRoundResetGesture>(FindObjectsInactive.Include);
        if (gesture != null)
            gesture.enabled = false;
    }

    private static void DisableXrCamerasAndListeners()
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera cam in cameras)
        {
            if (cam == null)
                continue;
            cam.enabled = false;
            AudioListener listener = cam.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = false;
        }

        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem != null)
            eventSystem.gameObject.SetActive(false);
    }

    private static void EnsureSpectatorCamera()
    {
        var cameraGo = new GameObject("SpectatorCamera");
        Camera camera = cameraGo.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 200f;
        cameraGo.AddComponent<AudioListener>();
        cameraGo.AddComponent<SpectatorFlyCamera>();

        cameraGo.transform.position = new Vector3(0f, 1.6f, -2.5f);
        cameraGo.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
    }
}
#endif
