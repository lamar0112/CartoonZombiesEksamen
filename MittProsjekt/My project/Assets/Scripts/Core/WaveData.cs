using UnityEngine;

// ScriptableObject lar oss lagre spilldata som egne asset-filer i prosjektet (PG2202-08)
// Én bølge per asset — Level01_By bruker typisk WaveData_Zone2, Level02_StrandSkog bruker WaveData_Zone3
// Lag nye via: Assets → Create → CartoonZombies → WaveData
[CreateAssetMenu(fileName = "WaveData", menuName = "CartoonZombies/WaveData")]
public class WaveData : ScriptableObject
{
    public int        zombieCount    = 5;     // antall zombier i bølgen
    public float      spawnInterval  = 1.5f;  // sekunder mellom hvert spawn
    public GameObject zombiePrefab;            // hoved-prefab (alltid med i pool hvis satt)

    [Tooltip("Ekstra prefabs — hver spawn velger tilfeldig blant zombiePrefab + disse (alle må ha ZombieAI/ZombieHealth/NavMeshAgent som hovedprefaben).")]
    public GameObject[] zombiePrefabVariants;
}
