using UnityEngine;

// WaveData — ScriptableObject for bølgeparametre (PG2202-08: data-driven design, gjenbruk mellom scener).
// Pensum: SerializeField-lignende public felt i SO; CreateAssetMenu for å lage nye assets fra Project-vinduet.
// Ekstra: zombiePrefabVariants for visuell variasjon — ikke obligatorisk i pensum, men viser forståelse for prefab-pooling.
[CreateAssetMenu(fileName = "WaveData", menuName = "CartoonZombies/WaveData")]
public class WaveData : ScriptableObject
{
    public int        zombieCount    = 5;     // antall zombier i bølgen
    public float      spawnInterval  = 1.5f;  // sekunder mellom hvert spawn
    public GameObject zombiePrefab;            // hoved-prefab (alltid med i pool hvis satt)

    [Tooltip("Ekstra prefabs — hver spawn velger tilfeldig blant zombiePrefab + disse (alle må ha ZombieAI/ZombieHealth/NavMeshAgent som hovedprefaben).")]
    public GameObject[] zombiePrefabVariants;
}
