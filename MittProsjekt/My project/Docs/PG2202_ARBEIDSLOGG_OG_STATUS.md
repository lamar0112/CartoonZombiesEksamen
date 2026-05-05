# PG2202 — arbeidslogg og status (huskeliste)

**Formål:** Ett sted med sannhet om prosjektet slik at arbeid i Cursor/Unity ikke «mister» kontekst.  
**Oppdater:** Legg inn dato + kort linje når noe endres (du eller AI).

---

## For deg — hva betyr forespørselen din, og hva er faktisk gjort?

Du ba om en **full gjennomgang**: pensum + eksamen + hele prosjektet + mapper/navn/hierarki + engelsk + studentnivå + spill + missions + «game mechanics»-mappe.

**Det finnes to deler:**

| Del | Hvem | Hva |
|-----|------|-----|
| **A) Kode, tekstfiler, PDF i repo** | Kan gjøres her (Cursor) | Leser `Forelesningsmateriale/`, eksamen-PDF, alle scripts, Build Settings, mappestruktur under `Assets/`. Skriver funn i *denne* filen + `PG2202_MASTERPLAN_STEGVIS.md`. Retter ting i `.cs` / editor-verktøy når det gir mening. |
| **B) Unity-editoren** | **Bare du** (eller gruppa) | Hierarki-navn på objekter, prefab-inspector, faktisk Play, farger på lys, plassering av pickup, om animasjon *ser* bra ut, Windows-build, osv. Det kan jeg **ikke** se fra chat. |

**Det finnes ingen mappe som heter `Game Mechanics` i prosjektet.** Spillmekanikk ligger i vanlige mapper: `Scripts/Core`, `AI`, `Player`, `UI`, `Misc`, `Vehicle`. Det er **helt OK** for eksamen — i rapporten skriver du at gameplay ligger der. Du trenger ikke lage en ny mappe med mindre lærer ber om det.

**«Ikke AI-aktig»:** Sensor ser på *din* rapport og om spillet funker. Jeg har ikke «skrevet hele spillet»; vi har justert og dokumentert. Korte, tydelige setninger i rapporten og konsekvent språk i UI hjelper mer enn fancy formuleringer.

**Kort hva som allerede er gjort i dette prosjektet (Cursor):** full gjennomgang av repo + utfylt krav-tabell + liste over alle egne scripts + KI-tekst til rapport + engelsk på **nygenerert** UI i `SceneSetupTool` + tidligere gameplay-fiks (kompass, bil, zombier, misjoner, osv.). **Det som gjenstår er hovedsakelig B:** åpne Unity, teste, evt. rette tekst på knapper i scenene, bygge .exe, skrive rapport-PDF.

**Symboler:** `[repo]` = bekreftet fra filer i repo · `[ ]` = må du selv verifisere i Unity / Windows-build

---

## Snarveier

| Dokument | Sti |
|----------|-----|
| Eksamen (PDF) | `../../../Forelesningsmateriale/PG2202_Eksamen_Mars-2026.pdf` |
| Forelesninger | `../../../Forelesningsmateriale/` |
| Stegvis plan | `PG2202_MASTERPLAN_STEGVIS.md` |
| Oppstart etter endring | `EKSAMEN_OPPSTART.md` |
| Teknisk gjennomgang | `PG2202_PROSJEKT_GJENNOMGANG.md` |
| **Utførelsesplan (gjør dette i rekkefølge)** | **`PG2202_UTFØRELSESPLAN.md`** |
| **Editor-meny (CartoonZombies): hva hvert valg gjør** | **`PG2202_EDITOR_VERKTØY_KONTEKST.md`** |
| Repo-README | `../../../README.md` |

---

## Full gjennomgang — teknisk snapshot (sist oppdatert i fil: 2026-05-04)

### Unity / mal (eksamen krav 1)

