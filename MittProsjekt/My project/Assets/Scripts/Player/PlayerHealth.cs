using UnityEngine;
using UnityEngine.Events; // UnityEvent er ikke direkte pensum, men gir løs kobling mellom scripts

// Håndterer spillerens helse - skiller helse-logikk fra bevegelse og skyting
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    public int  CurrentHealth { get; private set; }
    public bool IsDead        { get; private set; }

    // UnityEvent med parametere - HUDController abonnerer på dette i Inspector
    // Sender nåværende og maks helse slik at UI-et kan vise riktig verdi
    // (ikke pensum, men enklere enn å ha direkte referanse til HUD-scriptet)
    public UnityEvent<int, int> OnHealthChanged;
    public UnityEvent           OnDeath;

    private void Start()
    {
        CurrentHealth = maxHealth;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth); // oppdaterer UI ved start
    }

    // Kalles av ZombieAI når zombien angriper spilleren
    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        // God Mode fra CheatMenu - sjekker singleton direkte (CheatMenu er på Canvas, ikke på Player)
        if (CheatMenu.Instance != null && CheatMenu.Instance.IsGodMode) return;

        // Mathf.Max sørger for at helsen aldri går under 0
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0)
            Die();
    }

    // Kan brukes av power-ups eller heal-soner
    public void Heal(int amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount); // ikke over maks
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void Die()
    {
        IsDead = true;
        OnDeath?.Invoke();

        // GameManager håndterer scene-overgang til GameOver-scenen (PG2202-12)
        GameManager.Instance?.TriggerGameOver();
    }
}
