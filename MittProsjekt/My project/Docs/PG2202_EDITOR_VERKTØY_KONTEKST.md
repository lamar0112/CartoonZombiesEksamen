# CartoonZombies — editor-verktøy (kontekst / hva de gjør)

Alt under **Unity-menylinjen → CartoonZombies** kommer fra scripts i **`Assets/Scripts/Editor/`**. De kjører **bare i Unity Editor**, ikke i den ferdige `.exe`.

**Mål med verktøyene:** raskt fikse typiske eksamen-prosjekt-problemer (NavMesh, hierarchy, UI, Build Settings, zombier) uten å måtte huske alt manuelt. Mye av dette er laget/forbedret gjennom prosjektet ditt + Cursor-justeringer.

---

## Hurtigvalg — hva bruke når?

| Situasjon | Meny |
|-----------|------|
| Vil ha «mest mulig» på **én åpen** level-scene uten å ødelegge kart | **Fix → ★ FIX EVERYTHING (active scene)** |
| Stor oppstart / noe er ødelagt (mangler GameManager, HUD, NavMesh, …) | **Repair → 1 Repair BOTH level scenes** eller **3 Repair BOTH + sync Build Settings** |
| Bare rydde rot-objekter til **GameplaySystems** / **EnvironmentArt** | **Organize → 1 Cleanup hierarchy** (kjøres også inne i FIX EVERYTHING) |
| Undermapper under EnvironmentArt (Roads, Nature, …) | **Organize → 2 / 3 Sort environment art** |
| Gamle scener i Build Settings | **Project → Strip legacy scenes** → deretter **Add scenes to Build Settings** |

---

## Fix (`GameplayPolishTool.cs` + `IslandTreeTerrainWarningFix.cs`)

| Menyvalg | Hva det gjør |
|-----------|----------------|
| **★ FIX EVERYTHING (active scene)** | Kjører (i rekkefølge): **Organize hierarchy** → spawn snap → vegetasjon som **triggers** (MeshCollider + **BoxCollider**) → bil MeshCollider **convex** → **CarController** / **CarInteraction** / **DriverSeat** → **PlayerShooting** av ved start → **WeaponPickup** hvis mangler → **by:** skru av **auto scene advance** på `ZombieSpawner` → **MissionManager** + engelske missions + HUD mission panel → **HUD wiring** (TMP → HUDController) → **NavMesh bake**. Lagre scene når Unity spør. |
| Fix SpawnPoints | Snapper Player + barn under `SpawnPoints` til bakken (raycast). |
| Fix Vegetation Colliders | Gjør matchende busk/trær til **isTrigger** så CharacterController kan gå gjennom (navnebasert). |
| Fix Car MeshCollider (Convex) | Convex på MeshCollider under Rigidbody (unngår fysikk-feil). |
| Fix Car Full Setup | Sikrer CarController, Rigidbody, CarInteraction, DriverSeat + kabling. |
| Disable PlayerShooting at Start | Starter uten pistol til **WeaponPickup** plukkes. |
| Place WeaponPickup | Lager/plasserer pickup hvis scenen mangler den. |
| Fix HUD Wiring | Kobler TMP-felt på HUD Canvas til `HUDController` (health, ammo, …). |
| Bake NavMesh | `NavMeshSurface` på scenen, **Collect All**, Build. |
| Setup MissionManager | Setter missions-array, compass, mission panel (ofte overlapp med FIX EVERYTHING). |
| **Island palms – fjern Tree Creator** | Reduserer Soft Occlusion / Tree Creator-advarsler på palmer (Island-pakke). |

---

## Repair (`LevelSceneRepairTool.cs`, `MissingScriptsCleanupTool.cs`)

| Menyvalg | Hva det gjør |
|-----------|----------------|
| **1 Repair BOTH level scenes** | Åpner **Level01_By** og **Level02_StrandSkog** etter tur: GameManager-prefab, EventSystem, ett lys + én hovedkamera, SpawnPoints, ZombieSpawner, ZoneManager, WaveData, ZoneTrigger, Player, HUD, Pause, Cheat, lyd, crosshair, kamera, NavMesh, hierarchy-rotter. **Lagrer begge.** |
| **2 Repair ACTIVE level scene** | Samme logikk, kun scenen du har åpen (må være Level01 eller Level02). |
| **3 Repair BOTH + sync Build Settings** | Som 1 + legger inn manglende spillscener i Build Settings. |
| **4 Remove missing scripts (active scene)** | Fjerner «Missing (Mono Script)» på objekter i **aktiv** scene (backup først). |
| **5 Remove missing scripts (BOTH level scenes)** | Samme for begge level-filene. |

**Manuelt etter repair (står i dialog):** `CityParkourManager.beachZoneTrigger`, `BoatUnlockSystem`, plassering av `ZoneTrigger`, `IslandWinTrigger` på kiste.

---

## Scenes (`SceneSetupTool.cs` + noen entries i Zone2/Zone3 builders)

| Menyvalg | Hva det gjør |
|-----------|----------------|
| Setup MainMenu / Fix Main menu | Bygger eller fikser MainMenu (kamera, EventSystem, Canvas, knapper — **engelsk** i nygenerert UI). |
| Setup GameOver / Setup Win | Oppretter Game Over- og Win-scener med UI. |
| Add CheatMenu to active scene | Legger inn CheatCanvas (Y-meny). |
| Fix audio + crosshair | Kobler `AudioManager` / innstillinger + sikter i aktiv scene. |
| Re-Bake NavMesh | NavMesh på nytt. |
| Mark WATER NavMesh Not Walkable + Re-Bake | Vann-områder ikke gåbare for AI. |
| Fix ENVIRONMENTART mesh colliders | Veier/bygg får collider så man ikke faller gjennom. |
| Strip vegetation MeshColliders | Fjerner blokkerende trær-collidere (alternativ til «trigger»-tilnærming). |
| **Legacy — Full setup Level01/02** | Eldre flyt som legger **Floor** + systemer — bruk helst **Repair** på ferdige Kenney/Synty-kart. |
| Fix CITY / Fix BEACH mesh colliders | Kart-spesifikke collider-fiks fra `Zone2CityMapBuilder` / `Zone3BeachMapBuilder`. |
| Strip BEACH tree/bush MeshColliders | Strand: gjør skog mer gåbar. |

