using UnityEngine;

// Mynt/poeng-objekt i by-scenens parkour-soner
// Spilleren samler dem ved å gå gjennom — trigger-basert kollisjon (PG2202-04)
// Rapporterer til CityParkourManager hvilken sone mynten tilhører
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
        // Sørger for at collideren er trigger (PG2202-04)
        GetComponent<Collider>().isTrigger = true;
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
        if (!other.CompareTag("Player")) return;

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

    // Viser samlingsradius i Scene-view (PG2202-02)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.8f);
    }
}
