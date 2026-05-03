using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Reparerer Level01_By og Level02_StrandSkog uten å slette bygget kart.
// Meny: CartoonZombies → Repair → …
public static class LevelSceneRepairTool
{
    private const string L1 = "Level01_By";
    private const string L2 = "Level02_StrandSkog";

    [MenuItem("CartoonZombies/Repair/1 Repair BOTH level scenes (recommended)", false, 10)]
    public static void RepairBothLevelScenes()
    {
        if (!EditorUtility.DisplayDialog(
                "Repair both level scenes?",
                "Opens Level01_By and Level02_StrandSkog and fixes:\n" +
                "• GameManager (prefab), EventSystem\n" +
                "• One Directional Light + one main camera (extras disabled)\n" +
                "• SpawnPoints, ZombieSpawner, ZoneManager, WaveData, ZoneTrigger\n" +
                "• Player, HUD, Pause, Cheat, audio + crosshair + camera\n" +
                "• NavMesh bake (whole scene)\n" +
                "• Hierarchy roots: GameplaySystems / EnvironmentArt\n\n" +
                "Saves both scenes. Use version control / backup if unsure.",
                "Run",
                "Cancel"))
            return;

        string returnPath = SceneManager.GetActiveScene().path;

        RepairAndSaveScene(L1, progressionZone: 1, waveTier: 2, addCityParkour: true,  addBoat: false);
        RepairAndSaveScene(L2, progressionZone: 2, waveTier: 3, addCityParkour: false, addBoat: true);

        if (!string.IsNullOrEmpty(returnPath))
            EditorSceneManager.OpenScene(returnPath);

        EditorUtility.DisplayDialog("Done",
            "Both level scenes repaired and saved.\n\n" +
            "Optional: CartoonZombies → Organize → 3 Sort environment art (both level scenes)\n" +
            "(folders under EnvironmentArt — Ctrl+Z to undo).\n\n" +
            "Manual wiring in Inspector:\n" +
            "• CityParkourManager → beachZoneTrigger\n" +
            "• BoatUnlockSystem → boat / lock objects\n" +
            "• Move ZoneTrigger to the exit you want\n" +
            "• IslandWinTrigger on chest (level 2)", "OK");
    }

    [MenuItem("CartoonZombies/Repair/2 Repair ACTIVE level scene (Level01 or Level02)", false, 20)]
    public static void RepairActiveSceneOnly()
    {
        string n = SceneManager.GetActiveScene().name;
        if (n == L1)
            RepairCurrentSceneAndSave(1, 2, true, false);
        else if (n == L2)
            RepairCurrentSceneAndSave(2, 3, false, true);
        else
            EditorUtility.DisplayDialog("Wrong scene",
                $"Open {L1} or {L2} first.\nActive: {n}", "OK");
    }

    [MenuItem("CartoonZombies/Repair/3 Repair BOTH + sync Build Settings", false, 30)]
    public static void RepairBothAndSyncBuildSettings()
    {
        if (!EditorUtility.DisplayDialog(
                "Repair levels and update Build Settings?",
                "Runs the same repair as item 1 on both levels, then adds MainMenu → levels → GameOver → Win to Build Settings if missing.\n\n" +
                "Saves the currently open scene first, then returns to it.",
                "Run",
                "Cancel"))
            return;

        var activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(activeScene);
        string returnPath = activeScene.path;

        RepairAllLevelsAndReturnToScene(returnPath);
        int added = ProjectSetupTool.AddScenesToBuildSettings(showSummaryDialog: false);

        EditorUtility.DisplayDialog("Done",
            "Level01_By and Level02_StrandSkog repaired (spawn, UI, audio, lights, camera, NavMesh, hierarchy).\n\n" +
            $"Build Settings updated ({added} scene(s) added if any were missing).\n\n" +
            "Manual: CityParkourManager.beachZoneTrigger, BoatUnlockSystem, ZoneTrigger placement.", "OK");
    }

    /// <summary>Used by batch repair — no confirmation dialog.</summary>
    public static void RepairAllLevelsAndReturnToScene(string returnScenePath)
    {
        RepairAndSaveScene(L1, progressionZone: 1, waveTier: 2, addCityParkour: true,  addBoat: false);
        RepairAndSaveScene(L2, progressionZone: 2, waveTier: 3, addCityParkour: false, addBoat: true);

        if (!string.IsNullOrEmpty(returnScenePath))
            EditorSceneManager.OpenScene(returnScenePath);
    }

