using System.Collections;
using UnityEngine;

// ZombieHealth — HP, TakeDamage, død og kobling til ZombieAI (PG2202-02 metoder, PG2202-05 modulær agent).
// Pensum: lyd via PlayClipAtPoint; Instantiate for VFX; spillerens raycast treffer via ZombieHealth på zombie.
// Ekstra: tag «Zombie» i Awake (samsvar med TagManager og eventuelle CompareTag-sjekker); verdens-HUD ZombieHealthBarWorld.
[RequireComponent(typeof(ZombieAI))]
public class ZombieHealth : MonoBehaviour
{
    [SerializeField] private int       maxHealth  = 50;
    [SerializeField] private AudioClip deathSound; // ZombieMoan eller liknende
    [SerializeField] private GameObject deathVfxPrefab;

    private int      currentHealth;
    private ZombieAI ai;

    public bool IsDead => currentHealth <= 0;
    public int MaxHealth     => maxHealth;
    public int CurrentHealth => currentHealth;

    // current, max — abonnert av ZombieHealthBarWorld (PG2202-08 UI-mønster: observer uten hard kobling).
    public event System.Action<int, int> OnHealthChanged;

    private void Awake()
    {
        gameObject.tag = "Zombie";
        ai            = GetComponent<ZombieAI>();
        currentHealth = maxHealth;
        if (GetComponent<ZombieHealthBarWorld>() == null)
            gameObject.AddComponent<ZombieHealthBarWorld>();
    }

    private void Start()
    {
        // Coroutine gir NavMesh og MeshColliders 2 frames til å initialisere før snap (PG2202-08)
        StartCoroutine(DelayedSnapToGround());
    }

    private IEnumerator DelayedSnapToGround()
    {
        // NavMesh bake + agent placement often settle a few frames after Instantiate (beach / large scenes).
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        if (this != null && gameObject != null && !IsDead)
            ZombieSnapPositionUtility.SnapAgentToGround(gameObject);
    }

    // Telling av levende zombier håndteres av ZombieSpawner (pre-plassert ved Start + umiddelbart ved Instantiate)

    /// <summary>Cheat / debug: dreper umiddelbart, inkl. zombier utenfor kamera eller i «døende» tilstand.</summary>
    public void CheatForceKill()
    {
        if (IsDead)
        {
            Destroy(gameObject);
            return;
        }

        TakeDamage(Mathf.Max(maxHealth * 4, 99999));
    }

    // Kalles av PlayerShooting når en kule treffer zombien
    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return; // allerede død, ignorer

        currentHealth -= amount;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        currentHealth = 0;

        if (deathVfxPrefab != null)
        {
            Vector3 p = transform.position + Vector3.up * 0.5f;
            GameObject vfx = Instantiate(deathVfxPrefab, p, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        // Forteller AI-en at zombien er død - AI spiller death-animasjon og deaktiverer seg
        ai.SetState(ZombieState.Dead);

        // Spiller dødslyd via AudioManager så vi ikke trenger AudioSource på hver zombie
        if (deathSound != null)
            AudioManager.Instance?.PlaySFX(deathSound);

        // Registrerer drapet i GameManager - oppdaterer score og highscore (PG2202-12)
        GameManager.Instance?.RegisterKill();

        // Varsler ZombieSpawner om at én zombie er drept - spawner teller levende zombier
        // FindObjectOfType er litt treigt, men kalles bare én gang ved død - ikke i Update
        ZombieSpawner spawner = Object.FindFirstObjectByType<ZombieSpawner>();
        spawner?.OnZombieDied();

        // Venter 2.5 sekunder så death-animasjonen rekker å spille av, deretter sletter GameObject
        Destroy(gameObject, 2.5f);
    }
}
