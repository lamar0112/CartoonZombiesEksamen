using UnityEngine;
using TMPro; // TextMeshPro - bedre tekstkvalitet enn vanlig UI Text (PG2202-08)

// Viser spillinfo mens spillet pågår: helse, ammo, kills, bølge
// Abonnerer på events fra GameManager og PlayerHealth - trenger ingen direkte kall
public class HUDController : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Ammo")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI reloadText; // vises kun under reload

    [Header("Kills & wave")]
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI waveText;

    [Header("References")]
    [SerializeField] private PlayerHealth    playerHealth;
    [SerializeField] private PlayerShooting  playerShooting;
    [SerializeField] private ZombieSpawner   zombieSpawner;

    private bool _healthListenerBound;

#if UNITY_EDITOR
    // Editoren viser Game-view uten Play — Start() kjører ikke, så reload-tekst må skjules her også
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (reloadText != null) reloadText.gameObject.SetActive(false);
    }
#endif

    private void Start()
    {
        ResolveHudReferences();

        // Abonnerer på events fra GameManager (delegate/event - PG2202-02)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnKillRegistered   += UpdateKills;
            GameManager.Instance.OnGameStateChanged += OnStateChanged;
        }

        BindHealthListenerIfNeeded();

        // Skjuler reload-tekst ved start
        if (reloadText != null) reloadText.gameObject.SetActive(false);

        ApplyHudTypography();
        UpdateKills(GameManager.Instance != null ? GameManager.Instance.TotalKills : 0);
    }

    // Luft mellom linjer — reduserer «alt oppå hverandre» før du finjusterer RectTransform i Canvas (PG2202-08)
    private void ApplyHudTypography()
    {
        float spacing = 4f;
        if (healthText != null)
        {
            healthText.lineSpacing = spacing;
            healthText.textWrappingMode = TextWrappingModes.Normal;
            healthText.fontSize = 26;
            healthText.fontStyle = FontStyles.Bold;
            healthText.color = new Color(0.95f, 0.97f, 1f);
        }
        if (waveText != null)
        {
            waveText.lineSpacing = spacing;
            waveText.textWrappingMode = TextWrappingModes.Normal;
            waveText.richText = true;
            waveText.fontSize = 24;
            waveText.color = new Color(0.92f, 0.94f, 0.98f);
        }
        if (killsText != null)
        {
            killsText.lineSpacing = spacing;
            killsText.textWrappingMode = TextWrappingModes.Normal;
            killsText.fontSize = 24;
            killsText.color = new Color(0.9f, 0.92f, 0.96f);
        }
        if (ammoText != null)
        {
            ammoText.lineSpacing = spacing;
            ammoText.textWrappingMode = TextWrappingModes.Normal;
            ammoText.fontSize = 24;
            ammoText.fontStyle = FontStyles.Bold;
            ammoText.color = new Color(0.95f, 0.95f, 1f);
        }
        if (reloadText != null)
        {
            reloadText.lineSpacing = spacing;
            reloadText.fontSize = 22;
            reloadText.color = new Color(1f, 0.55f, 0.35f);
        }
    }

    private void Update()
    {
        if (playerShooting == null || zombieSpawner == null || playerHealth == null)
            ResolveHudReferences();
        BindHealthListenerIfNeeded();

        // Sjekker ammo og reload-status hvert frame (enklere enn events for dette)
        if (playerShooting != null)
        {
            if (ammoText != null)
                ammoText.text = $"{playerShooting.CurrentAmmo} / {playerShooting.MaxAmmo}";

            if (reloadText != null)
                reloadText.gameObject.SetActive(playerShooting.IsReloading);
        }
        else if (reloadText != null)
            reloadText.gameObject.SetActive(false);

        // Oppdaterer bølge-info fra ZombieSpawner
        if (zombieSpawner != null && waveText != null)
        {
            waveText.text =
                $"Bølge {zombieSpawner.CurrentWave} / {zombieSpawner.TotalWaves}  ·  " +
                $"<color=#FFB347>Zombier igjen: {zombieSpawner.ZombiesAlive}</color>";
        }
    }

    // Inkl. inaktive objekter — ellers blir scene-placeholder («Bølge…», synlig RELOADER) hengende igjen
    private void ResolveHudReferences()
    {
        var inactive = FindObjectsInactive.Include;
        if (playerHealth == null)   playerHealth   = FindFirstObjectByType<PlayerHealth>(inactive);
        if (playerShooting == null) playerShooting = FindFirstObjectByType<PlayerShooting>(inactive);
        if (zombieSpawner == null)  zombieSpawner  = FindFirstObjectByType<ZombieSpawner>(inactive);
    }

    private void BindHealthListenerIfNeeded()
    {
        if (_healthListenerBound || playerHealth == null) return;
        playerHealth.OnHealthChanged.AddListener(UpdateHealth);
        _healthListenerBound = true;
    }

    // Kalles av GameManager.OnKillRegistered-event
    private void UpdateKills(int totalKills)
    {
        if (killsText != null)
            killsText.text = $"Kills: {totalKills}";
    }

    // Kalles av PlayerHealth.OnHealthChanged UnityEvent
    private void UpdateHealth(int current, int max)
    {
        if (healthText != null)
            healthText.text = $"HP  {current} / {max}";
    }

    private void OnStateChanged(GameState newState)
    {
        // Skjuler HUD ved pause eller game over
        gameObject.SetActive(newState == GameState.Playing);
    }

    // Avmelder events når objektet slettes - viktig for å unngå minnelekkasje
    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnKillRegistered   -= UpdateKills;
            GameManager.Instance.OnGameStateChanged -= OnStateChanged;
        }
        if (playerHealth != null && _healthListenerBound)
            playerHealth.OnHealthChanged.RemoveListener(UpdateHealth);
    }
}
