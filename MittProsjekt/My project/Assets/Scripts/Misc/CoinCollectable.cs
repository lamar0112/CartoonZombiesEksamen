using UnityEngine;

// CoinCollectable — parkour-mynt i Level01_By (PG2202-04 OnTriggerEnter, PG2202-08 valgfri gameplay-utvidelse).
// Pensum: CompareTag / komponent-sjekk for spiller; Destroy etter oppsamling; enkel transform-rotasjon.
// Ekstra: konkav MeshCollider kan ikke være trigger — vi deaktiverer slike og legger SphereCollider (Unity-fysikkregel, forklares i rapport).
[RequireComponent(typeof(Collider))]
public class CoinCollectable : MonoBehaviour
{
    [Header("Tilhørighet")]
    [SerializeField] private int parkourZoneId = 1;   // 1 = ParkourZone1, 2 = ParkourZone2

    [Header("Visuell rotasjon")]
    [SerializeField] private float spinSpeed = 90f;   // grader per sekund

    [Header("Lyd og VFX")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private GameObject collectVFX;

    private bool collected = false;

    private void Awake()
    {
        // Triggers på konkave MeshCollider er ikke støttet — bruk sfære eller convex mesh (PG2202-04)
        EnsurePickupTriggers();
    }

    private void EnsurePickupTriggers()
    {
        Collider[] cols = GetComponents<Collider>();
        bool anyTrigger = false;
        foreach (Collider c in cols)
        {
            if (c == null) continue;
            if (c is MeshCollider mc && !mc.convex)
            {
                mc.enabled = false;
                continue;
            }
            c.enabled = true;
            c.isTrigger = true;
            anyTrigger = true;
        }
        if (!anyTrigger)
        {
            var sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.65f;
        }
    }

    private void Update()
    {
        // Roterer mynten visuelt (PG2202-02 transform-manipulasjon)
        transform.Rotate(Vector3.up * spinSpeed * Time.deltaTime, Space.World);
    }

    // Kalles automatisk av Unity-fysikkmotoren når noe med collider berører trigger (PG2202-04)
    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!IsPlayerCollider(other)) return;

        collected = true;

        // VFX på samlingsstedet
        if (collectVFX != null)
            Instantiate(collectVFX, transform.position, Quaternion.identity);

        // Lyd via en-shot (AudioSource trenger ikke å leve etter objektet er ødelagt)
        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        // Melder fra til CityParkourManager (observer-pattern via metode-kall, PG2202-03)
        CityParkourManager.Instance?.RegisterCoinCollected(parkourZoneId);

        // Ødelegger mynten umiddelbart etter samling
        Destroy(gameObject);
    }

    private static bool IsPlayerCollider(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player")) return true;
        return other.GetComponentInParent<PlayerMovement>() != null
            || other.GetComponentInParent<CharacterController>() != null;
    }

    // Viser samlingsradius i Scene-view (PG2202-02)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.8f);
    }
}