---

## Organize (`HierarchyLevelCleanupTool.cs`, `EnvironmentArtSortTool.cs`)

| Menyvalg | Hva det gjør |
|-----------|----------------|
| **1 Cleanup hierarchy (active scene)** | Lager **GameplaySystems** + **EnvironmentArt**, flytter rot-objekter inn (kamera, spiller, spawner, … vs miljø). Omdøper gamle norske rot-navn. |
| **2 Sort environment art (active scene)** | Undermapper under EnvironmentArt (Undo støttet). |
| **3 Sort environment art (both level scenes)** | Samme for Level01 + Level02. |

---

## Project (`ProjectSetupTool.cs`, `BuildSettingsLegacyCleanupTool.cs`, `ThirdPartyFolderSetup.cs`)

| Menyvalg | Hva det gjør |
|-----------|----------------|
| **Add scenes to Build Settings** | MainMenu → Level01_By → Level02_StrandSkog → GameOver → Win. |
| **Strip legacy scenes from Build Settings** | Fjerner gamle stier (Zone_*, gamle level-navn). |
| Move imported packages to ThirdParty | Flytter importerte pakker under `ThirdParty`. |
| Remove duplicate components on zombie prefabs | Rydder prefab-komponenter. |
| Add capsule collider to zombie prefabs | Enkel treffflate for skudd. |
| Reset saved MasterVolume | PlayerPrefs volum. |
| Set input handling to Both | Old + New Input (krever Unity restart). |

---

## Setup (`ProjectSetupTool.cs`, `PlayerSetupTool.cs`, `ZombieCartoonGameplayPrefabBuilder.cs`)

| Menyvalg | Hva det gjør |
|-----------|----------------|
| 01 Zombie prefab / 02 Player tag / 03 WaveData | Grunnoppsett av prefab, tag, bølge-assets. |
| 04 Run setup 01–03 | Kjører 01–03 i én operasjon. |
| 04 Zombie Cartoon → gameplay prefabs | Lager spillklare zombie-varianter (NavMesh, AI, helse, animator). |
| 05 Player prefab | Hjelper med player-prefab-oppsett. |

---

## Level Art (`ZoneLevelAuthoring.cs`, `Zone2CityMapBuilder.cs`, `Zone3BeachMapBuilder.cs`, `CityCompassMarkerMenu.cs`)

| Menyvalg | Hva det gjør |
|-----------|----------------|
| 1 Ensure Environment hierarchy | Miljø-struktur. |
| 2 Apply bright cartoon lighting | Lysstil. |
| **⚠ Rebuild procedural CITY / BEACH** | **DESTRUKTIVT** — sletter/regenererer kart; **backup først** (se `EKSAMEN_OPPSTART.md`). |
| Add DrivableCar only (safe) | Legger inn bil uten å slette resten av by-kartet. |
| Add Compass exit marker | Tom markør for `EnemyCompassHUD` / mission-pil. |

---

## Settings (`GameSettingsWindow.cs`)

| Menyvalg | Hva det gjør |
|-----------|----------------|
| **CartoonZombies → Settings** | Editor-vindu for prosjekt-/spillinnstillinger (samlet GUI). |

---

## Relasjon: FIX EVERYTHING vs Repair

- **Repair** = «stor pakke» som åpner og lagrer **begge** level-filer, sikrer hele grunnpakken (GameManager, UI, NavMesh, …).
- **FIX EVERYTHING** = kjør på **den scenen du har åpen**; fokus på polish (spawn, vegetasjon, bil, missions, HUD, NavMesh) + hierarchy cleanup. Bra etter du har et kart du liker.

Begge kan brukes; **ikke** kjør **destructive** Level Art rebuild på ferdige eksamen-scener uten Git/backup.

---

## Filer (referanse)

| Fil | Rolle |
|-----|--------|
| `GameplayPolishTool.cs` | Fix-menyen, FIX EVERYTHING |
| `LevelSceneRepairTool.cs` | Repair 1–3 |
| `SceneSetupTool.cs` | Scenes / MainMenu / colliders / NavMesh-hjelp |
| `HierarchyLevelCleanupTool.cs` | Organize 1 |
| `EnvironmentArtSortTool.cs` | Organize 2–3 |
| `ProjectSetupTool.cs` | Setup + Project-deler |
| `BuildSettingsLegacyCleanupTool.cs` | Strip legacy Build Settings |
| `ThirdPartyFolderSetup.cs` | Flytt til ThirdParty |
| `MissingScriptsCleanupTool.cs` | Repair 4–5 |
| `Zone2CityMapBuilder.cs` / `Zone3BeachMapBuilder.cs` | Procedural kart + scene-fiks |
| `ZoneLevelAuthoring.cs` | Level Art 1–2 |
| `CityCompassMarkerMenu.cs` | Compass-markør |
| `ZombieCartoonGameplayPrefabBuilder.cs` | Zombie prefabs |
| `PlayerSetupTool.cs` | Player prefab |
| `GameSettingsWindow.cs` | Settings-vindu |
| `IslandTreeTerrainWarningFix.cs` | Palm / Tree Creator warning |

---

_Sist oppdatert: oversikt fra repo (MenuItem-scan) — 2026-05-04._
