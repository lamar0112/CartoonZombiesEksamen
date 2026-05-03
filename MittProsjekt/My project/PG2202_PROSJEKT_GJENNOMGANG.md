# CartoonZombies — prosjektgjennomgang (teknisk)

**Formål:** Full oversikt over struktur, navn, risiko og hva som *må* verifiseres i Unity-editoren.  
**Begrensning:** Level-scener (`Level01_By`, `Level02_StrandSkog`) er ofte **binære** — plassering av objekter, Inspector-referanser og synlige feil sjekkes **kun i Editor** (Hierarchy, Console, Play mode).

---

## 1. Mappestruktur (Assets-roten)

| Mappe | Kommentar |
|--------|-----------|
| `Scripts/` | Ryddig: `Core`, `AI`, `Player`, `UI`, `Misc`, `Vehicle`, `Editor`. |
| `Scenes/` | Fem spillscener + meta (MainMenu, Level01, Level02, GameOver, Win). |
| `Prefabs/` | `GameManager`, `Player`, `FreeZombie` — lite og oversiktlig. |
| `ScriptableObjects/` | Waves, lyd, cheat-innstillinger. |
| `ThirdParty/` | Stor — forventet; vurder å **ikke** levere ubrukte demo-scener i eksamen-zip (kildehenvisning i rapport). |
| `Boats/` | Egne båt-assets **utenfor** `ThirdParty` — greit, men `ThirdPartyFolderSetup` flytter ikke disse automatisk; enten flytt til `ThirdParty/Boats` senere eller dokumenter i rapport. |
| `Castle 1 LITE/` | Stor pakke — hvis **ikke** brukt i ferdig spill: fjern fra prosjekt eller utelat fra innleverings-zip for å spare størrelse. |
| `_Recovery/` | Ofte Unity «recovery»-data — **slett** om innholdet ikke trengs; vurder `.gitignore` for `_Recovery/`. |

---

## 2. Navngiving og språk

- **Kodekommentarer:** blanding norsk/engelsk — ikke eksamenskrav, men rapport kan være konsekvent norsk eller engelsk.
- **UI-tekst:** blanding (f.eks. pause på norsk, **Win/GameOver**-tekster på engelsk i `WinScreen` / `GameOverScreen`) — vurder **ett språk** gjennom hele spillet før levering.
- **Råfiler:** `Boats/Models/Materials/No Name.mat` — dårlig navn; gi materialet et tydelig navn i Unity.
- **Hierarchy:** mål er `GameplaySystems` / `EnvironmentArt` (repair/sort) — sjekk at gamle norske rot-navn ikke henger igjen.

---

## 3. Funksjon og kode (lest fra repo)

### 3.1 Allerede adressert / kjent

- **Fall gjennom kart:** mitigert med mesh-kollidere på kart + `_SafetyGround` + spawn-justering — **verifiser i Play** etter hver stor sceneendring.
- **Missing Script (999+):** typisk **FSP** / tredjepart — ikke spill-KI; kan ryddes med Repair → Remove missing scripts (backup først).

### 3.2 Konfigurasjonsrisiko (Inspector)

- **`AudioManager`:** krever **to** `AudioSource` på samme GameObject + tildelt **`GameAudioSettings`** — tom referanse gir rød feil i Console.
- **`ZombieSpawner`:** må ha **`WaveData`** og prefab; flere spawners i samme scene gir advarsel om feil telling.
- **`GameManager`:** prefab må finnes; `DontDestroyOnLoad` krever rot-objekt (repair håndterer).
- **Manuell kobling (by/strand):** jf. `EKSAMEN_OPPSTART.md` — `CityParkourManager.beachZoneTrigger`, `BoatUnlockSystem`, `ZoneTrigger`, `IslandWinTrigger`.

### 3.3 Ingen TODO/FIXME i Scripts

- (Tomt søk i `Assets/Scripts`.) Likevel: **ferdig spill** krever fortsatt **designpass** (balanse, plassering, QA).

---

## 4. Dette MÅ sjekkes i Unity (per scene)

Gå gjennom **én scene av gangen** med denne listen:

### Alle nivå-scener

- [ ] **Console** uten røde errors ved Play.
- [ ] **Player** har tag `Player`, **CharacterController**, kamera følger.
- [ ] **EventSystem** + input fungerer (klikk på UI).
- [ ] **NavMesh** bakt der zombier skal gå; zombier har **NavMeshAgent** + **Animator**.
- [ ] **ZombieSpawner** har riktige **WaveData** og spawn-punkter over **gåbar** flate.

### Level01_By

- [ ] **ZoneTrigger** ut av by: riktige flags (`requireBothParkourZones` / zombier / båt etter design).
- [ ] **CityParkourManager** — referanser til mynter / soner / beach trigger.
- [ ] **GameplaySystems** vs **EnvironmentArt** — ingen viktige colliders «flytende» uten parent.

### Level02_StrandSkog

- [ ] **BoatUnlockSystem** — drap-krav, triggers, UI-ikon.
- [ ] **IslandWinTrigger** på kiste → **Win** (ikke feil scene).
- [ ] **ZoneManager** `zoneNumber = 2`.

### MainMenu / GameOver / Win

- [ ] Knapper: **Play**, **Kontroller**, **Avslutt** (MainMenu); **Retry** / **Hovedmeny** / **Avslutt** der det er satt opp.
- [ ] **KeybindPanel** tekst = faktiske taster (Y cheat, osv.).

### Build (Windows)

- [ ] **File → Build Profiles** — riktige scener, **ikke** demo-scener fra ThirdParty.
- [ ] Kjør **.exe**: start → spill → **Avslutt** uten Unity.

---

## 5. Leveranse og repo (ikke spill, men praktisk)

- **Git push** feilet tidligere (HTTP 500 / stor pakke) — vurder **SSH**, større `http.postBuffer`, eller lever **zip til WISEflow** uavhengig av Git.
- **Eksamen-zip:** ekskluder `Library`, `Temp`, `Logs`, `UserSettings`; vurder å ikke ta med ubrukte gigabyte-pakker (Castle, demo-scener).

---

## 6. Anbefalt rekkefølge videre (spillet «ikke ferdig»)

1. **Lås gameplay-loop:** by → strand → seier uten cheat (ett gjennomløp).
2. **Balancer:** bølger, skade, ammo, parkour-krav — etter følelse, ikke bare kode.
3. **Polish:** én språklinje i UI, lyder, lys, kjente Console-warnings.
4. **Rydd prosjekt:** fjern ubrukte store assets; fjern eller ignorer `_Recovery`.
5. **Før innlevering:** slett eller utelat `Scripts/Editor` i zip hvis dere vil (spillet påvirkes ikke); skriv **rapport** med kilder.

---

_Sist oppdatert: teknisk gjennomgang fra workspace (kode + mappestruktur). Scene-detaljer = manuell sjekk i Unity._
