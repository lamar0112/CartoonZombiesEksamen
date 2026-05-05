# PG2202 Unity utvikling — pensum, arbeidsmåte og eksamenskrav

**Kilde:** forelesnings-PDFer (vår 2026), `PG2202_Eksamen_Mars-2026.pdf`, og mappe  
`C:\Users\lamar\OneDrive\Skrivebord\UnityEksamen\Forelesningsmateriale` (inkl. lærebok-PDF og øvingsfiler).  
**Bruk:** mal for hva som skal dokumenteres i rapporten og hva sensur forventer at spillet demonstrerer.

---

## 1. Emne og arbeidsmåte (fra forelesning 1)

- **Lærebok:** *Unity Game Development in 24 Hours* (4. utg.) — gjennomsnitt ~2 kap. per økt; digital utgave via Canvas.
- **Undervisning:** 12 økter; ca. 4 t opplegg (2 t forelesning + 2 t øving) — **forventes tilsvarende selvstudie**.
- **Emnet er ikke** primært programmering eller 3D-modellering; fokus er Unity-byggeklosser + nok C# til spillogikk. Modeller/teksturer kan hentes (Asset Store m.m.).
- **Unity i undervisning:** Unity **6.3 LTS** nevnt i intro; **eksamen** krever eksplisitt **6000.3.x** (se nedenfor).
- **Anbefalt IDE:** Visual Studio (Windows); alternativ Rider / VS Code.
- **Første prosjekt i bok/intro:** template **3D (Built-In Render Pipeline)** for best samsvar med boka — eksamen tillater også **Universal 3D** og **HDRP 3D**.

---

## 2. Eksamen — formelle krav (`PG2202_Eksamen_Mars-2026`)

| Punkt | Innhold |
|--------|---------|
| **Leveranse** | **ZIP** (ikke obligatorisk PDF for hele besvarelsen). |
| **Gruppe** | 1–3 personer (semesteroppgave). |
| **Karakter** | Bestått / Ikke bestått (B/IB). |
| **Oppgi** | **Kandidatnummer** (ikke studentnummer) på besvarelsen. |
| **Frist / opplasting** | Start opplasting i god tid; leveranse etter kl. = ikke til sensur. |
| **Størrelse** | Max **5 GB** (ZIP) eller **5 MB** (dersom PDF brukes der det er aktuelt). |
| **Arbeidsmengde** | Ca. **5 effektive dager per person** over **6 ukers** periode. |
| **Innhold** | Lite spill i Unity + **kort rapport (PDF)**. |
| **Sjanger** | Valgfri; eget konsept eller bygg videre på innlevert arbeidskrav (samme gruppe). |

### 2.1 Ti obligatoriske spillkrav (må dokumenteres i rapport)

1. **Unity 6000.3.x** + en **3D-template** (Built-In **eller** Universal 3D **eller** HD 3D). Spillet kan *føles* 2D (eksempel Hearthstone).
2. **Vesentlig interaktivitet** — ikke bare avspilling av ferdige sekvenser; ikke bare én knapp uten timing/planlegging. **Gameplay:** hensikt, **vinne/tape**, **målbar prestasjon** (score, tid, e.l.).
3. **Egen C#** — obligatorisk; ferdig scripts *i tillegg* er OK.
4. **Bred bruk av Unity-byggeklosser:** bl.a. materialer, teksturer, lys, kamera, …
5. **Agenter med KI** — f.eks. **NavMesh pathfinding** og/eller **egne finite state machines** (godkjent).
6. **GUI:** **startmeny** (gameplay starter ikke før spiller går videre) + **in-game GUI** (liv/helse/poeng e.l.). Ved tvil om styring: **keybind-oversikt** som menypunkt.
7. **Rigg-animasjoner** i spillet (gjerne på agenter).
8. **Lyd:** GUI-lyder, bakgrunnsmusikk og/eller in-game effekter.
9. **Kjørbar build (.exe):** **tydelig måte å lukke spillet** (f.eks. knapp) — *ikke* forvent at sensor bruker Ctrl+Alt+Del. (Vises i **forelesning 12** / bok kap. 23.)
10. **To leveranser:** (a) mappe med **hele Unity-prosjektet**, (b) mappe med **ferdig build** (Windows om mulig; macOS hvis ikke). + **PDF-rapport**.

