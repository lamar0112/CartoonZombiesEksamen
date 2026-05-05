using UnityEngine;

// ZoneManager — per level-scene: setter GameManager.sone, Playing, starter riktig BGM (PG2202-10 + PG2202-12 scene-flyt).
// Pensum: én koordinator på rot; ZombieSpawner egen logikk i Start.
// Ekstra: zoneNumber synker med GameManager og lagring — dokumenter i rapport hvordan by → strand låses opp.
public class ZoneManager : MonoBehaviour
{
    [SerializeField] private ZombieSpawner zombieSpawner;
    // 1 = Level01_By, 2 = Level02_StrandSkog
    [SerializeField] private int           zoneNumber = 1;

    private void Start()
    {
        // Synkroniserer GameManager med hvilken sone vi er i
        GameManager.Instance?.SetZone(zoneNumber);

        // Setter spilltilstand til Playing så skyting og annet gameplay fungerer
        GameManager.Instance?.SetState(GameState.Playing);

        // Spiller riktig musikk for sonen via AudioManager (PG2202-10)
        // zoneNumber 1 = by, 2 = strand/skog
        switch (zoneNumber)
        {
            case 1: AudioManager.Instance?.PlayCityMusic();  break;
            case 2: AudioManager.Instance?.PlayBeachMusic(); break;
        }

        // ZombieSpawner starter automatisk i sin Start() - ingen ekstra kall nødvendig
    }
}
