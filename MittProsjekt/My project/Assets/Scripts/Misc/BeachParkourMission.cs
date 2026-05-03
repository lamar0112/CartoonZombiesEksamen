using UnityEngine;

// Parkour-sjekkpunkt-system for Zone3_Beach
// Spilleren hopper mellom hoppeplattformer sør for stranden
// Demonstrerer: trigger-basert state machine (PG2202-03), proximity-sjekk (PG2202-AI)
// Sjekkpunkter kobles til fra Zone3BeachMapBuilder via SerializedObject
public class BeachParkourMission : MonoBehaviour
{
    [Header("Sjekkpunkter — settes av Zone3BeachMapBuilder")]
    [SerializeField] private Transform[] checkpoints;
    [SerializeField] private float       checkRadius = 3f;

    [Header("Respawn")]
    [SerializeField] private Transform   respawnPoint;   // Hvis spilleren faller i vannet
    [SerializeField] private float       waterY = -1f;   // Drukner under denne Y-verdien

    // Intern state — demonstrerer state machine-mønsteret (PG2202-03)
    private int   nextCheckpoint = 0;
    private float elapsed        = 0f;
    private bool  running        = false;
    private bool  finished       = false;

    // Hendelse som GameManager/HUD kan lytte på (PG2202-03 observer pattern)
    public static System.Action<float> OnParkourComplete;

    private void Update()
    {
        if (finished) return;

        // Timer kun mens parkour-løpet pågår
        if (running) elapsed += Time.deltaTime;

        CheckProximity();
        CheckWaterDeath();
    }

    // Sjekker avstand til neste sjekkpunkt (PG2202-AI proximity-mønster)
    private void CheckProximity()
    {
        if (checkpoints == null || nextCheckpoint >= checkpoints.Length) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        float dist = Vector3.Distance(player.transform.position,
                                      checkpoints[nextCheckpoint].position);
        if (dist > checkRadius) return;

        // Startposisjonen starter timeren
        if (nextCheckpoint == 0 && !running)
        {
            running  = true;
            elapsed  = 0f;
            Debug.Log("[Parkour] START! Hopp mellom plattformene.");
        }

        nextCheckpoint++;

        if (nextCheckpoint >= checkpoints.Length)
        {
            // Parkour ferdig (PG2202-03 event)
            finished = true;
            running  = false;
            Debug.Log($"[Parkour] FERDIG! Tid: {elapsed:F1}s");
            OnParkourComplete?.Invoke(elapsed);
        }
        else
        {
            Debug.Log($"[Parkour] Sjekkpunkt {nextCheckpoint}/{checkpoints.Length - 1} — {elapsed:F1}s");
        }
    }

    // Respawn hvis spilleren faller i vannet
    private void CheckWaterDeath()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        if (player.transform.position.y > waterY) return;

        // Tilbake til siste passerte sjekkpunkt eller start
        int respawnIdx = Mathf.Max(0, nextCheckpoint - 1);
        if (checkpoints != null && respawnIdx < checkpoints.Length)
            player.transform.position = checkpoints[respawnIdx].position + Vector3.up * 1.5f;
        else if (respawnPoint != null)
            player.transform.position = respawnPoint.position;

        Debug.Log("[Parkour] Falt i vannet — respawner!");
    }

    // Viser sjekkpunktene i Scene-view (PG2202-02 Gizmos)
    private void OnDrawGizmos()
    {
        if (checkpoints == null) return;
        for (int i = 0; i < checkpoints.Length; i++)
        {
            if (checkpoints[i] == null) continue;
            Gizmos.color = i == 0                      ? Color.green
                         : i == checkpoints.Length - 1 ? Color.red
                                                       : Color.yellow;
            Gizmos.DrawWireSphere(checkpoints[i].position, checkRadius);
            if (i > 0 && checkpoints[i - 1] != null)
                Gizmos.DrawLine(checkpoints[i - 1].position, checkpoints[i].position);
        }
    }
}
