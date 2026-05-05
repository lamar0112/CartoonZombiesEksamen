using UnityEngine;

// GameBalance — enkle statiske multiplikatorer for vanskelighetsgrad (PG2202-02: sentral «tuning» uten spredt magi).
// Pensum: leses av PlayerShooting / ZombieAI; justeres av RuntimeHierarchyTuning eller CheatMenu ved testing.
// Ekstra: verdier under 1 på skade til zombie = «hardere»; over 1 på skade fra zombie = mer skade til spiller.
public static class GameBalance
{
    /// <summary>Multiplikator på skade zombier gjør mot spiller (1 = prefab-verdi).</summary>
    public static float ZombieDamageToPlayerMultiplier = 1.25f;

    /// <summary>Multiplikator på skade spillerens skudd gjør mot zombier (1 = prefab-verdi).</summary>
    public static float PlayerGunDamageMultiplier = 0.82f;

    public static void ResetToDefaults()
    {
        ZombieDamageToPlayerMultiplier = 1.25f;
        PlayerGunDamageMultiplier      = 0.82f;
    }
}
