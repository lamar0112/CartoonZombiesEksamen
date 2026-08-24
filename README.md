# Cartoon Zombies

3D zombie-overlevelsesspill laget i Unity som gruppeeksamen i emnet **PG2202 Spillprogrammering** ved Høyskolen Kristiania, våren 2026. Gruppe 8 (kandidatnummer 7, 39, 66).

Spilleren beveger seg gjennom to ulike baner — en by og en strand/skog — mens zombier spawner i bølger og må bekjempes. Spillet har full meny- og speil-flyt (start → spill → game over / seier), lagring av high score, og et oppdragssystem som guider spilleren med et kompass.

## Funksjonalitet

- **To baner:** en byscene med et parkour-/myntsystem, og en strand/skog-scene med et båt-basert progresjonssystem for å komme videre.
- **Zombie-AI:** tilstandsmaskin (Patrol → Chase → Attack → Dead) drevet av Unity NavMesh, i tillegg til sivile NPC-er.
- **Bølgebasert spawning:** fiender styres av en `WaveData`-konfigurasjon (ScriptableObject).
- **Oppdragssystem:** `MissionManager` med sekvensielle mål og retningskompass.
- **Kjøretøy:** båt med fysikkbasert bevegelse (`Rigidbody`) som låses opp underveis.
- **Lyd:** sentral `AudioManager` styrt av konfigurerbare lydinnstillinger (ScriptableObject).
- **UI:** hovedmeny, HUD, pausemeny, game over/seiers-skjerm, og en egen cheat-/debug-meny brukt under utvikling og QA.
- **Lagring:** high score og siste runde lagres lokalt (PlayerPrefs).
- **Egne editor-verktøy:** et innstillingsvindu (`GameSettingsWindow`) for å justere spillbalanse direkte i Unity-editoren, i tillegg til verktøy for rask oppsett av scener.

## Teknologier

- Unity 6000.3 (Universal Render Pipeline)
- C# — ca. 44 egne runtime-scripts fordelt på Core, AI, Player, UI, Misc og Vehicle, pluss egne editor-verktøy
- NavMesh for AI-pathfinding
- ScriptableObjects for konfigurasjon (bølger, lyd, cheat-innstillinger)
- Git LFS for binærfiler (teksturer, modeller, lyd)

## Hva jeg lærte

Dette var det mest omfattende Unity-prosjektet jeg har bygget, med fokus på å strukturere et spill av en viss størrelse uten at koden ble uoversiktlig:
- Dele spillogikk i klare ansvarsområder (spilltilstand, AI, spillerkontroll, UI, kjøretøy) i stedet for å samle alt i én stor kontroller.
- Bruke ScriptableObjects til å skille konfigurasjon (bølger, lyd, balanse) fra selve koden, slik at spillbalanse kan justeres uten å endre scripts.
- Bygge en enkel tilstandsmaskin for fiende-AI og en oppdragsflyt som fungerer på tvers av flere scener.
- Lage egne editor-verktøy for å gjøre iterasjon på spillbalanse raskere under utvikling.

## Kjøre prosjektet lokalt

Selve Unity-prosjektet ligger under `MittProsjekt/My project/`.

1. Klon repoet (krever [Git LFS](https://git-lfs.com) installert: `git lfs install` før kloning).
2. Åpne `MittProsjekt/My project/` i Unity Hub (Unity 6000.3.x, URP).
3. La Unity importere prosjektet, åpne `Assets/Scenes/MainMenu.unity` og trykk Play.

## Kontekst

Eksamensbesvarelsen ble levert i gruppe. Denne repoen inneholder min kopi av gruppens felles kode.