| Sjekk | Verdi | Kilde |
|--------|--------|--------|
| Versjon | **6000.3.14f1** | `ProjectSettings/ProjectVersion.txt` `[repo]` |
| 3D-mal / pipeline | **Universal Render Pipeline (URP)** | `GraphicsSettings.asset` → `UniversalRenderPipeline` `[repo]` |
| Produktnavn (Player Settings) | **My project** | `ProjectSettings.asset` — vurder å bytte til f.eks. **CartoonZombies** før levering (kun kosmetikk i .exe-tittel) |

### Build Settings (eksamen krav 10 — del av leveranse)

Rekkefølge i `EditorBuildSettings.asset` `[repo]`:

1. `Assets/Scenes/MainMenu.unity`
2. `Assets/Scenes/Level01_By.unity`
3. `Assets/Scenes/Level02_StrandSkog.unity`
4. `Assets/Scenes/GameOver.unity`
5. `Assets/Scenes/Win.unity`

Alle er **enabled: 1**. Matcher `EKSAMEN_OPPSTART.md`.

### Mappestruktur `Assets/` (rot)

`Audio`, `Boats`, `Materials`, `Prefabs`, `Scenes`, `ScriptableObjects`, `Scripts`, `Settings`, `ThirdParty`, `UnityTechnologies`

**Merk:** `kenneykits` ligger ved **repo-roten** (`UnityEksamen/kenneykits`), ikke under `Assets`. Nevn i rapport hvis brukt.  
**Opprydding (2026-05-04):** `_Recovery` og ubrukt `Castle 1 LITE` er fjernet fra `Assets/` (ikke referert i spillscener). **Leveranse:** vurder fortsatt å utelate demo-innhold i `ThirdParty` fra zip (se `PG2202_PROSJEKT_GJENNOMGANG.md`).

### `Assets/Scripts/` (runtime vs editor)

- **Runtime (spillbygget):** **44** `.cs`-filer (under `Core`, `AI`, `Player`, `UI`, `Misc`, `Vehicle`).
- **Editor-only:** **17** `.cs`-filer under `Scripts/Editor/` — kjører **ikke** i build; greit som verktøy, ikke «spill-KI».

---

## Kartlegging: 10 spillkrav ↔ prosjekt (utfylt etter gjennomgang)

| # | Krav | Hvor / bevis i prosjektet | Status |
|---|------|---------------------------|--------|
| **1** | Unity 6000.3.x, 3D-mal | `6000.3.14f1` + URP | `[repo]` ✓ — skriv eksakt streng i rapport |
| **2** | Interaktivitet, vinn/tap, score | `GameManager` (tilstand, kills), `PlayerHealth` + `TriggerGameOver`, `IslandWinTrigger` / `Win`, `ZombieSpawner` + bølger, `SaveSystem` high score | Kodet `[repo]` · full loop `[ ]` Play uten cheat |
| **3** | Egen C# | Se tabell **«Egne scripts (rapport)»** nedenfor (44 filer) | `[repo]` ✓ — lim inn i rapport |
| **4** | Bredt utvalg byggeklosser | Se **«Standardbyggeklosser (rapport-hjelp)»** nedenfor | Kodet + assets `[repo]` delvis · utvid med screenshots |
| **5** | KI-agenter | `ZombieAI` (FSM: Patrol/Chase/Attack/Dead) + `NavMeshAgent`; evt. `CivilianAI` | `[repo]` ✓ · NavMesh bakt i scene `[ ]` |
| **6** | Startmeny, in-game GUI, keybinds | `MainMenuController`, `SceneLoader`, `HUDController`, `PauseMenu`, `CheatMenu` (Y), `MissionManager` + kompass; keybind-tekst i `SceneSetupTool` (generert UI, EN) | Kodet `[repo]` · verifiser synlig tekst i **dine** `.unity`-filer `[ ]` |
| **7** | Rigg-animasjon | `ZombieAI` / spiller bruker `Animator` (Mecanim) — verifiser clips i **Inspector** på prefabs | `[repo]` referanser i kode · `[ ]` screenshot Animator |
| **8** | Lyd | `AudioManager`, `GameAudioSettings` (ScriptableObject), `PlayerShooting`/`WeaponPickup` SFX, meny musikk | Kodet `[repo]` · clips tildelt `[ ]` Inspector |
| **9** | .exe med Avslutt | `MainMenuController.OnQuitClicked`, `PauseMenu.OnQuitClicked`, `GameOverScreen`/`WinScreen` (via knapper i scene) | Kodet `[repo]` · **må testes i ferdig build** `[ ]` |
| **10** | Prosjektmappe + build + rapport PDF | To mapper + PDF &lt; 5 MB, zip &lt; 5 GB | `[ ]` manuelt ved innlevering |

