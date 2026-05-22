#if UNITY_EDITOR
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds FormationRoot, a bottom rotation grab handle, and rotation bridge to PlaceHolderFormation.prefab.
/// Menu: Virus Hot Potato → Setup Placeholder Formation (grab + rotate)
/// </summary>
public static class PlaceholderFormationGrabSetup
{
    private const string PrefabPath = "Assets/Prefabs/PlaceHolderFormation.prefab";
    private const string FormationRootName = "FormationRoot";
    private const string GrabHandleName = "RotationGrabHandle";
    private const string HandleVisualName = "HandleVisual";
    private const string HandGrabRoutineName = "[BuildingBlock] HandGrabInstallationRoutine";

    [MenuItem("Virus Hot Potato/Setup Placeholder Formation (grab + rotate)")]
    public static void SetupFromMenu()
    {
        SetupPrefab();
        AssetDatabase.SaveAssets();
        Debug.Log("[PlaceholderFormationGrabSetup] Done. Bottom rotation handle configured on PlaceHolderFormation.");
    }

    public static void ExecuteBatch()
    {
        SetupFromMenu();
        EditorApplication.Exit(0);
    }

    private static void SetupPrefab()
    {
        using var scope = new PrefabUtility.EditPrefabContentsScope(PrefabPath);
        GameObject root = scope.prefabContentsRoot;

        Transform formationRoot = EnsureFormationRoot(root);
        ReparentSlotsUnderFormationRoot(root.transform, formationRoot);

        RemoveFormationGrabFromRoot(root);
        GameObject handle = EnsureRotationGrabHandle(root, formationRoot);

        WirePlaceholderFormation(root, formationRoot);
        WireGrabBridge(root, formationRoot, handle);

        Debug.Log("[PlaceholderFormationGrabSetup] Updated PlaceHolderFormation prefab.");
    }

    private static Transform EnsureFormationRoot(GameObject root)
    {
        Transform existing = root.transform.Find(FormationRootName);
        if (existing != null)
            return existing;

        var formationRootGo = new GameObject(FormationRootName);
        formationRootGo.transform.SetParent(root.transform, false);
        formationRootGo.transform.localPosition = Vector3.zero;
        formationRootGo.transform.localRotation = Quaternion.identity;
        formationRootGo.transform.localScale = Vector3.one;
        return formationRootGo.transform;
    }

