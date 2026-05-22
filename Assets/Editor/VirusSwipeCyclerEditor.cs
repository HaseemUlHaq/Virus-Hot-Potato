using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VirusSwipeCycler))]
public class VirusSwipeCyclerEditor : Editor
{
    private static readonly string[] MaterialPaths =
    {
        "Assets/Viruses_FREE/Material/Virus 1.mat",
        "Assets/Viruses_FREE/Material/Virus 2.mat",
        "Assets/Viruses_FREE/Material/Virus 3.mat",
        "Assets/Viruses_FREE/Material/Virus 4.mat",
        "Assets/Viruses_FREE/Material/Virus 5.mat",
        "Assets/Viruses_FREE/Material/Virus 6.mat",
        "Assets/Viruses_FREE/Material/Virus 7.mat",
        "Assets/Viruses_FREE/Material/Virus 8.mat",
        "Assets/Viruses_FREE/Material/Virus 9.mat",
        "Assets/Viruses_FREE/Material/Virus 10.mat",
    };

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Click the button below to auto-fill Color Themes from the 10 Virus materials in Assets/Viruses_FREE/Material/.",
            MessageType.Info);

        if (GUILayout.Button("Auto-Populate Themes from Virus Materials", GUILayout.Height(32)))
            PopulateThemes();
    }

    private void PopulateThemes()
    {
        var cycler = (VirusSwipeCycler)target;
        var so = new SerializedObject(cycler);
        SerializedProperty themesProp = so.FindProperty("colorThemes");
        themesProp.arraySize = MaterialPaths.Length;

        for (int i = 0; i < MaterialPaths.Length; i++)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPaths[i]);
            if (mat == null)
            {
                Debug.LogWarning($"[VirusSwipeCyclerEditor] Could not load {MaterialPaths[i]} — skipping index {i}.");
                continue;
            }

            SerializedProperty entry     = themesProp.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("bodyColor").colorValue      = mat.GetColor("_Color_1");
            entry.FindPropertyRelative("spikeColor").colorValue     = mat.GetColor("_Color_2");
            entry.FindPropertyRelative("veinGlowColor").colorValue  = mat.GetColor("_Color_3_Overlay");
            entry.FindPropertyRelative("glowIntensity").floatValue  = mat.GetFloat("_Color3_Scale");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(cycler);
        Debug.Log("[VirusSwipeCyclerEditor] Color Themes populated from Virus 1–10 materials.");
    }
}
