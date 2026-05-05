# AI handoff — Cartoon Zombies (PG2202 Unity-eksamen)

**Les denne filen først** hvis du er Cursor, Claude Code, eller annen agent som fortsetter arbeidet.

| | |
|---|---|
| **Unity-prosjektsti** | `MittProsjekt/My project/` |
| **Unity-versjon** | **6000.3.x** (sjekk `ProjectSettings/ProjectVersion.txt`) — eksamen krever 6000.3.x |
| **Git remote** | `origin` → CartoonZombiesEksamen (GitHub) |
| **Språk i kode** | Kommentarer: **norsk** + PG2202-referanser (`Pensum:` / `Ekstra:`). Prosjektnavn/menyer: ofte **engelsk**. |

---

## 1. Pensum og eksamen (kort)

- **Full oversikt:** `MittProsjekt/My project/.agent/PG2202_PENSUM_OG_EKSAMEN.md`
- **Trinn mot leveranse:** `MittProsjekt/My project/Docs/PG2202_TRINNPLAN.md`
- **Prosjektkart (mapper, menyer, mekanikker):** `MittProsjekt/My project/.agent/PROSJEKT_KARTLEGGING.md`
- **Øvrig gruppedok:** `MittProsjekt/My project/Docs/` (INDEX, arbeidslogg, editor-verktøy, osv.)

**Ti obligatoriske spillkrav** (rapport må mappe mot disse): Unity 6000.3.x + 3D-mal, interaktivitet + vinn/tap + målbar score, **egen C#**, bred bruk av Unity-byggeklosser, **KI** (NavMesh og/eller FSM), **startmeny + in-game GUI**, **rigg-animasjon**, **lyd**, **kjørbar .exe med tydelig avslutt**, leveranse prosjekt + build + **PDF-rapport**. Cheat-meny er eksplisitt anbefalt for sensor.

---

## 2. Hva som er implementert (siste runder — oppsummert)

### Gameplay / kode
- **GameBalance** (`Core/GameBalance.cs`): globale multiplikatorer (zombie skade → spiller, spiller skudd → zombie). Brukes av `ZombieAI` og `PlayerShooting`.
- **RuntimeHierarchyTuning** (`UI/RuntimeHierarchyTuning.cs`): valgfritt **F10**-panel i Play Mode for tuning (ikke eksamenskrav).
- **VehicleRespawnHelper** (`Vehicle/VehicleRespawnHelper.cs`): **B** (standard) respawn av bil/båt til «hjem»-pos når ingen sitter i kjøretøyet.
- **CarController**: ekstra nedkraft, **`aquaticVehicle`** med egne Inspector-tall (max speed, motor scale, min steer, nedkraft-scale). **Ikke** ekte bølge-fysikk — bevisst enkelt (pensum).
- **CarInteraction.ExitCar**: rett opp spillerrotasjon + nullstill enkle Animator-floats (mot «ligger igjen» etter båt/bil).
- **CameraFollow**: horisontal mus når **noclip** er på (`CheatMenu.IsNoclipActive`).
- **IslandWinTrigger**: godtar `PlayerHealth` på **forelder** (ikke bare `CompareTag("Player")` på collider).
- **ZombieSpawner**: etter NavMesh-sample, hopper over posisjon der **vann** treffes under (`WaterDetection.GroundUnderPointIsLikelyWater`). `ApplyRuntimeSpawnTuning` for F10-panelet.
- **WaterDetection**: `GroundUnderPointIsLikelyWater` (raycast ned).
- **MissionManager**: `ShouldCompassPreferNearestZombie`, `CheatMarkAllMissionsComplete`, m.m.
- **EnemyCompassHUD**: nærmeste zombie når oppdrag er kill-count uten `arrowTarget`; **arrowVisualScale**.
- **BoatUnlockSystem**: `CheatForceUnlockBoat`, `EnsureInteractableIfUnlocked`.
- **CheatMenu (Y)**: utvidet (give gun, unlock/fix boat, respawn vehicles, jump til bil-oppdrag i by, fullfør oppdrag, osv.).

### Editor
- **EnsureSceneHelpersTool**: `CartoonZombies → Organize → Add RuntimeHierarchyTuning to GameplaySystems (if missing)`.
- Øvrige menyer uendret i funksjon, men kommentarer/tekst oppdatert der det er gjort (se git-historikk).

### Dokumentasjon (spiller / gruppe)
- `Assets/Documentation/ProjectStructure.txt` — engelsk mappeoversikt + notat om kommentarstil.
- `Assets/Documentation/FREMGANGSMATE_Spawns_Admin_Navigasjon.txt` — norsk steg-for-steg: spawns, F10, mission-piler, NavMesh/layers, bil/båt, kiste.

### Viktig om Hierarchy
- **Mission Manager** er ofte en **komponent på `GameplaySystems`**, ikke et eget objektnavn. Inspector → `Mission Manager (Script)` → `Missions`, `Arrow Target`, `Compass Hud`.

---

## 3. Det som **ikke** er automatisert (må i Unity Editor)

- **NavMesh bake**, riktig **Navigation Static**, unngå walkable på **vann**.
- **Layer** på miljø (f.eks. ikke **UI** på trær/gress).
- **Colliders** på flater spilleren skal gå på (mange Kenney/Synty-biter mangler collider).
- **Mission arrow targets**: tomme GameObjects plassert i verden og dratt inn i `MissionManager` per oppdrag.
- **Build Settings**, **Windows build**, **Avslutt-knapp** i standalone.
- **Rapport PDF** mot alle 10 krav + kilder + kjente bugs.

Meny som hjelper: **`CartoonZombies → Fix → Bake NavMesh (active scene)`** (åpne level-scene først).

---

## 4. Kjente restpunkter / risiko

- **Console:** kan ha warnings/errors — sjekk etter merge (bruker hadde 3 errors / 7 warnings i skjermbilde).
- **Castle 1 LITE** (stor tredjepartsmappe) er **fjernet** fra working tree i en sync — var ikke del av spillflyten; hvis gruppa trengte den, må den hentes fra eldre commit.
- **Slettet fra prosjektrot:** `EKSAMEN_OPPSTART.md`, `PG2202_TRINNPLAN.md`, `PG2202_PROSJEKT_GJENNOMGANG.md` under `My project/` — **erstattet/avledet** av innhold i **`MittProsjekt/My project/Docs/`**.
- **`.agent/`** inneholder agent-notater (pensum, kartlegging); trygt å versjonere for team/AI.

---

## 5. Anbefalt rekkefølge for neste agent

1. Les `Docs/PG2202_TRINNPLAN.md` del 1–6.
2. Les `FREMGANGSMATE_Spawns_Admin_Navigasjon.txt` for manuelle scene-steg.
3. Åpne Unity → Console → fjern **røde** feil.
4. Test flyt: MainMenu → Level01 → Level02 → Win/GameOver; deretter **Build and Run**.
5. Oppdater `Docs/PG2202_ARBEIDSLOGG_OG_STATUS.md` når noe endres.

---

## 6. Kontakt / repo

- Endringer committet med melding som beskriver sync av prosjekt + dokumentasjon.
- Nye agenter: **ikke** slett `ThirdParty`-innhold uten eksplisitt beskjed; følg kildehenvisning i rapport.

*Sist oppdatert i forbindelse med git-push og AI-handoff (gruppe Cartoon Zombies / PG2202).*
