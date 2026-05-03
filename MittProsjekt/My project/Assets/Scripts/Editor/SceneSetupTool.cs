using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.Rendering;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

public class SceneSetupTool
{
    private const string MainMenuKeybindHelpText =
        "WASD / piltaster — beveg\n" +
        "Mus — se deg rundt\n" +
        "Venstreklikk — skyt\n" +
        "R — reload\n" +
        "Mellomrom — hopp\n" +
        "ESC — pause\n" +
        "F — gå inn/ut av bil (nær bil)\n" +
        "Y — cheat-meny";

    // --- LEVEL SCENES (gamle «full setup» med ekstra gulv — bruk Repair-menyen for ferdige kart) ---

    [MenuItem("CartoonZombies/Scenes/Legacy — Full setup Level01_By (adds Floor + systems)", false, 100)]
    public static void SetupLevel01By() => SetupZoneSceneForFile(isCity: true);

    [MenuItem("CartoonZombies/Scenes/Legacy — Full setup Level02_StrandSkog (adds Floor + systems)", false, 101)]
    public static void SetupLevel02StrandSkog() => SetupZoneSceneForFile(isCity: false);

    [MenuItem("CartoonZombies/Scenes/Add CheatMenu to active scene", false, 30)]
    public static void AddCheatMenuToScene()
    {
        CreateCheatMenuCanvas();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Done",
            "CheatCanvas added.\nPress Y during play.\nSave with Ctrl+S.", "OK");
    }

    [MenuItem("CartoonZombies/Scenes/Fix audio + crosshair (active scene)", false, 31)]
    public static void FixAudioAndCrosshair() => FixAudioAndCrosshair(showDialog: true);

    public static void FixAudioAndCrosshair(bool showDialog)
    {
        // --- 1. Tildel lyder til PlayerShooting ---
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerShooting ps = player.GetComponent<PlayerShooting>();
            if (ps != null)
            {
                SerializedObject so = new SerializedObject(ps);
                so.FindProperty("shootSound").objectReferenceValue  =
                    AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/GunShotSound.wav");
                so.FindProperty("reloadSound").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/GunReloadSound.wav");
                so.FindProperty("emptySound").objectReferenceValue  =
                    AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ThirdParty/Kenney/Audio/Impact/impactGeneric_light_001.ogg");
                so.ApplyModifiedProperties();
                Debug.Log("Assigned sounds on PlayerShooting.");
            }
        }
        else Debug.LogWarning("No Player in scene. Run Repair or Scenes setup first.");

        // --- 2. Legg til crosshair i HUDCanvas ---
        GameObject hud = GameObject.Find("HUDCanvas");
        if (hud != null)
        {
            // Sletter gammel crosshair og lager ny med riktige innstillinger
            Transform existing = hud.transform.Find("Crosshair");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            GameObject ch = new GameObject("Crosshair");
            ch.transform.SetParent(hud.transform, false);
            TextMeshProUGUI tmp = ch.AddComponent<TextMeshProUGUI>();
            tmp.text      = "+";
            tmp.fontSize  = 28;
            tmp.color     = new Color(1f, 1f, 1f, 0.9f);
            tmp.alignment = TextAlignmentOptions.Center;
            RectTransform rt = ch.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(40f, 40f);
            Debug.Log("Crosshair set to '+'.");
        }
        else Debug.LogWarning("HUDCanvas not found. Run Repair or Scenes setup first.");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        if (showDialog)
            EditorUtility.DisplayDialog("Done", "Sounds assigned and crosshair added.\nSave with Ctrl+S.", "OK");
    }

    [MenuItem("CartoonZombies/Scenes/Re-Bake NavMesh (active scene)", false, 32)]
    public static void ReBakeNavMesh()
    {
        EnsureWorldNavMesh();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Done", "NavMesh baked (static objects in scene).\nSave with Ctrl+S.", "OK");
    }

    [MenuItem("CartoonZombies/Scenes/Mark WATER NavMesh Not Walkable + Re-Bake (active scene)", false, 33)]
    public static void WaterNavMeshAndRebake()
    {
        Undo.SetCurrentGroupName("Water NavMesh + bake");
        EnsureWaterObjectsHaveNavMeshNotWalkable();
        EnsureWorldNavMesh();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Done",
            "Vann markert som Not Walkable (area 1) og NavMesh bakt på nytt.\nLagre scenen (Ctrl+S).", "OK");
    }

    [MenuItem("CartoonZombies/Scenes/Mark WATER NavMesh Not Walkable + Re-Bake (active scene)", true)]
    private static bool WaterNavMeshAndRebakeValidate() => !Application.isPlaying;

    /// <summary>Veier under EnvironmentArt har ofte bare MeshRenderer — uten dette faller spilleren gjennom.</summary>
    [MenuItem("CartoonZombies/Scenes/Fix ENVIRONMENTART mesh colliders (Veier, bygg)", false, 33)]
    public static void FixEnvironmentArtMeshColliders()
    {
        GameObject env = GameObject.Find("EnvironmentArt");
        if (env == null)
        {
            EditorUtility.DisplayDialog("EnvironmentArt",
                "Ingen rot med navnet «EnvironmentArt» i denne scenen.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("EnvironmentArt mesh colliders");
        EnsureMeshCollidersUnderRoot(env, ShouldSkipMeshColliderForBeachVegetation);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Done",
            "MeshCollider (eller BoxCollider-fallback) lagt på objekter under EnvironmentArt som manglet Collider.\nLagre scenen (Ctrl+S).", "OK");
    }

    [MenuItem("CartoonZombies/Scenes/Fix ENVIRONMENTART mesh colliders (Veier, bygg)", true)]
    private static bool FixEnvironmentArtMeshCollidersValidate() => !Application.isPlaying;

    [MenuItem("CartoonZombies/Scenes/Strip vegetation MeshColliders (trees — active scene)", false, 34)]
    public static void StripVegetationMeshCollidersActiveScene()
    {
        Undo.SetCurrentGroupName("Strip vegetation MeshColliders");
        void Strip(GameObject go)
        {
            if (go == null) return;
            StripMeshCollidersFromVegetationUnderRoot(go);
        }

        Strip(GameObject.Find("EnvironmentArt"));
        Strip(LevelMapRootResolver.FindCityMapRoot());
        Strip(LevelMapRootResolver.FindBeachMapRoot());

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Done",
            "Fjernet MeshCollider på vegetasjon der det matchet skip-regelen.\n" +
            "(Trær/busker skal ikke stoppe spilleren.)\n\nLagre scenen (Ctrl+S).", "OK");
    }

    [MenuItem("CartoonZombies/Scenes/Strip vegetation MeshColliders (trees — active scene)", true)]
    private static bool StripVegetationMeshCollidersValidate() => !Application.isPlaying;

    // ── Reparasjon / NavMesh / systemer (beholder eksisterende kart) ─────────────

    /// <summary>Baker NavMesh fra alle walkable surfaces i scenen (krever ikke «Floor»).</summary>
    public static void EnsureWorldNavMesh()
    {
        NavMeshSurface surf = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surf == null)
        {
            GameObject go = new GameObject("NavMeshWorldBake");
            surf = go.AddComponent<NavMeshSurface>();
        }
        else if (surf.gameObject.name == "_NavMesh_WorldBake")
        {
            Undo.RecordObject(surf.gameObject, "Rename NavMesh holder");
            surf.gameObject.name = "NavMeshWorldBake";
        }

        surf.collectObjects = CollectObjects.All;
        surf.BuildNavMesh();
    }

    /// <summary>Trær, palmer, busker — ikke auto MeshCollider (spiller skal kunne passere tynn vegetasjon).</summary>
    public static bool IsBeachVegetationHierarchy(Transform t)
    {
        if (t == null) return false;
        for (Transform c = t; c != null; c = c.parent)
        {
            string n = c.name.ToLowerInvariant();
            if (n.Contains("palms") || n.Contains("forestedge") || n.Contains("groundcover"))
                return true;
            if (n.Contains("natur") || n.Contains("vegetation") || n.Contains("foliage") || n.Contains("forest"))
                return true;
            if (n.Contains("hedge") || n.Contains("shrub") || n.Contains("cactus"))
                return true;
            if (n.Contains("flower") || n.Contains("flora") || n.Contains("meadow") || n.Contains("grass"))
                return true;
            if (n.Contains("plant") || n.Contains("weed") || n.Contains("fern") || n.Contains("reed"))
                return true;
            if (n.Contains("lily") || n.Contains("scatter") || n.Contains("wild") || n.Contains("vine"))
                return true;
            if (n.Contains("mushroom") || n.Contains("tuft") || n.Contains("clover") || n.Contains("bloom"))
                return true;
            if (n.Contains("tree") || n.Contains("palm") || n.Contains("bush") || n.Contains("stump"))
                return true;
        }
        return false;
    }

    /// <summary>Vannflate på NavMesh gjør at AI går på vann — marker som «Not Walkable» (area 1) før bake.</summary>
    public static void EnsureWaterObjectsHaveNavMeshNotWalkable()
    {
        var touched = new HashSet<GameObject>();

        void Touch(GameObject go)
        {
            if (go == null || !touched.Add(go)) return;
            NavMeshModifier nm = go.GetComponent<NavMeshModifier>();
            if (nm == null) nm = Undo.AddComponent<NavMeshModifier>(go);
            Undo.RecordObject(nm, "Water NavMesh");
            nm.overrideArea = true;
            nm.area = 1;
        }

        try
        {
            foreach (GameObject g in GameObject.FindGameObjectsWithTag("Water"))
                Touch(g);
        }
        catch (UnityException)
        {
            // Tag «Water» finnes ikke i prosjektet
        }

        int waterLayer = LayerMask.NameToLayer("Water");
        foreach (Collider c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c == null) continue;
            GameObject go = c.gameObject;
            if (waterLayer >= 0 && go.layer == waterLayer)
                Touch(go);
            else if (WaterDetection.IsWaterObjectName(go.name))
                Touch(go);
        }
    }

    public static bool ShouldSkipMeshColliderForBeachVegetation(MeshFilter mf) =>
        IsBeachVegetationHierarchy(mf != null ? mf.transform : null);

    /// <summary>Fjerner MeshCollider på trær/busker (nyttig når gamle scener fikk collidere før skip-regelen).</summary>
    public static void StripMeshCollidersFromVegetationUnderRoot(GameObject root)
    {
        if (root == null) return;

        foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf == null) continue;
            if (!ShouldSkipMeshColliderForBeachVegetation(mf)) continue;
            MeshCollider mc = mf.gameObject.GetComponent<MeshCollider>();
            if (mc != null)
                Undo.DestroyObjectImmediate(mc);
        }
    }

    /// <summary>Adds a MeshCollider on every MeshFilter under <paramref name="root"/> that lacks one (walkable static meshes).</summary>
    public static void EnsureMeshCollidersUnderRoot(GameObject root) =>
        EnsureMeshCollidersUnderRoot(root, null);

    /// <param name="skipMeshFilter">If non-null and returns true for a <see cref="MeshFilter"/>, no collider is added there.</param>
    public static void EnsureMeshCollidersUnderRoot(GameObject root, System.Func<MeshFilter, bool> skipMeshFilter)
    {
        if (root == null) return;

        foreach (Terrain terr in root.GetComponentsInChildren<Terrain>(true))
        {
            if (terr == null || terr.terrainData == null) continue;
            TerrainCollider tc = terr.GetComponent<TerrainCollider>();
            if (tc == null)
                tc = Undo.AddComponent<TerrainCollider>(terr.gameObject);
            tc.terrainData = terr.terrainData;
        }

        foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (skipMeshFilter != null && skipMeshFilter(mf)) continue;
            if (mf.sharedMesh == null) continue;
            if (mf.GetComponent<Collider>() != null) continue;

            MeshCollider mc = Undo.AddComponent<MeshCollider>(mf.gameObject);
            mc.convex = false;
            mc.sharedMesh = mf.sharedMesh;

            if (mc.sharedMesh == null)
            {
                Undo.DestroyObjectImmediate(mc);
                AddBoxColliderFromLocalRendererBounds(mf);
            }
        }
    }

    /// <summary>When MeshCollider cannot use the mesh (import/readability), approximate with a box from the renderer AABB.</summary>
    private static void AddBoxColliderFromLocalRendererBounds(MeshFilter mf)
    {
        MeshRenderer mr = mf.GetComponent<MeshRenderer>();
        if (mr == null) return;
        Bounds lb = mr.localBounds;
        BoxCollider box = Undo.AddComponent<BoxCollider>(mf.gameObject);
        box.center = lb.center;
        box.size = Vector3.Max(lb.size, Vector3.one * 0.05f);
    }

    /// <summary>Large invisible box under the play space so the player cannot fall forever if a tile lacks a collider.</summary>
    public static void EnsureInvisibleSafetyGroundPlane()
    {
        string sn = SceneManager.GetActiveScene().name;
        if (sn != "Level01_By" && sn != "Level02_StrandSkog") return;

        Bounds? world = TryComputeLevelWorldBounds();
        float minY;
        float sizeX;
        float sizeZ;
        float centerX;
        float centerZ;
        if (world.HasValue)
        {
            Bounds b = world.Value;
            const float margin = 120f;
            minY = b.min.y - 14f;
            sizeX = Mathf.Max(b.size.x, 240f) + margin;
            sizeZ = Mathf.Max(b.size.z, 240f) + margin;
            centerX = b.center.x;
            centerZ = b.center.z;
        }
        else
        {
            minY = -24f;
            sizeX = 900f;
            sizeZ = 900f;
            centerX = 0f;
            centerZ = 0f;
        }

        const float thickness = 64f;
        Vector3 center = new Vector3(centerX, minY - thickness * 0.5f, centerZ);
        Vector3 size = new Vector3(sizeX, thickness, sizeZ);

        GameObject go = GameObject.Find("_SafetyGround");
        BoxCollider boxc;
        if (go == null)
        {
            go = new GameObject("_SafetyGround");
            Undo.RegisterCreatedObjectUndo(go, "_SafetyGround");
            boxc = Undo.AddComponent<BoxCollider>(go);
        }
        else
        {
            boxc = go.GetComponent<BoxCollider>();
            if (boxc == null) boxc = Undo.AddComponent<BoxCollider>(go);
        }

        go.transform.position = center;
        boxc.size = size;
    }

    /// <summary>World AABB from main map roots + optional environment art (for props that extend outside map root).</summary>
    private static Bounds? TryComputeLevelWorldBounds()
    {
        Bounds? acc = null;
        var processed = new HashSet<GameObject>();
        var names = new List<string>();
        names.AddRange(LevelMapRootResolver.CityMapRootCandidates);
        names.AddRange(LevelMapRootResolver.BeachMapRootCandidates);
        names.Add("EnvironmentArt");
        names.Add("Environment");

        foreach (string name in names)
        {
            GameObject go = GameObject.Find(name);
            if (go == null || !processed.Add(go)) continue;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || !r.enabled) continue;
                if (!acc.HasValue) acc = r.bounds;
                else
                {
                    Bounds b = acc.Value;
                    b.Encapsulate(r.bounds);
                    acc = b;
                }
            }
        }
        return acc;
    }

    /// <summary>At least one directional sun with sane intensity/shadows + moderate ambient fill (works with Built-in and URP sky).</summary>
    public static void EnsureLevelLightingBasics()
    {
        Light[] allDir = Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
            .Where(l => l != null && l.type == LightType.Directional).ToArray();

        Light active = allDir.FirstOrDefault(l => l.isActiveAndEnabled);
        if (active == null && allDir.Length > 0)
        {
            Undo.RecordObject(allDir[0].gameObject, "Enable sun");
            allDir[0].gameObject.SetActive(true);
            active = allDir[0];
        }

        if (active == null)
        {
            GameObject sunGo = new GameObject("Directional Light");
            Undo.RegisterCreatedObjectUndo(sunGo, "Directional Light");
            active = Undo.AddComponent<Light>(sunGo);
            active.type = LightType.Directional;
            active.color = new Color(1f, 0.97f, 0.92f);
            active.transform.rotation = Quaternion.Euler(52f, -38f, 0f);
        }

        Undo.RecordObject(active, "Sun settings");
        if (active.intensity < 0.35f)
            active.intensity = 1.15f;
        if (active.shadows == LightShadows.None)
            active.shadows = LightShadows.Soft;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.58f, 0.65f);
        RenderSettings.ambientEquatorColor = new Color(0.4f, 0.38f, 0.36f);
        RenderSettings.ambientGroundColor = new Color(0.22f, 0.2f, 0.18f);
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
            return;

        GameObject esGo = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(esGo, "EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();
    }

    public static void EnsureGameManagerFromPrefab()
    {
        if (Object.FindFirstObjectByType<GameManager>() != null) return;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameManager.prefab");
        if (prefab == null)
        {
            Debug.LogWarning("[Repair] Fant ikke Assets/Prefabs/GameManager.prefab");
            return;
        }
        PrefabUtility.InstantiatePrefab(prefab);
    }

    /// <summary>Én Directional Light og én aktiv hovedkamera aktiv — resten skrus av (slettes ikke).</summary>
    public static void DeduplicateDirectionalLightsAndCameras()
    {
        var dirLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
            .Where(l => l.type == LightType.Directional).ToArray();
        for (int i = 1; i < dirLights.Length; i++)
        {
            Debug.Log($"[Repair] Deaktiverer ekstra Directional Light: {dirLights[i].gameObject.name}");
            dirLights[i].gameObject.SetActive(false);
        }

        Camera[] cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        if (cams.Length <= 1) return;

        Camera keep = Camera.main;
        if (keep == null) keep = cams[0];

        foreach (Camera c in cams)
        {
            if (c == null || c == keep) continue;
            Debug.Log($"[Repair] Deaktiverer ekstra kamera: {c.gameObject.name}");
            c.gameObject.SetActive(false);
        }

        if (keep != null && !keep.gameObject.activeSelf)
            keep.gameObject.SetActive(true);
    }

    /// <summary>Kjerne gameplay uten å legge nytt gulv — for ferdig bygde level-scener.</summary>
    public static void RepairLevelGameplayCore(int progressionZone, int waveAssetTier)
    {
        GameObject spawnRoot = CreateOrFind("SpawnPoints");
        Vector3[] spawnPositions =
        {
            new Vector3(5f, 0f, 5f), new Vector3(-5f, 0f, 5f),
            new Vector3(5f, 0f, -5f), new Vector3(-5f, 0f, -5f)
        };
        for (int i = 0; i < spawnPositions.Length; i++)
        {
            string spName = $"SpawnPoint{i + 1}";
            if (spawnRoot.transform.Find(spName) == null)
            {
                GameObject sp = new GameObject(spName);
                sp.transform.SetParent(spawnRoot.transform);
                sp.transform.position = spawnPositions[i];
            }
        }

        ZombieSpawner[] allSpawners = Object.FindObjectsByType<ZombieSpawner>(FindObjectsSortMode.None);
        GameObject spawnerObj;
        if (allSpawners.Length == 0)
            spawnerObj = new GameObject("ZombieSpawner");
        else
        {
            spawnerObj = allSpawners[0].gameObject;
            for (int i = 1; i < allSpawners.Length; i++)
                Object.DestroyImmediate(allSpawners[i]);
            spawnerObj.name = "ZombieSpawner";
        }

        ZombieSpawner spawner = GetOrAdd<ZombieSpawner>(spawnerObj);
        ZoneManager   zm      = GetOrAdd<ZoneManager>(spawnerObj);

        SerializedObject soZM = new SerializedObject(zm);
        soZM.FindProperty("zoneNumber").SetValue(progressionZone);
        soZM.ApplyModifiedProperties();

        string waveDataPath = $"Assets/ScriptableObjects/Waves/WaveData_Zone{waveAssetTier}.asset";
        WaveData wd = AssetDatabase.LoadAssetAtPath<WaveData>(waveDataPath);
        if (wd != null)
        {
            SerializedObject soSpawner = new SerializedObject(spawner);
            SerializedProperty wavesProp = soSpawner.FindProperty("waves");
            wavesProp.arraySize = 1;
            wavesProp.GetArrayElementAtIndex(0).objectReferenceValue = wd;
            soSpawner.ApplyModifiedProperties();
        }

        int nSpawn = spawnRoot.transform.childCount;
        SerializedObject soSp = new SerializedObject(spawner);
        SerializedProperty spawnProp = soSp.FindProperty("spawnPoints");
        spawnProp.arraySize = nSpawn;
        for (int i = 0; i < nSpawn; i++)
            spawnProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnRoot.transform.GetChild(i);
        soSp.ApplyModifiedProperties();

        // By: ikke last scene automatisk når bølgene er ferdig — bruk ZoneTrigger. Strand: ofte OK med auto.
        SerializedObject soSpawnerFlags = new SerializedObject(spawner);
        var pAuto = soSpawnerFlags.FindProperty("loadNextSceneWhenAllWavesComplete");
        if (pAuto != null)
        {
            pAuto.boolValue = progressionZone != 1;
            soSpawnerFlags.ApplyModifiedProperties();
        }

        GameObject triggerObj = CreateOrFind("ZoneTrigger");
        BoxCollider bc = GetOrAdd<BoxCollider>(triggerObj);
        bc.isTrigger = true;
        if (bc.size.sqrMagnitude < 0.01f) bc.size = new Vector3(3f, 3f, 1f);
        GetOrAdd<ZoneTrigger>(triggerObj);

        SetupPlayerInScene();
        RebuildHUDCanvas();
        CreatePauseMenuCanvas();
        CreateCheatMenuCanvas();
        EnsureEventSystem();
        FixAudioAndCrosshair(showDialog: false);
        FixCameraInScene();
    }

    [MenuItem("CartoonZombies/Scenes/Setup MainMenu scene", false, 10)]
    public static void SetupMainMenu()
    {
        if (!ConfirmActiveScene("MainMenu")) return;
        EnsureMenuSceneCameraAndAudio();
        EnsureEventSystem();
        CreateMenuCanvas("MainMenuCanvas", typeof(MainMenuController));
        GameObject canvas = GameObject.Find("MainMenuCanvas");
        if (canvas != null)
            FixMainMenuRaycastsAndLayout(canvas);
        EditorUtility.DisplayDialog("Done", "MainMenu Canvas created.", "OK");
    }

    [MenuItem("CartoonZombies/Scenes/Fix Main menu (camera, EventSystem, UI)", false, 11)]
    public static void FixMainMenuComplete()
    {
        if (!ConfirmActiveScene("MainMenu")) return;
        Undo.SetCurrentGroupName("Main menu fix");
        int g = Undo.GetCurrentGroup();
        EnsureMenuSceneCameraAndAudio();
        EnsureEventSystem();
        GameObject canvas = GameObject.Find("MainMenuCanvas");
        if (canvas != null)
            FixMainMenuRaycastsAndLayout(canvas);
        Undo.CollapseUndoOperations(g);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Done",
            "Main Camera, AudioListener, EventSystem, and menu raycast/layout updated.\nSave scene (Ctrl+S).", "OK");
    }

    [MenuItem("CartoonZombies/Scenes/Setup GameOver scene", false, 12)]
    public static void SetupGameOver()
    {
        if (!ConfirmActiveScene("GameOver")) return;
        EnsureMenuSceneCameraAndAudio();
        EnsureEventSystem();
        CreateMenuCanvas("GameOverCanvas", typeof(GameOverScreen));
        EditorUtility.DisplayDialog("Done", "GameOver Canvas created.", "OK");
    }

    [MenuItem("CartoonZombies/Scenes/Setup Win scene", false, 13)]
    public static void SetupWin()
    {
        if (!ConfirmActiveScene("Win")) return;
        EnsureMenuSceneCameraAndAudio();
        EnsureEventSystem();
        CreateMenuCanvas("WinCanvas", typeof(WinScreen));
        EditorUtility.DisplayDialog("Done", "Win Canvas created.", "OK");
    }

    /// <summary>Screen Space UI still needs a Camera in the scene for Game view + AudioListener.</summary>
    public static void EnsureMenuSceneCameraAndAudio()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Camera[] all = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Camera c in all)
            {
                if (c != null && c.enabled && c.gameObject.activeInHierarchy)
                {
                    cam = c;
                    break;
                }
            }
        }

        if (cam == null)
        {
            GameObject camGo = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(camGo, "Main Menu Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 1f);
            camGo.transform.SetPositionAndRotation(new Vector3(0f, 1f, -10f), Quaternion.LookRotation(Vector3.forward));
        }
        else if (!cam.CompareTag("MainCamera"))
        {
            Undo.RecordObject(cam.gameObject, "Tag Main Camera");
            cam.gameObject.tag = "MainCamera";
        }

        if (cam.GetComponent<AudioListener>() == null)
        {
            Undo.AddComponent<AudioListener>(cam.gameObject);
        }
    }

    /// <summary>Dekorative UI-bilder skal ikke blokkere knapper; bedre avstand for to-linje tittel.</summary>
    public static void FixMainMenuRaycastsAndLayout(GameObject canvasRoot)
    {
        static void DisableRaycast(Transform t)
        {
            if (t == null) return;
            var img = t.GetComponent<Image>();
            if (img != null)
            {
                Undo.RecordObject(img, "Main menu UI raycast");
                img.raycastTarget = false;
            }
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                Undo.RecordObject(tmp, "Main menu UI raycast");
                tmp.raycastTarget = false;
            }
        }

        DisableRaycast(canvasRoot.transform.Find("Background"));
        DisableRaycast(canvasRoot.transform.Find("TitleStrip"));
        DisableRaycast(canvasRoot.transform.Find("TitleText"));
        DisableRaycast(canvasRoot.transform.Find("HighScoreText"));

        static void LayoutRt(Transform t, Vector2 pos, Vector2? size = null)
        {
            if (t == null) return;
            RectTransform rt = t.GetComponent<RectTransform>();
            if (rt == null) return;
            Undo.RecordObject(rt, "Main menu layout");
            rt.anchoredPosition = pos;
            if (size.HasValue) rt.sizeDelta = size.Value;
        }

        LayoutRt(canvasRoot.transform.Find("TitleText"), new Vector2(0f, 150f), new Vector2(880f, 150f));
        LayoutRt(canvasRoot.transform.Find("HighScoreText"), new Vector2(0f, 10f), new Vector2(720f, 44f));
        LayoutRt(canvasRoot.transform.Find("PlayBtn"), new Vector2(0f, -70f));
        LayoutRt(canvasRoot.transform.Find("KeybindsBtn"), new Vector2(0f, -140f));
        LayoutRt(canvasRoot.transform.Find("QuitBtn"), new Vector2(0f, -210f));

        Transform kb = canvasRoot.transform.Find("KeybindPanel");
        if (kb != null)
        {
            Undo.RecordObject(kb.gameObject, "Hide keybind panel");
            kb.gameObject.SetActive(false);
            Transform kt = kb.Find("KeybindText");
            if (kt != null)
            {
                var tmp = kt.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    Undo.RecordObject(tmp, "Keybind help text");
                    tmp.text = MainMenuKeybindHelpText;
                }
            }
        }
    }

    // isCity: Level01_By → spillnivå 1 + WaveData_Zone2; ellers Level02_StrandSkog → nivå 2 + WaveData_Zone3
    private static void SetupZoneSceneForFile(bool isCity)
    {
        string sceneName     = isCity ? "Level01_By" : "Level02_StrandSkog";
        int progressionZone  = isCity ? 1 : 2;
        int waveAssetTier    = isCity ? 2 : 3;
        if (!ConfirmActiveScene(sceneName)) return;

        // 1. Gulv-plane for NavMesh (kan erstattes med Kenney-miljø)
        CreateFloor();

        // 2. Spawn-punkter
        GameObject spawnRoot = CreateOrFind("SpawnPoints");
        Vector3[] spawnPositions = { new Vector3(5,0,5), new Vector3(-5,0,5), new Vector3(5,0,-5), new Vector3(-5,0,-5) };
        for (int i = 0; i < spawnPositions.Length; i++)
        {
            string spName = $"SpawnPoint{i+1}";
            if (spawnRoot.transform.Find(spName) == null)
            {
                GameObject sp = new GameObject(spName);
                sp.transform.SetParent(spawnRoot.transform);
                sp.transform.position = spawnPositions[i];
            }
        }

        // 3. ZombieSpawner + ZoneManager
        GameObject spawnerObj = CreateOrFind("ZombieSpawner");
        ZombieSpawner spawner = GetOrAdd<ZombieSpawner>(spawnerObj);
        ZoneManager   zm      = GetOrAdd<ZoneManager>(spawnerObj);

        // Setter ZoneNumber via SerializedObject (privat felt)
        SerializedObject soZM = new SerializedObject(zm);
        soZM.FindProperty("zoneNumber").SetValue(progressionZone);
        soZM.ApplyModifiedProperties();

        // Tilordner WaveData (filnavn følger fortsatt gamle sone-indeks 2/3)
        string waveDataPath = $"Assets/ScriptableObjects/Waves/WaveData_Zone{waveAssetTier}.asset";
        WaveData wd = AssetDatabase.LoadAssetAtPath<WaveData>(waveDataPath);
        if (wd != null)
        {
            SerializedObject soSpawner = new SerializedObject(spawner);
            SerializedProperty wavesProp = soSpawner.FindProperty("waves");
            wavesProp.arraySize = 1;
            wavesProp.GetArrayElementAtIndex(0).objectReferenceValue = wd;
            soSpawner.ApplyModifiedProperties();
        }

        // Tilordner spawn-punkter til spawner
        SerializedObject soSp = new SerializedObject(spawner);
        SerializedProperty spawnProp = soSp.FindProperty("spawnPoints");
        spawnProp.arraySize = spawnPositions.Length;
        for (int i = 0; i < spawnPositions.Length; i++)
            spawnProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnRoot.transform.GetChild(i);
        soSp.ApplyModifiedProperties();

        // 4. Zone-trigger (exit-portal) - BoxCollider MÅ legges til FØR ZoneTrigger (RequireComponent)
        GameObject triggerObj = CreateOrFind("ZoneTrigger");
        BoxCollider bc = GetOrAdd<BoxCollider>(triggerObj);
        bc.isTrigger = true;
        bc.size      = new Vector3(3f, 3f, 1f);
        GetOrAdd<ZoneTrigger>(triggerObj);
        triggerObj.transform.position = new Vector3(0f, 1.5f, 15f); // fremst i scenen

        // 5. Spiller
        SetupPlayerInScene();

        // 6. HUD Canvas
        CreateHUDCanvas();

        // 7. PauseMenu Canvas
        CreatePauseMenuCanvas();

        // 8. CheatMenu Canvas
        CreateCheatMenuCanvas();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Done",
            $"{sceneName} ferdig (spillnivå {progressionZone}).\n\n" +
            "NavMesh was baked on Floor.\n\n" +
            "Still to do (manual):\n" +
            "1. Place environment art (e.g. Kenney) in the scene\n" +
            "2. Move ZoneTrigger to the right spot\n" +
            "3. Save the scene (Ctrl+S)", "OK");
    }

    private static void CreateFloor()
    {
        if (GameObject.Find("Floor") != null) return;
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(5f, 1f, 5f); // 50x50 meter
        floor.isStatic = true;

        // Setter materialet til en enkel grå farge
        Renderer r = floor.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.3f, 0.3f, 0.3f);
        r.sharedMaterial = mat;

        // Unity 6: NavMesh bakes via NavMeshSurface-komponenten (ikke Bake-knapp i Navigation-vinduet)
        NavMeshSurface surface = floor.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.BuildNavMesh();
    }

    private static void SetupPlayerInScene()
    {
        Vector3 spawnPos = new Vector3(0f, 1f, -8f);
        GameObject spawnRoot = GameObject.Find("SpawnPoints");
        if (spawnRoot != null && spawnRoot.transform.childCount > 0)
            spawnPos = spawnRoot.transform.GetChild(0).position;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            string playerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
            if (playerPrefab == null)
            {
                Debug.LogWarning("Player prefab not found. Run 'Setup Player Prefab' first.");
                return;
            }

            player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            player.tag = "Player";
        }

        // Synk til første spawn, deretter treff bakke med raycast (Kenney-kart uten «Floor»-plan).
        Physics.SyncTransforms();
        Undo.RecordObject(player.transform, "Player spawn align");
        float stand = GetCharacterControllerStandOffset(player);
        player.transform.position = SnapSpawnPositionToGround(spawnPos, stand);

        EnsureCameraFollow(player);
    }

    private static float GetCharacterControllerStandOffset(GameObject player)
    {
        if (player != null && player.TryGetComponent<CharacterController>(out var cc))
            return cc.height * 0.5f + cc.skinWidth + 0.08f;
        return 1.1f;
    }

    /// <summary>Raycast ned fra høyde for å treffe mesh/terreng med collider under SpawnPoint.</summary>
    private static Vector3 SnapSpawnPositionToGround(Vector3 probeWorld, float standHeight)
    {
        const float fromAbove = 4000f;
        const float maxDown = 8000f;
        Vector3 origin = probeWorld + Vector3.up * fromAbove;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDown, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
            return hit.point + Vector3.up * standHeight;

        origin = probeWorld + Vector3.up * 80f;
        if (Physics.Raycast(origin, Vector3.down, out hit, 300f, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
            return hit.point + Vector3.up * standHeight;

        Debug.LogWarning(
            $"[Repair] Ingen collider traff under spawn ved {probeWorld}. " +
            "Flytt SpawnPoint1 over synlig bakke, eller slå på Mesh Collider på miljø. " +
            "Bruk Repair på nytt etterpå.");
        return probeWorld + Vector3.up * standHeight;
    }

    // Kalles alltid — legger til CameraFollow hvis den mangler og kobler target til spilleren
    private static void EnsureCameraFollow(GameObject player)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        CameraFollow cf = mainCam.gameObject.GetComponent<CameraFollow>();
        if (cf == null) cf = mainCam.gameObject.AddComponent<CameraFollow>();

        SerializedObject so = new SerializedObject(cf);
        so.FindProperty("target").objectReferenceValue = player.transform;
        so.ApplyModifiedProperties();
    }

    private static void CreateHUDCanvas()
    {
        if (GameObject.Find("HUDCanvas") != null) return;

        GameObject canvasObj = new GameObject("HUDCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        canvasObj.AddComponent<HUDController>();

        const float topBarH = 56f;
        Color topStripCol = new Color(0.08f, 0.1f, 0.16f, 0.93f);

        // Én stripe øverst — tekst uten ekstra «piller» (unngår dobbel skygge / feiljustering)
        GameObject topBar = new GameObject("TopBar");
        topBar.transform.SetParent(canvasObj.transform, false);
        topBar.transform.SetAsFirstSibling();
        Image topImg = topBar.AddComponent<Image>();
        topImg.color = topStripCol;
        RectTransform topRt = topBar.GetComponent<RectTransform>();
        topRt.anchorMin = Vector2.up; topRt.anchorMax = Vector2.one;
        topRt.offsetMin = new Vector2(0, -topBarH); topRt.offsetMax = Vector2.zero;
        topRt.pivot     = new Vector2(0.5f, 1f);

        var healthTMP = MakeText(canvasObj, "HealthText", "HP  100 / 100", 24,
            anchor: new Vector2(0, 1), pivot: new Vector2(0, 1),
            pos: new Vector2(18, -10), size: new Vector2(300, 40));

        var waveTMP = MakeText(canvasObj, "WaveText",
            "Bølge 1 / 1  ·  <color=#FFB347>Zombier igjen: 0</color>", 24,
            anchor: new Vector2(0.5f, 1), pivot: new Vector2(0.5f, 1),
            pos: new Vector2(0, -10), size: new Vector2(640, 40),
            align: TextAlignmentOptions.Center);
        waveTMP.richText = true;

        var killsTMP = MakeText(canvasObj, "KillsText", "Kills: 0", 24,
            anchor: new Vector2(1, 1), pivot: new Vector2(1, 1),
            pos: new Vector2(-18, -10), size: new Vector2(220, 40),
            align: TextAlignmentOptions.Right);

        // ── Ammo-boks — nederst høyre ───────────────────────────────
        GameObject ammoBox = new GameObject("AmmoBox");
        ammoBox.transform.SetParent(canvasObj.transform, false);
        ammoBox.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
        RectTransform ammoRt = ammoBox.GetComponent<RectTransform>();
        ammoRt.anchorMin = Vector2.zero; ammoRt.anchorMax = Vector2.zero;
        ammoRt.pivot     = Vector2.zero;
        ammoRt.anchoredPosition = new Vector2(Screen.width - 180, 10); // fallback; scaler ordner dette
        // Bruk fast pixel-posisjon fra høyre kant
        ammoRt.anchorMin = new Vector2(1, 0); ammoRt.anchorMax = new Vector2(1, 0);
        ammoRt.pivot     = new Vector2(1, 0);
        ammoRt.anchoredPosition = new Vector2(-12, 12);
        ammoRt.sizeDelta = new Vector2(178, 56);

        var ammoTMP = MakeText(ammoBox, "AmmoText", "30 / 30", 28,
            anchor: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            pos: Vector2.zero, size: new Vector2(160, 40),
            align: TextAlignmentOptions.Center);

        // ── Reloader-tekst — litt under midten ──────────────────────
        var reloadTMP = MakeText(canvasObj, "ReloadText", "RELOADING...", 30,
            anchor: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            pos: new Vector2(0, -100), size: new Vector2(320, 48),
            align: TextAlignmentOptions.Center);
        reloadTMP.color = new Color(1f, 0.85f, 0.2f, 1f);
        // Game-fanen i editoren kjører ikke Start() — må være av som standard i scenen
        reloadTMP.gameObject.SetActive(false);

        // ── Crosshair — absolutt midten ──────────────────────────────
        var chGo = new GameObject("Crosshair");
        chGo.transform.SetParent(canvasObj.transform, false);
        var ch = chGo.AddComponent<TextMeshProUGUI>();
        ch.text = "+"; ch.fontSize = 32; ch.alignment = TextAlignmentOptions.Center;
        ch.color = new Color(1f, 1f, 1f, 0.9f);
        var chRt = chGo.GetComponent<RectTransform>();
        chRt.anchorMin = chRt.anchorMax = new Vector2(0.5f, 0.5f);
        chRt.pivot = new Vector2(0.5f, 0.5f);
        chRt.anchoredPosition = Vector2.zero;
        chRt.sizeDelta = new Vector2(50, 50);

        // ── Kobler referanser til HUDController ─────────────────────
        HUDController hud = canvasObj.GetComponent<HUDController>();
        SerializedObject so = new SerializedObject(hud);
        so.FindProperty("healthText").objectReferenceValue  = healthTMP;
        so.FindProperty("ammoText").objectReferenceValue    = ammoTMP;
        so.FindProperty("killsText").objectReferenceValue   = killsTMP;
        so.FindProperty("waveText").objectReferenceValue    = waveTMP;
        so.FindProperty("reloadText").objectReferenceValue  = reloadTMP;
        so.ApplyModifiedProperties();

        GameObject ctrlPanel = new GameObject("ControlHintPanel");
        ctrlPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform ctrlPanelRt = ctrlPanel.AddComponent<RectTransform>();
        ctrlPanelRt.anchorMin = Vector2.zero; ctrlPanelRt.anchorMax = Vector2.zero;
        ctrlPanelRt.pivot = Vector2.zero;
        ctrlPanelRt.anchoredPosition = new Vector2(14, 148);
        ctrlPanelRt.sizeDelta = new Vector2(620, 44);
        ctrlPanel.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.12f, 0.82f);

        TextMeshProUGUI ctrlTMP = MakeText(ctrlPanel, "ControlHintsText",
            "WASD = gå  ·  Mus = se  ·  Skyt = venstreklikk  ·  R = reload  ·  ESC = pause  ·  Y = cheat  ·  F = bil",
            14,
            anchor: new Vector2(0, 0), pivot: new Vector2(0, 0),
            pos: new Vector2(14, 10), size: new Vector2(670, 40),
            align: TextAlignmentOptions.Left);
        ctrlTMP.fontStyle = FontStyles.Normal;
        ctrlTMP.color = new Color(0.93f, 0.94f, 0.96f, 0.95f);
        ctrlTMP.textWrappingMode = TextWrappingModes.Normal;

        GameObject missionPanel = new GameObject("MissionHintPanel");
        missionPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform missionPanelRt = missionPanel.AddComponent<RectTransform>();
        missionPanelRt.anchorMin = new Vector2(0.5f, 0f);
        missionPanelRt.anchorMax = new Vector2(0.5f, 0f);
        missionPanelRt.pivot = new Vector2(0.5f, 0f);
        missionPanelRt.anchoredPosition = new Vector2(0, 22);
        missionPanelRt.sizeDelta = new Vector2(860, 86);
        missionPanel.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.1f, 0.78f);

        TextMeshProUGUI missionTMP = MakeText(missionPanel, "MissionObjectiveText", " ", 17,
            anchor: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            pos: Vector2.zero, size: new Vector2(820, 78),
            align: TextAlignmentOptions.Center);
        missionTMP.richText = true;

        GameObject compassRoot = new GameObject("EnemyCompass");
        compassRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform compassRootRt = compassRoot.AddComponent<RectTransform>();
        compassRootRt.anchorMin = compassRootRt.anchorMax = new Vector2(0.5f, 0f);
        compassRootRt.pivot = new Vector2(0.5f, 0f);
        compassRootRt.anchoredPosition = new Vector2(0f, 228f);
        compassRootRt.sizeDelta = new Vector2(76f, 76f);
        CanvasGroup compassCg = compassRoot.AddComponent<CanvasGroup>();
        compassCg.alpha = 0f;
        compassCg.blocksRaycasts = false;
        compassCg.interactable = false;
        Image compassRing = compassRoot.AddComponent<Image>();
        compassRing.color = new Color(0.12f, 0.13f, 0.18f, 0.78f);

        GameObject arrowGo = new GameObject("Arrow");
        arrowGo.transform.SetParent(compassRoot.transform, false);
        RectTransform arrowRt = arrowGo.AddComponent<RectTransform>();
        arrowRt.anchorMin = arrowRt.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRt.anchoredPosition = Vector2.zero;
        arrowRt.sizeDelta = new Vector2(28f, 36f);
        Image arrowImg = arrowGo.AddComponent<Image>();
        arrowImg.color = new Color(1f, 0.52f, 0.12f, 1f);

        EnemyCompassHUD compassHud = compassRoot.AddComponent<EnemyCompassHUD>();
        SerializedObject soCompass = new SerializedObject(compassHud);
        soCompass.FindProperty("arrow").objectReferenceValue = arrowRt;
        soCompass.FindProperty("canvasGroup").objectReferenceValue = compassCg;
        soCompass.ApplyModifiedProperties();

        TextMeshProUGUI hintTMP = MakeText(canvasObj, "InteractionHintText", "", 20,
            anchor: new Vector2(0.5f, 0), pivot: new Vector2(0.5f, 0),
            pos: new Vector2(0, 118), size: new Vector2(880, 32),
            align: TextAlignmentOptions.Center);
        hintTMP.color = new Color(1f, 0.95f, 0.65f, 1f);

        InteractionHint ih = canvasObj.AddComponent<InteractionHint>();
        SerializedObject soIh = new SerializedObject(ih);
        soIh.FindProperty("label").objectReferenceValue = hintTMP;
        soIh.ApplyModifiedProperties();

        MissionObjectiveHUD moh = canvasObj.AddComponent<MissionObjectiveHUD>();
        SerializedObject soMoh = new SerializedObject(moh);
        soMoh.FindProperty("objectiveText").objectReferenceValue = missionTMP;
        soMoh.ApplyModifiedProperties();
    }

    // Lager TMP-tekst med eksplisitt anchor og pivot — unngår "halvparten utenfor skjermen"-feilen
    private static TextMeshProUGUI MakeText(GameObject parent, string name, string text,
        int fontSize, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
        TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = Color.white;
        tmp.alignment = align;
        tmp.fontStyle = FontStyles.Bold;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchor;
        rt.anchorMax        = anchor;
        rt.pivot            = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        return tmp;
    }

    private static void CreatePauseMenuCanvas()
    {
        if (GameObject.Find("PauseCanvas") != null) return;

        GameObject canvasObj = new GameObject("PauseCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        PauseMenu pm = canvasObj.AddComponent<PauseMenu>();

        // Panel
        GameObject panel = new GameObject("PausePanel");
        panel.transform.SetParent(canvasObj.transform, false);
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0,0,0,0.7f);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        CreateTMPText(panel, "PauseTitle", "PAUSE", Vector2.zero, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), 48);
        CreateButton(panel, "ResumeBtn",    "Fortsett",   new Vector2(0, 50),   pm, "Resume");
        CreateButton(panel, "MainMenuBtn",  "Hovedmeny",  new Vector2(0, -10),  pm, "OnMainMenuClicked");
        CreateButton(panel, "QuitBtn",      "Avslutt",    new Vector2(0, -70),  pm, "OnQuitClicked");

        panel.SetActive(false);

        SerializedObject so = new SerializedObject(pm);
        so.FindProperty("pausePanel").objectReferenceValue = panel;
        so.ApplyModifiedProperties();
    }

    public static void CreateCheatMenuCanvas()
    {
        if (GameObject.Find("CheatCanvas") != null) return;

        GameObject canvasObj = new GameObject("CheatCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // over HUD og pause
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        CheatMenu cm = canvasObj.AddComponent<CheatMenu>();

        // Bakgrunnspanel
        GameObject panel = new GameObject("CheatPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.05f, 0.05f, 0.1f, 0.88f);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta        = new Vector2(280, 360);

        // Tittel
        CreateTMPText(panel, "CheatTitle", "CHEAT-MENY  [Y]",
            new Vector2(0, 150), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 22);

        // Knapper
        CreateButton(panel, "GodModeBtn",  "Udødelig (toggle)",     new Vector2(0,  90), cm, "OnGodModeClicked");
        CreateButton(panel, "NoclipBtn",   "Noclip (toggle)",       new Vector2(0,  30), cm, "OnNoclipClicked");
        CreateButton(panel, "HealBtn",     "Full helse",            new Vector2(0, -30), cm, "OnHealClicked");
        CreateButton(panel, "KillAllBtn",  "Drep alle zombier",     new Vector2(0, -90), cm, "OnKillAllClicked");
        CreateButton(panel, "SkipBtn",     "Hopp til neste sone",   new Vector2(0,-150), cm, "OnSkipZoneClicked");

        panel.SetActive(false);

        SerializedObject soCm = new SerializedObject(cm);
        var tuning = AssetDatabase.LoadAssetAtPath<CheatMenuSettings>("Assets/ScriptableObjects/CheatMenuSettings.asset");
        if (tuning != null)
        {
            var p = soCm.FindProperty("tuning");
            if (p != null) p.objectReferenceValue = tuning;
        }
        soCm.ApplyModifiedProperties();
    }

    private static void CreateMenuCanvas(string name, System.Type scriptType)
    {
        if (GameObject.Find(name) != null) return;

        GameObject canvasObj = new GameObject(name);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        canvasObj.AddComponent(scriptType);

        // Bakgrunn - mørk med gradient-effekt via farge
        bool isGameOver = scriptType == typeof(GameOverScreen);
        bool isWin      = scriptType == typeof(WinScreen);
        Color bgColor   = isGameOver ? new Color(0.12f, 0.02f, 0.02f, 1f)   // mørk rød
                        : isWin      ? new Color(0.02f, 0.10f, 0.02f, 1f)   // mørk grønn
                        :              new Color(0.06f, 0.08f, 0.14f, 1f);  // mørk blå

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = bgColor;
        bgImg.raycastTarget = false;
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

        // Tittel-panel (strip bak tittelen)
        GameObject titleStrip = new GameObject("TitleStrip");
        titleStrip.transform.SetParent(canvasObj.transform, false);
        var tsImg = titleStrip.AddComponent<Image>();
        tsImg.color = new Color(0f, 0f, 0f, 0.3f);
        tsImg.raycastTarget = false;
        RectTransform tsRt = titleStrip.GetComponent<RectTransform>();
        tsRt.anchorMin = new Vector2(0, 0.5f); tsRt.anchorMax = new Vector2(1, 0.5f);
        tsRt.offsetMin = new Vector2(0, 60); tsRt.offsetMax = new Vector2(0, 140);

        string title = scriptType == typeof(MainMenuController) ? "CARTOON ZOMBIES"
                     : scriptType == typeof(GameOverScreen)     ? "GAME OVER"
                     : "DU VANT!";

        Color titleColor = isGameOver ? new Color(1f, 0.3f, 0.3f, 1f)
                         : isWin      ? new Color(0.4f, 1f, 0.4f, 1f)
                         :              new Color(1f, 0.85f, 0.3f, 1f);

        var titleTMP = CreateTMPText(canvasObj, "TitleText", title, new Vector2(0, 150), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 68);
        titleTMP.color     = titleColor;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.raycastTarget = false;
        {
            RectTransform trt = titleTMP.GetComponent<RectTransform>();
            trt.sizeDelta = new Vector2(880f, 150f);
        }

        if (scriptType == typeof(MainMenuController))
        {
            var hsText = CreateTMPText(canvasObj, "HighScoreText", "Rekord: 0 kills", new Vector2(0, 10), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 26);
            hsText.color = new Color(0.9f, 0.9f, 0.5f, 1f);
            hsText.raycastTarget = false;
            {
                RectTransform hsRt = hsText.GetComponent<RectTransform>();
                hsRt.sizeDelta = new Vector2(720f, 44f);
            }
            var ctrl = canvasObj.GetComponent<MainMenuController>();
            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("highScoreText").objectReferenceValue = hsText;
            so.ApplyModifiedProperties();

            CreateButton(canvasObj, "PlayBtn",     "▶  SPILL",       new Vector2(0, -70),  ctrl, "OnPlayClicked");
            CreateButton(canvasObj, "KeybindsBtn", "Kontroller",      new Vector2(0, -140), ctrl, "OnKeybindsClicked");
            CreateButton(canvasObj, "QuitBtn",     "Avslutt",         new Vector2(0, -210), ctrl, "OnQuitClicked");

            // Keybind-panel
            GameObject kbPanel = new GameObject("KeybindPanel");
            kbPanel.transform.SetParent(canvasObj.transform, false);
            kbPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);
            RectTransform kbRt = kbPanel.GetComponent<RectTransform>();
            kbRt.anchorMin = new Vector2(0.5f,0.5f); kbRt.anchorMax = new Vector2(0.5f,0.5f);
            kbRt.sizeDelta = new Vector2(340, 240);
            CreateTMPText(kbPanel, "KeybindText", MainMenuKeybindHelpText,
                Vector2.zero, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), 20);
            kbPanel.SetActive(false);
            so = new SerializedObject(ctrl);
            so.FindProperty("keybindPanel").objectReferenceValue = kbPanel;
            so.ApplyModifiedProperties();
        }
        else if (scriptType == typeof(GameOverScreen))
        {
            var ctrl = canvasObj.GetComponent<GameOverScreen>();
            var killsTMP = CreateTMPText(canvasObj, "KillsText",    "Du drepte 0 zombier", new Vector2(0, 14),   new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), 30);
            var zoneTMP  = CreateTMPText(canvasObj, "ZoneText",     "Du nådde sone 1",     new Vector2(0, -26),  new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), 24);
            var hsTMP    = CreateTMPText(canvasObj, "HighScoreText","Rekord: 0 kills",      new Vector2(0, -56),  new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), 20);
            killsTMP.color = Color.white;
            zoneTMP.color  = new Color(0.8f, 0.8f, 0.8f, 1f);
            hsTMP.color    = new Color(0.9f, 0.9f, 0.5f, 1f);
            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("killsText").objectReferenceValue     = killsTMP;
            so.FindProperty("zoneText").objectReferenceValue      = zoneTMP;
            so.FindProperty("highScoreText").objectReferenceValue = hsTMP;
            so.ApplyModifiedProperties();
            CreateButton(canvasObj, "RetryBtn", "Prøv igjen",  new Vector2(0, -110), ctrl, "OnRetryClicked");
            CreateButton(canvasObj, "MenuBtn",  "Hovedmeny",   new Vector2(0, -180), ctrl, "OnMainMenuClicked");
            CreateButton(canvasObj, "QuitBtn",  "Avslutt",     new Vector2(0, -250), ctrl, "OnQuitClicked");
        }
        else // WinScreen
        {
            var ctrl = canvasObj.GetComponent<WinScreen>();
            var killsTMP = CreateTMPText(canvasObj, "KillsText",     "Totalt: 0 zombier drept!", new Vector2(0, 14),  new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), 30);
            var hsTMP    = CreateTMPText(canvasObj, "HighScoreText", "Ny rekord: 0 kills",        new Vector2(0, -26), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), 22);
            killsTMP.color = Color.white;
            hsTMP.color    = new Color(0.9f, 0.9f, 0.5f, 1f);
            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("killsText").objectReferenceValue     = killsTMP;
            so.FindProperty("highScoreText").objectReferenceValue = hsTMP;
            so.ApplyModifiedProperties();
            CreateButton(canvasObj, "PlayAgainBtn", "▶  Spill igjen", new Vector2(0, -100), ctrl, "OnPlayAgainClicked");
            CreateButton(canvasObj, "MenuBtn",      "Hovedmeny",       new Vector2(0, -170), ctrl, "OnMainMenuClicked");
            CreateButton(canvasObj, "QuitBtn",      "Avslutt",         new Vector2(0, -240), ctrl, "OnQuitClicked");
        }

        EnsureEventSystem();
    }

    // --- HJELPEMETODER ---

    private static TextMeshProUGUI CreateTMPText(GameObject parent, string name, string text,
        Vector2 anchoredPos, Vector2 anchorMin, Vector2 anchorMax, int fontSize = 24)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin       = anchorMin;
        rt.anchorMax       = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta       = new Vector2(400, 40);
        return tmp;
    }

    private static void CreateButton(GameObject parent, string name, string label,
        Vector2 pos, Component target, string methodName)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent.transform, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.15f, 0.45f, 0.15f, 1f);
        UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();

        // Hover-farger
        var colors = btn.colors;
        colors.normalColor      = new Color(0.15f, 0.45f, 0.15f, 1f);
        colors.highlightedColor = new Color(0.25f, 0.65f, 0.25f, 1f);
        colors.pressedColor     = new Color(0.10f, 0.30f, 0.10f, 1f);
        btn.colors = colors;

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(260, 58);

        // Tekstlabel på knappen
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        RectTransform trt = txtObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

        // Kobler onClick-event
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
            btn.onClick,
            System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction),
                target, methodName) as UnityEngine.Events.UnityAction);
    }

    private static GameObject CreateOrFind(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        return go;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    // Batch-variant — waveAssetTier 2 = by (Zone2), 3 = strand (Zone3). ZoneManager.zoneNumber blir 1 eller 2.
    public static void SetupZoneSceneBatch(int waveAssetTier)
    {
        int progressionZone = waveAssetTier == 2 ? 1 : 2;

        CreateFloor();

        GameObject spawnRoot = CreateOrFind("SpawnPoints");
        Vector3[] spawnPositions = { new Vector3(5,0,5), new Vector3(-5,0,5), new Vector3(5,0,-5), new Vector3(-5,0,-5) };
        for (int i = 0; i < spawnPositions.Length; i++)
        {
            string spName = $"SpawnPoint{i+1}";
            if (spawnRoot.transform.Find(spName) != null) continue;
            GameObject sp = new GameObject(spName);
            sp.transform.SetParent(spawnRoot.transform);
            sp.transform.position = spawnPositions[i];
        }

        GameObject zmObj = CreateOrFind("ZombieSpawner");
        zmObj.transform.position = Vector3.zero;
        ZoneManager zm  = GetOrAdd<ZoneManager>(zmObj);
        ZombieSpawner spawner = GetOrAdd<ZombieSpawner>(zmObj);

        SerializedObject soZM = new SerializedObject(zm);
        soZM.FindProperty("zoneNumber").SetValue(progressionZone);
        soZM.ApplyModifiedProperties();

        string waveDataPath = $"Assets/ScriptableObjects/Waves/WaveData_Zone{waveAssetTier}.asset";
        WaveData wd = AssetDatabase.LoadAssetAtPath<WaveData>(waveDataPath);
        if (wd != null)
        {
            SerializedObject soSpawner = new SerializedObject(spawner);
            SerializedProperty wavesProp = soSpawner.FindProperty("waves");
            wavesProp.arraySize = 1;
            wavesProp.GetArrayElementAtIndex(0).objectReferenceValue = wd;
            soSpawner.ApplyModifiedProperties();
        }

        SerializedObject soSp = new SerializedObject(spawner);
        SerializedProperty spawnProp = soSp.FindProperty("spawnPoints");
        spawnProp.arraySize = spawnPositions.Length;
        for (int i = 0; i < spawnPositions.Length; i++)
            spawnProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnRoot.transform.GetChild(i);
        soSp.ApplyModifiedProperties();

        GameObject triggerObj = CreateOrFind("ZoneTrigger");
        BoxCollider bc = GetOrAdd<BoxCollider>(triggerObj);
        bc.isTrigger = true;
        bc.size      = new Vector3(3f, 3f, 1f);
        GetOrAdd<ZoneTrigger>(triggerObj);
        triggerObj.transform.position = new Vector3(0f, 1.5f, 15f);

        SetupPlayerInScene();
        CreateHUDCanvas();
        CreatePauseMenuCanvas();
    }

    // Kjøres alltid fra batch — sørger for CameraFollow på Main Camera
    public static void FixCameraInScene()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) { Debug.LogWarning("[FixCamera] No Player in scene."); return; }
        EnsureCameraFollow(player);
    }

    // Sletter og gjenskaper HUD med forbedret layout
    public static void RebuildHUDCanvas()
    {
        GameObject existing = GameObject.Find("HUDCanvas");
        if (existing != null) Object.DestroyImmediate(existing);
        CreateHUDCanvas();
    }

    private static bool ConfirmActiveScene(string expected)
    {
        string active = SceneManager.GetActiveScene().name;
        if (active != expected)
        {
            EditorUtility.DisplayDialog("Wrong scene",
                $"Open the {expected} scene first.\nActive scene: {active}", "OK");
            return false;
        }
        return true;
    }
}

// Hjelpemetode for SerializedProperty int
public static class SerializedPropertyExtensions
{
    public static void SetValue(this SerializedProperty prop, int value) => prop.intValue = value;
}
