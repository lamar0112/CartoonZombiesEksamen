# PG2202 — masterplan (steg for steg)

**Eksamen:** `../../../Forelesningsmateriale/PG2202_Eksamen_Mars-2026.pdf`  
**Forelesninger / bok:** `../../../Forelesningsmateriale/` (PG2202-01 … PG2202-12, bok-PDF)  
**Prosjekt:** Unity-mappe `My project/` + dokumentasjon i **`Docs/`** (`EKSAMEN_OPPSTART.md`, `PG2202_PROSJEKT_GJENNOMGANG.md`, …)  
**Pågående status / logg:** `PG2202_ARBEIDSLOGG_OG_STATUS.md` (oppdater etter hvert steg)  
**Konkret «gjør dette nå»-liste:** `PG2202_UTFØRELSESPLAN.md` ← **start her** hvis du vil ha én sjekkliste.

**Aktivt steg:** Fase 2–4 i utførelsesplanen (kjør **FIX EVERYTHING** på begge levels → Play → build).

Arbeid **ett steg av gangen**. Når ett steg er «grønt», gå til neste. Ikke hopp over **Play-test** og **build-test** der det står.

---

## Oversikt: 10 spillkrav ↔ deres prosjekt (CartoonZombies)

| # | Krav (kort) | Status | Hvor i prosjektet / notat |
|---|-------------|--------|---------------------------|
| 1 | Unity **6000.3.x**, mal **3D** (Built-in / URP / HDRP) | Sjekk | `ProjectSettings/ProjectVersion.txt` → dokumenter eksakt streng i rapport |
| 2 | Interaktivitet, gameplay, **vinne/tape**, **score e.l.** | Sjekk i Play | Bevegelse, skyting, bølger, parkour, bil, båt, portal; kills/high score |
| 3 | **Egen C#** | OK | `Assets/Scripts/` (unntatt `ThirdParty`) — list hovedfiler i rapport |
| 4 | **Bredt utvalg** Unity-byggeklosser | Rapport | Materialer, lys, kamera, prefabs, scener, UI, colliders, lyd, partikler … |
| 5 | **KI-agenter** (NavMesh og/eller FSM) | OK | `ZombieAI` + `NavMeshAgent`; evt. `CivilianAI` |
| 6 | **GUI:** startmeny, in-game UI, **keybind-meny** ved tvil | Delvis | MainMenu + HUD + Pause; verifiser «Kontroller»-tekst = faktiske taster |
| 7 | **Rigg-animasjon** | Sjekk | Spiller + zombie Animator; ta screenshot til rapport |
| 8 | **Lyd** (GUI / musikk / in-game) | Sjekk | `AudioManager`, `GameAudioSettings`, SFX på skudd osv. |
| 9 | **.exe** enkel **Avslutt** | Sjekk i **build** | Ikke bare i Editor — krav 9 gjelder kjørbar fil (forel. 12 / kap. 23) |
| 10 | **To leveranser:** prosjektmappe + build-mappe (+ rapport PDF) | Siste | Zip &lt; 5 GB; rapport PDF &lt; 5 MB |

**Rapport (obligatorisk innhold):** prosess, tabell/avsnitt for alle 10 spillkrav, pensum-highlights, utfordringer, bugs, **kildehenvisning** (Asset Store osv.) — se side 3 i eksamen PDF.

---

## Forelesningsmateriale — hva som typisk støtter hvert krav

| Tema | Filer (eksempler) |
|------|-------------------|
| Scripting, input | PG2202-02 |
| Terreng, lys, kamera | PG2202-03 |
| Kollisjon, modeller | PG2202-04 |
| FSM, agenter | PG2202-05 |
| Pathfinding | PG2202-07, PG2202-07b |
| Prefabs, UI, partikler | PG2202-08 |
| Animator | PG2202-09 |
| Animasjon, lyd | PG2202-10 |
| ML / eksamen Q&A | PG2202-11 |
| Polish, deploy, FAQ | PG2202-12 |

---

## Steg 0 — Forutsetninger (½–1 økt)

- [ ] Åpne prosjekt i Unity; vent ferdig compile; **Console uten røde errors** ved tom scene eller MainMenu.
- [ ] Les `EKSAMEN_OPPSTART.md` punkt 1–2 (Build Settings-rekkefølge).
- [ ] **Ikke** rebuild procedural city/beach uten backup (står i oppstartsguiden).

**Ferdig når:** du kan trykke Play på MainMenu uten blocking errors.

---