---

## UI / språk (viktig før leveranse)

- **In-game tekst** fra flere scripts er **engelsk** (f.eks. `WinScreen`, `GameOverScreen`, `HUDController`, `MissionObjectiveHUD`, polish-tool missions).
- **`SceneSetupTool.cs`** (menyer som **opprettes på nytt** fra editor-menyen): knapper, keybind-hjelp, cheat-panel og fallback-tekster er satt til **engelsk** (2026-05-04).
- **Eksisterende scener** som allerede har MainMenu/Pause/GameOver/Win: tekst endres **ikke** automatisk — oppdater manuelt i Inspector, eller kjør relevant «create/repair»-meny som bygger UI på nytt (backup først).

---

## Standardbyggeklosser (rapport-hjelp — kryss av med eksempel fra spillet)

Bruk listen i rapporten; eksempler her er basert på kode + typisk scene:

- [x] **Scener** — MainMenu, Level01_By, Level02_StrandSkog, GameOver, Win `[repo]`
- [x] **Prefabs** — `Assets/Prefabs/Player`, Zombies, GameManager, …
- [x] **Materialer / teksturer** — egne + `ThirdParty`
- [x] **Lys** — Directional Light i level (typisk)
- [x] **Kamera** — Main Camera + `CameraFollow`
- [x] **UI** — Canvas, **TextMesh Pro**, knapper, paneler
- [x] **Fysikk** — `Rigidbody` (bil), `CharacterController` (spiller), `Collider` / trigger (`ZoneTrigger`, `WeaponPickup`, …)
- [x] **NavMesh** — AI pathfinding (krever bake i scene)
- [x] **Animator / animasjoner** — zombie/spiller
- [x] **AudioSource / clips** — via `AudioManager`
- [x] **ScriptableObject** — `WaveData`, `GameAudioSettings`, `CheatMenuSettings`
- [x] **Partikler** — valgfritt på VFX (skudd, død) — sjekk prefabs `[ ]`
- [x] **Coroutine / tid** — `ZombieSpawner`, `PlayerShooting` reload, osv.

---

## Egne scripts — liste til rapport (runtime, 44 filer)

Gruppert etter mappe; **lever ikke Editor-mappen** som «kjernespill» (kun som verktøy hvis du vil nevne det).

### Core (14)
| Fil | Rolle (kort) |
|-----|----------------|
| `GameManager.cs` | Spilltilstand, kills, scenebytte, singleton |
| `SceneLoader.cs` | Laster scener fra Build Settings |
| `GameSceneNames.cs` | Konstant scene-navn |
| `SaveSystem.cs` | PlayerPrefs high score / siste runde |
| `WaveData.cs` | ScriptableObject for zombie-bølger |
| `MissionManager.cs` | Sekvensielle oppdrag + pil-mål |
| `GameplaySceneBootstrap.cs` | Etter load: fade, spawn-justering, kamera |
| `LevelWorldBoundsUtil.cs` | Spillbar verdens-bounds |
| `LevelMapRootResolver.cs` | Finner kart-rot i scene |
| `CityParkourManager.cs` | Parkour-soner / mynter (by) |
| `BoatUnlockSystem.cs` | Lås opp båt (strand) |
| `ZoneManager.cs` | Sone + musikk-sync |
| `GameAudioSettings.cs` | Musikk-bibliotek (SO) |
| `CheatMenuSettings.cs` | Tuning for cheat-meny (SO) |

