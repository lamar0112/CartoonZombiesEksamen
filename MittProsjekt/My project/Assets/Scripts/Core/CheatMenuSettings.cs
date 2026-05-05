using UnityEngine;

// CheatMenuSettings — ScriptableObject for noclip-hastighet m.m. (PG2202-08; PG2202-12 delt config).
// Pensum: SerializeField-lignende felter i asset; lastes av CheatMenu.
// Ekstra: utvikler-/testverktøy — ikke nødvendig for sensur, men nyttig for debugging; kan disables i release-build.
[CreateAssetMenu(fileName = "CheatMenuSettings", menuName = "CartoonZombies/Cheat Menu Settings")]
public class CheatMenuSettings : ScriptableObject
{
    [Header("Movement")]
    [Tooltip("Fly-hastighet når noclip er på (WASD + Space / Ctrl).")]
    public float noclipSpeed = 12f;
}
