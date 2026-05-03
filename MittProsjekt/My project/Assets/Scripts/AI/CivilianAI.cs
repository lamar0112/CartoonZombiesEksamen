using UnityEngine;
using UnityEngine.AI;

// Sivil NPC i Zone2_City — flykter fra zombier, vandrer tilfeldig ellers
// Viser NavMeshAgent i praksis (pensum PG2202-AI)
// Bruker State Machine-mønster: Idle → Wander → Flee (PG2202-03)
[RequireComponent(typeof(NavMeshAgent))]
public class CivilianAI : MonoBehaviour
{
    public enum State { Idle, Wander, Flee }

    [Header("Bevegelse")]
    [SerializeField] private float wanderRadius  = 18f;
    [SerializeField] private float wanderSpeed   =  2.5f;
    [SerializeField] private float fleeSpeed     =  5.5f;

    [Header("Sanser")]
    [SerializeField] private float detectRadius  = 14f;  // ser zombier innen denne radius
    [SerializeField] private float safeRadius    = 22f;  // stopper å flykte når zombiene er langt nok unna

    [Header("Timing")]
    [SerializeField] private float wanderInterval = 4f;  // ny vandremål hvert 4. sek
    [SerializeField] private float checkInterval  = 0.4f;// sjekker zombier 2.5 ganger per sekund

    private NavMeshAgent  agent;
    private State         currentState = State.Idle;
    private float         wanderTimer  = 0f;
    private float         checkTimer   = 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        agent.speed = wanderSpeed;
        SetState(State.Wander);
    }

    // Update er enkel dispatcher — logikken er i state-metodene (PG2202-03 state machine)
    private void Update()
    {
        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            CheckForZombies();
        }

        switch (currentState)
        {
            case State.Wander: UpdateWander(); break;
            case State.Flee:   UpdateFlee();   break;
        }
    }

    // Leter etter zombier med OverlapSphere (PG2202-AI)
    private void CheckForZombies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius);
        Transform nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Collider col in hits)
        {
            if (col.GetComponent<ZombieAI>() == null) continue;
            float d = Vector3.Distance(transform.position, col.transform.position);
            if (d < nearestDist) { nearestDist = d; nearest = col.transform; }
        }

        if (nearest != null)
        {
            fleeTarget = nearest.position;
            if (currentState != State.Flee) SetState(State.Flee);
        }
        else if (currentState == State.Flee)
        {
            SetState(State.Wander); // ingen zombier i nærheten — rolig igjen
        }
    }

    private Vector3 fleeTarget;

    private void UpdateWander()
    {
        wanderTimer += Time.deltaTime;
        if (wanderTimer >= wanderInterval || !agent.hasPath || agent.remainingDistance < 0.5f)
        {
            wanderTimer = 0f;
            Vector3 dest = RandomNavPoint(wanderRadius);
            agent.SetDestination(dest);
        }
    }

    private void UpdateFlee()
    {
        // Flykter i motsatt retning fra zombien (PG2202-AI flee-logikk)
        Vector3 fleeDir = (transform.position - fleeTarget).normalized;
        Vector3 fleeDest = transform.position + fleeDir * safeRadius;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(fleeDest, out hit, safeRadius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    private void SetState(State newState)
    {
        currentState = newState;
        agent.speed  = newState == State.Flee ? fleeSpeed : wanderSpeed;
        wanderTimer  = 0f;
    }

    // Finner et tilfeldig NavMesh-punkt innen radius (PG2202-AI standard pattern)
    private Vector3 RandomNavPoint(float radius)
    {
        Vector3 randomDir = Random.insideUnitSphere * radius + transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDir, out hit, radius, NavMesh.AllAreas))
            return hit.position;
        return transform.position;
    }

    // Viser sanseradius i Scene-view — nyttig for Inspector-debugging (PG2202-02)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, safeRadius);
    }
}
