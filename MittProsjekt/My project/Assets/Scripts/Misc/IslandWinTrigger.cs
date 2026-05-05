using UnityEngine;

// IslandWinTrigger — seier ved mål på øy, valgfri delay og VFX (PG2202-04 trigger; PG2202-12 Win-scene).
// Pensum: én gang-utløsning; kaller SceneLoader eller GameManager for win-flow.
// Ekstra: forsinkelse gir tid til kamerafølelse/partikler før meny — ren polish.
[RequireComponent(typeof(Collider))]
public class IslandWinTrigger : MonoBehaviour
{
    [Header("Valgfri forsinkelse før vinn-skjerm (sekunder)")]
    [SerializeField] private float winDelay = 2f;

    [Header("Visuell markering")]
    [SerializeField] private GameObject winVFX;    // Partikler over kisten

    private bool triggered = false;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        // Aktiver VFX for å gjøre kisten synlig for spilleren
        if (winVFX != null) winVFX.SetActive(true);
    }

    // Kalles av Unity når spilleren går inn i trigger (PG2202-04)
    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!IsPlayerCollider(other)) return;

        triggered = true;
        Debug.Log("[IslandWin] Spilleren nådde kisten — vinner spillet!");

        // Kjør vinn-sekvens med forsinkelse via Coroutine (PG2202-08)
        StartCoroutine(WinSequence());
    }

    // Spilleren kan ha CharacterController på rot og mesh-collider på barn uten «Player»-tag.
    private static bool IsPlayerCollider(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player")) return true;
        return other.GetComponentInParent<PlayerHealth>() != null;
    }

    private System.Collections.IEnumerator WinSequence()
    {
        // Vent litt så spilleren ser hva som skjer
        yield return new UnityEngine.WaitForSeconds(winDelay);
        GameManager.Instance?.TriggerWin();
    }

    // Grønn ramme i Scene-view
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.84f, 0f, 0.5f);  // gull
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
            Gizmos.DrawCube(transform.position, box.size);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position,
            col is BoxCollider b ? b.size * 1.05f : Vector3.one);
    }
}
