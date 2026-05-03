#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// Lager spillklare varianter av ArtStore3D «Zombie Cartoon» (NavMesh + ZombieAI + helse + Supercyan Animator)
// Originalprefabene mangler gameplay-komponenter og har ikke Animator Controller — dette er nødvendig for spawneren.
public static class ZombieCartoonGameplayPrefabBuilder
{
    const string ControllerPath =
        "Assets/ThirdParty/Supercyan Character Pack Zombie Sample/AnimatorControllers/FreeZombieController.controller";

    const string DeathSoundPath = "Assets/Audio/SoundForMyGame/ZombieMoan.wav";
    const string DeathVfxPath =
        "Assets/ThirdParty/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Eerie/CFXR2 WW Enemy Explosion.prefab";

    static readonly string[] SourcePrefabs =
    {
        "Assets/ThirdParty/ArtStore3D/Zombie Cartoon/Prefab/Zombie Cartoon_01.prefab",
        "Assets/ThirdParty/ArtStore3D/Zombie Cartoon/Prefab/Zombie Cartoon_02.prefab"
    };

    [MenuItem("CartoonZombies/Setup/04 Zombie Cartoon → gameplay prefabs + WaveData variants", false, 13)]
    public static void BuildAndRegister()
    {
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Zombie Cartoon",
                "Fant ikke FreeZombieController:\n" + ControllerPath, "OK");
            return;
        }

        var deathSound = AssetDatabase.LoadAssetAtPath<AudioClip>(DeathSoundPath);
        var deathVfx   = AssetDatabase.LoadAssetAtPath<GameObject>(DeathVfxPath);

        GameObject[] built = new GameObject[SourcePrefabs.Length];

        for (int i = 0; i < SourcePrefabs.Length; i++)
        {
            string src = SourcePrefabs[i];
            if (AssetDatabase.LoadAssetAtPath<GameObject>(src) == null)
            {
                EditorUtility.DisplayDialog("Zombie Cartoon", "Fant ikke prefab:\n" + src, "OK");
                return;
            }

            string baseName = System.IO.Path.GetFileNameWithoutExtension(src).Replace(" ", "");
            string outPath  = $"Assets/Prefabs/Zombies/{baseName}_Gameplay.prefab";

            GameObject root = PrefabUtility.LoadPrefabContents(src);
            try
            {
                EnsureGameplayComponents(root, controller, deathSound, deathVfx);
                PrefabUtility.SaveAsPrefabAsset(root, outPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            built[i] = AssetDatabase.LoadAssetAtPath<GameObject>(outPath);
        }

        RegisterWaveVariants(built);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Zombie Cartoon",
            "Opprettet/oppdatert gameplay-prefaber under Assets/Prefabs/Zombies/ (*_Gameplay).\n" +
            "WaveData_Zone2 og WaveData_Zone3 har nå zombiePrefabVariants satt til disse to.\n\n" +
            "Hoved-prefab forbearer FreeZombie; spawneren velger tilfeldig mellom hoved + varianter.",
            "OK");
    }

    static void EnsureGameplayComponents(GameObject root, RuntimeAnimatorController controller,
        AudioClip deathSound, GameObject deathVfx)
    {
        var agent = root.GetComponent<NavMeshAgent>();
        if (agent == null) agent = root.AddComponent<NavMeshAgent>();
        agent.radius           = 0.5f;
        agent.height           = 2f;
        agent.speed            = 3.5f;
        agent.acceleration     = 8f;
        agent.angularSpeed     = 120f;
        agent.stoppingDistance = 0f;
        agent.baseOffset       = 0f;

        var animator = root.GetComponent<Animator>();
        if (animator == null) animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion           = false;

        if (root.GetComponent<ZombieAI>() == null)
            root.AddComponent<ZombieAI>();

        ZombieHealth zh = root.GetComponent<ZombieHealth>();
        if (zh == null) zh = root.AddComponent<ZombieHealth>();

        var soZh = new SerializedObject(zh);
        if (deathSound != null)
            soZh.FindProperty("deathSound").objectReferenceValue = deathSound;
        if (deathVfx != null)
            soZh.FindProperty("deathVfxPrefab").objectReferenceValue = deathVfx;
        soZh.ApplyModifiedPropertiesWithoutUndo();

        var cap = root.GetComponent<CapsuleCollider>();
        if (cap == null) cap = root.AddComponent<CapsuleCollider>();
        cap.height = 1.8f;
        cap.radius = 0.3f;
        cap.center = new Vector3(0f, 0.9f, 0f);
    }

    static void RegisterWaveVariants(GameObject[] variants)
    {
        string[] wavePaths =
        {
            "Assets/ScriptableObjects/Waves/WaveData_Zone2.asset",
            "Assets/ScriptableObjects/Waves/WaveData_Zone3.asset"
        };

        foreach (string path in wavePaths)
        {
            var wd = AssetDatabase.LoadAssetAtPath<WaveData>(path);
            if (wd == null) continue;

            var so = new SerializedObject(wd);
            SerializedProperty prop = so.FindProperty("zombiePrefabVariants");
            prop.arraySize = variants.Length;
            for (int i = 0; i < variants.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = variants[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
