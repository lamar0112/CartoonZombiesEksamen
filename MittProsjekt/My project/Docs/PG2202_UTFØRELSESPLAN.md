# PG2202 — utførelsesplan (leveranse)

**Mål:** Oppfylle alle **10 spillkrav** + **rapport-PDF** + zip under **5 GB** / PDF under **5 MB** (WISEflow).

**Roller:** **[Repo]** = det som ligger i Git · **[Unity]** = du i editoren · **[Rapport]** = PDF

> **Merk (2026-05-06):** Tidligere «CartoonZombies → Fix / Repair / …»-editorverktøy er fjernet. Kun **CartoonZombies → Settings** (`GameSettingsWindow`) er igjen. Bruk `EKSAMEN_OPPSTART.md` for manuelle steg (NavMesh, Build Settings, Inspector).

---

## Fase 1 — Dokumentasjon og mapping

| # | Oppgave | Hvem | Status |
|---|---------|------|--------|
| 1.1 | Krav-tabell + script-liste + KI-tekst: `PG2202_ARBEIDSLOGG_OG_STATUS.md` | Repo | ✓ |
| 1.2 | Steg mot 10 krav: `PG2202_MASTERPLAN_STEGVIS.md` | Repo | ✓ |
| 1.3 | Rapport-PDF: prosess, krav 1–10, pensum-highlights, bugs, **kilder** | Rapport | [ ] |

---

## Fase 2 — Unity (scener, NavMesh, tuning)

| # | Oppgave | Hvem | Status |
|---|---------|------|--------|
| 2.1 | Les `EKSAMEN_OPPSTART.md` — Build Settings, begge levels, **Island Win Trigger** på kiste | Unity | [ ] |
| 2.2 | Bake NavMesh; flytt spawn-punkter **på** mesh (unngå «Failed to create agent») | Unity | [ ] |
| 2.3 | Valgfritt tuning: **CartoonZombies → Settings** (lyd, bølger, prefabs) | Unity | [ ] |

---

## Fase 3 — Språk og UI

| # | Oppgave | Hvem | Status |
|---|---------|------|--------|
| 3.1 | Ett språk konsekvent (norsk eller engelsk) i meny + viktig HUD | Unity | [ ] |
| 3.2 | Keybind-oversikt synlig (krav 6) — MainMenu eller hjelpepanel | Unity | [ ] |

---

## Fase 4 — Spilltest og build

| # | Oppgave | Hvem | Status |
|---|---------|------|--------|
| 4.1 | Play: MainMenu → Level01 → Level02 → Win (evt. **Y** cheat for sensor) | Unity | [ ] |
| 4.2 | Console: ingen røde errors | Unity | [ ] |
| 4.3 | **Windows .exe** → **Avslutt/Quit** fungerer (krav 9) | Unity | [ ] |

---

## Fase 5 — Leveranse

| # | Oppgave | Hvem | Status |
|---|---------|------|--------|
| 5.1 | Zip prosjekt **&lt; 5 GB** (uten `Library`/`Temp` om portal tillater det) | Du | [ ] |
| 5.2 | Egen mappe: build + nødvendige filer (jf. forelesning 12) | Du | [ ] |
| 5.3 | Rapport PDF **&lt; 5 MB**, **kandidatnummer** (ikke studentnummer) | Du | [ ] |

---

_Sist oppdatert: 2026-05-06._
