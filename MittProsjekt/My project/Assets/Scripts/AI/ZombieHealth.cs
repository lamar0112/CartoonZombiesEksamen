using UnityEngine;

// ZombieHealth håndterer skade og død - skiller helse-logikk fra AI-logikk
// Dette gjør det enkelt å f.eks. legge til helse-bar senere uten å endre ZombieAI
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

    /// <summary>current, max — for helsebar og UI.</summary>
    public event System.Action<int, int> OnHealthChanged;

    private void Awake()
    {
        ai            = GetComponent<ZombieAI>();
        currentHealth = maxHealth;
        if (GetComponent<ZombieHealthBarWorld>() == null)
            gameObject.AddComponent<ZombieHealthBarWorld>();
    }

    private void Start()
    {
        // Retter forhåndsplasserte zombier (scene) som ligger i veibanen / under mesh
        ZombieSnapPositionUtility.SnapAgentToGround(gameObject);
    }

    // Telling av levende zombier håndteres av ZombieSpawner (pre-plassert ved Start + umiddelbart ved Instantiate)

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
