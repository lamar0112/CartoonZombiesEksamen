# Oppstart — sjekkliste (PG2202)

Alle **PG2202**-notater ligger i **`Docs/`** — se **`INDEX.md`**.

Bruk denne etter at prosjektet er åpnet i Unity. Rekkefølge anbefalt.

## 1. Prosjekt åpner uten feil

- Vent til import/script compile er ferdig.
- **Window → General → Console**: ingen røde errors. (Shader/GrabPass advarsler: fiks materialer til URP senere.)

## 2. Build Settings

- **File → Build Settings**
- Aktiver og dra rekkefølge: **MainMenu** → **Level01_By** → **Level02_StrandSkog** → **GameOver** → **Win**
- Fjern gamle/demo-scener fra listen hvis de ikke skal leveres.

## 3. Lyd (GameAudioSettings)

- Velg **Assets/ScriptableObjects/GameAudioSettings**
- Sjekk at **Menu**, **City** (by), **Beach** (strand) har riktige AudioClip-referanser
- **GameManager**-prefab (eller scene-objekt): **AudioManager** har `musicLibrary` satt til denne asseten  
- Valgfritt tuning: **CartoonZombies → Settings** (Editor-vindu)

## 4. Bølger (zombier)

- **Assets/ScriptableObjects/Waves/** skal ha **WaveData_Zone2** (Level 1) og **WaveData_Zone3** (Level 2)
- I **by** skal **ZombieSpawner** ha **loadNextSceneWhenAllWavesComplete** = **av** (scenebytte via **ZoneTrigger** / oppdrag).

## 5. NavMesh og scener (manuelt)

- Åpne hver level-scene og **Window → AI → Navigation** (eller prosjektets NavMesh Surface / **NavMeshWorldBake** hvis dere bruker det).
- Marker walkable geometri som **Navigation Static** der det trengs; **ikke** bak vann om zombier skal unngå det.
- **Bake** NavMesh. Flytt **SpawnPoints** slik at zombier spawner **på** det blå NavMesh (unngår «Failed to create agent»).
- Sjekk at **GameplaySystems** / **EnvironmentArt** er fornuftig organisert (ingen krav om automatisk cleanup).

## 6. Level01_By (by) — Inspector

- **ZoneManager**: sone **1**
- **CityParkourManager**: koble **beachZoneTrigger** (GameObject med **ZoneTrigger** mot strand)
- **Myntene**: **CoinCollectable** med `parkourZoneId` 1 eller 2; teller må matche manager
- **ZoneTrigger** (ut til strand): f.eks. **requireBothParkourZones** = true; ikke krev båt her

## 7. Level02_StrandSkog (strand/skog) — Inspector

- **ZoneManager**: `zoneNumber = 2`
- **BoatUnlockSystem**: drap-krav, båt-trigger, lås-ikon; på båt: **Car Controller** → huk av **Aquatic Vehicle**
- **Island Win Trigger** på **Chest**-roten (eller eget trigger-objekt ved kiste) → kaller **Win** via `GameManager`
- **Ikke** bruk **ZoneTrigger** som «neste nivå» her med mindre du vil til en tredje scene

## 8. Spilltest

- **MainMenu → Play** → Level01_By → fullfør betingelser → Level02_StrandSkog → båt → kiste → **Win**
- **Y**: cheat-meny (anbefalt i oppgaven for sensor)

## 9. Ikke gjør (vanlig feil)

- Slett ikke store mapper under **ThirdParty** / **Assets** uten å sjekke at ingen scene eller prefab refererer til dem.
- Bygg ikke om hele kartet proceduralt uten backup hvis dere er fornøyde med nåværende level.

## 10. Mindre zip (valgfritt)

Slett **kun** etter backup og verifikasjon:

- Demo-scener under pakker (f.eks. **TextMesh Pro / Examples & Extras**, **SimpleNaturePack/Scenes**)
- **ThirdParty/TutorialInfo** (Unity-intro, ikke spillet)

---

_Tidligere «CartoonZombies → Repair / Fix»-menyer er fjernet fra prosjektet; bruk denne sjekklisten og standard Unity-menyer._
