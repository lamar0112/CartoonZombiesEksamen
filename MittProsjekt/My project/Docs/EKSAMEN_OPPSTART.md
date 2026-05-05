# Oppstart etter opprydding — sjekkliste

Alle **PG2202**-notater (plan, logg, editor-meny) ligger i **`Docs/`** ved siden av denne filen — se **`INDEX.md`**.

Bruk denne etter at prosjektet er åpnet i Unity. Rekkefølge anbefalt.

## 1. Prosjekt åpner uten feil

- Vent til import/script compile er ferdig.
- **Window → General → Console**: ingen røde errors. (Shader/GrabPass advarsler: fiks materialer til URP senere.)

## 2. Build Settings

- **File → Build Settings**
- Aktiver og dra rekkefølge: **MainMenu** → **Level01_By** → **Level02_StrandSkog** → **GameOver** → **Win**
- Eventuelt: **CartoonZombies → Project → Strip legacy scenes from Build Settings**, deretter **CartoonZombies → Project → Add scenes to Build Settings**

## 3. Lyd (GameAudioSettings)

- Velg **Assets/ScriptableObjects/GameAudioSettings**
- Sjekk at **Menu**, **City** (by), **Beach** (strand) har riktige AudioClip-referanser
- **GameManager**-prefab (eller scene-objekt): **AudioManager** har `musicLibrary` satt til denne asseten

## 4. Bølger (zombier)

- **Assets/ScriptableObjects/Waves/** skal ha **WaveData_Zone2** (Level 1) og **WaveData_Zone3** (Level 2)
- Etter **Repair** er spawner koblet automatisk; i **by** er **loadNextSceneWhenAllWavesComplete** satt **av** (scenebytte via **ZoneTrigger**).

## 5. ★ Automatisk reparasjon (gjør dette nå)

- **CartoonZombies → Repair → 1 Repair BOTH level scenes (recommended)**  
  Legger inn / fikser: **GameManager**-prefab, **EventSystem**, én aktiv sol + én hovedkamera, **SpawnPoints**, **ZombieSpawner** + **ZoneManager** + **WaveData**, **ZoneTrigger**-komponenter, **Player** fra prefab, **HUD** (bygges på nytt), Pause + Cheat, lyd + crosshair, **NavMesh**-bake for hele scenen, og rydder **Hierarchy** (**GameplaySystems** / **EnvironmentArt**). **MeshCollider** på mesh under **by-/strand-kartrot** (f.eks. **CityMap** / **BeachMap** eller eldre **CityMap_Zone2** / **BeachMap_Zone3** — se `LevelMapRootResolver`) og **EnvironmentArt** som mangler det, pluss usynlig **\_SafetyGround** under verden som siste sikkerhetsnett. Kun hierarchy-rydding (uten full repair): **CartoonZombies → Organize → 1 Cleanup hierarchy (active scene)**. Valgfritt etterpå: **2 / 3 Sort environment art** for undermapper (Roads, Nature, …). NavMesh-holder: **NavMeshWorldBake**.  
  Oppretter **CityParkourManager** (by) og **BoatUnlockSystem** (strand) hvis de mangler.

- Mange gule **Missing (Script)** i Console (f.eks. FSP **Prop\_Pipe\_***) er tredjeparts-prefaber — kjør **CartoonZombies → Repair → 4 Remove missing scripts (active scene)** på en **backup-kopi** av scenen hvis du vil rydde (eller ignorer om spillet fungerer).

- **CartoonZombies → Repair → 3 Repair BOTH + sync Build Settings** gjør det samme som over **og** legger inn manglende scener i Build Settings (samme som tidligere «Fix alt i alle scener»).

## 6. Level01_By (by) — manuelt i Inspector etter repair

- **ZoneManager**: skal være **1** (repair setter det)
- **CityParkourManager**: koble **beachZoneTrigger** (GameObject med **ZoneTrigger** som skal til strand)
- **Myntene**: **CoinCollectable** med `parkourZoneId` 1 eller 2; teller må matche manager
- **ZoneTrigger** (ut til strand): f.eks. **requireBothParkourZones** = true; ikke krev båt her
- **NavMesh**: repair baker allerede; kjør **CartoonZombies → Scenes → Re-Bake NavMesh (active scene)** etter store kartendringer.

## 7. Level02_StrandSkog (strand/skog) — manuelt

- **ZoneManager**: `zoneNumber = 2`
- **BoatUnlockSystem**: drap-krav, båt-trigger, lås-ikon
- **IslandWinTrigger** på kisten → **Win**-scene (ikke **LoadNextZone** for seier)
- **Ikke** bruk **ZoneTrigger** som «neste nivå» her med mindre du vil til en tredje scene (spillet er to nivåer + Win)

## 8. Spilltest

- **MainMenu → Play** → Level01_By → fullfør betingelser → Level02_StrandSkog → båt → kiste → Win
- **Y**: cheat-meny (sensor/eksamen)

## 9. Ikke gjør (vanlig feil)

- **Ikke** kjør **CartoonZombies → Level Art → ⚠ Rebuild procedural CITY/BEACH map** på ferdige scener uten Git-backup (sletter kartrot + **Environment**; nye bygg får **CityMap** / **BeachMap**, eldre **CityMap_Zone2** / **BeachMap_Zone3** ryddes også ved rebuild).

## 10. Valgfritt: mindre prosjekt (ThirdParty)

Slett **kun** etter at du har commit/backup og har sjekket at ingen prefab i scenene peker dit:

- Demo-scener under pakker (f.eks. **TextMesh Pro / Examples & Extras**, **SimpleNaturePack/Scenes**, **castleDemo**)
- **ThirdParty/TutorialInfo** (Unity-intro, ikke spillet)

---

_Fjernet i opprydding: ubrukt `ParticleManager`, `WaveData_Zone1`, `FloorStone_Zone1.mat`, kastell-editor-scripts, castle-greybox-meny, gamle Zone-scener._
