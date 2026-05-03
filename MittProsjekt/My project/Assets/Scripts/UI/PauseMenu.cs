using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Pausemeny - åpnes med ESC, fryser spillet via Time.timeScale (PG2202-12)
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel; // selve pause-panelet
    [SerializeField] private UnityEngine.UI.Slider volumeSlider; // ikke pensum, standard Unity UI

    private bool isPaused = false;

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);

        // Setter slider til lagret volum
        if (volumeSlider != null)
            volumeSlider.value = SaveSystem.GetVolume();

        ApplyPausePanelStyle();
    }

    private void ApplyPausePanelStyle()
    {
        if (pausePanel == null) return;

        var bg = pausePanel.GetComponent<Image>();
        if (bg != null)
            bg.color = new Color(0.04f, 0.05f, 0.08f, 0.92f);

        foreach (var btn in pausePanel.GetComponentsInChildren<Button>(true))
        {
            var colors = btn.colors;
            colors.normalColor     = new Color(0.14f, 0.42f, 0.32f, 1f);
            colors.highlightedColor = new Color(0.18f, 0.52f, 0.38f, 1f);
            colors.pressedColor    = new Color(0.1f, 0.32f, 0.24f, 1f);
            colors.selectedColor   = colors.highlightedColor;
            btn.colors = colors;

            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.fontStyle = FontStyles.Bold;
                label.fontSize = Mathf.Max(label.fontSize, 26);
                label.color = Color.white;
            }
        }
    }

    private void Update()
    {
        // ESC toggler pause - men ikke i menyen eller ved game over
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState == GameState.Playing)  TogglePause();
            else if (GameManager.Instance.CurrentState == GameState.Paused) Resume();
        }
    }

    private void TogglePause()
    {
        if (isPaused) Resume();
        else          Pause();
    }

    private void Pause()
    {
        isPaused = true;
        if (pausePanel != null) pausePanel.SetActive(true);
        GameManager.Instance?.SetState(GameState.Paused); // setter timeScale = 0
    }

    // Knyttet til "Fortsett"-knappen i Inspector
    public void Resume()
    {
        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        GameManager.Instance?.SetState(GameState.Playing); // setter timeScale = 1
    }

    // Slider OnValueChanged-event kobles til denne i Inspector
    public void OnVolumeChanged(float value)
    {
        AudioManager.Instance?.SetVolume(value);
    }

    // Knyttet til "Hovedmeny"-knappen
    public void OnMainMenuClicked()
    {
        Time.timeScale = 1f; // viktig! resetter timeScale før sceneskifte
        GameManager.Instance?.GoToMainMenu();
    }

    // Knyttet til "Avslutt"-knappen (krav 9 i eksamen)
    public void OnQuitClicked()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }
}
