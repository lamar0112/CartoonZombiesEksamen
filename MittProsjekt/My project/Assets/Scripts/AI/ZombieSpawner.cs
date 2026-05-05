using System.Collections; // trengs for IEnumerator / Coroutine
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// ZombieSpawner leser WaveData og spawner zombier med jevne mellomrom
// Bruker Coroutine i stedet for Update() fordi vi vil vente mellom hvert spawn (PG2202-08)
public class ZombieSpawner : MonoBehaviour
{
    [Header("Wave setup")]
    [SerializeField] private WaveData[] waves; // assign WaveData assets here

    [Header("Spawn points")]
    // Empty = spawn at this object's position. Otherwise assign spawn points in the Inspector
    [SerializeField] private Transform[] spawnPoints;

    [Header("Progression")]
    [Tooltip("When true, loads next zone after the last wave is cleared. Keep false on the city level — use ZoneTrigger + missions instead.")]
    [SerializeField] private bool loadNextSceneWhenAllWavesComplete = false;

    [Tooltip("Når siste WaveData er ferdig, start bølgene på nytt (horde) — bra sammen med oppdrag «drep X zombier».")]
    [SerializeField] private bool repeatWavesForever = true;

    [Header("Scale")]
    [Tooltip("Uniform skala på instansierte zombier (1 = prefab-størrelse). Øk litt hvis de ser for små ut mot spiller.")]
    [SerializeField] private float spawnedZombieUniformScale = 1.22f;

    [Header("Spawn spread")]
    [Tooltip("Tilfeldig offset rundt hvert spawn-punkt — mer spredning på kartet.")]
    [SerializeField] private float randomSpawnScatterRadius = 22f;

    [Tooltip("Sjanse (0–1) for å prøve et tilfeldig NavMesh-punkt langt fra spawn — mer zombier «rundt om» på kartet.")]
    [SerializeField] [Range(0f, 1f)] private float wideMapScatterChance = 0.42f;

    [Tooltip("Radius rundt et tilfeldig valgt spawn-punkt når wide scatter brukes.")]
    [SerializeField] private float wideMapScatterSampleRadius = 88f;

    private int  currentWave  = 0;
    private int  zombiesAlive = 0;
    private bool allWavesDone = false;

    private void Start()
    {
        if (waves == null || waves.Length == 0)
        {
            Debug.LogWarning("ZombieSpawner: No WaveData assigned. Check the Inspector.");
            return;
        }

        // Zombier som allerede står i scenen (Instantiate kjører ikke Start før senere på framen — tell dem her)
        foreach (ZombieHealth z in FindObjectsByType<ZombieHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            zombiesAlive++;

        ZombieSpawner[] all = FindObjectsByType<ZombieSpawner>(FindObjectsSortMode.None);
        if (all.Length > 1)
            Debug.LogWarning($"ZombieSpawner: {all.Length} spawners in scene — use only one for correct counting.");

        StartCoroutine(StartWave(currentWave));
    }

    // Coroutine: kjøres asynkront uten å fryse resten av spillet (PG2202-08)
    private IEnumerator StartWave(int waveIndex)
    {
        WaveData wave = waves[waveIndex];

        if (wave == null)
        {
            Debug.LogError($"ZombieSpawner: WaveData for wave {waveIndex + 1} is null!");
            yield break;
        }

        // Spawner zombier én etter én med pause mellom
        for (int i = 0; i < wave.zombieCount; i++)
        {
            SpawnZombie(PickPrefabForWave(wave));

            // WaitForSeconds er standard måte å lage pause i Coroutine (PG2202-08)
            yield return new WaitForSeconds(Mathf.Max(0.1f, wave.spawnInterval));
        }

        // Venter til alle zombier er drept før neste bølge starter
        yield return new WaitUntil(() => zombiesAlive <= 0);

        OnWaveComplete();
    }

    private static GameObject PickPrefabForWave(WaveData wave)
    {
        if (wave == null) return null;

        var options = new List<GameObject>();
        if (wave.zombiePrefab != null) options.Add(wave.zombiePrefab);
        if (wave.zombiePrefabVariants != null)
        {
            foreach (GameObject p in wave.zombiePrefabVariants)
            {
                if (p != null && !options.Contains(p))
                    options.Add(p);
            }
        }

        if (options.Count == 0) return null;
        return options[Random.Range(0, options.Count)];
    }

    // Velger et tilfeldig spawn-punkt og instansierer zombie-prefaben (PG2202-08)
    private void SpawnZombie(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("ZombieSpawner: zombiePrefab is null. Check WaveData in the Inspector.");
            return;
        }

        // Velger spawn-punkt: tilfeldig fra lista, eller bruker spawnerens posisjon
        Transform spawnPoint = (spawnPoints != null && spawnPoints.Length > 0)
            ? spawnPoints[Random.Range(0, spawnPoints.Length)]
            : transform;

        Vector3 spawnPos = GetScatteredSpawnPosition(spawnPoint);

        // Tell med én gang her — Start() på ny instans kjører først senere, ellers henger WaitUntil etter (PG2202-08)
        GameObject z = Instantiate(prefab, spawnPos, spawnPoint.rotation);
        if (spawnedZombieUniformScale > 0f && !Mathf.Approximately(spawnedZombieUniformScale, 1f))
        {
            Vector3 s = prefab.transform.localScale;
            z.transform.localScale = new Vector3(
                s.x * spawnedZombieUniformScale,
                s.y * spawnedZombieUniformScale,
                s.z * spawnedZombieUniformScale);
        }

        ZombieSnapPositionUtility.SnapAgentToGround(z);
        zombiesAlive++;
        // NavMeshAgent trenger ofte én–to frames etter Instantiate før Warp sitter riktig
        StartCoroutine(DeferredSnapZombie(z));
    }

