using UnityEngine;

// CompassObjectiveMarker — valgfri «utgangsmål»-transform for kompass (PG2202-04 plassering i scene).
// Pensum: enkel referanse GetExitTarget() brukt av EnemyCompassHUD når bølger er ferdig.
// Ekstra: targetOverride lar samme prefab peke mot annet punkt uten å flytte selve marker-objektet.
public class CompassObjectiveMarker : MonoBehaviour
{
    [Tooltip("Hvis satt: pil peker hit. Hvis tom: brukes denne transformens posisjon.")]
    [SerializeField] private Transform targetOverride;

    public Transform GetExitTarget() => targetOverride != null ? targetOverride : transform;
}