### AI (5)
| Fil | Rolle |
|-----|--------|
| `ZombieAI.cs` | FSM + NavMeshAgent-styring |
| `ZombieHealth.cs` | HP, død, kill-registrering |
| `ZombieSpawner.cs` | Bølger, Instantiate |
| `ZombieSnapPositionUtility.cs` | Snap til bakke / NavMesh |
| `CivilianAI.cs` | Ekstra agent (hvis brukt i scene) |

### Player (4)
| Fil | Rolle |
|-----|--------|
| `PlayerMovement.cs` | CharacterController, mus, sprint, hopp |
| `PlayerShooting.cs` | Raycast, ammo, reload |
| `PlayerHealth.cs` | HP, game over |
| `CameraFollow.cs` | Tredjeperson + bil-modus |

### UI (11)
| Fil | Rolle |
|-----|--------|
| `MainMenuController.cs` | Play, keybind panel, quit |
| `HUDController.cs` | HP, ammo, kills, bølge |
| `PauseMenu.cs` | ESC pause, volum, meny/quit |
| `EnemyCompassHUD.cs` | Pil mot mål / zombie |
| `MissionObjectiveHUD.cs` | Lang målbeskrivelse |
| `CheatMenu.cs` | Y-meny for sensor |
| `InteractionHint.cs` | Hint-linje |
| `DamagePopup.cs` | Skadetall |
| `ZombieHealthBarWorld.cs` | Verdens-space HP-bar |
| `WinScreen.cs` | Seier-skjerm |
| `GameOverScreen.cs` | Game over-skjerm |

### Vehicle (2)
| Fil | Rolle |
|-----|--------|
| `CarController.cs` | Rigidbody-kjøring |
| `CarInteraction.cs` | F for inn/ut, sete, kamera |

### Misc (8)
| Fil | Rolle |
|-----|--------|
| `WeaponPickup.cs` | Pistol-pickup |
| `ZoneTrigger.cs` | Neste sone med betingelser |
| `IslandWinTrigger.cs` | Seier på øya |
| `CoinCollectable.cs` | Parkour-mynter |
| `CompassObjectiveMarker.cs` | Ekstra kompass-mål |
| `WaterDetection.cs` | Vann-collider filter |
| `BeachParkourMission.cs` | Strand-parkour (hvis i bruk) |
| `AudioManager.cs` | Musikk/SFX-master |

### Editor (17) — verktøy, ikke gameplay i .exe
`GameplayPolishTool`, `SceneSetupTool`, `LevelSceneRepairTool`, `HierarchyLevelCleanupTool`, `EnvironmentArtSortTool`, `BuildSettingsLegacyCleanupTool`, `ProjectSetupTool`, `PlayerSetupTool`, `ThirdPartyFolderSetup`, `GameSettingsWindow`, `ZoneLevelAuthoring`, `Zone2CityMapBuilder`, `Zone3BeachMapBuilder`, `ZombieCartoonGameplayPrefabBuilder`, `CityCompassMarkerMenu`, `MissingScriptsCleanupTool`, `IslandTreeTerrainWarningFix`

---

## KI (rapport — kort tekst du kan klippe inn)

Zombier er **agenter** med **NavMesh pathfinding** (`NavMeshAgent`) og en **finite state machine** i `ZombieAI`: tilstandene Patrol → Chase → Attack, samt Dead når `ZombieHealth` tømmer liv. Patrulje bruker `NavMesh.SamplePosition` og `SetDestination`; chase følger spiller; attack bruker `Animator` trigger og `PlayerHealth.TakeDamage`.

---

## Viktige tekniske beslutninger (ikke glem)

