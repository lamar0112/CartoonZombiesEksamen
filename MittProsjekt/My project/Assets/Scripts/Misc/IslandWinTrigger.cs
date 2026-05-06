using UnityEngine;

// IslandWinTrigger — seier ved mål på øy, valgfri delay og VFX (PG2202-04 trigger; PG2202-12 Win-scene).
// Pensum: én gang-utløsning; kaller SceneLoader eller GameManager for win-flow.
// Ekstra: forsinkelse gir tid til kamerafølelse/partikler før meny — ren polish.
public class IslandWinTrigger : MonoBehaviour
{
    [Header("Valgfri forsinkelse før vinn-skjerm (sekunder)")]
    [SerializeField] private float winDelay = 2f;

    [Header("Trigger-oppsett (auto hvis tomt)")]
    [Tooltip("Hvis GameObject mangler Collider, legges BoxCollider på automatisk — slipper Unity-feil ved å legge på script først.")]
    [SerializeField] private bool autoAddBoxColliderIfMissing = true;
    [Tooltip("Størrelse på auto BoxCollider (verdensrom — juster i Inspector etter kiste-størrelse).")]
    [SerializeField] private Vector3 autoTriggerSize = new Vector3(4f, 3f, 4f);
    [Tooltip("Ekstra radius for «spiller i nærheten»-fallback (XZ) hvis OnTriggerEnter ikke fyrer (f.eks. barn-colliders som blokkerer).")]
    [SerializeField] private float proximityXZRadius = 2.2f;

    [Header("Visuell markering")]
    [SerializeField] private GameObject winVFX;    // Partikler over kisten

    private bool triggered = false;
    private Collider _col;

    private void Awake()
    {
        EnsureTriggerCollider();
    }

    private void EnsureTriggerCollider()
    {
        _col = GetComponent<Collider>();
        if (_col == null && autoAddBoxColliderIfMissing)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            box.size = autoTriggerSize;
            box.center = Vector3.zero;
            box.isTrigger = true;
            _col = box;
            Debug.Log($"[IslandWin] La til BoxCollider (trigger) på «{name}». Juster Size/Center i Inspector om nødvendig.");
        }

        if (_col != null)
            _col.isTrigger = true;
        else
            Debug.LogError($"[IslandWin] «{name}» har ingen Collider — seier kan ikke trigges. Legg til BoxCollider eller slå på autoAddBoxColliderIfMissing.");
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

    private void Update()
    {
        if (triggered) return;
        if (_col == null) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Vector3 p = player.transform.position;

        // Fallback 1: noclip — CharacterController av → triggers kan utebli
        if (CheatMenu.Instance != null && CheatMenu.Instance.IsNoclipActive && _col.bounds.Contains(p))
        {
            triggered = true;
            Debug.Log("[IslandWin] Noclip fallback: spiller inne i trigger-bounds — vinner spillet!");
            StartCoroutine(WinSequence());
            return;
        }

        // Fallback 2: spiller «ved siden av» kista (XZ) innen radius — hjelper når kun barn har fysiske colliders
        if (proximityXZRadius > 0.05f)
        {
            Vector3 c = _col.bounds.center;
            float dx = p.x - c.x;
            float dz = p.z - c.z;
            float dy = Mathf.Abs(p.y - c.y);
            if (dx * dx + dz * dz <= proximityXZRadius * proximityXZRadius && dy <= Mathf.Max(3f, _col.bounds.extents.y + 1.5f))
            {
                triggered = true;
                Debug.Log("[IslandWin] Nærhets-fallback: spiller ved kiste — vinner spillet!");
                StartCoroutine(WinSequence());
            }
        }
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
        // Vent litt så spilleren ser hva som skjer (Realtime: fungerer selv om timeScale er 0)
        yield return new UnityEngine.WaitForSecondsRealtime(winDelay);
        GameManager.Instance?.TriggerWin();
    }

    // Grønn ramme i Scene-view
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.84f, 0f, 0.5f);  // gull
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
            Gizmos.DrawCube(col.bounds.center, box.size);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(col != null ? col.bounds.center : transform.position,
            col is BoxCollider b ? b.size * 1.05f : Vector3.one);

        if (proximityXZRadius > 0.05f && col != null)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.25f);
            Vector3 c = col.bounds.center;
            Gizmos.DrawWireSphere(new Vector3(c.x, c.y, c.z), proximityXZRadius);
        }
    }
}
