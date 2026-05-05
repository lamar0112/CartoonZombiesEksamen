using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// EnvironmentArtSortTool — sorterer barn under EnvironmentArt i kategorimapper (Undo); flytter gameplay til GameplaySystems.
// Pensum: ryddig scene-hierarki (PG2202-01); engelske kategorinavn = prosjektkonvensjon.
// Ekstra: automatisk sortering etter import — reduserer manuelt rot; meny på engelsk.
// Menu: CartoonZombies → Organize → …
public static class EnvironmentArtSortTool
{
    private const string L1 = "Level01_By";
    private const string L2 = "Level02_StrandSkog";

    private static readonly string[] CategoryOrder =
    {
        "Roads", "Nature", "Buildings", "Vehicles", "Props", "Water", "Misc"
    };

    /// <summary>Folder names that stay under EnvironmentArt as a single unit.</summary>
    private static readonly HashSet<string> AtomicGroupNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "Lights", "Lys", "Camera", "Kamera", "Terrain", "Lighting"
    };

    /// <summary>Wrappers whose children are sorted into categories; empty wrappers are removed.</summary>
    private static readonly HashSet<string> FlattenChildrenNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "Environment", "Components", "Art"
    };

    /// <summary>Older Norwegian folder names from a previous sort — do not treat as props to re-sort.</summary>
    private static readonly HashSet<string> LegacyCategoryFolderNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "Veier", "Natur", "Bygninger", "Kjoretoy", "Rekvisitter", "Vann", "Diverse"
    };

    [MenuItem("CartoonZombies/Organize/2 Sort environment art (active scene, Undo)", false, 20)]
    public static void SortActiveScene()
    {
        Scene s = SceneManager.GetActiveScene();
        if (!s.isLoaded || string.IsNullOrEmpty(s.path))
        {
            EditorUtility.DisplayDialog("Environment sort", "Open a saved scene first.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Sort environment art?",
                "Creates folders under «EnvironmentArt» (Roads, Nature, …) and moves props there.\n" +
                "Gameplay objects (coins, zone triggers, beach parkour) move under «GameplaySystems».\n\n" +
                "Use Ctrl+Z to undo. Save the scene afterwards (Ctrl+S).\n\n" +
                "Continue?",
                "Yes",
                "Cancel"))
            return;

        RunSort(s);
        EditorSceneManager.MarkSceneDirty(s);
        EditorUtility.DisplayDialog("Done", $"Sort finished in «{s.name}».\nSave with Ctrl+S.", "OK");
    }

    [MenuItem("CartoonZombies/Organize/3 Sort environment art (both level scenes)", false, 21)]
    public static void SortBothLevelScenes()
    {
        if (!EditorUtility.DisplayDialog(
                "Sort both?",
                "Runs environment sort on Level01_By and Level02_StrandSkog.\n" +
                "Commit to git first if you want a safety snapshot.",
                "Run",
                "Cancel"))
            return;

        string ret = SceneManager.GetActiveScene().path;
        SortSceneByFileName(L1);
        SortSceneByFileName(L2);
        if (!string.IsNullOrEmpty(ret))
            EditorSceneManager.OpenScene(ret);
        EditorUtility.DisplayDialog("Done", "Both level scenes were processed.\nSave project / scenes if needed.", "OK");
    }

    [MenuItem("CartoonZombies/Organize/2 Sort environment art (active scene, Undo)", true)]
    [MenuItem("CartoonZombies/Organize/3 Sort environment art (both level scenes)", true)]
    private static bool ValidateSort() => !Application.isPlaying;

    private static void SortSceneByFileName(string sceneFileName)
    {
        string path = FindScenePath(sceneFileName);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning($"[EnvSort] Scene not found: {sceneFileName}");
            return;
        }

        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        RunSort(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[EnvSort] Saved: {path}");
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

    private static void RunSort(Scene s)
    {
        Undo.SetCurrentGroupName("Environment art sort");
        int gid = Undo.GetCurrentGroup();

        Transform art = FindEnvironmentRoot(s);
        Transform systems = FindGameplayRoot(s);
        if (art == null)
        {
            Debug.LogWarning("[EnvSort] Could not find EnvironmentArt (or legacy environment root) in the scene.");
            Undo.CollapseUndoOperations(gid);
            return;
        }

        var categories = EnsureCategoryFolders(art);

        var queue = new List<Transform>();
        CollectSortTargets(art, queue);

        foreach (Transform t in queue)
        {
            if (t == null) continue;
            if (IsCategoryFolder(t.name)) continue;
            if (ShouldReparentToSystems(t))
            {
                if (systems != null && t.parent != systems)
                    Undo.SetTransformParent(t, systems, "Move gameplay object to GameplaySystems");
                else if (systems == null)
                    Debug.LogWarning("[EnvSort] «GameplaySystems» missing — run Cleanup Hierarchy or Repair first.");
                continue;
            }

            string cat = PickCategory(t.name);
            Transform folder = categories[cat];
            if (t.parent != folder)
                Undo.SetTransformParent(t, folder, "Sort environment");
        }

        RemoveEmptyFlattenWrappers(art);
        Undo.CollapseUndoOperations(gid);
    }

    private static Transform FindEnvironmentRoot(Scene s)
    {
        foreach (GameObject go in s.GetRootGameObjects())
        {
            if (go.name == HierarchyLevelCleanupTool.EnvironmentRootName)
                return go.transform;
            foreach (string leg in HierarchyLevelCleanupTool.LegacyEnvironmentRootNames)
            {
                if (go.name == leg) return go.transform;
            }
        }
        return null;
    }

    private static Transform FindGameplayRoot(Scene s)
    {
        foreach (GameObject go in s.GetRootGameObjects())
        {
            if (go.name == HierarchyLevelCleanupTool.GameplayRootName)
                return go.transform;
            foreach (string leg in HierarchyLevelCleanupTool.LegacyGameplayRootNames)
            {
                if (go.name == leg) return go.transform;
            }
        }
        return null;
    }

    private static Dictionary<string, Transform> EnsureCategoryFolders(Transform artRoot)
    {
        var map = new Dictionary<string, Transform>();
        foreach (string cat in CategoryOrder)
        {
            Transform existing = artRoot.Find(cat);
            if (existing != null)
            {
                map[cat] = existing;
                continue;
            }

            GameObject go = new GameObject(cat);
            Undo.RegisterCreatedObjectUndo(go, "Category " + cat);
            Undo.SetTransformParent(go.transform, artRoot, "Environment category");
            map[cat] = go.transform;
        }
        return map;
    }

    private static void CollectSortTargets(Transform artRoot, List<Transform> outList)
    {
        var direct = new List<Transform>();
        for (int i = 0; i < artRoot.childCount; i++)
            direct.Add(artRoot.GetChild(i));

        foreach (Transform child in direct)
        {
            if (FlattenChildrenNames.Contains(child.name))
            {
                for (int i = 0; i < child.childCount; i++)
                    outList.Add(child.GetChild(i));
                continue;
            }

            if (AtomicGroupNames.Contains(child.name))
                continue;

            outList.Add(child);
        }
    }

    private static void RemoveEmptyFlattenWrappers(Transform artRoot)
    {
        var toCheck = new List<Transform>();
        for (int i = 0; i < artRoot.childCount; i++)
            toCheck.Add(artRoot.GetChild(i));

        foreach (Transform t in toCheck)
        {
            if (!FlattenChildrenNames.Contains(t.name)) continue;
            if (t.childCount > 0) continue;
            Undo.DestroyObjectImmediate(t.gameObject);
        }
    }

    private static bool IsCategoryFolder(string name)
    {
        foreach (string c in CategoryOrder)
        {
            if (name == c) return true;
        }
        return LegacyCategoryFolderNames.Contains(name);
    }

    private static bool ShouldReparentToSystems(Transform t)
    {
        if (t.GetComponent<IslandWinTrigger>() != null) return true;
        if (t.GetComponent<ZoneTrigger>() != null) return true;
        if (t.GetComponent<CoinCollectable>() != null) return true;
        if (t.GetComponent<BeachParkourMission>() != null) return true;
        return false;
    }

    private static string PickCategory(string objectName)
    {
        string n = objectName.ToLowerInvariant();

        if (n.StartsWith("vehicle_") || n.Contains("vehicle_")
            || n.Contains("vehicle ") || n.Contains("police") || n.Contains("truck")
            || n.Contains("pick up") || n.Contains("container_color")
            || (n.Contains("boat") && !n.Contains("boathouse")))
            return "Vehicles";

        if (n.StartsWith("natures_") || n.StartsWith("nature_"))
            return "Nature";

        if (n.StartsWith("road") || n.Contains("road_") || n.Contains("_road")
            || n.Contains("lane") || n.Contains("split line") || n.Contains("street strip"))
            return "Roads";

        if (n.StartsWith("props_") || n.StartsWith("props "))
            return "Props";

        if (n.Contains("water") || n.Contains("ocean") || n.Contains("hav") || n.Contains("sea") || n.Contains("vannflate"))
            return "Water";

        if (n.Contains("tree") || n.Contains("bush") || n.Contains("stump")
            || n.Contains("grass") || n.Contains("rock") || n.Contains("cliff")
            || n.Contains("fern") || n.Contains("mushroom"))
            return "Nature";

        if (n.Contains("house") || n.Contains("building") || n.Contains("roof")
            || n.Contains("wall") || n.Contains("window") || n.Contains("door"))
            return "Buildings";

        if (n.Contains("traffic") || n.Contains("signal") || n.Contains("windmill")
            || n.Contains("street light") || n.Contains("sign") || n.Contains("prop"))
            return "Props";

        return "Misc";
    }
}
