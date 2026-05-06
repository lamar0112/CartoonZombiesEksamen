using UnityEngine;
using UnityEditor;

// GameSettingsWindow — EditorWindow under CartoonZombies → Settings (PG2202-08 ScriptableObject + editor UI).
// Pensum: SerializedObject/Property for WaveData, GameAudioSettings, CheatMenuSettings m.m.
// Ekstra: én «kontrollsentral» for gruppa — reduserer spredte assets; faner er enkel state i OnGUI.
public class GameSettingsWindow : EditorWindow
{
    // Faner - bruker int-indeks som enkel state-maskin for hvilken fane som vises
    private int    selectedTab = 0;
    private string[] tabs      = { "Audio", "Waves", "Player", "Zombies", "Cheat (dev)" };

    private Vector2 scrollPos;

    // Bufrede referanser - lastes én gang, ikke i OnGUI (unngår allokering per frame)
    private WaveData           wave2, wave3;
    private GameAudioSettings  gameAudio;
    private CheatMenuSettings  cheatSettings;
    private GameObject         playerPrefab;
    private GameObject         zombiePrefab;

    [MenuItem("CartoonZombies/Settings", false, 5)]
    public static void ShowWindow()
    {
        var w = GetWindow<GameSettingsWindow>("Cartoon Zombies — Settings");
        w.minSize = new Vector2(400, 520);
        w.LoadAllReferences();
    }

    private void OnEnable() => LoadAllReferences();

    // Lastes én gang — ikke i OnGUI som kjøres 60+ ganger per sekund
    private void LoadAllReferences()
    {
        wave2        = AssetDatabase.LoadAssetAtPath<WaveData>("Assets/ScriptableObjects/Waves/WaveData_Zone2.asset");
        wave3        = AssetDatabase.LoadAssetAtPath<WaveData>("Assets/ScriptableObjects/Waves/WaveData_Zone3.asset");
        gameAudio    = AssetDatabase.LoadAssetAtPath<GameAudioSettings>("Assets/ScriptableObjects/GameAudioSettings.asset");
        cheatSettings = AssetDatabase.LoadAssetAtPath<CheatMenuSettings>("Assets/ScriptableObjects/CheatMenuSettings.asset");
        playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
        zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Zombies/FreeZombie.prefab");
    }

