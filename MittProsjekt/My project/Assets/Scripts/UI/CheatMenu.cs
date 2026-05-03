using System.Collections;
using UnityEngine;

// Cheat-meny for sensor/eksaminator - eksamen anbefaler dette eksplisitt (PG2202-12 FAQ)
// Trykk Y for å åpne/lukke. Bruker OnGUI for enkel, panel-uavhengig visning.
public class CheatMenu : MonoBehaviour
{
    public static CheatMenu Instance { get; private set; }

    private PlayerHealth      playerHealth;
    private PlayerMovement    playerMovement;
    private CharacterController charController;

    private bool isOpen       = false;
    public  bool IsCheatMenuOpen => isOpen;
    public  bool IsGodMode    { get; private set; } = false;
    private bool noclipActive = false;

    [Header("Tuning (optional)")]
    [Tooltip("Dra inn Assets/ScriptableObjects/CheatMenuSettings — felles for gruppa.")]
    [SerializeField] private CheatMenuSettings tuning;

    [SerializeField] private float noclipSpeed = 12f;

    // GUI-stil - initialiseres første gang OnGUI kjøres
    private GUIStyle boxStyle;
    private GUIStyle btnStyle;
    private bool     stylesInit = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (tuning != null)
            noclipSpeed = tuning.noclipSpeed;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth   = player.GetComponent<PlayerHealth>();
            playerMovement = player.GetComponent<PlayerMovement>();
            charController = player.GetComponent<CharacterController>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            isOpen = !isOpen;
            // Fryser gameplay med timeScale; ved lukk må vi respektere ESC-pause (GameState.Paused)
            if (isOpen)
                Time.timeScale = 0f;
            else
                ApplyTimeScaleAfterClosingCheatMenu();
            Cursor.visible   = isOpen;
            Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        }

        if (noclipActive)
            HandleNoclipMovement();
    }

    // OnGUI tegner UI direkte uten Canvas - enklere og mer pålitelig (PG2202 Unity UI)
    private void OnGUI()
    {
        if (!isOpen) return;

        InitStyles();

        float w   = 260f;
        float h   = 340f;
        float x   = (Screen.width  - w) * 0.5f;
        float y   = (Screen.height - h) * 0.5f;
        float bh  = 44f;   // button height
        float gap = 50f;   // vertical gap

        GUI.Box(new Rect(x, y, w, h), "  CHEAT MENU  [Y]", boxStyle);

        float by = y + 50f;
        if (GUI.Button(new Rect(x + 10, by,         w - 20, bh), $"God mode: {(IsGodMode    ? "ON" : "OFF")}",  btnStyle)) OnGodModeClicked();
        if (GUI.Button(new Rect(x + 10, by + gap,   w - 20, bh), $"Noclip:   {(noclipActive ? "ON" : "OFF")}",  btnStyle)) OnNoclipClicked();
        if (GUI.Button(new Rect(x + 10, by + gap*2, w - 20, bh), "Full health",                                     btnStyle)) OnHealClicked();
        if (GUI.Button(new Rect(x + 10, by + gap*3, w - 20, bh), "Kill all zombies",                              btnStyle)) OnKillAllClicked();
        if (GUI.Button(new Rect(x + 10, by + gap*4, w - 20, bh), "Skip to next zone →",                         btnStyle)) OnSkipZoneClicked();
    }

    private void InitStyles()
    {
        if (stylesInit) return;
        stylesInit = true;

        boxStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize  = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter
        };
        boxStyle.normal.background    = MakeTex(1, 1, new Color(0.05f, 0.05f, 0.12f, 0.92f));
        boxStyle.normal.textColor     = Color.white;

        btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 14,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter
        };
    }

    // --- Cheat actions (PG2202-12) ---

    public void OnGodModeClicked()
    {
        IsGodMode = !IsGodMode;
        if (IsGodMode && playerHealth != null)
            playerHealth.Heal(9999);
    }

    public void OnNoclipClicked()
    {
        noclipActive = !noclipActive;
        if (playerMovement != null) playerMovement.enabled = !noclipActive;
        if (charController != null) charController.enabled = !noclipActive;
    }

    public void OnSkipZoneClicked()
    {
        if (noclipActive) OnNoclipClicked();
        isOpen = false;
        Time.timeScale = 1f;
        Cursor.visible   = false;
        Cursor.lockState = CursorLockMode.Locked;
        GameManager.Instance?.LoadNextZone();
    }

    public void OnKillAllClicked()
    {
        StartCoroutine(KillAllZombiesRoutine());
    }

    public void OnHealClicked()
    {
        playerHealth?.Heal(9999);
    }

    // Ved lukk: respekter ESC-pause (Paused) så spillet ikke starter mens pausemeny fortsatt er aktiv
    private void ApplyTimeScaleAfterClosingCheatMenu()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;
    }

    // Destroy(delay) bruker skalert tid — midlertidig timeScale=1 så zombier faktisk fjernes
    private IEnumerator KillAllZombiesRoutine()
    {
        Time.timeScale = 1f;
        ZombieHealth[] zombies = Object.FindObjectsByType<ZombieHealth>(FindObjectsSortMode.None);
        foreach (ZombieHealth z in zombies)
            z.TakeDamage(99999);
        yield return new WaitForSecondsRealtime(2.6f);
        if (isOpen)
            Time.timeScale = 0f;
        else
            ApplyTimeScaleAfterClosingCheatMenu();
    }

    // Noclip: fri fly for testing (sensor/eksamen)

    private void HandleNoclipMovement()
    {
        // Noclip etter testing: lukk menyen med Y — ellers beveger karakter mens GUI er åpen
        if (isOpen) return;
        if (Camera.main == null || playerMovement == null) return;

        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W))           move += Camera.main.transform.forward;
        if (Input.GetKey(KeyCode.S))           move -= Camera.main.transform.forward;
        if (Input.GetKey(KeyCode.A))           move -= Camera.main.transform.right;
        if (Input.GetKey(KeyCode.D))           move += Camera.main.transform.right;
        if (Input.GetKey(KeyCode.Space))       move += Vector3.up;
        if (Input.GetKey(KeyCode.LeftControl)) move -= Vector3.up;

        playerMovement.transform.position += move * noclipSpeed * Time.unscaledDeltaTime;
    }

    // Lager en 1x1 farge-tekstur for GUI-stil bakgrunn
    private static Texture2D MakeTex(int w, int h, Color col)
    {
        Color[] pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D t = new Texture2D(w, h);
        t.SetPixels(pix);
        t.Apply();
        return t;
    }
}
