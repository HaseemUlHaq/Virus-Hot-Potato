#if UNITY_EDITOR
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds FormationRoot, hand-grab components, and rotation bridge to PlaceHolderFormation.prefab.
/// Menu: Virus Hot Potato → Setup Placeholder Formation (grab + rotate)
/// </summary>
public static class PlaceholderFormationGrabSetup
{
    private const string PrefabPath = "Assets/Prefabs/PlaceHolderFormation.prefab";
    private const string FormationRootName = "FormationRoot";
    private const string HandGrabRoutineName = "[BuildingBlock] HandGrabInstallationRoutine";

    [MenuItem("Virus Hot Potato/Setup Placeholder Formation (grab + rotate)")]
    public static void SetupFromMenu()
    {
        SetupPrefab();
        AssetDatabase.SaveAssets();
        Debug.Log("[PlaceholderFormationGrabSetup] Done. PlaceHolderFormation prefab is ready for networked grab-rotate.");
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

        EnsureGrabComponents(root, formationRoot);
        WirePlaceholderFormation(root, formationRoot);
        WireGrabBridge(root, formationRoot);

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
            if (child.name.StartsWith("Slot_"))
                toMove.Add(child);
        }

        foreach (Transform slot in toMove)
            slot.SetParent(formationRoot, true);
    }

    private static void EnsureGrabComponents(GameObject root, Transform formationRoot)
    {
        if (!root.TryGetComponent(out BoxCollider box))
        {
            box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(1.2f, 0.35f, 1.2f);
            box.center = new Vector3(0f, 0.08f, 0f);
            box.isTrigger = true;
        }

        if (!root.TryGetComponent(out Rigidbody rb))
        {
            rb = root.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        GrabFreeTransformer transformer;
        if (!root.TryGetComponent(out transformer))
            transformer = root.AddComponent<GrabFreeTransformer>();
        LockTransformer(transformer);

        Grabbable grabbable;
        if (!root.TryGetComponent(out grabbable))
            grabbable = root.AddComponent<Grabbable>();

        var grabbableSo = new SerializedObject(grabbable);
        grabbableSo.FindProperty("_oneGrabTransformer").objectReferenceValue = transformer;
        grabbableSo.FindProperty("_twoGrabTransformer").objectReferenceValue = transformer;
        grabbableSo.FindProperty("_targetTransform").objectReferenceValue = formationRoot;
        grabbableSo.FindProperty("_rigidbody").objectReferenceValue = rb;
        grabbableSo.FindProperty("_kinematicWhileSelected").boolValue = true;
        grabbableSo.FindProperty("_throwWhenUnselected").boolValue = false;
        grabbableSo.ApplyModifiedPropertiesWithoutUndo();

        EnsureHandGrabRoutine(root, grabbable, rb);
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

    private static void EnsureHandGrabRoutine(GameObject root, Grabbable grabbable, Rigidbody rb)
    {
        Transform routine = root.transform.Find(HandGrabRoutineName);
        GameObject routineGo;
        if (routine == null)
        {
            routineGo = new GameObject(HandGrabRoutineName);
            routineGo.transform.SetParent(root.transform, false);
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

    private static void WireGrabBridge(GameObject root, Transform formationRoot)
    {
        if (!root.TryGetComponent(out FormationGrabRotateBridge bridge))
            bridge = root.AddComponent<FormationGrabRotateBridge>();

        Grabbable grabbable = root.GetComponent<Grabbable>();
        PlaceholderFormation formation = root.GetComponent<PlaceholderFormation>();

        var so = new SerializedObject(bridge);
        so.FindProperty("grabbable").objectReferenceValue = grabbable;
        so.FindProperty("formation").objectReferenceValue = formation;
        so.FindProperty("rotationPivot").objectReferenceValue = formationRoot;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
