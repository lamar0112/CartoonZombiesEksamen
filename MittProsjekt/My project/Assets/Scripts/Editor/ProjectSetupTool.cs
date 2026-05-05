using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// ProjectSetupTool — tidlige «Setup»-menyer for prefabs og grunnleggende assets.
// Pensum: prefab-arbeid, Animator, enkel pipeline (PG2202-08).
// Ekstra: gruppe-spesifikke stier (ithappy, Kenney); kjøres kun i editor — kan fjernes etter eksamen om ønskelig.
public class ProjectSetupTool
{
    [MenuItem("CartoonZombies/Setup/01 Zombie prefab", false, 10)]
    public static void SetupZombiePrefab()
    {
        string[] guids = AssetDatabase.FindAssets("FreeZombie t:Prefab");
        GameObject zombiePrefab = null;
        string prefabPath = "";

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Supercyan") && path.Contains("Base") && !path.Contains("Simple") && !path.Contains("Mobile"))
            {
                zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                prefabPath   = path;
                break;
            }
        }

        if (zombiePrefab == null) { EditorUtility.DisplayDialog("Error", "FreeZombie prefab not found.", "OK"); return; }

        // Finner FreeZombieController
        string[] ctrlGuids = AssetDatabase.FindAssets("FreeZombieController t:AnimatorController");
        AnimatorController controller = null;
        foreach (string guid in ctrlGuids)
        {
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GUIDToAssetPath(guid));
            break;
        }

        if (controller == null) { EditorUtility.DisplayDialog("Error", "FreeZombieController not found.", "OK"); return; }

        // Legger til Animator-parametere
        AddParameterIfMissing(controller, "Speed",  AnimatorControllerParameterType.Float);
        AddParameterIfMissing(controller, "Attack", AnimatorControllerParameterType.Trigger);
        AddParameterIfMissing(controller, "Death",  AnimatorControllerParameterType.Trigger);
        EditorUtility.SetDirty(controller);

        // Redigerer prefaben og fjerner duplikater
        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;

            // Tilordner controller
            Animator animator = root.GetComponentInChildren<Animator>();
            if (animator != null) animator.runtimeAnimatorController = controller;

            // Fjerner duplikater og sørger for kun én av hver komponent
            RemoveDuplicates<ZombieAI>(root);
            RemoveDuplicates<ZombieHealth>(root);

            // Legger til hvis de mangler
            if (root.GetComponent<ZombieAI>()     == null) root.AddComponent<ZombieAI>();
            if (root.GetComponent<ZombieHealth>()  == null) root.AddComponent<ZombieHealth>();

            // CapsuleCollider er nødvendig for at raycast-skyting skal treffe zombien
            CapsuleCollider cap = root.GetComponent<CapsuleCollider>();
            if (cap == null) cap = root.AddComponent<CapsuleCollider>();
            cap.height = 1.8f;
            cap.radius = 0.3f;
            cap.center = new Vector3(0f, 0.9f, 0f);

            // Tilordner ZombieMoan som dødslyd
            ZombieHealth zh = root.GetComponent<ZombieHealth>();
            if (zh != null)
            {
                SerializedObject soZh = new SerializedObject(zh);
                soZh.FindProperty("deathSound").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SoundForMyGame/ZombieMoan.wav");
                soZh.ApplyModifiedProperties();
            }
        }

        // Kopierer til Prefabs/Zombies/ om den ikke er der allerede
        string destPath = "Assets/Prefabs/Zombies/FreeZombie.prefab";
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(destPath))
            AssetDatabase.CopyAsset(prefabPath, destPath);

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Done", "Zombie prefab configured (no duplicate components).", "OK");
    }

    // Fjerner alle duplikat-komponenter av type T, beholder kun den første
    private static void RemoveDuplicates<T>(GameObject go) where T : Component
    {
        T[] components = go.GetComponents<T>();
        for (int i = 1; i < components.Length; i++)
            Object.DestroyImmediate(components[i]);
    }

    [MenuItem("CartoonZombies/Setup/02 Player tag", false, 11)]
    public static void AddPlayerTag()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProp.arraySize; i++)
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == "Player")
            {
                EditorUtility.DisplayDialog("Info", "Player tag already exists.", "OK");
                return;
            }

        tagsProp.InsertArrayElementAtIndex(0);
        tagsProp.GetArrayElementAtIndex(0).stringValue = "Player";
        tagManager.ApplyModifiedProperties();
        EditorUtility.DisplayDialog("Done", "Player tag added.", "OK");
    }

    [MenuItem("CartoonZombies/Setup/03 WaveData assets", false, 12)]
    public static void CreateWaveDataAssets()
    {
        string[] guids = AssetDatabase.FindAssets("FreeZombie t:Prefab", new[] { "Assets/Prefabs/Zombies" });
        GameObject zombiePrefab = guids.Length > 0
            ? AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;

        CreateWaveData("WaveData_Zone2", 8,  1.5f, zombiePrefab);
        CreateWaveData("WaveData_Zone3", 12, 1.0f, zombiePrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done", "WaveData_Zone2 and WaveData_Zone3 created under ScriptableObjects/Waves/.", "OK");
    }

    private static void CreateWaveData(string name, int count, float interval, GameObject prefab)
    {
        string path = $"Assets/ScriptableObjects/Waves/{name}.asset";
        if (AssetDatabase.LoadAssetAtPath<WaveData>(path) != null) return;

        WaveData data      = ScriptableObject.CreateInstance<WaveData>();
        data.zombieCount   = count;
        data.spawnInterval = interval;
        data.zombiePrefab  = prefab;
        AssetDatabase.CreateAsset(data, path);
    }

    [MenuItem("CartoonZombies/Project/Remove duplicate components on zombie prefabs", false, 30)]
    public static void FixDuplicates()
    {
        // Fikser duplikater på alle FreeZombie-prefaber i prosjektet
        string[] guids = AssetDatabase.FindAssets("FreeZombie t:Prefab");
        int fixed_count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = scope.prefabContentsRoot;
                int before = root.GetComponents<ZombieAI>().Length + root.GetComponents<ZombieHealth>().Length;
                RemoveDuplicates<ZombieAI>(root);
                RemoveDuplicates<ZombieHealth>(root);
                int after = root.GetComponents<ZombieAI>().Length + root.GetComponents<ZombieHealth>().Length;
                if (before != after) fixed_count++;
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Done", $"Removed duplicate components from {fixed_count} prefab(s).", "OK");
    }

    [MenuItem("CartoonZombies/Setup/04 Run setup 01–03 (tag + zombie + waves)", false, 13)]
    public static void SetupAll()
    {
        AddPlayerTag();
        SetupZombiePrefab();
        CreateWaveDataAssets();
        EditorUtility.DisplayDialog("Done", "Player tag, zombie prefab, and WaveData assets are set up.", "OK");
    }

    [MenuItem("CartoonZombies/Project/Add capsule collider to zombie prefabs", false, 31)]
    public static void FixZombieCollider()
    {
        // Patcher BEGGE prefaber: originalen og vår kopi som WaveData spawner fra
        string[] paths = {
            "Assets/Prefabs/Zombies/FreeZombie.prefab"
        };

        // Finn også originalen fra Supercyan
        string[] guids = AssetDatabase.FindAssets("FreeZombie t:Prefab");
        foreach (string guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.Contains("Supercyan") && p.Contains("Base") && !p.Contains("Simple") && !p.Contains("Mobile"))
            {
                System.Array.Resize(ref paths, paths.Length + 1);
                paths[paths.Length - 1] = p;
                break;
            }
        }

        int fixed_count = 0;
        foreach (string path in paths)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = scope.prefabContentsRoot;
                CapsuleCollider cap = root.GetComponent<CapsuleCollider>();
                if (cap == null) cap = root.AddComponent<CapsuleCollider>();
                cap.height = 1.8f;
                cap.radius = 0.3f;
                cap.center = new Vector3(0f, 0.9f, 0f);

                ZombieHealth zh = root.GetComponent<ZombieHealth>();
                if (zh != null)
                {
                    SerializedObject soZh = new SerializedObject(zh);
                    soZh.FindProperty("deathSound").objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SoundForMyGame/ZombieMoan.wav");
                    soZh.ApplyModifiedProperties();
                }
                fixed_count++;
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Done",
            $"CapsuleCollider added on {fixed_count} prefab(s).\n\n" +
            "Spawned zombies can be hit by shooting.", "OK");
    }

    [MenuItem("CartoonZombies/Project/Add scenes to Build Settings", false, 10)]
    public static void AddScenesToBuildSettingsMenu() => AddScenesToBuildSettings(showSummaryDialog: true);

    /// <summary>Adds MainMenu → levels → GameOver → Win if missing. Returns count of newly added scenes.</summary>
    public static int AddScenesToBuildSettings(bool showSummaryDialog = true)
    {
        string[] sceneNames = { "MainMenu", "Level01_By", "Level02_StrandSkog", "GameOver", "Win" };
        var existingScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        int added = 0;
        foreach (string sceneName in sceneNames)
        {
            string[] guids = AssetDatabase.FindAssets($"{sceneName} t:Scene");
            string path = "";
            foreach (string guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == sceneName) { path = p; break; }
            }

            if (string.IsNullOrEmpty(path)) { Debug.LogWarning($"Scene not found: {sceneName}"); continue; }

            bool already = existingScenes.Exists(s => s.path == path);
            if (!already)
            {
                existingScenes.Add(new EditorBuildSettingsScene(path, true));
                added++;
            }
        }

        EditorBuildSettings.scenes = existingScenes.ToArray();
        if (showSummaryDialog)
        {
            EditorUtility.DisplayDialog("Done",
                $"{added} scene(s) added to Build Settings.\n\n" +
                "Order: MainMenu → Level01_By → Level02_StrandSkog → GameOver → Win", "OK");
        }

        return added;
    }

    [MenuItem("CartoonZombies/Project/Reset saved MasterVolume (PlayerPrefs)", false, 40)]
    public static void ResetVolume()
    {
        PlayerPrefs.DeleteKey("MasterVolume");
        PlayerPrefs.Save();
        EditorUtility.DisplayDialog("Done", "Saved volume key removed. Default volume applies on next play.", "OK");
    }

    [MenuItem("CartoonZombies/Project/Set input handling to Both (restart Unity)", false, 41)]
    public static void FixInputSystem()
    {
        // Prosjektet har ny Input System installert - vi vil bruke BEGGE slik at
        // Input.GetKey/GetMouseButton fra gamle system fortsatt fungerer (PG2202-04)
        SerializedObject settings = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        SerializedProperty prop = settings.FindProperty("activeInputHandler");
        if (prop == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find activeInputHandler in ProjectSettings.", "OK");
            return;
        }
        prop.intValue = 2; // 0 = Old only, 1 = New only, 2 = Both
        settings.ApplyModifiedProperties();
        EditorUtility.DisplayDialog("Done",
            "Input handling set to Both.\n\n" +
            "Restart Unity for the change to apply.", "OK");
    }

    private static void AddParameterIfMissing(AnimatorController ctrl, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in ctrl.parameters)
            if (p.name == name) return;
        ctrl.AddParameter(name, type);
    }
}