## Steg 1 — Krav 1 + 2 + 3 (versjon, gameplay, egen kode)

- [ ] Noter **eksakt** Unity-versjon til rapport (krav 1).
- [ ] **Ett fullt gjennomløp** uten cheat: MainMenu → Level01 → (betingelser) → Level02 → seier — eller noter hvor det stopper.
- [ ] Liste til rapport: **5–10 egne scripts** som viser *din* logikk (GameManager, ZombieAI, MissionManager, …).

**Ferdig når:** tabellrad for krav 1–3 kan fylles ut; kjente blokker er logget som bugs.

---

## Steg 2 — Krav 4 (standardbyggeklosser)

- [ ] Gå gjennom `PG2202_PROSJEKT_GJENNOMGANG.md` §1 (mapper).
- [ ] I rapport: **punktliste** (lys, kamera, Rigidbody, CharacterController, NavMesh, UI Canvas, TMP, AudioSource, Particle System, Material/Texture, Prefab, Scene, Trigger collider, …) med **eksempel fra spillet**.

**Ferdig når:** minst ~10 forskjellige byggekloss-typer nevnt med konkret bruk.

---

## Steg 3 — Krav 5 (KI)

- [ ] Verifiser: zombier bruker **NavMesh** + **FSM** (`ZombieAI` tilstander).
- [ ] Rapport: kort tegning eller tekst «tilstander → overganger».

**Ferdig når:** sensor kan se KI uten å lese hele koden.

---

## Steg 4 — Krav 6 (GUI + keybind)

- [ ] **Ett språk** i spillet (anbefalt: engelsk overalt, eller norsk overalt).
- [ ] MainMenu: Play, kontroller/keybinds, avslutt — fungerer.
- [ ] In-game: HUD (liv, ammo, kills, bølge) + eventuelt mission-panel.
- [ ] Oppdater keybind-panelet så det stemmer med **Y** (cheat), **ESC**, **F**, **WASD**, osv.

**Ferdig når:** ingen «mystery controls» uten at det står i menyen.

---

## Steg 5 — Krav 7 + 8 (animasjon + lyd)

- [ ] Spiller og/eller zombie: **Animator** med locomotion + (valgfritt) attack/death.
- [ ] `GameAudioSettings`: meny-, level- og SFX-klipp tildelt; test at musikk bytter med scene der det er ment.
- [ ] Kort avsnitt i rapport + screenshot av Animator.

**Ferdig når:** lyd og animasjon er synlige i første 2 min Play.

---

## Steg 6 — Krav 9 (Avslutt i .exe)

- [ ] Bygg **Windows Player** (Development Build valgfritt først).
- [ ] Start **.exe**; finn **Avslutt** (hovedmeny og/eller pause) — fungerer uten Unity.
- [ ] Hvis mangler: koble knapp til `Application.Quit()` / eksisterende `PauseMenu.OnQuitClicked`.

**Ferdig når:** krav 9 er verifisert i build, ikke bare Editor.

---

## Steg 7 — Krav 10 (leveranse)

- [ ] Mappe A: hele Unity-prosjektet (uten `Library`/`Temp` i zip hvis lærer/studentportal tillater — følg WISEflow).
- [ ] Mappe B: build + `…_Data` + evt. Mono/UnityPlayer — som forelesning 12.
- [ ] Zip samlet **&lt; 5 GB**; vurder å kutte ubrukte gigabyte-assets.
- [ ] Rapport PDF **&lt; 5 MB**.

**Ferdig når:** du har testet zip på en annen PC/mappe (åpne prosjekt + kjør exe).

---

## Steg 8 — Rapport PDF (alle punkter fra eksamen side 3)

- [ ] Prosess (anonym: «jeg» / «person 1» …)
- [ ] Tabell: alle **10 spillkrav** med henvisning til script/scene
- [ ] Pensum-elementer (terreng, kollisjon, partikler, …)
- [ ] Utfordringer + det dere er fornøyde med
- [ ] Kjente bugs
- [ ] **Kildehenvisning** (Asset Store, Kenney, Synty, … — ikke plagiat)

---

## Steg 9 — Siste polish (valgfritt men lønnsomt)

- [ ] `CartoonZombies → Fix → ★ FIX EVERYTHING` på begge level-scener (etter backup).
- [ ] Hierarchy: `GameplaySystems` / `EnvironmentArt`.
- [ ] Console: fiks det dere kan; rest noteres som kjente bugs i rapport.

---

_Neste handling i chat: si «start steg N» så jobber vi kun med det steget._
