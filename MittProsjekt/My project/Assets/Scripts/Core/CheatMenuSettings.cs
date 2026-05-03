using UnityEngine;

// Valgfri tuning for cheat-meny — del mellom scenene via én .asset (PG2202-12)
[CreateAssetMenu(fileName = "CheatMenuSettings", menuName = "CartoonZombies/Cheat Menu Settings")]
public class CheatMenuSettings : ScriptableObject
{
    [Header("Movement")]
    [Tooltip("Fly-hastighet når noclip er på (WASD + Space / Ctrl).")]
    public float noclipSpeed = 12f;
}