    private static void RepairAndSaveScene(string sceneFileName, int progressionZone, int waveTier,
        bool addCityParkour, bool addBoat)
    {
        string[] guids = AssetDatabase.FindAssets($"{sceneFileName} t:Scene");
        string path = "";
        foreach (string g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (System.IO.Path.GetFileNameWithoutExtension(p) == sceneFileName)
            { path = p; break; }
        }
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning($"[Repair] Fant ikke scene: {sceneFileName}");
            return;
        }

        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        RunRepairOnOpenScene(progressionZone, waveTier, addCityParkour, addBoat);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Repair] Lagret: {path}");
    }

    private static void RepairCurrentSceneAndSave(int progressionZone, int waveTier,
        bool addCityParkour, bool addBoat)
    {
        var scene = SceneManager.GetActiveScene();
        RunRepairOnOpenScene(progressionZone, waveTier, addCityParkour, addBoat);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("Done", $"{scene.name} repaired and saved.", "OK");
    }

    private static void RunRepairOnOpenScene(int progressionZone, int waveTier,
        bool addCityParkour, bool addBoat)
    {
        // Trær / busker under EnvironmentArt skal ikke bli «mur» — samme regel i by og strand.
        System.Func<MeshFilter, bool> vegetationSkip = SceneSetupTool.ShouldSkipMeshColliderForBeachVegetation;

        Undo.SetCurrentGroupName("Level scene repair");
        SceneSetupTool.EnsureGameManagerFromPrefab();
        SceneSetupTool.DeduplicateDirectionalLightsAndCameras();
        SceneSetupTool.EnsureLevelLightingBasics();
        SceneSetupTool.RepairLevelGameplayCore(progressionZone, waveTier);

        if (addCityParkour)
        {
            GameObject city = LevelMapRootResolver.FindCityMapRoot();
            if (city != null)
            {
                SceneSetupTool.EnsureMeshCollidersUnderRoot(city, vegetationSkip);
                SceneSetupTool.StripMeshCollidersFromVegetationUnderRoot(city);
            }
            if (UnityEngine.Object.FindFirstObjectByType<CityParkourManager>() == null)
            {
                GameObject go = new GameObject("CityParkourManager");
                go.AddComponent<CityParkourManager>();
            }
        }

        if (addBoat)
        {
            GameObject beach = LevelMapRootResolver.FindBeachMapRoot();
            if (beach != null)
            {
                SceneSetupTool.EnsureMeshCollidersUnderRoot(beach, vegetationSkip);
                SceneSetupTool.StripMeshCollidersFromVegetationUnderRoot(beach);
            }
            if (UnityEngine.Object.FindFirstObjectByType<BoatUnlockSystem>() == null)
            {
                GameObject go = new GameObject("BoatUnlockSystem");
                go.AddComponent<BoatUnlockSystem>();
            }
        }

        // Veier og bygg ofte ligger under EnvironmentArt — uten Collider faller spilleren gjennom.
        GameObject envArt = GameObject.Find("EnvironmentArt");
        if (envArt != null)
        {
            SceneSetupTool.EnsureMeshCollidersUnderRoot(envArt, vegetationSkip);
            SceneSetupTool.StripMeshCollidersFromVegetationUnderRoot(envArt);
        }

        SceneSetupTool.EnsureInvisibleSafetyGroundPlane();
        SceneSetupTool.EnsureWaterObjectsHaveNavMeshNotWalkable();
        SceneSetupTool.EnsureWorldNavMesh();
        HierarchyLevelCleanupTool.OrganizeRoots(SceneManager.GetActiveScene());
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    [MenuItem("CartoonZombies/Repair/1 Repair BOTH level scenes (recommended)", true)]
    [MenuItem("CartoonZombies/Repair/2 Repair ACTIVE level scene (Level01 or Level02)", true)]
    [MenuItem("CartoonZombies/Repair/3 Repair BOTH + sync Build Settings", true)]
    private static bool ValidateRepair() => !Application.isPlaying;
}
