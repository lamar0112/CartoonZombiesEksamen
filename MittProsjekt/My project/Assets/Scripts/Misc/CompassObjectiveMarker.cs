using UnityEngine;

// Dra inn i scenen og plasser der spilleren skal gå når alle bølger er ferdig (f.eks. foran utgang).
// EnemyCompassHUD bruker dette før den faller tilbake til ZoneTrigger.
public class CompassObjectiveMarker : MonoBehaviour
{
    [Tooltip("Hvis satt: pil peker hit. Hvis tom: brukes denne transformens posisjon.")]
    [SerializeField] private Transform targetOverride;

    public Transform GetExitTarget() => targetOverride != null ? targetOverride : transform;
}
