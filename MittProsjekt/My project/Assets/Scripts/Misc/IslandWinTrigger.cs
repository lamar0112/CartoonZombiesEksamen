using UnityEngine;

// Trigger på øya — spilleren vinner når de når kisten/målet (PG2202-04)
// Plasseres på et tomt GameObject med BoxCollider (Is Trigger = true)
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
        if (!other.CompareTag("Player")) return;

        triggered = true;
        Debug.Log("[IslandWin] Spilleren nådde kisten — vinner spillet!");

        // Kjør vinn-sekvens med forsinkelse via Coroutine (PG2202-08)
        StartCoroutine(WinSequence());
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
