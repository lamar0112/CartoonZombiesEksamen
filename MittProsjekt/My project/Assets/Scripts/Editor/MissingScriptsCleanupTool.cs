using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// MissingScriptsCleanupTool — fjerner «Missing (Script)» i aktiv scene (etter slettede/flyttede scripts).
// Pensum: GameObject-hierarki-helse før innlevering (PG2202-01).
// Ekstra: batch for level01/02 — sparer tid; dialog på engelsk matcher Repair-serien.
// Meny: CartoonZombies → Repair → Remove missing scripts (active scene)
public static class MissingScriptsCleanupTool
{
    private const string L1 = "Level01_By";
    private const string L2 = "Level02_StrandSkog";

    [MenuItem("CartoonZombies/Repair/4 Remove missing scripts (active scene)", false, 40)]
    public static void RemoveMissingInActiveScene()
    {
        Scene s = SceneManager.GetActiveScene();
        if (!s.isLoaded || string.IsNullOrEmpty(s.path))
        {
            EditorUtility.DisplayDialog("Missing scripts", "Open a saved scene first.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Remove missing scripts?",
                "Deletes broken script components on all GameObjects in the active scene.\n" +
                "Use Ctrl+Z to undo. Save the scene after (Ctrl+S).\n\n" +
                "Continue?",
                "Yes",
                "Cancel"))
            return;

        Undo.SetCurrentGroupName("Remove missing scripts");
        int removed = 0;
        foreach (GameObject root in s.GetRootGameObjects())
            removed += RemoveMissingRecursive(root);

        EditorSceneManager.MarkSceneDirty(s);
        EditorUtility.DisplayDialog("Done",
            $"Removed missing script components from {removed} GameObject(s).\nSave the scene (Ctrl+S).", "OK");
    }

    [MenuItem("CartoonZombies/Repair/5 Remove missing scripts (BOTH level scenes)", false, 41)]
    public static void RemoveMissingInBothLevels()
    {
        if (!EditorUtility.DisplayDialog(
                "Remove missing scripts on both levels?",
                "Opens Level01_By and Level02_StrandSkog, strips broken MonoBehaviour slots, saves.\n\nCtrl+Z works per scene if you re-open it.",
                "Run",
                "Cancel"))
            return;

        string returnPath = SceneManager.GetActiveScene().path;
        int total = 0;
        total += CleanSceneByName(L1);
        total += CleanSceneByName(L2);

        if (!string.IsNullOrEmpty(returnPath))
            EditorSceneManager.OpenScene(returnPath);

        EditorUtility.DisplayDialog("Done",
            $"Removed missing script component(s) from {total} GameObject(s) across both levels.\nSave if Unity still shows unsaved changes.", "OK");
    }

    [MenuItem("CartoonZombies/Repair/5 Remove missing scripts (BOTH level scenes)", true)]
    private static bool ValidateBoth() => !Application.isPlaying;

    private static int CleanSceneByName(string sceneFileName)
    {
        string path = FindScenePath(sceneFileName);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("[MissingScripts] Fant ikke scene: " + sceneFileName);
            return 0;
        }

        Scene s = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        int removed = 0;
        foreach (GameObject root in s.GetRootGameObjects())
            removed += RemoveMissingRecursive(root);

        EditorSceneManager.MarkSceneDirty(s);
        EditorSceneManager.SaveScene(s);
        return removed;
    }

    private static string FindScenePath(string sceneFileName)
    {
        foreach (string g in AssetDatabase.FindAssets($"{sceneFileName} t:Scene"))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (System.IO.Path.GetFileNameWithoutExtension(p) == sceneFileName)
                return p;
        }
        return null;
    }

    [MenuItem("CartoonZombies/Repair/4 Remove missing scripts (active scene)", true)]
    private static bool Validate() => !Application.isPlaying;

    private static int RemoveMissingRecursive(GameObject go)
    {
        int touched = 0;
        int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
        if (count > 0)
        {
            Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts");
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            touched = 1;
        }

        for (int i = 0; i < go.transform.childCount; i++)
            touched += RemoveMissingRecursive(go.transform.GetChild(i).gameObject);

        return touched;
    }
}
