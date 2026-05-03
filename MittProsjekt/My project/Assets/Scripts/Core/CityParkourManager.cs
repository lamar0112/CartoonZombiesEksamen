using UnityEngine;
using UnityEngine.Events;

// Styrer by-scenens to parkour-soner (ParkourZone1 og ParkourZone2)
// Singleton — kun én CityParkourManager per scene (PG2202-12 Singleton)
// Aktiverer sone-triggeren til strand-nivået når begge sonene er fullført
//
// Oppsett i Inspector:
//  - coinsInZone1 / coinsInZone2: tell mynter i hver sone i Scene-view (Gizmos)
//  - zone2ZoneTrigger: drag inn GameObject med ZoneTrigger-script (til stranden)
public class CityParkourManager : MonoBehaviour
{
    // ── Singleton (PG2202-12) ─────────────────────────────────────────────────
    public static CityParkourManager Instance { get; private set; }

    [Header("Coins per parkour zone")]
    [SerializeField] private int coinsInZone1 = 10;
    [SerializeField] private int coinsInZone2 = 10;

    [Header("Beach exit (enabled when both zones are complete)")]
    [SerializeField] private GameObject beachZoneTrigger;

    [Header("Events — wire UI here (PG2202-02 UnityEvent)")]
    public UnityEvent<int, int> OnZone1Progress;  // (collected, total)
    public UnityEvent<int, int> OnZone2Progress;
    public UnityEvent           OnAllZonesComplete;

    // ── Intern state (PG2202-03 state tracking) ───────────────────────────────
    private int  zone1Collected = 0;
    private int  zone2Collected = 0;
    private bool zone1Complete  = false;
    private bool zone2Complete  = false;

    public bool BothAreasComplete => zone1Complete && zone2Complete;

    private void Awake()
    {
        // Singleton-mønster: ødelegger duplikat (PG2202-12)
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Sone-triggeren til strand er låst til begge soner er fullført
        if (beachZoneTrigger != null)
            beachZoneTrigger.SetActive(false);
    }

    // Kalles av CoinCollectable for hver mynt som samles (PG2202-03 observer)
    public void RegisterCoinCollected(int zoneId)
    {
        if (zoneId == 1)
        {
            zone1Collected = Mathf.Min(zone1Collected + 1, coinsInZone1);
            OnZone1Progress?.Invoke(zone1Collected, coinsInZone1);
            Debug.Log($"[ParkourCity] Sone 1: {zone1Collected}/{coinsInZone1} mynter");

            if (zone1Collected >= coinsInZone1)
                CompleteZone(1);
        }
        else if (zoneId == 2)
        {
            zone2Collected = Mathf.Min(zone2Collected + 1, coinsInZone2);
            OnZone2Progress?.Invoke(zone2Collected, coinsInZone2);
            Debug.Log($"[ParkourCity] Sone 2: {zone2Collected}/{coinsInZone2} mynter");

            if (zone2Collected >= coinsInZone2)
                CompleteZone(2);
        }
    }

    // Kan kalles manuelt fra editor (Cheat) eller fra andre triggere
    public void CompleteZone(int zoneId)
    {
        if (zoneId == 1) zone1Complete = true;
        if (zoneId == 2) zone2Complete = true;

        Debug.Log($"[ParkourCity] Sone {zoneId} fullført! " +
                  $"Status: Z1={zone1Complete} Z2={zone2Complete}");

        if (BothAreasComplete)
            OnAllZonesCompleted();
    }

    private void OnAllZonesCompleted()
    {
        Debug.Log("[ParkourCity] BEGGE soner fullført — strand-exit aktivert!");

        // Aktiverer zone-trigger til strand
        if (beachZoneTrigger != null)
            beachZoneTrigger.SetActive(true);

        OnAllZonesComplete?.Invoke();
    }

    // ── Debug / Editor ────────────────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        Gizmos.color = BothAreasComplete ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}
