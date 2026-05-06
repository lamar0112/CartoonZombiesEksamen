using UnityEngine;

// CarController — enkel bil med Rigidbody (PG2202-04: kraft i FixedUpdate, ikke WheelCollider for enkelhets skyld).
// Pensum: AddForce med Acceleration; constraints mot velting; Input-aksler + tast fallback (PG2202-02).
// Ekstra: motor rettes i XZ-plan (skrå mesh forward); Physics.IgnoreCollision mot alle ZombieHealth-hierarkier når spiller sitter i bil —
// hindrer at horden skyver bilen (gameplay-valg, ikke i lærebokas «enkle kube-eksempel»).
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    public enum DriveAxis
    {
        Auto,
        Forward,
        Right,
        Up,
    }

    [Header("Driving")]
    [Tooltip("Akselerasjon i m/s² (ForceMode.Acceleration — uavhengig av masse og friksjon).")]
    [SerializeField] private float motorForce  = 14f;   // m/s² akselerasjon — bytt ikke til 5600
    [SerializeField] private float brakeForce  = 3000f;
    [SerializeField] private float maxSteer    = 30f;   // maks svingvinkel i grader
    [SerializeField] private float maxSpeed    = 20f;   // maks hastighet m/s
    [Tooltip("Hvilken lokal akse som peker 'fremover' for dette kjøretøyet. Bruk Auto hvis modellen er rotert/uvant.")]
    [SerializeField] private DriveAxis driveAxis = DriveAxis.Auto;

    [Header("Steering feel")]
    [Tooltip("Hvor raskt kjøretøyet roterer i yaw når du svinger (høyere = mer responsivt).")]
    [SerializeField] private float steerYawResponsiveness = 30f;

    [Header("Stabilitet")]
    [Tooltip("Ekstra nedover-kraft når noen kjører — holder hjulene bedre på bakken (PG2202-04 Rigidbody).")]
    [SerializeField] private float extraDownForceWhileDriving = 28f;
    [Tooltip("Sant for båt: lavere toppfart og mer demping (samme script som bil — enklere for pensum).")]
    [SerializeField] private bool  aquaticVehicle = false;

    [Header("Aquatic (kun når aquaticVehicle er på)")]
    [Tooltip("Toppfart m/s for båt — juster i Inspector hvis båten føles for treg eller for rask.")]
    [SerializeField] private float aquaticMaxSpeed = 16f;
    [Tooltip("Multiplikator på motorForce (0.9 ≈ 10 % mindre trekk).")]
    [SerializeField] private float aquaticMotorScale = 0.9f;
    [Tooltip("Minimum sving-faktor i lav fart — høyere = lettere å snu båten (var for lav).")]
    [SerializeField] private float aquaticMinSteerFactor = 0.52f;
    [Tooltip("Ekstra yaw-respons for båt — gjør det lettere å svinge/rette opp kurs.")]
    [SerializeField] private float aquaticSteerYawResponsiveness = 55f;
    [Tooltip("Andel av extraDownForceWhileDriving på båt (bil trenger mer «trykk» mot bakken).")]
    [Range(0f, 1f)] [SerializeField] private float aquaticExtraDownScale = 0.2f;
    [Tooltip("Døsone på styring (unngår at små Input Manager-verdier gir konstant rotasjon / «sirkel-spinner»).")]
    [SerializeField] private float aquaticSteerDeadZone = 0.12f;
    [Tooltip("Når styring er innenfor døsonen: multipliser Y-angularVelocity med denne hver FixedUpdate (lavere = mer demping).")]
    [Range(0.5f, 0.99f)] [SerializeField] private float aquaticYawDampingPerStep = 0.88f;

    [Header("Wheel transforms (visual only, not WheelCollider)")]
    [SerializeField] private Transform frontLeftWheel;
    [SerializeField] private Transform frontRightWheel;
    [SerializeField] private Transform rearLeftWheel;
    [SerializeField] private Transform rearRightWheel;

    private Rigidbody rb;
    private float     steerInput;
    private float     throttleInput;
    private bool      isBraking;
    private bool      _hasWheels; // sant hvis minst ett hjul er satt i Inspector

    // Kan bare kjøres hvis en spiller er inne i bilen
    public bool IsOccupied { get; set; } = false;

    private float _speedCap = 20f;
    private float _motorScale = 1f;
    private float _downForceScale = 1f;
    private float _minSteerFactor = 0.22f;
    private float _steerYaw = 30f;
    private float _steerForPhysics;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic   = false;
        rb.centerOfMass  = new Vector3(0f, -0.5f, 0f);
        rb.mass          = Mathf.Max(rb.mass, 3200f);
        rb.linearDamping = Mathf.Min(rb.linearDamping, 0.12f);
        rb.angularDamping = Mathf.Max(rb.angularDamping, 2.5f);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        // Hindrer bilen i å velte — viktig uten WheelColliders (PG2202-04)
        rb.constraints   = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.useGravity = true;

        _speedCap       = maxSpeed;
        _motorScale     = 1f;
        _downForceScale = 1f;
        _minSteerFactor = 0.22f;
        _steerYaw       = Mathf.Max(1f, steerYawResponsiveness);

        if (aquaticVehicle)
        {
            _speedCap       = Mathf.Min(maxSpeed, Mathf.Max(4f, aquaticMaxSpeed));
            _motorScale     = Mathf.Clamp(aquaticMotorScale, 0.35f, 1.25f);
            _downForceScale = Mathf.Clamp01(aquaticExtraDownScale);
            _minSteerFactor = Mathf.Clamp(aquaticMinSteerFactor, 0.22f, 1f);
            _steerYaw       = Mathf.Max(_steerYaw, aquaticSteerYawResponsiveness);
            rb.angularDamping = Mathf.Max(rb.angularDamping, 4.5f);
            rb.linearDamping  = Mathf.Max(rb.linearDamping, 0.28f);
        }

        _hasWheels = frontLeftWheel != null || frontRightWheel != null
                  || rearLeftWheel  != null || rearRightWheel  != null;
    }

    private void Update()
    {
        if (!IsOccupied) return;
        if (Time.timeScale <= 0f) return;
        if (CheatMenu.Instance != null && CheatMenu.Instance.IsCheatMenuOpen) return;

        // Henter input — fallback på tastatur hvis Input Manager-aksene er døde (Active Input Handling)
        throttleInput = Input.GetAxis("Vertical");
        steerInput    = Input.GetAxis("Horizontal");
        if (Mathf.Abs(throttleInput) < 0.02f)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) throttleInput = 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) throttleInput = -1f;
        }
        if (Mathf.Abs(steerInput) < 0.02f)
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) steerInput = -1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) steerInput = 1f;
        }
        isBraking = Input.GetKey(KeyCode.Space);

        _steerForPhysics = steerInput;
        if (aquaticVehicle && Mathf.Abs(_steerForPhysics) < aquaticSteerDeadZone)
            _steerForPhysics = 0f;

        if (_hasWheels) RotateWheels();
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

        if (aquaticVehicle && Mathf.Abs(_steerForPhysics) < 0.02f)
        {
            Vector3 av = rb.angularVelocity;
            av.y *= aquaticYawDampingPerStep;
            rb.angularVelocity = av;
        }

        if (extraDownForceWhileDriving > 0.01f)
            rb.AddForce(Vector3.down * (extraDownForceWhileDriving * _downForceScale), ForceMode.Acceleration);
    }

    private void ApplyMotor()
    {
        if (Mathf.Abs(throttleInput) < 0.01f) return;
        Vector3 dir = GetDriveDirectionOnXZ();
        if (dir.sqrMagnitude < 1e-6f) return;
        rb.AddForce(dir * throttleInput * motorForce * _motorScale, ForceMode.Acceleration);
    }

    private Vector3 GetDriveDirectionOnXZ()
    {
        Vector3 ProjectXZ(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        Vector3 axis = driveAxis switch
        {
            DriveAxis.Forward => transform.forward,
            DriveAxis.Right   => transform.right,
            DriveAxis.Up      => transform.up,
            _                 => Vector3.zero,
        };

        // Auto: velg den aksen som faktisk har tydelig XZ-komponent.
        if (driveAxis == DriveAxis.Auto)
        {
            Vector3 f = ProjectXZ(transform.forward);
            Vector3 r = ProjectXZ(transform.right);
            Vector3 u = ProjectXZ(transform.up);

            float f2 = f.sqrMagnitude;
            float r2 = r.sqrMagnitude;
            float u2 = u.sqrMagnitude;

            axis = f2 >= r2 && f2 >= u2 ? transform.forward
                : r2 >= u2              ? transform.right
                :                         transform.up;
        }

        Vector3 planar = ProjectXZ(axis);
        if (planar.sqrMagnitude < 1e-6f) return Vector3.zero;
        return planar.normalized;
    }

    /// <summary>Ignorer kollisjon mellom alle bil-kollidere og alle zombie-kollidere (unngå at horden skyver bilen).</summary>
    public void SetZombieCollisionsIgnored(bool ignore)
    {
        Collider[] carCols = GetComponentsInChildren<Collider>();
        if (carCols == null || carCols.Length == 0) return;

        foreach (ZombieHealth zh in Object.FindObjectsByType<ZombieHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (zh == null) continue;
            foreach (Collider zc in zh.GetComponentsInChildren<Collider>())
            {
                if (zc == null) continue;
                foreach (Collider cc in carCols)
                {
                    if (cc == null) continue;
                    Physics.IgnoreCollision(cc, zc, ignore);
                }
            }
        }
    }

    private void ApplySteering()
    {
        float steer = aquaticVehicle ? _steerForPhysics : steerInput;
        if (Mathf.Abs(steer) < 0.02f) return;

        // Roterer bilen basert på hastighet - svinger bedre ved høy fart
        float speed = rb.linearVelocity.magnitude;
        // Litt sving også i lav fart (ellers «låst» når man prøver å snu på stedet)
        float steerFactor = Mathf.Max(_minSteerFactor, Mathf.Clamp01(speed / 5f));
        float steerAmount = steer * maxSteer * steerFactor;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, steerAmount * Time.fixedDeltaTime * _steerYaw, 0f));
    }

    private void ApplyBrake()
    {
        if (!isBraking) return;
        Vector3 v = rb.linearVelocity;
        if (v.sqrMagnitude < 0.05f) return;
        rb.AddForce(-v.normalized * brakeForce, ForceMode.Force);
    }

    private void ClampSpeed()
    {
        // Begrenser toppfart
        if (rb.linearVelocity.magnitude > _speedCap)
            rb.linearVelocity = rb.linearVelocity.normalized * _speedCap;
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