**Tips fra oppgaven:** Små scener med godt gameplay > store scener med lite innhold. **Cheat menu** anbefales hvis spillet er stort/vanskelig — letter sensur.

### 2.2 Rapport (PDF) — obligatorisk innhold

1. **Prosess:** et avsnitt eller to (anonym: «jeg», «person 1», … — ikke navn).
2. **Hvordan de 10 spillkravene oppfylles** — tabell eller avsnitt per punkt, inkl.:
   - Unity-versjonsnummer  
   - Interaktivitet (hva spilleren styrer/påvirker)  
   - Oversikt over **egen** C#  
   - Standard Unity-byggeklosser brukt  
   - Beskrivelse av **KI** (agenter, FSM, steering, pathfinding)  
   - GUI (startmeny + øvrig)  
   - Animasjonssystem  
   - Lyd  
   - Hvordan spillet **lukkes** i standalone build  
   - **Navn på mappe** med prosjekt og på mappe med build  
3. **Andre pensumelementer** (valgfritt løfte sensur): terreng, kollisjon, partikler, m.m. — *hvor* og *i hvilken grad*.
4. **Utfordringer og «fancy features»** — kort.
5. **Kjente bugs** — list dem.
6. **Kildehenvisning** — **obligatorisk** for alt som ikke er egenprodusert; unnlatelse kan regnes som juks. (Lærebok + slides trenger ikke listes som kilder.)

**Vurdering:** Utleie av assets gir ofte **lav uttelling** for det som er lånt — poenget er å bruke det for å få et **helhetlig** spill.

---

## 3. Forelesninger → temaer og lærebok-kapitler

| # | PDF-tema | Merknad |
|---|----------|---------|
| **01** | Intro, GameObjects | Kap. 1–2; Hierarchy/Project/Inspector; **ikke** flytte filer i Utforsker — bruk Unity. |
| **02** | C# scripting 1+2 | Kap. 7–8; MonoBehaviour; `Start`/`Update`/`FixedUpdate`; public + Inspector; `[SerializeField]`; enum; `GetComponent`; `Find` / `FindObjectOfType` i **Start**, ikke `Update`; `Time.deltaTime`; **Active Input Handling = Both** ved `InvalidOperationException` med gammelt `Input`. |
| **03** | Terreng, miljø, lys, kamera | Kap. 4–5; Terrain, heightmap; trær/gress; vann trenger ofte ekstra logikk; lys-typer; baking; kamera depth. |
| **04** | Modeller, materialer, tekstur, kollisjon | Kap. 3, 9; mesh vs model; Rigidbody; **`linearVelocity`** i nyere Unity (ikke `velocity`); colliders; Physic Material; triggers. |
| **05** | Spill-KI, agenter, FSM | Ikke i boka; agenter (sensor → beslutning → handling); character / virtual player / director agents; **states, transitions, behaviors**. |
| **06** | Steering behaviors | Ikke i boka; Seek/Flee med `AddForce`, normaliserte vektorer; Pursue, Arrive, Wander, avoidance, path following m.m.; **Simple Soccer**-kontekst. |
| **07** | Pathfinding | NavMesh Surface, bake; **NavMeshAgent**; `SetDestination`; Nav Mesh Obstacle + Carve; AI Navigation-vindu; teori: BFS, A* m.fl. |
| **07b** | Ekstra pathfinding | Table lookup, JPS, hierarkisk, soner — mest teori/selvstudie. |
| **08** | Prefabs, UI, partikler | Kap. 11, 14, 16; **kap. 12–13–15 (2D) ikke pensum**; prefab/instance/instantiate; Canvas, RectTransform, EventSystem; `Instantiate` signaturer; partikkelsystemer. |
| **09** | Animators | Kap. 18; rigg; **Mecanim**: clips + **Animator Controller (FSM)** + Animator; Humanoid vs Generic; parametre (Float, Bool, …); overganger. |
| **10** | Animation-vindu, Timeline, lyd | Kap. 17, 19, 21; `PlayableDirector` / `Play()` / `Stop()`; Audio Listener/Source/Clip; 2D vs 3D lyd. |
| **11** | Maskinlæring + eksamen | Genetiske algoritmer, supervised learning, ANN, deep learning, reinforcement — **oversikt**; ikke krav om ML på eksamen. |
| **12** | Eksamen FAQ, polish, deploy, mobil | Kap. 23 (+ 22 mobil); **SceneManager.LoadScene**; `DontDestroyOnLoad`; **PlayerPrefs**; **Build Profiles**; **`Application.Quit()`** (kun standalone, ikke i editor); ofte **Esc**. |

