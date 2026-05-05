using UnityEngine;

// Våpenpickup — spilleren starter uten pistol, plukker den opp i scenen (PG2202-04 collision)
// Legg dette scriptet på et synlig 3D-objekt (eks. pistol-mesh) i scenen.
// Krav: PlayerShooting-scriptet på Player starter disabled.
[RequireComponent(typeof(Collider))]
public class WeaponPickup : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private float rotateSpeed = 60f;
    [Tooltip("Lav bob — unngår at mesh klipper under terreng ved pickup-animasjon.")]
    [SerializeField] private float bobHeight   = 0.06f;
    [Tooltip("Ekstra luft over raycast-treff (meter) før bob trekker Y ned.")]
    [SerializeField] private float minClearanceAboveGround = 0.55f;

    [Header("Pickup")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private GameObject pickupVfx; // valgfri partikkel-effekt

    private Vector3 _startPos;
    private Collider _pickupCollider;

    private void Awake()
    {
        _pickupCollider = ResolvePickupCollider();
    }

    private void Start()
    {
        if (_pickupCollider == null)
            _pickupCollider = ResolvePickupCollider();
        if (_pickupCollider != null)
            _pickupCollider.isTrigger = true;
        _startPos = transform.position;
        ClampStartAboveGround();
    }

    private void ClampStartAboveGround()
    {
        Vector3 rayOrigin = new Vector3(_startPos.x, _startPos.y + 120f, _startPos.z);
        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 260f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;

        float floorY = hit.point.y + minClearanceAboveGround + bobHeight;
        if (_startPos.y < floorY)
            _startPos = new Vector3(_startPos.x, floorY, _startPos.z);

        transform.position = _startPos;
    }

    /// <summary>Velger en kollider som trygt kan være trigger (ikke konkav mesh).</summary>
    private Collider ResolvePickupCollider()
    {
        var sphere = GetComponent<SphereCollider>();
        if (sphere != null) return sphere;

        foreach (var c in GetComponents<Collider>())
        {
            if (!c.enabled) continue;
            if (c is MeshCollider mc && !mc.convex) continue;
            return c;
        }

        return GetComponent<Collider>();
    }

    private void Update()
    {
        // Roterer og bopper (PG2202-03 — enkelt visuelt feedback)
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
        float y = _startPos.y + Mathf.Sin(Time.time * 2f) * bobHeight;
        transform.position = new Vector3(_startPos.x, y, _startPos.z);
    }

    // Utløses når en Collider med Rigidbody entrer trigger (PG2202-04)
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Aktiverer skyting på spilleren
        PlayerShooting shooting = other.GetComponentInParent<PlayerShooting>();
        if (shooting == null) shooting = other.GetComponent<PlayerShooting>();
        if (shooting != null) shooting.enabled = true;

        HUDController hud = Object.FindFirstObjectByType<HUDController>();
        if (hud != null)
            hud.ShowPickupMessage("<color=#FFE066><b>Weapon found!</b></color>");

        // Informer misjonssystemet om at spilleren har plukket opp våpen
        MissionManager.Instance?.CompleteCurrent();

        if (pickupVfx != null) Instantiate(pickupVfx, transform.position, Quaternion.identity);
        if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        Destroy(gameObject);
    }

    // Viser en markering i Scene-view
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.2f);
    }
}
