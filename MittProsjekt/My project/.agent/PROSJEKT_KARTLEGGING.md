# Cartoon Zombies — prosjektkartlegging (for agent / videre arbeid)

**Generert:** ved gjennomgang av repo. **Unity:** `6000.3.14f1` (Unity 6). **Hovedsti:** `MittProsjekt/My project/`.

---

## 1. Repo- og spillstruktur

| Område | Innhold |
|--------|---------|
| **Spillkode** | `Assets/Scripts/` — **62** egne `.cs`-filer (runtime + editor) |
| **Scener (spill)** | `Assets/Scenes/MainMenu`, `Level01_By`, `Level02_StrandSkog`, `GameOver`, `Win` |
| **Build Settings** | Kun de fem scenene over (aktive) |
| **Prefabs (spill)** | `Assets/Prefabs/`: `Player/Player`, `Zombies/*`, `GameManager` |
| **Data** | `Assets/ScriptableObjects/Waves/`: `WaveData_Zone2`, `WaveData_Zone3` |
| **Tredjepart** | `Assets/ThirdParty/` (Synty, Supercyan zombie pack, ArtStore3D zombies, CFXR, Island, m.m.) — mange demo-scener |

**Scene-navn (konstanter):** `GameSceneNames.cs` — `MainMenu`, `Level01_By`, `Level02_StrandSkog`, `GameOver`, `Win`.

**Tags (`TagManager`):** `Player`, `Water`, `Zombie`.

---

## 2. Runtime-skript etter mappe

### `Assets/Scripts/Core/`
- `GameManager`, `SceneLoader`, `GameSceneNames`, `GameplaySceneBootstrap`
- `MissionManager` — oppdrag / triggers (inkl. `AllZombiesDead` vs spawner)
- `ZoneManager`, `WaveData` (ScriptableObject for bølger)
- `CityParkourManager` — parkour-mynter sone 1/2 (`CoinCollectable` rapporterer hit)
- `SaveSystem`, `GameAudioSettings`, `BoatUnlockSystem`, `CheatMenuSettings`
- `LevelWorldBoundsUtil`, `LevelMapRootResolver`

### `Assets/Scripts/AI/`
- `ZombieAI`, `ZombieHealth`, `ZombieSpawner`, `ZombieSnapPositionUtility`
- `CivilianAI`

### `Assets/Scripts/Player/`
- `PlayerMovement`, `PlayerShooting`, `PlayerHealth`, `CameraFollow`

### `Assets/Scripts/Vehicle/`
- `CarController` (Rigidbody, `SetZombieCollisionsIgnored`)
- `CarInteraction`

### `Assets/Scripts/UI/`
- `HUDController`, `PauseMenu`, `MainMenuController`, `CheatMenu`, `MissionObjectiveHUD`
- `EnemyCompassHUD`, `ParkourCoinsDisplay`, `InteractionHint`
- `DamagePopup`, `ZombieHealthBarWorld`, `WinScreen`, `GameOverScreen`

### `Assets/Scripts/Misc/`
- `WeaponPickup`, `CoinCollectable`, `ZoneTrigger`, `IslandWinTrigger`
- `WaterDetection`, `CompassObjectiveMarker`, `AudioManager`, `BeachParkourMission`

---

## 3. Viktige mekanikker (kort)

- **Zombier:** `ZombieHealth` setter `tag = Zombie` i `Awake`; spawner setter tag etter `Instantiate`. Gameplay-prefabs under `Assets/Prefabs/Zombies/`.
- **Skyting:** `PlayerShooting` raycast fra kamera, treffer `ZombieHealth` via `GetComponentInParent`.
- **Bil:** `CarController` + `CarInteraction`; kollisjon med zombier kan ignoreres via `Physics.IgnoreCollision` mot alle `ZombieHealth`-hierarkier.
- **Bølger:** `ZombieSpawner` bruker `WaveData`, holder `minimumZombiesAlive`, `OnZombieDied` fra `ZombieHealth`.
- **Parkour-mynter:** kun meningsfullt i `Level01_By` i `FixParkourCoins`-verktøyet; `CoinCollectable` håndterer konkav `MeshCollider` (deaktiverer / legger til `SphereCollider`).
- **Fysikk / vegetasjon:** Editor-verktøy setter ikke `isTrigger` på **konkav** mesh — deaktiverer eller bruker convex+trigger der det er lov.

---

## 4. Editor-menyer (`CartoonZombies/…`)

| Meny | Fil (typisk) |
|------|----------------|
| **Fix/** ★ FIX EVERYTHING, spawn, vegetasjon, bil, mynter, spiller-skala, HUD-layout, weapon pickup, HUD wiring, NavMesh bake, MissionManager, merge env, fjern duplikat-kameraer, Unfix triggers | `GameplayPolishTool.cs` |
| **Repair/** level scenes, missing scripts | `LevelSceneRepairTool.cs`, `MissingScriptsCleanupTool.cs` |
| **Scenes/** legacy setup, lyd, NavMesh, vann, mesh colliders, strip trær, MainMenu/GameOver/Win, CITY/BEACH fixes | `SceneSetupTool.cs`, `Zone2CityMapBuilder.cs`, `Zone3BeachMapBuilder.cs` |
| **Setup/** zombie prefab, tag, WaveData, cartoon gameplay prefabs, player prefab | `ProjectSetupTool.cs`, `ZombieCartoonGameplayPrefabBuilder.cs`, `PlayerSetupTool.cs` |
| **Organize/** hierarchy cleanup, sort environment art | `HierarchyLevelCleanupTool.cs`, `EnvironmentArtSortTool.cs` |
| **Level Art/** procedural rebuild CITY/BEACH (destructive), DrivableCar, compass marker, lighting | `Zone2CityMapBuilder.cs`, `Zone3BeachMapBuilder.cs`, `CityCompassMarkerMenu.cs`, `ZoneLevelAuthoring.cs` |
| **Project/** Build Settings, ThirdParty move, duplikater, PlayerPrefs, input | `ProjectSetupTool.cs`, `BuildSettingsLegacyCleanupTool.cs`, … |
| **Settings** | `GameSettingsWindow.cs` |

**Hierarki-konvensjon (cleanup):** rot `GameplaySystems`, `EnvironmentArt` (se `HierarchyLevelCleanupTool` for legacy-navn).

---

## 5. Pakker (utdrag `Packages/manifest.json`)

- **URP** (`com.unity.render-pipelines.universal` 17.3.x)
- **AI Navigation** 2.x, **Input System** 1.19, **Cinemachine** 2.10
- **TextMeshPro**, **Visual Scripting**, Post Processing, Recorder, Timeline, test-framework

---

## 6. Kjente fallgruver (fra tidligere feilsøking)

- **`CompareTag("Zombie")` uten tag:** løst med `Zombie` i `TagManager` + tagging i kode/prefabs.
- **Trigger på konkav `MeshCollider`:** ikke støttet — mynt-/vegetasjon-verktøy må ikke sette `isTrigger` på konkav mesh (bruk disable convex mesh eller `SphereCollider`).
- **FIX EVERYTHING / vegetasjon:** «grass» m.m. i `neverTrigger` for å ikke ødelegge gulv/gress-tiles; `Unfix Mistake Triggers` gjenoppretter walkable props.

---

## 7. Tellinger

- **~204** `.cs` totalt under `My project/Assets` (inkl. ThirdParty)
- **~62** egne spill/editor-skript under `Assets/Scripts/`

---

*Denne filen er ment som arbeidsminne for senere steg i samme prosjekt. Oppdater ved større arkitektur-/scene-endringer.*