---

## 4. Kodesnutter og mønstre (fra pensum — typisk «riktig stil»)

**Bevegelse / input (gammelt Input System):**
```csharp
if (Input.GetKey(KeyCode.W)) transform.Translate(0, 0, moveSpeed * Time.deltaTime);
float h = Input.GetAxis("Horizontal");
```

**NavMesh-agent (klikk-til-bevegelse):**
```csharp
using UnityEngine.AI;
// I Update: ray fra Camera.main, Physics.Raycast, pathingAgent.SetDestination(hit.point);
```

**Bytte scene:**
```csharp
using UnityEngine.SceneManagement;
SceneManager.LoadScene(1); // eller LoadScene("Navn");
```

**Avslutt standalone:**
```csharp
if (Input.GetKey(KeyCode.Escape)) Application.Quit();
```

**Timeline (PlayableDirector):**
```csharp
using UnityEngine.Playables;
// director = GetComponent<PlayableDirector>(); director.Play(); director.Stop();
```

**PlayerPrefs:**
```csharp
PlayerPrefs.SetInt("Score", score);
score = PlayerPrefs.GetInt("Score");
```

**Prefab instantiate (fra leksjon 8):**
```csharp
Instantiate(lampPrefab, position, Quaternion.identity);
```

**Rigidbody hastighet (merk `linearVelocity` i Unity 6):**
```csharp
GetComponent<Rigidbody>().linearVelocity = new Vector3(startSpeed, 0, startSpeed);
```

**Seek-steering (kortversjon):**
```csharp
Vector3 dir = (target.position - transform.position).normalized;
_rigidBody.AddForce(dir * (_powerPerSecond * Time.deltaTime));
```

---

## 5. Mappe «Forelesningsmateriale» (desktop)

**Sti:** `C:\Users\lamar\OneDrive\Skrivebord\UnityEksamen\Forelesningsmateriale`

Inneholder blant annet:
- Alle PG2202-0x PDFer + **`PG2202_Eksamen_Mars-2026.pdf`**
- **Lærebok:** `Unity Game Development in 24 Hours - Mike Geig.pdf`
- ZIP-assets per økt (`PG2202-04_Assets.zip`, `08`, `09`, `10`, `12`, …)
- **`MoveObject.cs`**, **`PG2202-06_Steering_Seek-Flee.cs`**
- **`PG2202-06_SeekFleeInUnity_FullProjectFolder`** (og tilsvarende `.zip`)
- **`PG2202-02_UnityProjectFromLesson.zip`**

Større filer (>10 MB) som ikke ble vedlagt i chat ligger her.

---

## 6. Avvik mellom «klasserom»-default og ditt prosjekt

- Eksamen krever **6000.3.x** — ditt prosjekt bruker **6000.3.14f1** (OK).
- Eksamen tillater **URP** — prosjektet er **URP**, ikke Built-In (OK).
- Lærer bruker ofte **`Input.GetKey` / `GetAxis`** — prosjektet kan bruke **ny Input System**; hold **Active Input Handling = Both** eller konsekvent nytt API, og **forklar i rapport**.

---

*Denne filen er et arbeidsdokument for agent og student — oppdater ved endringer i eksamenstekst eller versjonskrav.*
