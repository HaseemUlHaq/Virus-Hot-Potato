#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-time setup: nests Virus 3 (1) under Virus_Tindra_Copy and disables the duplicate scene instance.
/// Menu: Virus Hot Potato → Setup Virus_Tindra_Copy (Virus 3 visual)
/// </summary>
public static class VirusTindraCopyVirus3Setup
{
    private const string VirusPrefabPath = "Assets/Prefabs/Virus_Tindra_Copy.prefab";
    private const string VisualPrefabPath = "Assets/Viruses_FREE/Prefabs/Virus 3 (1).prefab";
    private const string SpatialAnchorsScenePath = "Assets/Scenes/Spatial-Anchors.unity";
    private const string VisualChildName = "Virus3_Visual";

    [MenuItem("Virus Hot Potato/Setup Virus_Tindra_Copy (Virus 3 visual)")]
    public static void SetupFromMenu()
    {
        SetupVirusPrefab();
        DisableScenePlacedVirusInstance();
        AssetDatabase.SaveAssets();
        Debug.Log("[VirusTindraCopyVirus3Setup] Done. Virus_Tindra_Copy uses Virus 3 (1) visual; scene-placed copy disabled.");
    }

    /// <summary>Called from Unity batchmode: -executeMethod VirusTindraCopyVirus3Setup.ExecuteBatch</summary>
    public static void ExecuteBatch()
    {
        SetupFromMenu();
        EditorApplication.Exit(0);
    }

    private static void SetupVirusPrefab()
    {
        var visualSource = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);
        if (visualSource == null)
        {
            Debug.LogError($"[VirusTindraCopyVirus3Setup] Missing visual prefab: {VisualPrefabPath}");
            return;
        }

        using var editScope = new PrefabUtility.EditPrefabContentsScope(VirusPrefabPath);
        GameObject root = editScope.prefabContentsRoot;

        DisableRootMeshVisual(root);
        ReplaceVisualChild(root, visualSource);
        TunePhysics(root);
        ConfigureSwipeCycler(root);

        Debug.Log("[VirusTindraCopyVirus3Setup] Updated Virus_Tindra_Copy prefab.");
    }

    private static void DisableRootMeshVisual(GameObject root)
    {
        if (root.TryGetComponent(out MeshRenderer meshRenderer))
            meshRenderer.enabled = false;

        if (root.TryGetComponent(out MeshFilter meshFilter))
            meshFilter.sharedMesh = null;
    }

    private static void ReplaceVisualChild(GameObject root, GameObject visualSource)
    {
        Transform existing = root.transform.Find(VisualChildName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var visual = (GameObject)PrefabUtility.InstantiatePrefab(visualSource, root.transform);
        visual.name = VisualChildName;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
    }

    private static void TunePhysics(GameObject root)
    {
        if (!root.TryGetComponent(out SphereCollider sphere))
            return;

        sphere.radius = 0.045f;
        sphere.center = Vector3.zero;
    }

    private static void ConfigureSwipeCycler(GameObject root)
    {
        if (!root.TryGetComponent(out VirusSwipeCycler cycler))
            return;

        var serialized = new SerializedObject(cycler);
        serialized.FindProperty("targetRenderer").objectReferenceValue = null;
        serialized.FindProperty("applyToChildRenderers").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void DisableScenePlacedVirusInstance()
    {
        Scene scene = EditorSceneManager.OpenScene(SpatialAnchorsScenePath, OpenSceneMode.Single);
        bool changed = false;

        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(rootObject))
                continue;

            Object source = PrefabUtility.GetCorrespondingObjectFromSource(rootObject);
            if (source == null || source.name != "Virus_Tindra_Copy")
                continue;

            if (rootObject.activeSelf)
            {
                rootObject.SetActive(false);
                changed = true;
                Debug.Log($"[VirusTindraCopyVirus3Setup] Disabled scene instance '{rootObject.name}' (spawner owns the networked virus).");
            }
        }

        if (changed)
            EditorSceneManager.SaveScene(scene);
    }
}
#endif