    // ─── MAIN GUI ───────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        DrawHeader();
        selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(28));
        EditorGUILayout.Space(6);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        switch (selectedTab)
        {
            case 0: DrawAudioTab();     break;
            case 1: DrawWavesTab();     break;
            case 2: DrawPlayerTab();    break;
            case 3: DrawZombieTab();    break;
            case 4: DrawCheatTab();     break;
        }
        EditorGUILayout.EndScrollView();
    }

    // ─── LYD ────────────────────────────────────────────────────────────────────

    private void DrawAudioTab()
    {
        SectionLabel("Volume");
        EditorGUILayout.HelpBox(
            "Stored in PlayerPrefs (PG2202-12) — persists between sessions.",
            MessageType.Info);

        float current = PlayerPrefs.GetFloat("MasterVolume", 0.4f);
        float updated = EditorGUILayout.Slider("Master volume", current, 0f, 1f);

        if (!Mathf.Approximately(updated, current))
        {
            PlayerPrefs.SetFloat("MasterVolume", updated);
            PlayerPrefs.Save();
        }

        EditorGUILayout.LabelField(
            $"   BGM: {updated * 50f:0}%   |   SFX: {updated * 100f:0}%",
            EditorStyles.miniLabel);

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Reset volume to 40%"))
        {
            PlayerPrefs.SetFloat("MasterVolume", 0.4f);
            PlayerPrefs.Save();
        }

        EditorGUILayout.Space(12);
        SectionLabel("Background music (one asset for the whole team)");
        EditorGUILayout.HelpBox(
            "Edit clips in GameAudioSettings. The GameManager prefab → AudioManager references this asset.\n" +
            "Runtime: AudioSource.clip + loop (PG2202-10).",
            MessageType.None);

        if (gameAudio == null)
        {
            EditorGUILayout.HelpBox(
                "Missing Assets/ScriptableObjects/GameAudioSettings.asset — create via Assets → Create → CartoonZombies → Game Audio Settings.",
                MessageType.Warning);
        }
        else
        {
            SerializedObject soA = new SerializedObject(gameAudio);
            soA.Update();
            EditorGUILayout.PropertyField(soA.FindProperty("menuMusic"),  new GUIContent("Menu"));
            EditorGUILayout.PropertyField(soA.FindProperty("cityMusic"),  new GUIContent("Level 1 — By"));
            EditorGUILayout.PropertyField(soA.FindProperty("beachMusic"), new GUIContent("Level 2 — Strand/skog"));
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Per-track music volume (× master slider)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(soA.FindProperty("menuMusicVolume"),  new GUIContent("Menu"));
            EditorGUILayout.PropertyField(soA.FindProperty("cityMusicVolume"),  new GUIContent("By"));
            EditorGUILayout.PropertyField(soA.FindProperty("beachMusicVolume"), new GUIContent("Strand"));
            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(soA.FindProperty("sfxGameplayScale"), new GUIContent("SFX via AudioManager (e.g. zombie death)"));
            if (soA.ApplyModifiedProperties()) EditorUtility.SetDirty(gameAudio);

            GameObject gmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameManager.prefab");
            if (gmPrefab != null)
            {
                var am = gmPrefab.GetComponent<AudioManager>();
                if (am != null)
                {
                    SerializedObject soAm = new SerializedObject(am);
                    soAm.Update();
                    EditorGUILayout.PropertyField(soAm.FindProperty("musicLibrary"), new GUIContent("Prefab link (should be GameAudioSettings above)"));
                    if (soAm.ApplyModifiedProperties())
                    {
                        EditorUtility.SetDirty(gmPrefab);
                        AssetDatabase.SaveAssets();
                    }
                }
            }
        }

        EditorGUILayout.Space(12);
        SectionLabel("Weapon SFX (PlayerShooting on Player prefab)");
        DrawPrefabFields(playerPrefab, typeof(PlayerShooting), true,
            ("shootSound",         "Shoot clip"),
            ("reloadSound",        "Reload clip"),
            ("emptySound",         "Empty clip"),
            ("shootVolumeScale",   "Shoot volume (0–1)"),
            ("reloadVolumeScale",  "Reload volume (0–1)"),
            ("emptyVolumeScale",   "Empty click volume (0–1)"));
    }

    private void DrawCheatTab()
    {
        SectionLabel("Cheat menu tuning (ScriptableObject)");
        EditorGUILayout.HelpBox(
            "Values apply when CheatMenu runs Start(). Assign Assets/ScriptableObjects/CheatMenuSettings on the CheatMenu component (CheatCanvas), or use CartoonZombies → Scenes → Add CheatMenu to active scene.",
            MessageType.Info);

        if (cheatSettings == null)
        {
            EditorGUILayout.HelpBox(
                "Missing CheatMenuSettings.asset — Assets → Create → CartoonZombies → Cheat Menu Settings.",
                MessageType.Warning);
            return;
        }

        SerializedObject so = new SerializedObject(cheatSettings);
        so.Update();
        EditorGUILayout.PropertyField(so.FindProperty("noclipSpeed"), new GUIContent("Noclip move speed"));
        if (so.ApplyModifiedProperties()) EditorUtility.SetDirty(cheatSettings);
    }

    // ─── BØLGER ─────────────────────────────────────────────────────────────────

    private void DrawWavesTab()
    {
        SectionLabel("Waves (WaveData ScriptableObjects)");
        EditorGUILayout.HelpBox(
            "WaveData = ScriptableObject assets (PG2202-08). Changes save to the .asset files and apply on next Play.\n\n" +
            "Runtime (in scene): add component RuntimeHierarchyTuning on a GameObject under GameplaySystems — F10 opens sliders for damage multipliers and ZombieSpawner spread without using this editor window.",
            MessageType.Info);

        EditorGUILayout.Space(6);
        DrawWaveAsset("Level 1 — By (WaveData_Zone2)", wave2);
        DrawWaveAsset("Level 2 — Strand (WaveData_Zone3)", wave3);

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Reload WaveData from disk"))
            LoadAllReferences();
    }

    private void DrawWaveAsset(string label, WaveData data)
    {
        if (data == null)
        {
            EditorGUILayout.HelpBox($"{label}: not found — run Setup → Create WaveData Assets.", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        SerializedObject so = new SerializedObject(data);
        so.Update();

        EditorGUILayout.PropertyField(so.FindProperty("zombieCount"),   new GUIContent("Zombie count"));
        EditorGUILayout.PropertyField(so.FindProperty("spawnInterval"), new GUIContent("Spawn interval (sec)"));
        EditorGUILayout.PropertyField(so.FindProperty("zombiePrefab"),  new GUIContent("Zombie prefab"));

        if (so.ApplyModifiedProperties()) EditorUtility.SetDirty(data);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    // ─── SPILLER ────────────────────────────────────────────────────────────────

    private void DrawPlayerTab()
    {
        SectionLabel("Player (Player.prefab)");
        EditorGUILayout.HelpBox(
            "Changes save directly to the prefab.\n" +
            "PlayerHealth uses UnityEvent<int,int> for loose HUD coupling (PG2202-02).",
            MessageType.Info);

        if (playerPrefab == null)
        {
            EditorGUILayout.HelpBox("Player.prefab not found — sjekk sti Assets/Prefabs/Player/Player.prefab.", MessageType.Error);
            return;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Health", EditorStyles.boldLabel);
        DrawPrefabFields(playerPrefab, typeof(PlayerHealth), true,
            ("maxHealth", "Max HP"));

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
        DrawPrefabFields(playerPrefab, typeof(PlayerMovement), true,
            ("moveSpeed",        "Move speed (m/s)"),
            ("mouseSensitivity", "Mouse sensitivity"));

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Shooting", EditorStyles.boldLabel);
        DrawPrefabFields(playerPrefab, typeof(PlayerShooting), true,
            ("damage",     "Damage per shot"),
            ("range",      "Range (m)"),
            ("maxAmmo",    "Max ammo"),
            ("reloadTime", "Reload time (sec)"));
    }

    // ─── ZOMBIER ────────────────────────────────────────────────────────────────

    private void DrawZombieTab()
    {
        SectionLabel("Zombies (FreeZombie.prefab)");
        EditorGUILayout.HelpBox(
            "ZombieAI: enum FSM Patrol → Chase → Attack → Dead (PG2202-05).\n" +
            "NavMeshAgent pathing on baked NavMesh (PG2202-07).",
            MessageType.Info);

        if (zombiePrefab == null)
        {
            EditorGUILayout.HelpBox("FreeZombie.prefab not found — sjekk sti Assets/Prefabs/Zombies/FreeZombie.prefab.", MessageType.Error);
            return;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Health", EditorStyles.boldLabel);
        DrawPrefabFields(zombiePrefab, typeof(ZombieHealth), true,
            ("maxHealth", "Max HP"));

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("AI / FSM", EditorStyles.boldLabel);
        DrawPrefabFields(zombiePrefab, typeof(ZombieAI), true,
            ("detectionRange", "Detection radius (m)"),
            ("attackRange",    "Attack radius (m)"),
            ("attackDamage",   "Damage per hit"),
            ("attackCooldown", "Attack cooldown (sec)"),
            ("patrolRadius",   "Patrol radius (m)"),
            ("patrolWaitTime", "Wait at patrol point (sec)"));
    }

    // ─── HJELPEMETODER ──────────────────────────────────────────────────────────

    // Redigerer felt direkte på prefab-komponenten via SerializedObject
    private void DrawPrefabFields(GameObject prefab, System.Type compType, bool saveOnChange,
        params (string field, string label)[] fields)
    {
        if (prefab == null) return;

        Component comp = prefab.GetComponent(compType);
        if (comp == null)
        {
            EditorGUILayout.HelpBox($"{compType.Name} ikke funnet på prefaben.", MessageType.Warning);
            return;
        }

        SerializedObject so = new SerializedObject(comp);
        so.Update();

        foreach (var (field, label) in fields)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, new GUIContent(label));
        }

        if (so.ApplyModifiedProperties() && saveOnChange)
        {
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
        }
    }

    private void SectionLabel(string title)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        Rect r = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(r, new Color(0.4f, 0.4f, 0.4f, 0.6f));
        EditorGUILayout.Space(3);
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(6);
        var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 };
        EditorGUILayout.LabelField("Cartoon Zombies — settings hub", style);
        EditorGUILayout.Space(4);
    }
}