    private IEnumerator DeferredSnapZombie(GameObject z)
    {
        yield return null;
        yield return null;
        if (z == null) yield break;
        ZombieSnapPositionUtility.SnapAgentToGround(z);
        if (z.TryGetComponent(out NavMeshAgent agent) && agent.enabled && agent.isOnNavMesh)
        {
            try { agent.ResetPath(); }
            catch { /* agent ikke klar */ }
        }
    }

    private Vector3 GetScatteredSpawnPosition(Transform spawnPoint)
    {
        Vector3 basePos = spawnPoint.position;

        // Bred spredning: tilfeldig punkt på NavMesh innenfor stor radius (krever bakt NavMesh)
        if (wideMapScatterChance > 0.001f && Random.value < wideMapScatterChance && wideMapScatterSampleRadius > 2f)
        {
            Vector3 hub = PickScatterHubPosition(spawnPoint);
            for (int attempt = 0; attempt < 14; attempt++)
            {
                Vector3 jitter = Random.insideUnitSphere * wideMapScatterSampleRadius;
                jitter.y = 0f;
                Vector3 candidate = hub + jitter;
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, wideMapScatterSampleRadius + 12f, NavMesh.AllAreas))
                    return hit.position;
            }
        }

        if (randomSpawnScatterRadius <= 0.05f)
            return basePos;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 jitter = Random.insideUnitSphere * randomSpawnScatterRadius;
            jitter.y = 0f;
            Vector3 candidate = basePos + jitter;
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, randomSpawnScatterRadius + 8f, NavMesh.AllAreas))
                return hit.position;
        }

        return basePos;
    }

    private Vector3 PickScatterHubPosition(Transform fallback)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
        return fallback.position;
    }

    // Kalles av ZombieHealth når en zombie dør
    public void OnZombieDied()
    {
        zombiesAlive = Mathf.Max(0, zombiesAlive - 1);
    }

    private void OnWaveComplete()
    {
        currentWave++;

        if (currentWave >= waves.Length)
        {
            if (repeatWavesForever && !loadNextSceneWhenAllWavesComplete)
            {
                currentWave = 0;
                allWavesDone = false;
                StartCoroutine(WaitAndStartNextWave());
                return;
            }

            allWavesDone = true;
            if (loadNextSceneWhenAllWavesComplete)
                GameManager.Instance?.LoadNextZone();
            return;
        }

        StartCoroutine(WaitAndStartNextWave());
    }

    private IEnumerator WaitAndStartNextWave()
    {
        // 3 sekunder hvile mellom bølger - gir spilleren tid til å puste
        yield return new WaitForSeconds(3f);
        StartCoroutine(StartWave(currentWave));
    }

    // Public getters for HUD (wave text, etc.)
    /// <summary>Visnings-bølge (unngår «Wave 2/1» når siste bølge nettopp ble fullført).</summary>
    public int CurrentWave => allWavesDone ? Mathf.Max(1, TotalWaves) : Mathf.Clamp(currentWave + 1, 1, Mathf.Max(1, TotalWaves));
    public int  TotalWaves    => waves != null ? waves.Length : 0;
    public bool AllWavesDone  => allWavesDone;
    public int  ZombiesAlive  => zombiesAlive;
}
