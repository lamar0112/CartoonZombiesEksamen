using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

// Quick level-art helpers for zone scenes — replace greybox with real prefabs afterward (PG2202-08 editor)
public static class ZoneLevelAuthoring
{
    private const string EnvironmentRoot = "Environment";

    [MenuItem("CartoonZombies/Level Art/1 Ensure Environment hierarchy", false, 10)]
    public static void EnsureEnvironmentHierarchy()
    {
        if (GameObject.Find(EnvironmentRoot) != null)
        {
            Selection.activeGameObject = GameObject.Find(EnvironmentRoot);
            EditorGUIUtility.PingObject(Selection.activeGameObject);
            EditorUtility.DisplayDialog("Environment", "Environment already exists — selected in Hierarchy.\nDrag modular kit prefabs under Terrain_ModularKit.", "OK");
            return;
        }

        GameObject root = new GameObject(EnvironmentRoot);
        GameObject terrain = new GameObject("Terrain_ModularKit");
        terrain.transform.SetParent(root.transform, false);
        GameObject props = new GameObject("Props");
        props.transform.SetParent(root.transform, false);

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Environment",
            "Created:\n" +
            "• Environment\n" +
            "  – Terrain_ModularKit (walls / floor blocks)\n" +
            "  – Props (decor)\n\n" +
            "Mark walk meshes Static, then Re-Bake NavMesh.\nSave scene (Ctrl+S).", "OK");
    }

    [MenuItem("CartoonZombies/Level Art/2 Apply bright cartoon lighting", false, 11)]
    public static void ApplyBrightCartoonLighting()
    {
        GameObject sunGo = GameObject.Find("Directional Light");
        if (sunGo != null)
        {
            Light L = sunGo.GetComponent<Light>();
            if (L != null)
            {
                L.type = LightType.Directional;
                L.color = new Color(1f, 0.97f, 0.9f);
                L.intensity = 1.2f;
                L.shadows = LightShadows.Soft;
            }
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.7f, 0.88f);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Lighting",
            "Directional light warmed slightly; ambient set to soft blue fill.\n" +
            "Tweak further in Window → Rendering → Lighting if you use URP volumes.\nSave scene.", "OK");
    }
}
