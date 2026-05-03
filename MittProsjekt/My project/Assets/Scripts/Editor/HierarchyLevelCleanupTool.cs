using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Moves root GameObjects under GameplaySystems and EnvironmentArt (English hierarchy names).
// Older scenes (_Gameplay_Systems, Spill_Systemer, Miljo, etc.) are renamed on cleanup.
// Menu: CartoonZombies → Organize → 1 Cleanup hierarchy (active scene)
public static class HierarchyLevelCleanupTool
{
    public const string GameplayRootName = "GameplaySystems";
    public const string EnvironmentRootName = "EnvironmentArt";

    /// <summary>Any of these root names are renamed to <see cref="GameplayRootName"/> when organizing.</summary>
    public static readonly string[] LegacyGameplayRootNames =
    {
        "_Gameplay_Systems", "Spill_Systemer", "__Gameplay_Systems"
    };

    /// <summary>Any of these root names are renamed to <see cref="EnvironmentRootName"/> when organizing.</summary>
    public static readonly string[] LegacyEnvironmentRootNames =
    {
        "_Environment_Art", "Miljo", "__Environment_Art"
    };

    private const string MenuPath = "CartoonZombies/Organize/1 Cleanup hierarchy (active scene)";

    [MenuItem(MenuPath, false, 10)]
    private static void Run()
    {
        Scene s = SceneManager.GetActiveScene();
        if (!s.isLoaded)
        {
            EditorUtility.DisplayDialog("No scene", "Open a level scene (or any saved scene) first.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Clean up hierarchy?",
                "Creates GameplaySystems and EnvironmentArt at the scene root and moves existing root objects under them.\n" +
                "(Older root names are renamed automatically.)\n\n" +
                "Continue?",
                "Yes",
                "Cancel"))
            return;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        OrganizeRoots(s);
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(s);
        EditorUtility.DisplayDialog("Done",
            "Scene root is organized.\nSave the scene (Ctrl+S).", "OK");
    }

    /// <summary>Called from repair tools without an extra dialog.</summary>
    public static void OrganizeRoots(Scene s)
    {
        Transform systemsRoot = GetOrCreateRoot(GameplayRootName, LegacyGameplayRootNames, s);
        Transform artRoot     = GetOrCreateRoot(EnvironmentRootName, LegacyEnvironmentRootNames, s);

        var gameplayNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "Main Camera", "MainCamera", "GameManager", "SpawnPoints", "ZombieSpawner", "ZoneTrigger",
            "Player", "PauseCanvas", "CheatCanvas", "HUDCanvas", "EventSystem", "Floor",
            "Reflection Probe", "Post Processing", "WindZone",
            "AudioManager", "CityParkourManager", "BoatUnlockSystem", "BoatTrigger",
        };

        GameObject[] roots = s.GetRootGameObjects();
        foreach (GameObject go in roots)
        {
            if (go.transform == systemsRoot || go.transform == artRoot) continue;
            string n = go.name;

            bool isGameplay = gameplayNames.Contains(n)
                              || n.StartsWith("Directional Light", System.StringComparison.Ordinal)
                              || n.StartsWith("Main Camera", System.StringComparison.Ordinal)
                              || n.StartsWith("NavMesh", System.StringComparison.Ordinal)
                              || n.StartsWith("_NavMesh", System.StringComparison.Ordinal);

            if (go.GetComponent<GameManager>() != null
                || go.GetComponent<AudioManager>() != null
                || go.GetComponent<CityParkourManager>() != null
                || go.GetComponent<BoatUnlockSystem>() != null
                || go.GetComponent<IslandWinTrigger>() != null)
                isGameplay = true;

            Transform parent = isGameplay ? systemsRoot : artRoot;
            if (go.transform.parent == parent) continue;

            Undo.SetTransformParent(go.transform, parent, "Hierarchy cleanup");
        }
    }

    private static Transform GetOrCreateRoot(string preferredName, string[] legacyNames, Scene s)
    {
        foreach (GameObject go in s.GetRootGameObjects())
        {
            if (go.name == preferredName) return go.transform;
        }

        foreach (string legacy in legacyNames)
        {
            foreach (GameObject go in s.GetRootGameObjects())
            {
                if (go.name != legacy) continue;
                Undo.RecordObject(go, "Rename hierarchy root");
                go.name = preferredName;
                return go.transform;
            }
        }

        GameObject created = new GameObject(preferredName);
        Undo.RegisterCreatedObjectUndo(created, "Create " + preferredName);
        EditorSceneManager.MoveGameObjectToScene(created, s);
        return created.transform;
    }

    [MenuItem(MenuPath, true)]
    private static bool Validate() => !Application.isPlaying && !string.IsNullOrEmpty(SceneManager.GetActiveScene().path);
}
