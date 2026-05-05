# PG2202 — utførelsesplan (alt du har bedt om)

**Mål:** Pensum + eksamenkrav + ryddig prosjekt + engelsk der det teller + spill/missions + editor-basert sort/fiks — uten «AI-essay» i rapporten (du skriver selv, enkelt).

**Roller:** **[Cursor]** = gjort i kode/repo her · **[Unity]** = du i editoren · **[Rapport]** = du i PDF

---

## Fase 0 — Forståelse (5 min)

| # | Oppgave | Hvem | Status |
|---|---------|------|--------|
| 0.1 | Les «For deg»-boksen i `PG2202_ARBEIDSLOGG_OG_STATUS.md` (hva AI kan vs Unity) | Du | [ ] |
| 0.2 | Det finnes **ingen** mappe `Game Mechanics` — mekanikk ligger i `Scripts/Core`, `AI`, `Player`, … Det er OK i rapport. | Info | ✓ |

---

## Fase 1 — Eksamen & dokumentasjon i repo

| # | Oppgave | Hvem | Status |
|---|---------|------|--------|
| 1.1 | Krav-tabell + script-liste + KI-tekst ligger i `PG2202_ARBEIDSLOGG_OG_STATUS.md` | Cursor | ✓ |
| 1.2 | Steg-for-steg mot 10 krav: `PG2202_MASTERPLAN_STEGVIS.md` | Cursor | ✓ |
| 1.3 | Fyll ut rapport-PDF (prosess, krav 1–10, kilder, bugs) | Rapport | [ ] |

---

## Fase 2 — Editor: sort, fiks, hierarki (CartoonZombies-menyen)

**Rekkefølge anbefalt (begge level-scener):**

1. Åpne `Level01_By` → **CartoonZombies → Fix → ★ FIX EVERYTHING (active scene)** → lagre scene (**Ctrl+S**).  
   - Gjør: hierarchy **GameplaySystems / EnvironmentArt**, spawn-snap, vegetasjon (mesh **+ box** colliders som triggers), bil, pistol-disable, **city: spawner auto-advance av**, missions (EN), HUD-kabling, NavMesh-bake.  
2. Åpne `Level02_StrandSkog` → samme meny → lagre.  
3. **CartoonZombies → Project → Add All Scenes to Build Settings** (hvis ikke allerede).  
4. Valgfritt: **Organize → 2/3 Sort environment art** etter behov.

| # | Oppgave | Hvem | Status |
|---|---------|------|--------|
| 2.1 | `GameplayPolishTool` oppdatert (EN menyer/dialoger, box-vegetasjon, hierarchy, city spawner, EN missions) | Cursor | ✓ |
| 2.2 | Kjør FIX EVERYTHING på **begge** nivåer + lagre | Unity | [ ] |

---

## Fase 3 — Språk (engelsk, student-nivå)

| # | Oppgave | Hvem | Status |
|---|---------|------|--------|
| 3.1 | `SceneSetupTool` generert UI: **engelsk** knapper/keybinds (nyopprettet UI) | Cursor | ✓ |
| 3.2 | `MissionObjectiveHUD`, `HUDController` wave/kills, `MainMenuController` highscore: **engelsk** | Cursor | ✓ |
| 3.3 | `MissionManager` header + polish missions: **engelsk** (kjør Setup MissionManager / FIX EVERYTHING) | Unity | [ ] |
| 3.4 | Eksisterende TMP i **MainMenu / Pause** (gamle norske knapper): **endre i Inspector** eller regenerer UI med backup | Unity | [ ] |

---

## Fase 4 — Spilltest & build (eksamen krav 2, 6, 9)

| # | Oppgave | Hvem | Status |
|---|---------|------|--------|
| 4.1 | Play: MainMenu → by → strand → Win (evt. med **Y** cheat hvis står fast) | Unity | [ ] |
| 4.2 | Console: **ingen røde errors** | Unity | [ ] |
| 4.3 | **Windows build** → start **.exe** → **Quit** fungerer | Unity | [ ] |

---

## Fase 5 — Leveranse (krav 10)

| # | Oppgave | Hvem | Status |
|---|---------|------|--------|
| 5.1 | Zip Unity-prosjekt (uten `Library`/`Temp` om mulig) **&lt; 5 GB** | Du | [ ] |
| 5.2 | Egen mappe med **build** + `_Data` | Du | [ ] |
| 5.3 | Rapport PDF **&lt; 5 MB** | Du | [ ] |

---

## Fase 6 — «Ikke AI-aktig»

- Rapport: korte avsnitt, **tabell** mot de 10 kravene, **ekte** utfordringer og bugs, **kildehenvisning** (Asset Store-pakker du bruker).  
- Ikke lim inn lange AI-tekster; sensor vil se om spillet og rapporten henger sammen.

---

## Hvis du står fast — si ifra med:

- Screenshot av **Console** (røde linjer)  
- Hvilken **scene** og hva du gjorde rett før  
- Om det er **Play** eller **build** som feiler  

---

_Sist oppdatert: 2026-05-04 — plan + GameplayPolishTool batch._
