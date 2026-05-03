using UnityEngine;

// Enkel Rigidbody-basert bilkontroller for Zone 2 (City)
// Bruker ikke WheelCollider - for komplisert for dette prosjektet
// Rigidbody er standard Unity-komponent for fysikkbasert bevegelse (PG2202-04)
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Driving")]
    [SerializeField] private float motorForce  = 1500f; // fremover-kraft
    [SerializeField] private float brakeForce  = 3000f;
    [SerializeField] private float maxSteer    = 30f;   // maks svingvinkel i grader
    [SerializeField] private float maxSpeed    = 20f;   // maks hastighet m/s

    [Header("Wheel transforms (visual only, not WheelCollider)")]
    [SerializeField] private Transform frontLeftWheel;
    [SerializeField] private Transform frontRightWheel;
    [SerializeField] private Transform rearLeftWheel;
    [SerializeField] private Transform rearRightWheel;

    private Rigidbody rb;
    private float     steerInput;
    private float     throttleInput;
    private bool      isBraking;

    // Kan bare kjøres hvis en spiller er inne i bilen
    public bool IsOccupied { get; set; } = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Senker tyngdepunktet så bilen ikke velter like lett
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
    }

    private void Update()
    {
        if (!IsOccupied) return;
        if (Time.timeScale <= 0f) return;
        if (CheatMenu.Instance != null && CheatMenu.Instance.IsCheatMenuOpen) return;

        // Henter input fra tastatur (WASD / piltaster)
        throttleInput = Input.GetAxis("Vertical");   // W/S eller pil opp/ned
        steerInput    = Input.GetAxis("Horizontal"); // A/D eller pil venstre/høyre
        isBraking     = Input.GetKey(KeyCode.Space); // mellomrom for brems

        RotateWheels();
    }

    // FixedUpdate brukes for fysikk - kjøres med fast intervall uavhengig av framerate
    private void FixedUpdate()
    {
        if (!IsOccupied) return;
        if (Time.timeScale <= 0f) return;
        if (CheatMenu.Instance != null && CheatMenu.Instance.IsCheatMenuOpen) return;

        ApplyMotor();
        ApplySteering();
        ApplyBrake();
        ClampSpeed();
    }

    private void ApplyMotor()
    {
        // Legger til kraft fremover/bakover via AddRelativeForce (PG2202-04)
        rb.AddRelativeForce(Vector3.forward * throttleInput * motorForce);
    }

    private void ApplySteering()
    {
        // Roterer bilen basert på hastighet - svinger bedre ved høy fart
        float speed = rb.linearVelocity.magnitude;
        // Litt sving også i lav fart (ellers «låst» når man prøver å snu på stedet)
        float steerFactor = Mathf.Max(0.22f, Mathf.Clamp01(speed / 5f));
        float steerAmount = steerInput * maxSteer * steerFactor;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, steerAmount * Time.fixedDeltaTime * 30f, 0f));
    }

    private void ApplyBrake()
    {
        if (isBraking)
            rb.AddRelativeForce(-rb.linearVelocity.normalized * brakeForce);
    }

    private void ClampSpeed()
    {
        // Begrenser toppfart
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
    }

    // Roterer hjul-meshene visuelt (ikke funksjonell fysikk, kun for utseende)
    private void RotateWheels()
    {
        float speed    = rb.linearVelocity.magnitude;
        float rotation = speed * Time.deltaTime * 200f;

        // Ruller alle hjul fremover
        foreach (var wheel in new[] { frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel })
            wheel?.Rotate(rotation * Mathf.Sign(throttleInput), 0f, 0f, Space.Self);

        // Svinger forhjulene - bruker if-sjekk fordi ?. ikke kan brukes på venstre side av =
        float steerAngle = steerInput * maxSteer;
        if (frontLeftWheel  != null) frontLeftWheel.localRotation  = Quaternion.Euler(frontLeftWheel.localEulerAngles.x,  steerAngle, 0f);
        if (frontRightWheel != null) frontRightWheel.localRotation = Quaternion.Euler(frontRightWheel.localEulerAngles.x, steerAngle, 0f);
    }
}
