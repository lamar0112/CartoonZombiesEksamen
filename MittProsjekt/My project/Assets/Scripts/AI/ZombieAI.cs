using UnityEngine;
using UnityEngine.AI; // NavMeshAgent ligger i dette namespace (PG2202-07)

// Enum-basert FSM - "Active State Generates"-mønsteret fra PG2202-05
// Hver tilstand håndterer sin egen logikk i Update()
public enum ZombieState { Patrol, Chase, Attack, Dead }

// RequireComponent sørger for at NavMeshAgent og Animator alltid er på samme GameObject
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ZombieAI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRange = 10f; // hvor langt zombien "ser"
    [SerializeField] private float attackRange    = 2f;  // når den begynner å angripe

    [Header("Combat")]
    [SerializeField] private int   attackDamage   = 10;
    [SerializeField] private float attackCooldown = 1.5f; // sekunder mellom hvert angrep

    [Header("Patrol")]
    [SerializeField] private float patrolRadius   = 15f;  // maks radius for tilfeldig vandring
    [SerializeField] private float patrolWaitTime = 2f;   // pause mellom patruljepunkter

    // Private felt - ikke eksponert i Inspector
    private NavMeshAgent  agent;    // styrer bevegelse langs NavMesh (PG2202-07)
    private Animator      animator; // Mecanim Animator Controller (PG2202-09)
    private Transform     player;

    private ZombieState currentState = ZombieState.Patrol;
    private float       attackTimer;
    private float       patrolTimer;
    private float       groundFixTimer;
    private Vector3     lastDryNavPosition;

    private void Awake()
    {
        // Henter komponenter på samme GameObject
        agent    = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // Finner spilleren via tag - spilleren MÅ ha tag "Player" satt i Inspector
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        SetState(ZombieState.Patrol); // starter i patrulje
        lastDryNavPosition = transform.position;
    }

    // Update kjøres hver frame - FSM-switchen bestemmer hvilken tilstand som er aktiv
    private void Update()
    {
        if (currentState == ZombieState.Dead) return; // gjør ingenting når død

        UpdateLocomotionAnimator();
        MaybeFixWaterOrBadGround();

        // PG2202-05: FSM - delegerer til tilstandens egen metode
        switch (currentState)
        {
            case ZombieState.Patrol: UpdatePatrol(); break;
            case ZombieState.Chase:  UpdateChase();  break;
            case ZombieState.Attack: UpdateAttack(); break;
        }
    }

    // --- PATROL: vandrer til tilfeldige punkter på NavMesh ---
    private void UpdatePatrol()
    {
        patrolTimer += Time.deltaTime;
        if (patrolTimer >= patrolWaitTime || !agent.hasPath)
        {
            patrolTimer = 0f;
            MoveToRandomPoint();
        }

        // Bytter til Chase når spilleren er nær nok
        if (player != null && DistanceToPlayer() <= detectionRange)
            SetState(ZombieState.Chase);
    }

    // Finner et tilfeldig punkt innen radius som faktisk ligger på NavMesh
    private void MoveToRandomPoint()
    {
        Vector3 randomDir = Random.insideUnitSphere * patrolRadius + transform.position;

        // NavMesh.SamplePosition - viktig! punktet må snappes til NavMesh (PG2202-07)
        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    // --- CHASE: forfølger spilleren ---
    private void UpdateChase()
    {
        if (player == null) { SetState(ZombieState.Patrol); return; }

        agent.SetDestination(player.position); // NavMeshAgent finner vei automatisk

        float dist = DistanceToPlayer();
        if (dist <= attackRange)
            SetState(ZombieState.Attack);      // nær nok til å angripe
        else if (dist > detectionRange * 1.5f)
            SetState(ZombieState.Patrol);      // mistet spilleren av syne
    }

    // --- ATTACK: stopper og angriper ---
    private void UpdateAttack()
    {
        if (player == null) { SetState(ZombieState.Patrol); return; }

        agent.ResetPath();             // stopper bevegelsen
        animator.SetFloat("Speed", 0f);
        transform.LookAt(player);      // snur seg mot spilleren

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
            animator.SetTrigger("Attack"); // trigger i Animator Controller (PG2202-09)

            // Sjekker avstand på nytt - spilleren kan ha beveget seg
            if (DistanceToPlayer() <= attackRange)
            {
                PlayerHealth ph = player.GetComponent<PlayerHealth>();
                if (ph != null) ph.TakeDamage(attackDamage);
            }
        }

        // Går tilbake til Chase hvis spilleren løp unna
        if (DistanceToPlayer() > attackRange)
            SetState(ZombieState.Chase);
    }

    // Offentlig metode - kalles av ZombieHealth når zombien dør
    public void SetState(ZombieState newState)
    {
        currentState = newState;

        if (newState == ZombieState.Dead)
        {
            agent.ResetPath();
            agent.enabled = false;             // deaktiverer NavMesh-navigasjon
            animator.SetTrigger("Death");      // spiller death-animasjon
            enabled = false;                   // stopper Update() på denne komponenten
        }
    }

    private float DistanceToPlayer()
    {
        if (player == null) return Mathf.Infinity;
        return Vector3.Distance(transform.position, player.position);
    }

    private void UpdateLocomotionAnimator()
    {
        if (animator == null || agent == null) return;

        float speedNorm = agent.speed > 0.01f
            ? Mathf.Clamp01(agent.velocity.magnitude / agent.speed)
            : 0f;

        if (currentState == ZombieState.Patrol && agent.hasPath &&
            agent.remainingDistance > agent.stoppingDistance + 0.05f)
            speedNorm = Mathf.Max(speedNorm, 0.32f);

        animator.SetFloat("Speed", speedNorm);
    }

    private void MaybeFixWaterOrBadGround()
    {
        groundFixTimer += Time.deltaTime;
        if (groundFixTimer < 0.14f) return;
        groundFixTimer = 0f;

        if (ZombieSnapPositionUtility.IsStandingOnWater(transform))
        {
            agent.Warp(lastDryNavPosition);
            transform.position = lastDryNavPosition;
            ZombieSnapPositionUtility.SnapAgentToGround(gameObject);
            return;
        }

        lastDryNavPosition = transform.position;
    }

    // Viser deteksjons- og angrepsradius i Scene-view (kun i editor, ikke i build)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
