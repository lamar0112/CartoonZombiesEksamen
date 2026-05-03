using UnityEngine;
using UnityEngine.Events;

// Level02_StrandSkog — låser opp båten etter X zombie-drap
// Abonnerer på GameManager.OnKillRegistered (Observer-pattern, PG2202-03)
// Aktiverer båt-trigger-collider og viser melding til spilleren
//
// Oppsett i Inspector:
//  - killsRequired: antall drap (standard: 5)
//  - boatTriggerObject: GameObject med trigger som sender spilleren til øya
//  - boatVisualIndicator: valgfritt — blink/markering på båten
public class BoatUnlockSystem : MonoBehaviour
{
    [Header("Unlock requirement")]
    [SerializeField] private int killsRequired = 5;

    [Header("Objects toggled on unlock")]
    [SerializeField] private GameObject boatTriggerObject;     // trigger → til øya
    [SerializeField] private GameObject boatVisualIndicator;   // eks. grønn partikkel/pil over båten
    [SerializeField] private GameObject lockedIndicator;       // eks. rød lås-ikon

    [Header("Audio")]
    [SerializeField] private AudioClip  unlockSound;

    [Header("Events (PG2202-02 UnityEvent)")]
    public UnityEvent<int, int> OnKillProgress;  // (kills, required)
    public UnityEvent           OnBoatUnlocked;

    // ── Intern state ──────────────────────────────────────────────────────────
    private int  localKills   = 0;
    private bool boatUnlocked = false;

    public bool IsUnlocked => boatUnlocked;

    private void Start()
    {
        // Båten er låst fra start
        SetBoatLocked(true);

        // Abonnerer på GameManager.OnKillRegistered (PG2202-03 observer/event)
        if (GameManager.Instance != null)
            GameManager.Instance.OnKillRegistered += HandleKill;
        else
            Debug.LogWarning("[BoatUnlock] GameManager.Instance er null i Start()!");
    }

    private void OnDestroy()
    {
        // Viktig: meld av abonnementet for å unngå null-referanse-feil (PG2202-03)
        if (GameManager.Instance != null)
            GameManager.Instance.OnKillRegistered -= HandleKill;
    }

    // Kalles av GameManager.OnKillRegistered for hvert zombie-drap (PG2202-03)
    private void HandleKill(int totalKillsInGame)
    {
        if (boatUnlocked) return;

        localKills++;
        Debug.Log($"[BoatUnlock] {localKills}/{killsRequired} drap i strand-scenen");

        OnKillProgress?.Invoke(localKills, killsRequired);

        if (localKills >= killsRequired)
            UnlockBoat();
    }

    private void UnlockBoat()
    {
        boatUnlocked = true;
        SetBoatLocked(false);

        if (unlockSound != null)
            AudioSource.PlayClipAtPoint(unlockSound, transform.position);

        Debug.Log("[BoatUnlock] BÅTEN ER LÅST OPP! Ro til øya.");
        OnBoatUnlocked?.Invoke();
    }

    // Hjelpemetode for å veksle visuell tilstand
    private void SetBoatLocked(bool locked)
    {
        if (boatTriggerObject    != null) boatTriggerObject.SetActive(!locked);
        if (boatVisualIndicator  != null) boatVisualIndicator.SetActive(!locked);
        if (lockedIndicator      != null) lockedIndicator.SetActive(locked);
    }

    // Tegner status i Scene-view
    private void OnDrawGizmos()
    {
        Gizmos.color = boatUnlocked ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 1.2f);
    }
}