    private static void ReparentSlotsUnderFormationRoot(Transform root, Transform formationRoot)
    {
        var toMove = new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == formationRoot) continue;
            if (child.name == GrabHandleName) continue;
            if (child.name.StartsWith("Slot_"))
                toMove.Add(child);
        }

        foreach (Transform slot in toMove)
            slot.SetParent(formationRoot, true);
    }

    private static void RemoveFormationGrabFromRoot(GameObject root)
    {
        foreach (var col in root.GetComponents<BoxCollider>())
            Object.DestroyImmediate(col);

        foreach (var rb in root.GetComponents<Rigidbody>())
            Object.DestroyImmediate(rb);

        foreach (var g in root.GetComponents<Grabbable>())
            Object.DestroyImmediate(g);

        foreach (var t in root.GetComponents<GrabFreeTransformer>())
            Object.DestroyImmediate(t);

        Transform handRoutine = root.transform.Find(HandGrabRoutineName);
        if (handRoutine != null)
            Object.DestroyImmediate(handRoutine.gameObject);
    }

    private static GameObject EnsureRotationGrabHandle(GameObject root, Transform formationRoot)
    {
        Transform handleTransform = root.transform.Find(GrabHandleName);
        GameObject handleGo;
        if (handleTransform == null)
        {
            handleGo = new GameObject(GrabHandleName);
            handleGo.transform.SetParent(formationRoot, false);
            handleGo.transform.localPosition = new Vector3(0f, -0.14f, 0f);
            handleGo.transform.localRotation = Quaternion.identity;
            handleGo.transform.localScale = Vector3.one;
        }
        else
        {
            handleGo = handleTransform.gameObject;
            // Child of FormationRoot so the handle spins with the formation.
            if (handleGo.transform.parent != formationRoot)
                handleGo.transform.SetParent(formationRoot, true);
        }

        EnsureHandleVisual(handleGo);
        EnsureGrabComponentsOnHandle(handleGo, formationRoot);

        return handleGo;
    }

    // Petri-disk mesh from slots; lab-virus material so the handle reads as a control, not a slot.
    private const string SlotDiskAssetGuid = "d373038051eb6dd4e914d28686e13545";
    private const string HandleMaterialPath = "Assets/Materials/FormationRotationHandle.mat";

    private static void EnsureHandleVisual(GameObject handleGo)
    {
        Transform visual = handleGo.transform.Find(HandleVisualName);
        GameObject visualGo;
        if (visual == null)
        {
            visualGo = new GameObject(HandleVisualName);
            visualGo.transform.SetParent(handleGo.transform, false);
            visualGo.AddComponent<MeshFilter>();
            visualGo.AddComponent<MeshRenderer>();
        }
        else
        {
            visualGo = visual.gameObject;
        }

        // Same disk mesh as slots; virus-lab material and slightly larger scale so it stands out.
        visualGo.transform.localPosition = Vector3.zero;
        visualGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        visualGo.transform.localScale = new Vector3(5.6f, 5.6f, 5.6f);

        Mesh diskMesh = LoadFirstAssetAtGuid<Mesh>(SlotDiskAssetGuid);
        Material handleMaterial = AssetDatabase.LoadAssetAtPath<Material>(HandleMaterialPath);

        if (visualGo.TryGetComponent(out MeshFilter meshFilter) && diskMesh != null)
            meshFilter.sharedMesh = diskMesh;

        if (visualGo.TryGetComponent(out MeshRenderer meshRenderer) && handleMaterial != null)
            meshRenderer.sharedMaterial = handleMaterial;
    }

    private static T LoadFirstAssetAtGuid<T>(string guid) where T : Object
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path))
            return null;

        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is T typed)
                return typed;
        }

        return null;
    }

    private static void EnsureGrabComponentsOnHandle(GameObject handleGo, Transform formationRoot)
    {
        // Trigger only — hand grab still works; viruses pass through to slots below.
        foreach (Collider col in handleGo.GetComponents<Collider>())
            col.isTrigger = true;

        if (!handleGo.TryGetComponent(out Collider grabCollider))
        {
            var sphere = handleGo.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.28f;
            sphere.center = Vector3.zero;
            grabCollider = sphere;
        }

        if (grabCollider is SphereCollider sphereCol)
        {
            sphereCol.radius = 0.28f;
            sphereCol.center = Vector3.zero;
        }

        if (!handleGo.TryGetComponent(out Rigidbody rb))
        {
            rb = handleGo.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        GrabFreeTransformer transformer;
        if (!handleGo.TryGetComponent(out transformer))
            transformer = handleGo.AddComponent<GrabFreeTransformer>();
        LockTransformer(transformer);

        Grabbable grabbable;
        if (!handleGo.TryGetComponent(out grabbable))
            grabbable = handleGo.AddComponent<Grabbable>();

        var grabbableSo = new SerializedObject(grabbable);
        grabbableSo.FindProperty("_oneGrabTransformer").objectReferenceValue = transformer;
        grabbableSo.FindProperty("_twoGrabTransformer").objectReferenceValue = transformer;
        grabbableSo.FindProperty("_targetTransform").objectReferenceValue = formationRoot;
        grabbableSo.FindProperty("_rigidbody").objectReferenceValue = rb;
        grabbableSo.FindProperty("_kinematicWhileSelected").boolValue = true;
        grabbableSo.FindProperty("_throwWhenUnselected").boolValue = false;
        grabbableSo.ApplyModifiedPropertiesWithoutUndo();

        EnsureHandGrabRoutine(handleGo, grabbable, rb);
    }

    private static void LockTransformer(GrabFreeTransformer transformer)
    {
        var so = new SerializedObject(transformer);

        SetAxisLocked(so.FindProperty("_positionConstraints.XAxis"), locked: true);
        SetAxisLocked(so.FindProperty("_positionConstraints.YAxis"), locked: true);
        SetAxisLocked(so.FindProperty("_positionConstraints.ZAxis"), locked: true);

        SetAxisLocked(so.FindProperty("_rotationConstraints.XAxis"), locked: true);
        SetAxisLocked(so.FindProperty("_rotationConstraints.YAxis"), locked: true);
        SetAxisLocked(so.FindProperty("_rotationConstraints.ZAxis"), locked: true);

        SetAxisLocked(so.FindProperty("_scaleConstraints.XAxis"), locked: true);
        SetAxisLocked(so.FindProperty("_scaleConstraints.YAxis"), locked: true);
        SetAxisLocked(so.FindProperty("_scaleConstraints.ZAxis"), locked: true);

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetAxisLocked(SerializedProperty axisProp, bool locked)
    {
        if (axisProp == null) return;
        axisProp.FindPropertyRelative("ConstrainAxis").boolValue = locked;
    }

    private static void EnsureHandGrabRoutine(GameObject handleGo, Grabbable grabbable, Rigidbody rb)
    {
        Transform routine = handleGo.transform.Find(HandGrabRoutineName);
        GameObject routineGo;
        if (routine == null)
        {
            routineGo = new GameObject(HandGrabRoutineName);
            routineGo.transform.SetParent(handleGo.transform, false);
        }
        else
        {
            routineGo = routine.gameObject;
        }

        HandGrabInteractable handGrab = routineGo.GetComponent<HandGrabInteractable>();
        if (handGrab == null)
            handGrab = routineGo.AddComponent<HandGrabInteractable>();

        var handSo = new SerializedObject(handGrab);
        handSo.FindProperty("_pointableElement").objectReferenceValue = grabbable;
        handSo.FindProperty("_rigidbody").objectReferenceValue = rb;
        handSo.ApplyModifiedPropertiesWithoutUndo();

        if (routineGo.GetComponent<GrabInteractable>() == null)
        {
            var distanceGrab = routineGo.AddComponent<GrabInteractable>();
            var distSo = new SerializedObject(distanceGrab);
            distSo.FindProperty("_pointableElement").objectReferenceValue = grabbable;
            distSo.FindProperty("_rigidbody").objectReferenceValue = rb;
            distSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void WirePlaceholderFormation(GameObject root, Transform formationRoot)
    {
        if (!root.TryGetComponent(out PlaceholderFormation formation))
            return;

        var slots = formationRoot.GetComponentsInChildren<PlaceholderSlot>(true);
        var so = new SerializedObject(formation);
        so.FindProperty("formationRoot").objectReferenceValue = formationRoot;
        so.FindProperty("slots").arraySize = slots.Length;
        for (int i = 0; i < slots.Length; i++)
            so.FindProperty("slots").GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        so.FindProperty("rotationSensitivity").floatValue = 1f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireGrabBridge(GameObject root, Transform formationRoot, GameObject handleGo)
    {
        if (!root.TryGetComponent(out FormationGrabRotateBridge bridge))
            bridge = root.AddComponent<FormationGrabRotateBridge>();

        Grabbable grabbable = handleGo.GetComponent<Grabbable>();
        PlaceholderFormation formation = root.GetComponent<PlaceholderFormation>();

        var so = new SerializedObject(bridge);
        so.FindProperty("grabbable").objectReferenceValue = grabbable;
        so.FindProperty("formation").objectReferenceValue = formation;
        so.FindProperty("rotationPivot").objectReferenceValue = formationRoot;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
