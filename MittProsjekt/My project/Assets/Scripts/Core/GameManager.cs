using UnityEngine;
using UnityEngine.SceneManagement;

// Enum for å holde styr på hvilken tilstand spillet er i
// Brukes av GameManager og andre scripts for å reagere på endringer
public enum GameState { MainMenu, Playing, Paused, GameOver, Win }

// Singleton - kun én GameManager eksisterer i hele spillets levetid (PG2202-12)
// DontDestroyOnLoad gjør at den ikke slettes ved sceneskifte
public class GameManager : MonoBehaviour
{
    // Statisk referanse - andre scripts henter GameManager via GameManager.Instance
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public int TotalKills         { get; private set; }
    public int CurrentZone        { get; private set; } = 1;

    // Delegate og event - PG2202-02
    // Andre scripts kan abonnere på disse uten at GameManager trenger å kjenne dem
    public delegate void OnGameStateChangedDelegate(GameState newState);
    public event OnGameStateChangedDelegate OnGameStateChanged;

    public delegate void OnKillDelegate(int totalKills);
    public event OnKillDelegate OnKillRegistered;

    // Awake kjøres før Start - brukes til Singleton-oppsett (PG2202-12)
    private void Awake()
    {
        // Hvis det allerede finnes en GameManager, slett denne kopien
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Må være rot-GameObject for DontDestroyOnLoad (Unity-krav). Repair legger prefab under GameplaySystems.
        transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject); // overlever sceneskifte
    }

    // Endrer spilltilstand og varsler alle abonnenter via event
    public void SetState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState); // ?. sjekker om noen abonnerer først

        // Time.timeScale = 0 fryser all fysikk og animasjon i Unity
        switch (newState)
        {
            case GameState.Paused:
                Time.timeScale   = 0f;
                Cursor.visible   = true;
                Cursor.lockState = CursorLockMode.None;
                break;

            case GameState.Playing:
                Time.timeScale   = 1f;
                Cursor.visible   = false;
                Cursor.lockState = CursorLockMode.Locked; // låser mus til midten under gameplay
                break;

            case GameState.MainMenu:
                Time.timeScale   = 1f;
                Cursor.visible   = true;
                Cursor.lockState = CursorLockMode.None;
                break;

            case GameState.GameOver:
            case GameState.Win:
                Time.timeScale   = 1f;
                Cursor.visible   = true;
                Cursor.lockState = CursorLockMode.None;
                break;
        }
    }

    // Kalles av ZombieHealth når en zombie dør
    public void RegisterKill()
    {
        TotalKills++;
        OnKillRegistered?.Invoke(TotalKills);

        // Lagrer ny highscore automatisk via PlayerPrefs (PG2202-12)
        if (TotalKills > SaveSystem.GetHighScore())
            SaveSystem.SaveHighScore(TotalKills);
    }

    public void SetZone(int zone) => CurrentZone = zone;

    public void StartGame()
    {
        ResetStats();
        SetState(GameState.Playing);
        SceneLoader.Load(GameSceneNames.Level01By);
    }

    public void TriggerGameOver()
    {
        SaveSystem.SaveLastRunKills(TotalKills);
        SetState(GameState.GameOver);
        SceneLoader.Load(GameSceneNames.GameOver);
    }

    public void TriggerWin()
    {
        SaveSystem.SaveLastRunKills(TotalKills);
        SetState(GameState.Win);
        SceneLoader.Load(GameSceneNames.Win);
    }

    public void GoToMainMenu()
    {
        SaveSystem.SaveLastRunKills(TotalKills);
        ResetStats();
        SetState(GameState.MainMenu);
        SceneLoader.Load(GameSceneNames.MainMenu);
    }

    // Laster neste nivå: kun by → strand. Seier skjer via IslandWinTrigger på kisten (nivå 2).
    public void LoadNextZone()
    {
        if (CurrentZone >= 2)
        {
            Debug.LogWarning("[GameManager] LoadNextZone: allerede på siste nivå — bruk IslandWinTrigger for seier.");
            return;
        }

        int next = CurrentZone + 1;
        SetZone(next);
        SaveSystem.SaveZoneUnlocked(next);

        if (next == 2)
            SceneLoader.Load(GameSceneNames.Level02StrandSkog);
    }

    private void ResetStats()
    {
        TotalKills  = 0;
        CurrentZone = 1;
    }
}
