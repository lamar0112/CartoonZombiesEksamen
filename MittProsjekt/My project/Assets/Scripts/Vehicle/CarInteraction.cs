using UnityEngine;

// CarInteraction — trigger-sone / avstand for F: sett spiller som barn ved sete, skjul mesh, slå av PlayerMovement (PG2202-04, PG2202-02 GetComponent).
// Pensum: enkel interaksjon med tast; CharacterController må disables når spiller parentes til bil for å unngå dobbel kollisjon.
// Ekstra: kaller CarController.SetZombieCollisionsIgnored — se CarController for begrunnelse (horde vs bil-fysikk).
public class CarInteraction : MonoBehaviour
{
    [SerializeField] private float        interactRange = 4.75f;
    [SerializeField] private Transform    driverSeat;   // tom GameObject inne i bilen
    [SerializeField] private CarController carController;

    private Transform           player;
    private MonoBehaviour       playerMovement;
    private CharacterController playerController;
    private Renderer[]          playerRenderers; // skjules mens spilleren sitter i bil
    private bool                playerInCar = false;

    private void Awake()
    {
        if (carController == null)
            carController = GetComponent<CarController>();
        if (driverSeat == null)
        {
            Transform t = transform.Find("DriverSeat");
            if (t != null) driverSeat = t;
        }
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        player = playerObj.transform;

        playerMovement    = playerObj.GetComponentInChildren<PlayerMovement>(true);
        playerController  = playerObj.GetComponentInChildren<CharacterController>(true);
        // Samler alle synlige mesh-renderers på spillerkroppen (skjules i bil)
        playerRenderers   = playerObj.GetComponentsInChildren<Renderer>(true);
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        bool blocked = Time.timeScale <= 0f
            || (CheatMenu.Instance != null && CheatMenu.Instance.IsCheatMenuOpen);

        if (!blocked && Input.GetKeyDown(KeyCode.F))
        {
            if (!playerInCar && dist <= interactRange)
                EnterCar();
            else if (playerInCar)
                ExitCar();
        }

        if (InteractionHint.Instance != null)
        {
            if (blocked)
                InteractionHint.Instance.Hide();
            else if (!playerInCar && dist <= interactRange)
                InteractionHint.Instance.Show("[F] Sett deg inn i bilen  ·  WASD når du kjører");
            else if (playerInCar)
                InteractionHint.Instance.Show("Kjører: WASD / piler  ·  Mellomrom = brems  ·  [F] gå ut");
            else
                InteractionHint.Instance.Hide();
        }

        // Holder spilleren festet til setet mens de er i bilen
        if (playerInCar && driverSeat != null)
        {
            player.position = driverSeat.position;
            player.rotation = driverSeat.rotation;
        }
    }

    private void EnterCar()
    {
        if (carController == null)
        {
            Debug.LogWarning($"{nameof(CarInteraction)} på {name}: mangler {nameof(CarController)} — kan ikke kjøre.");
            return;
        }

        playerInCar = true;

        if (playerMovement   != null) playerMovement.enabled   = false;
        if (playerController != null) playerController.enabled = false;

        // Skjul spillerkroppen — hindrer hode/armer å stikke gjennom biltak (PG2202-04)
        SetPlayerVisible(false);

        // Kamera: følg bilen med bil-offset (høyere, lengre bak)
        CameraFollow cf = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cf != null) { cf.SetTarget(transform); cf.SetVehicleMode(true); }

        carController.IsOccupied = true;
        carController.SetZombieCollisionsIgnored(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        // Fullfør bare «nå bilen»-steget — ikke hopp over pistol/bølge (MissionManager-sekvens)
        MissionManager.Instance?.TryCompleteReachCarMission();
    }

    private void ExitCar()
    {
        playerInCar = false;

        player.position = transform.position + transform.right * 2.5f + Vector3.up * 0.5f;
        // Unngå «ligger ned»-glitch: rett opp rotasjon (båt/bil kan ha skrå sete-transform).
        Vector3 euler = player.eulerAngles;
        player.rotation = Quaternion.Euler(0f, euler.y, 0f);

        if (playerMovement   != null) playerMovement.enabled   = true;
        if (playerController != null) playerController.enabled = true;

        // Nullstill enkel locomotion-animator etter kjøring (hvis ithappy/Synty bruker blend trees).
        var locomotionAnimator = player.GetComponentInChildren<Animator>(true);
        if (locomotionAnimator != null)
        {
            locomotionAnimator.SetFloat("Hor", 0f);
            locomotionAnimator.SetFloat("Vert", 0f);
            locomotionAnimator.SetFloat("State", 0f);
            locomotionAnimator.SetBool("IsJump", false);
        }

        // Vis spillerkroppen igjen
        SetPlayerVisible(true);

        // Kamera tilbake til spiller-modus
        CameraFollow cf = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cf != null) { cf.SetVehicleMode(false); cf.SetTarget(player); }

        if (carController != null)
        {
            carController.SetZombieCollisionsIgnored(false);
            carController.IsOccupied = false;
        }
    }

    private void SetPlayerVisible(bool on)
    {
        if (playerRenderers == null) return;
        foreach (Renderer r in playerRenderers)
            if (r != null) r.enabled = on;
    }

    // Viser interaksjonsradius i Scene-view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
