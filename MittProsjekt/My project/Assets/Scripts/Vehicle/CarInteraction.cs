using UnityEngine;

// Håndterer inn og ut av bil - spilleren trykker F for å entre/forlate (PG2202-04)
public class CarInteraction : MonoBehaviour
{
    [SerializeField] private float        interactRange = 4.75f;
    [SerializeField] private Transform    driverSeat;   // tom GameObject inne i bilen
    [SerializeField] private CarController carController;

    private Transform           player;
    private MonoBehaviour       playerMovement;  // PlayerMovement — deaktiveres i bil
    private CharacterController playerController;
    private MonoBehaviour       playerCamera;    // CameraFollow
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

        // Henter våre egne bevegelse-scripts som deaktiveres mens spilleren kjører bil
        playerMovement    = playerObj.GetComponent<PlayerMovement>();
        playerController  = playerObj.GetComponent<CharacterController>();
        playerCamera      = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
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
                InteractionHint.Instance.Show("[F] Gå inn i kjørbar bil (WASD når du sitter)");
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

        // Deaktiverer spillerens bevegelse mens de er i bilen (PG2202-04)
        if (playerMovement != null) playerMovement.enabled = false;
        // CharacterController må av — ellers kolliderer «kroppen» med bilens Rigidbody og låser bevegelse
        if (playerController != null) playerController.enabled = false;

        // Kamera følger bilen i stedet for å bli slått av — bruker SetTarget (PG2202-12)
        CameraFollow cf = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cf != null) cf.SetTarget(transform);

        carController.IsOccupied = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void ExitCar()
    {
        playerInCar = false;

        // Plasserer spilleren ved siden av bilen og reaktiverer scripts
        player.position = transform.position + transform.right * 2.5f + Vector3.up * 0.5f;

        if (playerMovement != null) playerMovement.enabled = true;
        if (playerController != null) playerController.enabled = true;

        // Kamera tilbake til spilleren (PG2202-12)
        CameraFollow cf = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cf != null) cf.SetTarget(player);

        if (carController != null)
            carController.IsOccupied = false;
    }

    // Viser interaksjonsradius i Scene-view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
