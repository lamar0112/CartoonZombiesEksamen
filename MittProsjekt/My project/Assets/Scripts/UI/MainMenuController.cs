using UnityEngine;
using TMPro;

// Håndterer startmenyen - første skjerm spilleren ser (krav 6 i eksamen)
public class MainMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private GameObject      keybindPanel; // panel med tastaturoversikt

    private void Awake()
    {
        // Må skjules før første frame — ellers overlapper panelet hovedmenyen (lagret som aktiv i scene).
        if (keybindPanel != null) keybindPanel.SetActive(false);
    }

    private void OnEnable() => RefreshScoreTexts();

    private void Start()
    {
        RefreshScoreTexts();
        if (keybindPanel != null) keybindPanel.SetActive(false);
        AudioManager.Instance?.PlayMenuMusic();
    }

    private void RefreshScoreTexts()
    {
        if (highScoreText == null) return;

        int hs   = SaveSystem.GetHighScore();
        int last = SaveSystem.GetLastRunKills();

        if (hs <= 0 && last < 0)
        {
            highScoreText.text = "Play a run to see your best kill count here.";
            return;
        }

        var parts = new System.Collections.Generic.List<string>();
        if (hs > 0) parts.Add($"Best: <color=#FFB347>{hs}</color> kills");
        if (last >= 0) parts.Add($"Last run: {last} kills");
        highScoreText.richText = true;
        highScoreText.text = string.Join("\n", parts);
    }

    // Knyttet til Play-knappen i Inspector
    public void OnPlayClicked()
    {
        GameManager.Instance?.StartGame();
    }

    // Viser/skjuler tastaturoversikt (krav 6: keybind-oversikt hvis styringen er uklar)
    public void OnKeybindsClicked()
    {
        if (keybindPanel != null)
            keybindPanel.SetActive(!keybindPanel.activeSelf);
    }

    // Avslutter spillet - Application.Quit() virker kun i ferdig build, ikke i editor
    // (krav 9 i eksamen: lett forståelig måte å lukke spillet på)
    public void OnQuitClicked()
    {
        Application.Quit();
    }
}
