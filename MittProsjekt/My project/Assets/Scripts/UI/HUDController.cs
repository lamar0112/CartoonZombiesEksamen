using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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

    [Header("Feedback")]
    [Tooltip("Valgfritt — lages automatisk som barn «HUD_ToastText» hvis tom.")]
    [SerializeField] private TextMeshProUGUI toastText;

    [Header("References")]
    [SerializeField] private PlayerHealth    playerHealth;
    [SerializeField] private PlayerShooting  playerShooting;
    [SerializeField] private ZombieSpawner   zombieSpawner;

    private bool _healthListenerBound;
    private Coroutine _toastRoutine;

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
        EnsureToastText();
        UpdateKills(GameManager.Instance != null ? GameManager.Instance.TotalKills : 0);
    }

    /// <summary>Kort melding (f.eks. våpen plukket opp). Trygg hvis toast-TMP mangler.</summary>
    public void ShowPickupMessage(string richMessage, float visibleSeconds = 2.6f)
    {
        EnsureToastText();
        if (toastText == null) return;
        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(ToastRoutine(richMessage, visibleSeconds));
    }

    private void EnsureToastText()
    {
        if (toastText != null) return;
        Transform existing = transform.Find("HUD_ToastText");
        if (existing != null)
        {
            toastText = existing.GetComponent<TextMeshProUGUI>();
            if (toastText != null && toastText.font == null && healthText != null && healthText.font != null)
                toastText.font = healthText.font;
            return;
        }

        var go = new GameObject("HUD_ToastText");
        go.transform.SetParent(transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.62f);
        rt.anchorMax = new Vector2(0.5f, 0.62f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520f, 56f);
        toastText = go.AddComponent<TextMeshProUGUI>();
        toastText.alignment = TextAlignmentOptions.Center;
        toastText.richText = true;
        toastText.textWrappingMode = TextWrappingModes.Normal;
        if (healthText != null && healthText.font != null)
            toastText.font = healthText.font;
        StyleHudText(toastText, 26, FontStyles.Bold, new Color(1f, 0.92f, 0.35f, 0f), outlined: true);
    }

    private IEnumerator ToastRoutine(string msg, float seconds)
    {
        toastText.text = msg;
        Color c = toastText.color;
        c.a = 1f;
        toastText.color = c;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            yield return null;
        }
        t = 0f;
        while (t < 0.45f)
        {
            t += Time.deltaTime;
            c = toastText.color;
            c.a = 1f - t / 0.45f;
            toastText.color = c;
            yield return null;
        }
        toastText.text = string.Empty;
        c.a = 0f;
        toastText.color = c;
        _toastRoutine = null;
    }

    // Semi-transparent panel behind the top-left stats so text stays readable on any background (PG2202-08).
    private void EnsureHudReadabilityPanel()
    {
        if (healthText == null) return;
        RectTransform anchor = healthText.rectTransform;
        Transform parent = anchor.parent;
        if (parent == null) return;
        if (parent.Find("HUD_ReadabilityPanel") != null) return;

        var panelGo = new GameObject("HUD_ReadabilityPanel");
        panelGo.transform.SetParent(parent, false);
        panelGo.transform.SetAsFirstSibling();

        var rt = panelGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(6f, -6f);
        rt.sizeDelta = new Vector2(420f, 220f);

        var img = panelGo.AddComponent<Image>();
        img.color = new Color(0.02f, 0.02f, 0.04f, 0.78f);
        img.raycastTarget = false;
    }

    // Luft mellom linjer — reduserer «alt oppå hverandre» før du finjusterer RectTransform i Canvas (PG2202-08)
    private void ApplyHudTypography()
    {
        EnsureHudReadabilityPanel();

        StyleHudText(healthText,  28,  FontStyles.Bold,   new Color(0.2f, 1f, 0.4f),    outlined: true);
        StyleHudText(waveText,    24,  FontStyles.Normal, new Color(1f, 1f, 1f),          outlined: true);
        StyleHudText(killsText,   24,  FontStyles.Bold,   new Color(1f, 0.85f, 0.2f),    outlined: true);
        StyleHudText(ammoText,    26,  FontStyles.Bold,   new Color(1f, 1f, 1f),          outlined: true);
        if (waveText != null) waveText.richText = true;
        if (reloadText != null)
        {
            StyleHudText(reloadText, 22, FontStyles.Bold, new Color(1f, 0.3f, 0.1f), outlined: true);
        }
    }

    // Setter farge, størrelse og svart outline for god lesbarhet uten bakgrunnspanel (PG2202-08)
    private static void StyleHudText(TextMeshProUGUI t, float size, FontStyles style, Color col, bool outlined)
    {
        if (t == null) return;
        t.fontSize         = size;
        t.fontStyle        = style;
        t.color            = col;
        t.lineSpacing      = 4f;
        t.textWrappingMode = TextWrappingModes.Normal;
        if (outlined && t.font != null)
        {
            try
            {
                t.outlineWidth = 0.28f;
                t.outlineColor = new Color32(0, 0, 0, 200);
            }
            catch (System.Exception)
            {
                // TMP kan kaste hvis material/font ikke er klar — hopp over outline
            }
        }
    }

    private void Update()
    {
        if (playerShooting == null || zombieSpawner == null || playerHealth == null)
            ResolveHudReferences();
        BindHealthListenerIfNeeded();

        // Sjekker ammo og reload-status hvert frame (enklere enn events for dette)
        bool hasGun = playerShooting != null && playerShooting.enabled;
        if (hasGun)
        {
            if (ammoText != null)
            {
                int cur = playerShooting.CurrentAmmo;
                int max = playerShooting.MaxAmmo;
                string ammoCol = cur <= 5 ? "#FF3333" : cur <= 10 ? "#FFCC00" : "#FFFFFF";
                ammoText.text = $"<color={ammoCol}><b>{cur}</b></color> / {max}";
            }

            if (reloadText != null)
                reloadText.gameObject.SetActive(playerShooting.IsReloading);
        }
        else
        {
            // Spilleren har ikke funnet pistolen ennå
            if (ammoText != null)
                ammoText.text = "<color=#888888>No pistol equipped</color>";
            if (reloadText != null)
                reloadText.gameObject.SetActive(false);
        }

        // Oppdaterer bølge-info fra ZombieSpawner
        if (zombieSpawner != null && waveText != null)
        {
            string zombieColor = zombieSpawner.ZombiesAlive > 0 ? "#FF4444" : "#44FF88";
            waveText.text =
                $"<b>Wave {zombieSpawner.CurrentWave}/{zombieSpawner.TotalWaves}</b>  ·  " +
                $"<color={zombieColor}>Zombies left: {zombieSpawner.ZombiesAlive}</color>";
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
            killsText.text = $"<b>Kills: {totalKills}</b>";
    }

    // Kalles av PlayerHealth.OnHealthChanged UnityEvent
    private void UpdateHealth(int current, int max)
    {
        if (healthText == null) return;
        float pct = max > 0 ? (float)current / max : 0f;
        // Grønn ved full helse → gul → rød ved lavt (PG2202-08 fargebruk)
        string col = pct > 0.6f ? "#33FF66" : pct > 0.3f ? "#FFCC00" : "#FF3333";
        healthText.text = $"<color={col}><b>HP {current}/{max}</b></color>";
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
