using UnityEngine;
using TMPro;

// Game Over UI — én tekstblokk + kamera om scenen mangler det (PG2202-08)
public class GameOverScreen : MonoBehaviour
{
    [Header("Text (recommended)")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;

    [Header("Fallback: three fields (only if bodyText is empty)")]
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI zoneText;

    private void Awake()
    {
        EnsureMenuCameraExists();
        if (titleText != null)
            titleText.text = "GAME OVER";
    }

    private void Start()
    {
        int kills = GameManager.Instance != null ? GameManager.Instance.TotalKills : 0;
        int zone  = GameManager.Instance != null ? GameManager.Instance.CurrentZone : 0;
        int hs    = SaveSystem.GetHighScore();

        if (bodyText != null)
        {
            bodyText.text =
                $"You killed <b>{kills}</b> zombies.\n\n" +
                $"You reached <b>zone {zone}</b>.\n\n" +
                $"High score: <b>{hs}</b> kills.";
            bodyText.richText = true;
            bodyText.lineSpacing = 8f;

            if (killsText != null)    killsText.gameObject.SetActive(false);
            if (highScoreText != null) highScoreText.gameObject.SetActive(false);
            if (zoneText != null)     zoneText.gameObject.SetActive(false);
        }
        else
        {
            killsText?.SetText($"You killed {kills} zombies");
            zoneText?.SetText($"You reached zone {zone}");
            highScoreText?.SetText($"High score: {hs} kills");
        }

        AudioManager.Instance?.StopBGM();
    }

    private static void EnsureMenuCameraExists()
    {
        Camera cam = Camera.main;
        if (cam == null)
            cam = Object.FindFirstObjectByType<Camera>();

        if (cam == null)
        {
            var go = new GameObject("MenuCameraGameOver");
            cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = new Color(0.05f, 0.05f, 0.08f, 1f);
            cam.cullingMask      = 0;
            cam.orthographic     = true;
            cam.orthographicSize = 1f;
            cam.depth            = -10;
            cam.transform.position = new Vector3(0f, 0f, -10f);
        }

        if (Object.FindFirstObjectByType<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();
    }

    public void OnRetryClicked()
    {
        GameManager.Instance?.StartGame();
    }

    public void OnMainMenuClicked()
    {
        GameManager.Instance?.GoToMainMenu();
    }

    public void OnQuitClicked()
    {
        Application.Quit();
    }
}
