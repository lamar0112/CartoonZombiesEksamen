using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Sekvensielle oppdrag med pil mot mål (PG2202-08 UI, PG2202-03 Observer)
// Heng dette scriptet på GameplaySystems i scenen.
// Konfigurer missions[] i Inspector med tekst og mål-Transform.
public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    // ────────────────────────────── Data ──────────────────────────────
    public enum CompleteTrigger
    {
        Manual,            // fullføres via CompleteCurrent()
        AllZombiesDead,    // automatisk når ZombiesAlive == 0 og AllWavesDone
        ParkourBothDone,   // automatisk av CityParkourManager
        BoatUnlocked,      // automatisk av BoatUnlockSystem
        KillCountReached, // drept X zombier siden dette oppdraget startet (GameManager.TotalKills)
    }

    [Serializable]
    public class Mission
    {
        [TextArea(2, 4)]
        public string descriptionText;
        public CompleteTrigger trigger = CompleteTrigger.Manual;
        [Tooltip("Tom = ingen pil. Sett et tomt GameObject i scenen (f.eks. 'MissionTarget_Car').")]
        public Transform arrowTarget;
        [Tooltip("Kun for KillCountReached: antall drap siden oppdraget startet.")]
        public int killCountRequired = 12;
    }

    [Header("Missions")]
    [SerializeField] private Mission[] missions;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI missionText;  // Dra inn TMP-tekst fra HUD Canvas
    [Tooltip("Panel (Image) bak misjonsteksten — vises/skjules med tekst.")]
    [SerializeField] private UnityEngine.UI.Image missionPanel;
    [Tooltip("Pil-sprites (EnemyCompassHUD) — valgfritt, peker mot arrowTarget.")]
    [SerializeField] private EnemyCompassHUD compassHud;

    // ────────────────────────────── State ──────────────────────────────
    private int  _current = 0;
    private bool _allDone = false;
    private int  _killBaseline;

    /// <summary>Aktivt oppdrag (0-basert). Nyttig for betingede fullføringer (f.eks. bil).</summary>
    public int CurrentMissionIndex => _current;

    // ────────────────────────────── Init ──────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Abonner på eksisterende UnityEvents (Observer-pattern PG2202-02)
        if (CityParkourManager.Instance != null)
            CityParkourManager.Instance.OnAllZonesComplete.AddListener(() => TryAutoComplete(CompleteTrigger.ParkourBothDone));

        // Oppgrader gamle scenedata (AllZombiesDead + horde-loop fullfører aldri; feil UI-tekst i eldre scener)
        ApplySceneMissionOverridesIfNeeded();

        // BoatUnlock: polles i Update (event-arkitektur finnes ikke der)
        ShowCurrent();
    }

    /// <summary>
    /// Eldre scener: misjon 2 = «drep alle» med AllZombiesDead — med repeatWavesForever blir AllWavesDone aldri true.
    /// Fikser også tekst som sier «every zombie» / «Clear the zombie wave».
    /// </summary>
    private void ApplySceneMissionOverridesIfNeeded()
    {
        if (missions == null || missions.Length < 2) return;

        string sn = SceneManager.GetActiveScene().name;
        Mission m1 = missions[1];
        string desc = m1.descriptionText ?? string.Empty;
        string d = desc.ToLowerInvariant();

        bool looksLikeClearAllWave =
            m1.trigger == CompleteTrigger.AllZombiesDead
            || d.Contains("every zombie")
            || d.Contains("alle zombier")
            || d.Contains("drep alle")
            || d.Contains("clear the zombie")
            || d.Contains("clear the wave")
            || d.Contains("eliminate every")
            || d.Contains("rydd bølgen")
            || d.Contains("rydd zombie")
            || d.Contains("eliminer alle");

        if (!looksLikeClearAllWave) return;

        if (sn == "Level01_By")
        {
            missions[1].trigger           = CompleteTrigger.KillCountReached;
            missions[1].killCountRequired = Mathf.Max(1, missions[1].killCountRequired > 0 ? missions[1].killCountRequired : 12);
            missions[1].descriptionText =
                "Eliminate 12 zombies\n<size=80%>Waves keep looping — this step completes after <b>12 kills</b>.</size>";
        }
        else if (sn == "Level02_StrandSkog")
        {
            missions[1].trigger           = CompleteTrigger.KillCountReached;
            missions[1].killCountRequired = Mathf.Max(1, missions[1].killCountRequired > 0 ? missions[1].killCountRequired : 14);
            missions[1].descriptionText =
                "Eliminate 14 zombies\n<size=80%>Horde keeps coming — complete after <b>14 kills</b>.</size>";
        }
    }

    private void Update()
    {
        if (_allDone || missions == null || _current >= missions.Length) return;

        Mission m = missions[_current];
        switch (m.trigger)
        {
            case CompleteTrigger.AllZombiesDead:
                ZombieSpawner sp = FindFirstObjectByType<ZombieSpawner>();
                if (sp != null && sp.AllWavesDone && sp.ZombiesAlive <= 0)
                    CompleteCurrent();
                break;

            case CompleteTrigger.BoatUnlocked:
                BoatUnlockSystem boat = FindFirstObjectByType<BoatUnlockSystem>();
                if (boat != null && boat.IsUnlocked)
                    CompleteCurrent();
                break;

            case CompleteTrigger.KillCountReached:
                if (GameManager.Instance != null)
                {
                    int need = Mathf.Max(1, m.killCountRequired);
                    if (GameManager.Instance.TotalKills - _killBaseline >= need)
                        CompleteCurrent();
                }
                break;
        }
    }

    // ────────────────────────────── Public API ──────────────────────────────
    // Kall fra editor-knapp, trigger, eller UnityEvent for manual-steg
    public void CompleteCurrent()
    {
        if (_allDone || missions == null || _current >= missions.Length) return;

        _current++;

        if (_current >= missions.Length)
        {
            _allDone = true;
            HidePanel();
            if (compassHud != null) compassHud.ClearMissionTarget();
            return;
        }

        ShowCurrent();
    }

    // Hoppar till et spesifikt steg (f.eks. fra CheatMenu)
    public void JumpTo(int index)
    {
        _current = Mathf.Clamp(index, 0, missions != null ? missions.Length - 1 : 0);
        ShowCurrent();
    }

    /// <summary>
    /// By: fullfør bare «kjør til bil»-steget når spilleren faktisk er på det steget (unngår å hoppe over bølge-oppdrag).
    /// </summary>
    public void TryCompleteReachCarMission()
    {
        if (_allDone || missions == null) return;
        if (SceneManager.GetActiveScene().name != "Level01_By") return;
        // SetupMissionManagerInScene: 0 pistol, 1 kills, 2 bil, 3 parkour, 4 exit
        if (_current != 2) return;
        CompleteCurrent();
    }

    // ────────────────────────────── Internal ──────────────────────────────
    private void TryAutoComplete(CompleteTrigger which)
    {
        if (_allDone || missions == null || _current >= missions.Length) return;
        if (missions[_current].trigger == which)
            CompleteCurrent();
    }

    private void ShowCurrent()
    {
        if (missions == null || _current >= missions.Length) { HidePanel(); return; }

        Mission m = missions[_current];

        // Vis nummer + beskrivelse
        string header = $"<b>Mission {_current + 1}/{missions.Length}</b>\n";
        if (missionText != null)
        {
            missionText.richText = true;
            missionText.text     = header + m.descriptionText;
        }

        // Bakgrunnspanel
        if (missionPanel != null) missionPanel.enabled = true;

        _killBaseline = GameManager.Instance != null ? GameManager.Instance.TotalKills : 0;

        // Pil mot mål (EnemyCompassHUD)
        if (compassHud != null)
        {
            if (m.arrowTarget != null)
                compassHud.SetMissionTarget(m.arrowTarget);
            else
                compassHud.ClearMissionTarget();
        }
    }

    private void HidePanel()
    {
        if (missionText  != null) missionText.text = string.Empty;
        if (missionPanel != null) missionPanel.enabled = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