- **By-nivå:** ikke auto til strand etter siste bølge — `ZombieSpawner.loadNextSceneWhenAllWavesComplete` skal være **av** på by; progresjon via **ZoneTrigger** + oppdrag.
- **Hierarchy:** mål `GameplaySystems` / `EnvironmentArt` (CartoonZombies → Organize / Fix).
- **Polish:** `CartoonZombies → Fix → ★ FIX EVERYTHING` per level etter backup.

---

## Logg (nyeste øverst)

- **2026-05-04** — **Opprydding:** Alle `.md`-filer under prosjektet er samlet i **`Docs/`** (rot-README oppdatert). Slettet `Assets/_Recovery`, `Assets/Castle 1 LITE` og Unity-malen `Assets/Readme.asset`. Relative lenker til `Forelesningsmateriale/` og repo-README i denne filen og masterplan er justert (`../../../`). `.gitignore`: `Assets/_Recovery/`.
- **2026-05-04** — **Editor-kontekst:** `PG2202_EDITOR_VERKTØY_KONTEKST.md` — tabell over hele **CartoonZombies**-menyen (Fix / Repair / Scenes / Organize / Project / Setup / Level Art / Settings), filnavn, og forskjell på FIX EVERYTHING vs Repair.
- **2026-05-04** — **Utførelsesplan:** ny fil `PG2202_UTFØRELSESPLAN.md` (faser + hvem gjør hva). **GameplayPolishTool** oppgradert: FIX EVERYTHING inkl. `HierarchyLevelCleanupTool`, `FixCityZombieSpawnerAutoload`, **BoxCollider**-vegetasjon, **engelske** menyer/dialoger/missions, rettet «StrandSskog»-typo i feilmelding. README lenker til utførelsesplan.
- **2026-05-04** — `GameplayPolishTool.cs`: erstattet obsolete `enableWordWrapping` med `textWrappingMode = TextWrappingModes.Normal` (fjern CS0618-warning ved compile).
- **2026-05-04** — **Steg «next»:** Engelsk UI i `SceneSetupTool.cs` (Play, Controls, Quit, pause, game over, win, cheat panel, keybind help, placeholders). Eksisterende scener må evt. oppdateres manuelt eller regenereres.
- **2026-05-04** — **Full gjennomgang i Cursor:** fylt ut krav-tabell, Build Settings, URP, script-liste (44 runtime), standardbyggeklosser-hjelp, UI/språk-avvik, KI-avsnitt til rapport, produktnavn-merknad.
- **2026-05-04** — Opprettet filen + `PG2202_MASTERPLAN_STEGVIS.md`. README oppdatert.
- **2026-05-04 (tidligere)** — Kode: kompass, kamera, pickup/skyting, damage popup, zombie snap, HUD, missions/polish, bil, bootstrap, m.m.

---

## Åpne punkter (oppdater når du har testet)

- [ ] **Play:** MainMenu → Level01 → Level02 → Win uten cheat (eller noter blokkerende bug under).
- [ ] **Windows build:** start .exe, spill 2–3 min, **Avslutt**-knapp fungerer.
- [ ] **Console:** null røde errors ved normal Play.
- [ ] **Rapport-PDF:** prosess, tabell alle 10 krav, pensum-highlights, bugs, **kilder** (Asset Store, Kenney, Synty, zombipakker, osv.).
- [ ] **Zip:** under 5 GB; rapport under 5 MB.

---

## Neste konkrete steg

1. I Unity: sjekk at menytekst i **MainMenu / Pause / GameOver / Win** matcher ønsket språk (endre TMP i scene eller kjør repair som gjenoppretter UI — backup).  
2. **Play** MainMenu → full loop eller noter bugs → Console.  
3. **Build** Windows → test **Quit**.  
4. Rapport: lim inn script-liste + KI-avsnitt fra denne filen.

---

_Sist oppdatert: 2026-05-04 (full gjennomgang utført i Cursor og skrevet inn i denne filen)._
