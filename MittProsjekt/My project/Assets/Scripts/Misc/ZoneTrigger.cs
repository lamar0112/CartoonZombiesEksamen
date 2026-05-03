using UnityEngine;

// Usynlig trigger-sone som laster neste sone når spilleren går gjennom (PG2202-04 kollisjon)
// Legg denne på et tomt GameObject med en BoxCollider (Is Trigger = true)
//
// Tre valgfrie betingelser (alle må være oppfylt for at trigger skal fyre):
//  • requireAllZombiesDead      — alle zombier i scenen er drept
//  • requireBothParkourZones    — CityParkourManager melder at begge parkour-soner er fullført (by-scene)
//  • requireBoatUnlocked        — BoatUnlockSystem melder at båten er låst opp (strand-scene)
[RequireComponent(typeof(Collider))]
public class ZoneTrigger : MonoBehaviour
{
    [Header("Betingelser — aktiver kun de som er relevante for denne scenen")]
    [SerializeField] private bool requireAllZombiesDead   = false;
    [SerializeField] private bool requireBothParkourZones = false; // by-scene: bruker CityParkourManager
    [SerializeField] private bool requireBoatUnlocked     = false; // strand-scene: bruker BoatUnlockSystem

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    // Kalles automatisk av Unity når et objekt med Rigidbody går inn i trigger (PG2202-04)
    private void OnTriggerEnter(Collider other)
    {
        // Sjekker at det er spilleren (ikke zombier eller andre objekter)
        if (!other.CompareTag("Player")) return;

        // ── Sjekk 1: alle zombier i scenen døde ──────────────────────────────
        if (requireAllZombiesDead)
        {
            // Døde zombier ligger fortsatt i scenen til Destroy(2.5s) — tell kun levende (PG2202-04)
            foreach (ZombieHealth z in Object.FindObjectsByType<ZombieHealth>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!z.IsDead) return;
            }
        }

        // ── Sjekk 2: begge parkour-soner i by-scenen fullført (PG2202-03 observer) ──
        if (requireBothParkourZones)
        {
            if (CityParkourManager.Instance == null || !CityParkourManager.Instance.BothAreasComplete)
                return;
        }

        // ── Sjekk 3: båt låst opp av BoatUnlockSystem (PG2202-03 observer) ───
        if (requireBoatUnlocked)
        {
            BoatUnlockSystem boat = Object.FindFirstObjectByType<BoatUnlockSystem>();
            if (boat == null || !boat.IsUnlocked) return;
        }

        GameManager.Instance?.LoadNextZone();
    }

    // Tegner trigger-boksen i Scene-view så vi ser den i editor (PG2202-02)
    private void OnDrawGizmos()
    {
        bool zombiesOk = !requireAllZombiesDead;
        if (requireAllZombiesDead)
        {
            zombiesOk = true;
            foreach (ZombieHealth z in Object.FindObjectsByType<ZombieHealth>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!z.IsDead) { zombiesOk = false; break; }
            }
        }

        bool ready = zombiesOk
                  && (!requireBothParkourZones || (CityParkourManager.Instance?.BothAreasComplete ?? false))
                  && (!requireBoatUnlocked     || (Object.FindFirstObjectByType<BoatUnlockSystem>()?.IsUnlocked ?? false));

        Gizmos.color = ready
            ? new Color(0f, 1f, 0f, 0.35f)   // grønn = betingelser oppfylt
            : new Color(1f, 0.3f, 0f, 0.35f); // oransje = venter fortsatt

        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
            Gizmos.DrawCube(transform.position, box.size);
    }
}
