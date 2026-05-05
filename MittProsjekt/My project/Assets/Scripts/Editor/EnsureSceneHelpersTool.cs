#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Editor-hjelp: legger ofte glemte runtime-komponenter på plass (PG2202-01 scene-struktur).
// Kan ikke erstatte NavMesh-bake, layers eller MissionManager-piler — det krever scene-innhold.
public static class EnsureSceneHelpersTool
{
    const string Menu = "CartoonZombies/Organize/";

    [MenuItem(Menu + "Add RuntimeHierarchyTuning to GameplaySystems (if missing)", false, 60)]
    public static void AddRuntimeTuning()
    {
        GameObject gs = GameObject.Find("GameplaySystems");
        if (gs == null)
        {
            EditorUtility.DisplayDialog("GameplaySystems",
                "Fant ikke et GameObject med nøyaktig navnet «GameplaySystems» i aktiv scene.\n" +
                "Opprett det, eller kjør hierarchy cleanup-menyen deres først.", "OK");
            return;
        }

        if (gs.GetComponent<RuntimeHierarchyTuning>() != null)
        {
            EditorUtility.DisplayDialog("RuntimeHierarchyTuning", "Komponenten finnes allerede på GameplaySystems.", "OK");
            Selection.activeGameObject = gs;
            return;
        }

        Undo.AddComponent<RuntimeHierarchyTuning>(gs);
        EditorUtility.SetDirty(gs);
        Selection.activeGameObject = gs;
        EditorUtility.DisplayDialog("RuntimeHierarchyTuning",
            "Lagt på GameplaySystems.\n\nPlay Mode: trykk F10 for tuning-panel (valgfritt — ikke eksamenskrav).", "OK");
    }
}
#endif
