#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// GameplayPolishTool — editor-only «verkstedmeny» under CartoonZombies/Fix (IKKE en del av standalone build).
// Kommentarer på norsk (rapport/pensum); menytekster og dialoger på engelsk = prosjektkonvensjon for Unity-menyen.
// Pensum: NavMesh-bake (F7), prefab/trigger-regler (F4/F8), TMP-HUD (F8), SceneManager/scenenavn (PG2202-12).
// Ekstra: mange steg i én knapp (Fix Everything) — sparer tid for gruppa; forklar i rapport at dette er utviklingsverktøy, ikke gameplay.
// ──────────────────────────────────────────────────────────────────────────────
public static class GameplayPolishTool
{
    // ──────────────────── MASTER ─────────────────────────────────────────────
    [MenuItem("CartoonZombies/Fix/★ FIX EVERYTHING (active scene)", false, 1)]
    public static void FixEverything()
    {
        string sn = SceneManager.GetActiveScene().name;
        if (sn != "Level01_By" && sn != "Level02_StrandSkog")
        {
            EditorUtility.DisplayDialog("Wrong scene",
                $"Open Level01_By or Level02_StrandSkog. Active scene: {sn}", "OK");
            return;
        }

        int fixes = 0;
        fixes += MergeDuplicateEnvironmentGroups();
        fixes += RemoveDuplicateCameras();
        HierarchyLevelCleanupTool.OrganizeRoots(SceneManager.GetActiveScene());
        fixes++;

        fixes += UnfixMistakeTriggers(); // Angrer feilaktige triggers fra tidligere run
        fixes += FixSpawnPoints();
        fixes += FixVegetationColliders();
        fixes += FixCarConvex();
        fixes += FixCarSetup();
        fixes += FixPlayerScale();
        fixes += DisablePlayerShootingAtStart();
        fixes += PlaceWeaponPickupIfMissing();
        fixes += FixParkourCoins();
        fixes += FixCityZombieSpawnerAutoload();
        fixes += EnsureZombieSpawnerHordeDefaults();
        fixes += SetupMissionManagerInScene();
        fixes += FixHUDWiring();
        fixes += PositionHUDElements();
        fixes += EnsureParkourCoinsHud();
        fixes += BakeNavMesh();

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorUtility.DisplayDialog("Fix Everything — done",
            $"{fixes} fix steps applied to {sn}.\n\n" +
            "Next:\n" +
            "• CartoonZombies → Fix → Add All Scenes to Build Settings (if needed)\n" +
            "• Press Play and test\n" +
            "• PlayerShooting should start disabled until WeaponPickup", "OK");
    }

    // ──────────────────── SPAWN POINTS ───────────────────────────────────────
    [MenuItem("CartoonZombies/Fix/Fix SpawnPoints (snap to ground)", false, 10)]
    public static void FixSpawnPointsMenu() => Debug.Log($"[Polish] SpawnPoints: {FixSpawnPoints()}");

    private static int FixSpawnPoints()
    {
        int count = 0;

        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            if (TrySnapToGround(playerGo.transform.position, out Vector3 snapped))
            {
                Undo.RecordObject(playerGo.transform, "Snap Player");
                playerGo.transform.position = snapped + Vector3.up * 0.1f;
                count++;
            }
        }

        GameObject spRoot = GameObject.Find("SpawnPoints");
        if (spRoot != null)
        {
            foreach (Transform child in spRoot.transform)
            {
                if (TrySnapToGround(child.position, out Vector3 snapped))
                {
                    Undo.RecordObject(child, "Snap SpawnPoint");
                    child.position = snapped + Vector3.up * 0.5f;
                    count++;
                }
            }
        }

        return count;
    }

    // ──────────────────── VEGETATION ─────────────────────────────────────────
    [MenuItem("CartoonZombies/Fix/Fix Vegetation Colliders (walk-through foliage)", false, 20)]
    public static void FixVegetationCollidersMenu() => Debug.Log($"[Polish] Vegetation: {FixVegetationColliders()}");

    private static int FixVegetationColliders()
    {
        int count = 0;

        // Ord i navn (på objektet ELLER foreldre) som indikerer walkthrough-vegetasjon
        // NB: "grass"/"gress" er fjernet — de matcher gresstiles (Terrain_Grass_Flat, Natures_Grass_Tile)
        //     som ER walkable terreng, ikke vegetasjon. Tilsvarende er "sm_" fjernet.
        var vegWords = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "bush", "shrub", "weed", "fern", "plant", "flower", "hedge",
            "undergrowth", "foliage", "busk", "ugress",
            "palm", "pine", "birch", "oak", "tree", "leaf", "leaves",
            "branch", "vegetation", "frond",
            "forest", "jungle", "canopy", "sapling",
            "stump", "ivy", "moss", "reed", "rush", "kelp", "seaweed", "dune", "crop",
            "_veg", "foliage_", "tree_", "-tree", "_tree", "trees"
        };

        // Ord som ALDRI skal gjøres til trigger
        var neverTrigger = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "wall", "floor", "road", "building", "roof", "ground", "terrain",
            "platform", "collider", "barrier", "fence", "water", "vann",
            "ocean", "sea", "player", "zombie", "car", "vehicle",
            "rock", "boulder", "stone", "cliff", "bridge", "concrete", "brick", "window", "glass",
            // Walkable terreng og navigasjonsprops — aldri trigger
            "pipe", "ramp", "slide", "tile", "flat", "escarpment", "mast",
            "stairs", "step", "ledge", "walkway", "catwalk", "grass",
            // Karakterdeler — kan ligge under «tree»/«branch» i hierarkiet uten å være vegetasjon
            "hair", "skin", "hand", "foot", "glove", "boot", "face", "head",
            "finger", "toe", "teeth", "eye", "mouth", "helmet", "armor",
            "character", "cloth", "shirt", "pants", "torso", "skull", "jaw"
        };

        // Scan hele scene-hierarkiet — fanger opp busker under alle foreldrenavn
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (MeshCollider mc in root.GetComponentsInChildren<MeshCollider>(true))
            {
                if (mc == null || mc.isTrigger) continue;
                if (mc.GetComponent<CharacterController>() != null) continue;

                string goName = mc.gameObject.name.ToLowerInvariant();

                // Sjekk at objektet IKKE er bygg/veier/underlag
                bool blocked = neverTrigger.Any(w => goName.Contains(w));
                if (blocked) continue;
                if (LooksLikePlaygroundOrSolidProp(goName)) continue;

                // Sjekk om noe i hierarkiet inneholder et vegetasjonsnavn
                bool isVeg = false;
                Transform t = mc.transform;
                while (t != null && !isVeg)
                {
                    string lower = t.name.ToLower();
                    if (neverTrigger.Any(w => lower.Contains(w))) break; // foreldrenavn sier det er infrastruktur
                    isVeg = vegWords.Any(w => lower.Contains(w));
                    t = t.parent;
                }

                if (!isVeg) continue;

                // Konkav MeshCollider kan ikke være trigger. convex=true på tunge trær gir ofte PhysX-feil (>255 trekanter).
                if (!mc.convex)
                {
                    Undo.RecordObject(mc, "Vegetation walk-through (disable concave mesh)");
                    mc.enabled = false;
                    count++;
                    Debug.Log($"[Polish] Disabled concave mesh: {mc.gameObject.name}");
                    continue;
                }
                Undo.RecordObject(mc, "Vegetation trigger");
                mc.isTrigger = true;
                count++;
                Debug.Log($"[Polish] Trigger: {mc.gameObject.name}");
            }

            foreach (BoxCollider bc in root.GetComponentsInChildren<BoxCollider>(true))
            {
                if (bc == null || bc.isTrigger) continue;
                if (bc.GetComponent<CharacterController>() != null) continue;

                string goName = bc.gameObject.name.ToLowerInvariant();
                if (neverTrigger.Any(w => goName.Contains(w))) continue;
                if (LooksLikePlaygroundOrSolidProp(goName)) continue;

                bool isVegBox = false;
                Transform t = bc.transform;
                while (t != null && !isVegBox)
                {
                    string lower = t.name.ToLower();
                    if (neverTrigger.Any(w => lower.Contains(w))) break;
                    isVegBox = vegWords.Any(w => lower.Contains(w));
                    t = t.parent;
                }

                if (!isVegBox) continue;

                Undo.RecordObject(bc, "Vegetation trigger (box)");
                bc.isTrigger = true;
                count++;
                Debug.Log($"[Polish] Trigger (box): {bc.gameObject.name}");
            }
        }

        if (count == 0)
            count += FixVegetationUnderEnvironmentArt(neverTrigger);

        if (count == 0)
            Debug.Log("[Polish] Vegetation: ingen passende MeshCollider/BoxCollider funnet (mange assets har allerede trigger, eller kun Terrain).");

        return count;
    }

    /// <summary>
    /// Ekstra pass: typisk Synty-løv under <c>EnvironmentArt</c> heter SM_* uten «tree» i strengen.
    /// </summary>
    private static int FixVegetationUnderEnvironmentArt(HashSet<string> neverTrigger)
    {
        GameObject env = GameObject.Find("EnvironmentArt");
        if (env == null) return 0;

        int count = 0;
        // «grass» er med vilje utelatt — gresstiles / terreng skal ikke bli walk-through triggers
        string[] hints =
        {
            "sm_", "veg", "tree", "bush", "palm", "fern", "flower", "plant",
            "leaf", "moss", "ivy", "stump", "log", "crop", "branch", "jungle", "forest", "reed", "foliage"
        };

        foreach (MeshCollider mc in env.GetComponentsInChildren<MeshCollider>(true))
        {
            if (mc == null || mc.isTrigger) continue;
            if (mc.GetComponent<CharacterController>() != null) continue;

            string goName = mc.gameObject.name.ToLowerInvariant();
            if (goName.Contains("pipe") || goName.Contains("ramp") || goName.Contains("slide") ||
                goName.Contains("prop_pipe") || goName.Contains("prop_slide") || goName.Contains("mario"))
                continue;
            if (neverTrigger.Any(w => goName.Contains(w))) continue;
            if (LooksLikePlaygroundOrSolidProp(goName)) continue;
            if (!hints.Any(h => goName.Contains(h))) continue;

            if (!mc.convex)
            {
                Undo.RecordObject(mc, "EnvArt walk-through (disable concave mesh)");
                mc.enabled = false;
                count++;
                Debug.Log($"[Polish] Disabled concave mesh (env art): {mc.gameObject.name}");
                continue;
            }
            Undo.RecordObject(mc, "EnvArt vegetation trigger");
            mc.isTrigger = true;
            count++;
            Debug.Log($"[Polish] Trigger (env art): {mc.gameObject.name}");
        }

        foreach (BoxCollider bc in env.GetComponentsInChildren<BoxCollider>(true))
        {
            if (bc == null || bc.isTrigger) continue;
            string goName = bc.gameObject.name.ToLowerInvariant();
            if (goName.Contains("pipe") || goName.Contains("ramp") || goName.Contains("slide") ||
                goName.Contains("prop_pipe") || goName.Contains("prop_slide") || goName.Contains("mario"))
                continue;
            if (neverTrigger.Any(w => goName.Contains(w))) continue;
            if (LooksLikePlaygroundOrSolidProp(goName)) continue;
            if (!hints.Any(h => goName.Contains(h))) continue;

            Undo.RecordObject(bc, "EnvArt vegetation trigger (box)");
            bc.isTrigger = true;
            count++;
            Debug.Log($"[Polish] Trigger (env art box): {bc.gameObject.name}");
        }

        return count;
    }

    private static bool LooksLikePlaygroundOrSolidProp(string lower)
    {
        if (string.IsNullOrEmpty(lower)) return false;
        return lower.Contains("crate") || lower.Contains("slide") || lower.Contains("lollipop") ||
               lower.Contains("question") || lower.Contains("block_play") || lower.Contains("playground") ||
               lower.Contains("prop_block") || lower.Contains("bot_submesh");
    }

    // ──────────────────── CAR CONVEX ─────────────────────────────────────────
    [MenuItem("CartoonZombies/Fix/Fix Car MeshCollider (Convex)", false, 30)]
    public static void FixCarConvexMenu() => Debug.Log($"[Polish] Car convex: {FixCarConvex()}");

    private static int FixCarConvex()
    {
        int count = 0;
        // Kun bilen — å sette convex=true på ALLE Rigidbodies ødelegger parkour-props
        // (Prop_Slide_Bot, Prop_Block_Play osv. overskrider PhysX 256-polygon-grensen og krasjer)
        GameObject carGo = FindCarRootGameObject(out CarController _);
        if (carGo == null) return 0;

        foreach (MeshCollider mc in carGo.GetComponentsInChildren<MeshCollider>(true))
        {
            if (mc.convex) continue;
            Undo.RecordObject(mc, "Car MeshCollider Convex");
            mc.convex = true;
            count++;
        }
        return count;
    }

    private static GameObject FindCarRootGameObject(out CarController carCtrl)
    {
        carCtrl = Object.FindFirstObjectByType<CarController>(FindObjectsInactive.Include);
        if (carCtrl != null)
            return carCtrl.gameObject;

        CarInteraction ci = Object.FindFirstObjectByType<CarInteraction>(FindObjectsInactive.Include);
        if (ci != null)
        {
            Rigidbody prb = ci.GetComponentInParent<Rigidbody>();
            if (prb != null)
            {
                carCtrl = prb.GetComponent<CarController>();
                return prb.gameObject;
            }

            carCtrl = ci.GetComponent<CarController>();
            return ci.gameObject;
        }

        // Søk også inne i kjøretøy-grupper (Kjoretoy / Vehicles)
        string[] vehicleGroupNames = { "Kjoretoy", "Vehicles", "Vehicle", "Cars" };
        foreach (string grpName in vehicleGroupNames)
        {
            GameObject grp = GameObject.Find(grpName);
            if (grp == null) continue;
            // Returner første barn som har Rigidbody — det er bilen
            foreach (Rigidbody childRb in grp.GetComponentsInChildren<Rigidbody>(true))
            {
                carCtrl = childRb.GetComponent<CarController>();
                return childRb.gameObject;
            }
            // Ingen Rigidbody? Returner første barn med mesh (lar FixCarSetup legge til Rigidbody)
            if (grp.transform.childCount > 0)
            {
                carCtrl = null;
                return grp.transform.GetChild(0).gameObject;
            }
        }

        string[] names =
        {
            "DrivableCar", "Car", "CarRoot", "Vehicle", "CarVisual", "PlayerCar", "Sedan", "Truck",
            "PoliceCar", "SportCar", "CityCar", "Kenney_Car"
        };
        foreach (string n in names)
        {
            GameObject go = GameObject.Find(n);
            if (go == null) continue;
            carCtrl = go.GetComponent<CarController>();
            return go;
        }

        foreach (Rigidbody rb in Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (rb == null) continue;
            string lower = rb.gameObject.name.ToLowerInvariant();
            if (!LooksLikeCarObjectName(lower)) continue;
            carCtrl = rb.GetComponent<CarController>();
            return rb.gameObject;
        }

        carCtrl = null;
        return null;
    }

    private static bool LooksLikeCarObjectName(string lower)
    {
        if (string.IsNullOrEmpty(lower)) return false;
        if (lower.Contains("card") || lower.Contains("carpet") || lower.Contains("careful") || lower.Contains("carousel"))
            return false;
        return lower.Contains("car") || lower.Contains("vehicle") || lower.Contains("sedan") ||
               lower.Contains("truck") || lower.Contains("drivable") || lower.Contains("auto");
    }

    // ──────────────────── CAR FULL SETUP ─────────────────────────────────────
    [MenuItem("CartoonZombies/Fix/Fix Car Full Setup (controller, Rigidbody, seat)", false, 32)]
    public static void FixCarSetupMenu() => Debug.Log($"[Polish] Car setup: {FixCarSetup()}");

    private static int FixCarSetup()
    {
        int count = 0;

        GameObject carGo = FindCarRootGameObject(out CarController carCtrl);

        if (carGo == null)
        {
            Debug.Log("[Polish] Ingen bil funnet (ingen CarController/CarInteraction, og intet GameObject med vanlige bil-navn). By: legg CarController på rot eller døp bilen «DrivableCar».");
            return 0;
        }

        // Sikre CarController
        if (carCtrl == null)
        {
            Undo.AddComponent<CarController>(carGo);
            carCtrl = carGo.GetComponent<CarController>();
            count++;
            Debug.Log($"[Polish] Added CarController on {carGo.name}");
        }

        // Sikre Rigidbody med riktige innstillinger
        Rigidbody rb = carGo.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = Undo.AddComponent<Rigidbody>(carGo);
            count++;
            Debug.Log($"[Polish] Added Rigidbody on {carGo.name}");
        }

        Undo.RecordObject(rb, "Fix Car Rigidbody");
        rb.isKinematic   = false;
        rb.mass          = Mathf.Max(rb.mass, 3200f);
        rb.linearDamping = Mathf.Min(rb.linearDamping, 0.15f);
        rb.angularDamping = Mathf.Max(rb.angularDamping, 2.5f);
        rb.constraints   = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        EditorUtility.SetDirty(rb);
        count++;

        // Sikre at CarInteraction er på SAMME GameObject som CarController
        CarInteraction ci = carGo.GetComponent<CarInteraction>();
        if (ci == null)
        {
            // Sjekk om CI sitter et annet sted i hierarkiet
            ci = carGo.GetComponentInChildren<CarInteraction>();
            if (ci == null)
            {
                ci = Undo.AddComponent<CarInteraction>(carGo);
                count++;
                Debug.Log($"[Polish] Added CarInteraction on {carGo.name}");
            }
        }

        // Koble CarController til CarInteraction
        if (ci != null)
        {
            var ciSo = new SerializedObject(ci);
            var ctrlProp = ciSo.FindProperty("carController");
            if (ctrlProp != null && ctrlProp.objectReferenceValue == null)
            {
                ctrlProp.objectReferenceValue = carCtrl;
                ciSo.ApplyModifiedProperties();
                count++;
                Debug.Log($"[Polish] Wired CarController → CarInteraction on {carGo.name}");
            }

            // Sikre DriverSeat-barn
            Transform seat = carGo.transform.Find("DriverSeat");
            if (seat == null)
            {
                var seatGo = new GameObject("DriverSeat");
                Undo.RegisterCreatedObjectUndo(seatGo, "Create DriverSeat");
                seatGo.transform.SetParent(carGo.transform, false);
                seatGo.transform.localPosition = new Vector3(-0.4f, 0.5f, 0.1f);
                seat = seatGo.transform;
                count++;
                Debug.Log("[Polish] Created DriverSeat");
            }

            var seatProp = ciSo.FindProperty("driverSeat");
            if (seatProp != null && seatProp.objectReferenceValue == null)
            {
                var ciSo2 = new SerializedObject(ci);
                ciSo2.FindProperty("driverSeat").objectReferenceValue = seat;
                ciSo2.ApplyModifiedProperties();
                count++;
            }

            EditorUtility.SetDirty(ci);
        }

        return count;
    }

    // ──────────────────── PARKOUR COINS ─────────────────────────────────────────
    [MenuItem("CartoonZombies/Fix/Fix Parkour Coins (add CoinCollectable)", false, 37)]
    public static void FixParkourCoinsMenu() => Debug.Log($"[Polish] Coins: {FixParkourCoins()}");

    private static int FixParkourCoins()
    {
        if (SceneManager.GetActiveScene().name != "Level01_By") return 0;
        int count = 0;

        // Finn alle objekter med "coin" i navnet i hele scenen
        var allCoins = new List<GameObject>();
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == null) continue;
                string lower = t.name.ToLowerInvariant();
                if (!lower.Contains("coin") && !lower.Contains("mynt")) continue;
                if (t.GetComponent<CoinCollectable>() != null) continue;
                allCoins.Add(t.gameObject);
            }
        }

        if (allCoins.Count == 0) { Debug.Log("[Polish] Ingen mynter funnet uten CoinCollectable"); return 0; }

        // Spatial split: beregn median X — venstre halvdel = sone 1, høyre = sone 2
        // (tilpasses automatisk til hvilken som helst scene-layout)
        allCoins.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        float medX = allCoins[allCoins.Count / 2].transform.position.x;

        // Sjekk også Z for å skille bedre
        float medZ = 0f;
        {
            var sortedZ = new List<GameObject>(allCoins);
            sortedZ.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));
            medZ = sortedZ[sortedZ.Count / 2].transform.position.z;
        }

        // Cluster-tildeling: sone 1 = større X, sone 2 = større Z (justeres etter layout)
        // Bruk hierarki-navn som første prioritet
        int z1 = 0, z2 = 0;
        foreach (GameObject go in allCoins)
        {
            int zoneId = DetermineZoneIdForCoin(go, medX, medZ);

            PrepareGameObjectForCoin(go);
            var coin = Undo.AddComponent<CoinCollectable>(go);
            var so = new SerializedObject(coin);
            so.FindProperty("parkourZoneId").intValue = zoneId;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(go);
            if (zoneId == 1) z1++; else z2++;
            count++;
            Debug.Log($"[Polish] CoinCollectable sone {zoneId} → {go.name}");
        }

        // Oppdater CityParkourManager med korrekte tall
        CityParkourManager cpm = Object.FindFirstObjectByType<CityParkourManager>(FindObjectsInactive.Include);
        if (cpm != null)
        {
            var so = new SerializedObject(cpm);
            if (z1 > 0) so.FindProperty("coinsInZone1").intValue = z1;
            if (z2 > 0) so.FindProperty("coinsInZone2").intValue = z2;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(cpm);
            Debug.Log($"[Polish] CityParkourManager: sone1={z1} mynter, sone2={z2} mynter");
        }

        Debug.Log($"[Polish] Fikset {count} mynter totalt (sone1={z1}, sone2={z2})");
        return count;
    }

    private static int DetermineZoneIdForCoin(GameObject go, float medX, float medZ)
    {
        // 1. Sjekk foreldre-hierarki for «1» / «2» i kombinasjon med «zone/sone/parkour»
        Transform t = go.transform;
        while (t != null)
        {
            string lower = t.name.ToLowerInvariant();
            bool isParkourNode = lower.Contains("zone") || lower.Contains("parkour") ||
                                 lower.Contains("sone") || lower.Contains("area");
            if (isParkourNode)
            {
                if (lower.Contains("1") || lower.Contains("one") || lower.Contains("mario") ||
                    lower.Contains("green") || lower.Contains("gronn"))
                    return 1;
                if (lower.Contains("2") || lower.Contains("two") || lower.Contains("blue") ||
                    lower.Contains("blaa") || lower.Contains("blå"))
                    return 2;
            }
            t = t.parent;
        }

        // 2. Spatial: soner på ulike sider av kartet
        Vector3 p = go.transform.position;
        // Bruk Chebyshev-distanse til median for å bestemme den mest avvikende aksen
        bool rightSide = p.x > medX;
        bool farSide   = p.z > medZ;
        // Velg sone basert på kombinasjon — tilpass til din layout
        return (rightSide || farSide) ? 2 : 1;
    }

    // ──────────────────── PLAYER SCALE ───────────────────────────────────────
    [MenuItem("CartoonZombies/Fix/Fix Player Scale (0.75 — map looks bigger)", false, 38)]
    public static void FixPlayerScaleMenu() => Debug.Log($"[Polish] Player scale: {FixPlayerScale()}");

    private static int FixPlayerScale()
    {
        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo == null) { Debug.LogWarning("[Polish] Ingen Player-tag funnet"); return 0; }

        // Ikke skaler ned for mye — 0.75 ser bra ut mot Synty low-poly assets
        Vector3 target = new Vector3(0.75f, 0.75f, 0.75f);
        if (Vector3.Distance(playerGo.transform.localScale, target) < 0.01f) return 0;

        Undo.RecordObject(playerGo.transform, "Scale Player");
        playerGo.transform.localScale = target;
        EditorUtility.SetDirty(playerGo);
        Debug.Log($"[Polish] Spiller skalert til 0.75 (kart ser større ut)");
        return 1;
    }

    // ──────────────────── HUD LAYOUT ─────────────────────────────────────────
    [MenuItem("CartoonZombies/Fix/Position HUD Elements (clean layout)", false, 47)]
    public static void PositionHUDElementsMenu() => Debug.Log($"[Polish] HUD layout: {PositionHUDElements()}");

    /// <summary>
    /// Setter alle HUD-TMP-elementers RectTransform til et rent, spillvennlig layout:
    /// HP øverst venstre · Bølge/Zombier øverst midt · Drap øverst høyre · Ammo nederst høyre.
    /// </summary>
    private static int PositionHUDElements()
    {
        Canvas hudCanvas = FindHudCanvas();
        if (hudCanvas == null) return 0;

        int count = 0;

        // Fjern gammel readability-panel (lages på nytt med bedre posisjon)
        Transform oldPanel = hudCanvas.transform.Find("HUD_ReadabilityPanel");
        if (oldPanel != null) { Undo.DestroyObjectImmediate(oldPanel.gameObject); count++; }
        Transform oldInner = null;
        foreach (Transform child in hudCanvas.transform)
        {
            if (child.Find("HUD_ReadabilityPanel") != null)
            { oldInner = child.Find("HUD_ReadabilityPanel"); break; }
        }
        if (oldInner != null) { Undo.DestroyObjectImmediate(oldInner.gameObject); count++; }

        // Lag en mørk toppbar som bakgrunn for stats
        if (hudCanvas.transform.Find("HUD_TopBar") == null)
        {
            var barGo = new GameObject("HUD_TopBar");
            Undo.RegisterCreatedObjectUndo(barGo, "HUD TopBar");
            barGo.transform.SetParent(hudCanvas.transform, false);
            barGo.transform.SetAsFirstSibling();
            var rt = barGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 62f);
            var img = barGo.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0f, 0f, 0f, 0.55f);
            img.raycastTarget = false;
            count++;
        }

        // HP — øverst venstre i toppbaren
        count += PositionTMP(hudCanvas, "health", "hp",
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f),
            pivot:     new Vector2(0f, 1f),
            pos:       new Vector2(14f, -10f), size: new Vector2(240f, 44f));

        // Bølge/Zombier — øverst midt
        count += PositionTMP(hudCanvas, "wave", "bølge", "bolge",
            anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
            pivot:     new Vector2(0.5f, 1f),
            pos:       new Vector2(0f, -10f),  size: new Vector2(480f, 44f));

        // Drap — øverst høyre
        count += PositionTMP(hudCanvas, "kill",
            anchorMin: new Vector2(1f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot:     new Vector2(1f, 1f),
            pos:       new Vector2(-14f, -10f), size: new Vector2(180f, 44f));

        // Ammo — nederst høyre
        count += PositionTMP(hudCanvas, "ammo",
            anchorMin: new Vector2(1f, 0f), anchorMax: new Vector2(1f, 0f),
            pivot:     new Vector2(1f, 0f),
            pos:       new Vector2(-18f, 18f), size: new Vector2(200f, 56f));

        // Reload — rett over ammo
        count += PositionTMP(hudCanvas, "reload",
            anchorMin: new Vector2(1f, 0f), anchorMax: new Vector2(1f, 0f),
            pivot:     new Vector2(1f, 0f),
            pos:       new Vector2(-18f, 78f), size: new Vector2(200f, 36f));

        count += LayoutMissionPanel(hudCanvas);
        count += LayoutGoalsAndControlStrip(hudCanvas);

        if (count > 0) Debug.Log($"[Polish] HUD layout: {count} elementer posisjonert");
        return count > 0 ? 1 : 0;
    }

    private static int LayoutMissionPanel(Canvas canvas)
    {
        Transform p = canvas.transform.Find("MissionPanel");
        if (p == null) return 0;
        var rt = p.GetComponent<RectTransform>();
        if (rt == null) return 0;
        Undo.RecordObject(rt, "MissionPanel layout");
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(16f, 40f);
        rt.sizeDelta = new Vector2(292f, 96f);
        var img = p.GetComponent<Image>();
        if (img != null)
        {
            Undo.RecordObject(img, "MissionPanel tint");
            img.color = new Color(0.04f, 0.06f, 0.10f, 0.76f);
        }
        return 1;
    }

    private static int LayoutGoalsAndControlStrip(Canvas canvas)
    {
        int n = 0;
        foreach (var tmp in canvas.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
        {
            string l = tmp.gameObject.name.ToLowerInvariant();
            if (l.Contains("control") && l.Contains("hint"))
            {
                var rt = tmp.rectTransform;
                Undo.RecordObject(rt, "ControlHints layout");
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0.58f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(12f, 102f);
                rt.sizeDelta = new Vector2(540f, 68f);
                n++;
            }
        }

        Transform goals = canvas.transform.Find("GoalsHintPanel");
        if (goals != null)
        {
            var rt = goals.GetComponent<RectTransform>();
            if (rt != null)
            {
                Undo.RecordObject(rt, "GoalsHint layout");
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0.58f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(12f, 14f);
                rt.sizeDelta = new Vector2(540f, 78f);
                n++;
            }
        }

        return n > 0 ? 1 : 0;
    }

    private static int EnsureParkourCoinsHud()
    {
        if (SceneManager.GetActiveScene().name != "Level01_By") return 0;
        Canvas c = FindHudCanvas();
        if (c == null) return 0;
        if (Object.FindFirstObjectByType<ParkourCoinsDisplay>(FindObjectsInactive.Include) != null) return 0;

        var go = new GameObject("ParkourCoinsDisplay");
        Undo.RegisterCreatedObjectUndo(go, "ParkourCoinsDisplay");
        go.transform.SetParent(c.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -62f);
        rt.sizeDelta = new Vector2(540f, 28f);
        go.AddComponent<ParkourCoinsDisplay>();
        Debug.Log("[Polish] ParkourCoinsDisplay lagt på HUD (Level01_By)");
        return 1;
    }

    private static int PositionTMP(Canvas canvas, params string[] keywords)
        => PositionTMP(canvas, keywords,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), Vector2.zero, new Vector2(200f, 40f));

    private static int PositionTMP(Canvas canvas, string kw1,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        => PositionTMP(canvas, new[] { kw1 }, anchorMin, anchorMax, pivot, pos, size);

    private static int PositionTMP(Canvas canvas, string kw1, string kw2,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        => PositionTMP(canvas, new[] { kw1, kw2 }, anchorMin, anchorMax, pivot, pos, size);

    private static int PositionTMP(Canvas canvas, string kw1, string kw2, string kw3,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        => PositionTMP(canvas, new[] { kw1, kw2, kw3 }, anchorMin, anchorMax, pivot, pos, size);

    private static int PositionTMP(Canvas canvas, string[] keywords,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        foreach (var tmp in canvas.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
        {
            string lower = tmp.gameObject.name.ToLowerInvariant();
            if (!keywords.Any(k => lower.Contains(k))) continue;

            var rt = tmp.rectTransform;
            Undo.RecordObject(rt, "Position HUD TMP");
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            EditorUtility.SetDirty(rt);
            return 1;
        }
        return 0;
    }

    // City level must not auto-load the beach when the last wave ends — use ZoneTrigger + missions.
    private static int FixCityZombieSpawnerAutoload()
    {
        if (SceneManager.GetActiveScene().name != "Level01_By") return 0;

        int n = 0;
        foreach (ZombieSpawner sp in Object.FindObjectsByType<ZombieSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sp == null) continue;
            var so = new SerializedObject(sp);
            SerializedProperty p = so.FindProperty("loadNextSceneWhenAllWavesComplete");
            if (p == null || !p.boolValue) continue;
            p.boolValue = false;
            so.ApplyModifiedProperties();
            n++;
            Debug.Log($"[Polish] Disabled auto scene advance on {sp.name}");
        }

        return n;
    }

    private static int EnsureZombieSpawnerHordeDefaults()
    {
        int changed = 0;
        foreach (ZombieSpawner sp in Object.FindObjectsByType<ZombieSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sp == null) continue;
            var so = new SerializedObject(sp);
            bool dirty = false;

            void SetBool(string f, bool v)
            { var p = so.FindProperty(f); if (p != null && p.boolValue != v) { p.boolValue = v; dirty = true; } }
            void SetFloat(string f, float min, float v)
            { var p = so.FindProperty(f); if (p != null && p.floatValue < min) { p.floatValue = v; dirty = true; } }
            void SetInt(string f, int min, int v)
            { var p = so.FindProperty(f); if (p != null && p.intValue < min) { p.intValue = v; dirty = true; } }

            SetBool("repeatWavesForever", true);
            SetFloat("randomSpawnScatterRadius",    18f, 28f);
            SetFloat("wideMapScatterChance",        0.5f, 0.65f);  // 65 % sjanse for spredt spawn
            SetFloat("wideMapScatterSampleRadius",  60f, 110f);    // bred spredning over hele kartet
            SetFloat("topUpInterval",                0f, 2.8f);    // regelmessig «top-up»
            SetInt  ("minimumZombiesAlive",         10, 14);       // alltid minst 14 zombier

            if (dirty) { so.ApplyModifiedProperties(); changed++; }
        }
        return changed > 0 ? 1 : 0;
    }

    // ──────────────────── DISABLE PLAYER SHOOTING AT START ───────────────────
    [MenuItem("CartoonZombies/Fix/Disable PlayerShooting at Start (needs WeaponPickup)", false, 35)]
    public static void DisablePlayerShootingMenu() => Debug.Log($"[Polish] PlayerShooting: {DisablePlayerShootingAtStart()}");

    private static int DisablePlayerShootingAtStart()
    {
        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo == null) { Debug.LogWarning("[Polish] Ingen Player-tag funnet"); return 0; }

        PlayerShooting shooting = playerGo.GetComponentInChildren<PlayerShooting>(true);
        if (shooting == null) { Debug.LogWarning("[Polish] PlayerShooting ikke funnet på Player"); return 0; }

        if (!shooting.enabled) return 0; // allerede disablet

        Undo.RecordObject(shooting, "Disable PlayerShooting");
        shooting.enabled = false;
        EditorUtility.SetDirty(shooting);
        Debug.Log("[Polish] PlayerShooting deaktivert — spilleren starter uten pistol (aktiveres av WeaponPickup)");
        return 1;
    }

    // ──────────────────── WEAPON PICKUP PLACEMENT ────────────────────────────
    [MenuItem("CartoonZombies/Fix/Place WeaponPickup (Mission 1 — find pistol)", false, 36)]
    public static void PlaceWeaponPickupMenu() => Debug.Log($"[Polish] WeaponPickup: {PlaceWeaponPickupIfMissing()}");

    private static int PlaceWeaponPickupIfMissing()
    {
        // Allerede i scenen?
        if (Object.FindFirstObjectByType<WeaponPickup>() != null)
        {
            Debug.Log("[Polish] WeaponPickup finnes allerede i scenen");
            return 0;
        }

        // Prøv å bruke eksisterende M1911 / Pistol-mesh
        string[] pistolNames = { "M1911", "Pistol", "Gun", "WeaponPickup", "Weapon" };
        foreach (string n in pistolNames)
        {
            GameObject existing = GameObject.Find(n);
            if (existing != null && existing.GetComponent<WeaponPickup>() == null)
            {
                // RequireComponent(Collider) — legg til brukbar kollider FØR WeaponPickup
                PrepareGameObjectForWeaponPickup(existing);
                Undo.AddComponent<WeaponPickup>(existing);
                EditorUtility.SetDirty(existing);
                Debug.Log($"[Polish] WeaponPickup lagt til på eksisterende {existing.name}");
                return 1;
            }
        }

        // Lag enkel kubus-pickup nær Player spawn
        Vector3 spawnPos = Vector3.zero;
        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
            spawnPos = playerGo.transform.position + playerGo.transform.forward * 22f + Vector3.up * 3.4f;
        else
            spawnPos = new Vector3(0f, 2f, 12f);

        // Snap til bakken — ekstra høyde så bob-animasjon ikke klipper under mesh
        if (TrySnapToGround(spawnPos, out Vector3 groundPos))
            spawnPos = groundPos + Vector3.up * 2.15f;
        else
            spawnPos += Vector3.up * 0.6f;

        GameObject pickupGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(pickupGo, "Create WeaponPickup");
        pickupGo.name = "WeaponPickup_Pistol";
        pickupGo.transform.position = spawnPos;
        pickupGo.transform.localScale = new Vector3(0.25f, 0.5f, 0.12f);

        // Gull-farge
        Renderer rend = pickupGo.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color    = new Color(1f, 0.82f, 0.1f);
            rend.material = mat;
        }

        // Gjør til trigger og legg til script
        Collider col = pickupGo.GetComponent<Collider>();
        if (col != null) { Undo.RecordObject(col, "isTrigger"); col.isTrigger = true; }

        Undo.AddComponent<WeaponPickup>(pickupGo);
        Debug.Log($"[Polish] WeaponPickup_Pistol opprettet på {spawnPos} — Marker M1911-meshen som child av dette objektet for riktig utseende");
        return 1;
    }

    // ──────────────────── HUD WIRING ─────────────────────────────────────────
    [MenuItem("CartoonZombies/Fix/Fix HUD Wiring (wire TMP to HUDController)", false, 45)]
    public static void FixHUDWiringMenu() => Debug.Log($"[Polish] HUD wiring: {FixHUDWiring()}");

    private static int FixHUDWiring()
    {
        HUDController hud = Object.FindFirstObjectByType<HUDController>(FindObjectsInactive.Include);
        if (hud == null) { Debug.LogWarning("[Polish] HUDController ikke funnet i scenen"); return 0; }

        Canvas hudCanvas = FindHudCanvas();
        if (hudCanvas == null) { Debug.LogWarning("[Polish] HUD Canvas ikke funnet"); return 0; }

        var so = new SerializedObject(hud);

        // Koble tekst-felter — søker etter TMP-objekter med matchende navn i HUD-canvas
        WireTMP(so, "healthText",  hudCanvas, "health", "hp");
        WireTMP(so, "ammoText",    hudCanvas, "ammo");
        WireTMP(so, "reloadText",  hudCanvas, "reload");
        WireTMP(so, "killsText",   hudCanvas, "kill");
        WireTMP(so, "waveText",    hudCanvas, "wave", "bølge", "bolge");

        bool changed = so.ApplyModifiedProperties();
        if (changed)
        {
            EditorUtility.SetDirty(hud);
            Debug.Log("[Polish] HUDController tekst-felt koblet");
            return 1;
        }

        Debug.Log("[Polish] HUDController: alle felt allerede koblet, eller tekst-objekter ikke funnet");
        return 0;
    }

    private static void WireTMP(SerializedObject so, string propName, Canvas canvas, params string[] keywords)
    {
        var prop = so.FindProperty(propName);
        if (prop == null || prop.objectReferenceValue != null) return;

        foreach (var tmp in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            string lower = tmp.gameObject.name.ToLower();
            if (keywords.Any(k => lower.Contains(k)))
            {
                prop.objectReferenceValue = tmp;
                Debug.Log($"[Polish] {propName} → {tmp.gameObject.name}");
                return;
            }
        }
    }

    // ──────────────────── NAVMESH BAKE ───────────────────────────────────────
    [MenuItem("CartoonZombies/Fix/Bake NavMesh (active scene)", false, 40)]
    public static void BakeNavMeshMenu() => Debug.Log($"[Polish] NavMesh: {BakeNavMesh()}");

    private static int BakeNavMesh()
    {
        string sn = SceneManager.GetActiveScene().name;
        if (sn != "Level01_By" && sn != "Level02_StrandSkog") return 0;

        NavMeshSurface surf = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surf == null)
        {
            var go = new GameObject("NavMeshWorldBake");
            surf = go.AddComponent<NavMeshSurface>();
            Undo.RegisterCreatedObjectUndo(go, "NavMeshSurface");
        }
        surf.collectObjects = CollectObjects.All;
        surf.BuildNavMesh();
        Debug.Log($"[Polish] NavMesh bakt for {sn}");
        return 1;
    }

    // ──────────────────── MISSION MANAGER SETUP ──────────────────────────────
    [MenuItem("CartoonZombies/Fix/Setup MissionManager (active scene)", false, 50)]
    public static void SetupMissionManagerMenu() => Debug.Log($"[Polish] MissionManager: {SetupMissionManagerInScene()}");

    private static int SetupMissionManagerInScene()
    {
        string sn = SceneManager.GetActiveScene().name;

        // Finn eller lag GameplaySystems root
        GameObject gps = GameObject.Find("GameplaySystems");
        if (gps == null) gps = GameObject.Find("GameSystem");
        if (gps == null) gps = GameObject.Find("Systems");
        if (gps == null)
        {
            gps = new GameObject("GameplaySystems");
            Undo.RegisterCreatedObjectUndo(gps, "Create GameplaySystems");
            Debug.Log("[Polish] Opprettet GameplaySystems GameObject");
        }

        MissionManager mm = Object.FindFirstObjectByType<MissionManager>();
        if (mm == null)
        {
            mm = Undo.AddComponent<MissionManager>(gps);
        }
        if (mm == null) return 0;

        // Koble EnemyCompassHUD
        EnemyCompassHUD compass = Object.FindFirstObjectByType<EnemyCompassHUD>();
        if (compass != null)
        {
            var so = new SerializedObject(mm);
            so.FindProperty("compassHud").objectReferenceValue = compass;
            so.ApplyModifiedProperties();
        }

        // Lag / finn mission-panel i HUD Canvas
        Canvas hudCanvas = FindHudCanvas();
        if (hudCanvas != null)
        {
            var missionUI = EnsureMissionUI(hudCanvas);
            var so = new SerializedObject(mm);
            if (missionUI.text  != null) so.FindProperty("missionText").objectReferenceValue  = missionUI.text;
            if (missionUI.panel != null) so.FindProperty("missionPanel").objectReferenceValue = missionUI.panel;
            so.ApplyModifiedProperties();
        }

        // Mål-transforms i scenen
        Transform carTarget    = FindTransformByName("DrivableCar", "Car", "CarVisual");
        Transform exitTarget   = FindTransformByName("ZoneTrigger", "Exit", "LevelExit");
        Transform boatTarget   = FindTransformByName("BoatUnlockSystem", "Boat", "boat");
        Transform chestTarget  = FindTransformByName("IslandWinTrigger", "Chest", "chest");
        Transform weaponTarget = FindTransformByName("WeaponPickup_Pistol", "M1911", "Pistol", "WeaponPickup");

        var mso = new SerializedObject(mm);
        var missionsProp = mso.FindProperty("missions");

        if (sn == "Level01_By")
        {
            SetMissions(missionsProp, new[]
            {
                new MissionData
                {
                    text    = "Finn og plukk opp pistolen\n<size=80%>Følg den oransje kompass-pilen</size>",
                    trigger = 0,
                    target  = weaponTarget
                },
                new MissionData
                {
                    text    = "Drep 12 zombier\n<size=80%>Bølgene fortsetter — oppdraget fullføres etter <b>12 drap</b></size>",
                    trigger = 4,
                    target  = null,
                    killCount = 12
                },
                new MissionData
                {
                    text    = "Finn den kjørbare bilen\n<size=80%>Stå nær bilen og trykk F for å sette deg inn</size>",
                    trigger = 0,
                    target  = carTarget
                },
                new MissionData
                {
                    text    = "Fullfør begge parkour-sonene\n<size=80%>Saml alle myntene i begge soner</size>",
                    trigger = 2,
                    target  = null
                },
                new MissionData
                {
                    text    = "Gå gjennom utgangen!\n<size=80%>Kjør eller løp til den grønne portalen</size>",
                    trigger = 0,
                    target  = exitTarget
                },
            });
        }
        else if (sn == "Level02_StrandSkog")
        {
            SetMissions(missionsProp, new[]
            {
                new MissionData
                {
                    text    = "Finn pistolen\n<size=80%>Følg den oransje kompass-pilen</size>",
                    trigger = 0,
                    target  = weaponTarget
                },
                new MissionData
                {
                    text    = "Drep 14 zombier\n<size=80%>Hordesne fortsetter — fullføres etter <b>14 drap</b>, deretter til båten</size>",
                    trigger = 4,
                    target  = null,
                    killCount = 14
                },
                new MissionData
                {
                    text    = "Gå til båten!\n<size=80%>Følg pilen etter at den er låst opp</size>",
                    trigger = 3,
                    target  = boatTarget
                },
                new MissionData
                {
                    text    = "Ro til øya og åpne kisten\n<size=80%>Gå inn i den gylne kisten for seier!</size>",
                    trigger = 0,
                    target  = chestTarget
                },
            });
        }

        mso.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(mm);
        return 1;
    }

    // ──────────────────── DUPLICATE ENVIRONMENT GROUPS ──────────────────────
    [MenuItem("CartoonZombies/Fix/Merge Duplicate Environment Groups", false, 60)]
    public static void MergeDuplicateEnvironmentGroupsMenu() =>
        Debug.Log($"[Polish] Merged env groups: {MergeDuplicateEnvironmentGroups()}");

    /// <summary>
    /// Cursor lager engelske rot-grupper (Roads, Nature, Buildings, …) ved siden av de
    /// norske under EnvironmentArt (Veier, Natur, Bygninger, …).
    /// Denne metoden flytter barn fra duplikatgruppen til den kanoniske og sletter duplikaten.
    /// </summary>
    private static int MergeDuplicateEnvironmentGroups()
    {
        int count = 0;

        // Norsk → mulige duplikatnavn (Cursor-genererte engelske + andre varianter)
        var mergeMap = new Dictionary<string, string[]>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Veier",       new[] { "Roads", "Road", "Streets", "Gater" } },
            { "Natur",       new[] { "Nature", "Natural", "Foliage", "Trees", "Vegetation" } },
            { "Bygninger",   new[] { "Buildings", "Building", "Structures", "Bygg" } },
            { "Kjoretoy",    new[] { "Vehicles", "Vehicle", "Cars", "Kjøretøy" } },
            { "Rekvisitter", new[] { "Props", "Prop", "Decor", "Dekor" } },
            { "Vann",        new[] { "Water", "Ocean", "Sea", "Hav" } },
            { "Diverse",     new[] { "Misc", "Miscellaneous", "Other", "Andre" } },
        };

        // Finn EnvironmentArt-parent
        GameObject envArt = GameObject.Find("EnvironmentArt");

        foreach (var kv in mergeMap)
        {
            // Finn kanonisk gruppe (norsk navn)
            GameObject canonical = null;
            if (envArt != null)
            {
                Transform t = envArt.transform.Find(kv.Key);
                if (t != null) canonical = t.gameObject;
            }
            if (canonical == null) canonical = GameObject.Find(kv.Key);
            if (canonical == null) continue;

            // Finn og merge alle duplikatgrupper
            foreach (string dupName in kv.Value)
            {
                // Søk etter alle GameObjects med dette navnet i scenen
                foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                    MergeGroupRecursive(root, dupName, canonical, ref count);
            }
        }

        if (count > 0)
            Debug.Log($"[Polish] Merget {count} duplikat-grupper under EnvironmentArt");
        return count;
    }

    private static void MergeGroupRecursive(GameObject searchRoot, string dupName, GameObject canonical, ref int count)
    {
        if (searchRoot == canonical) return;
        if (searchRoot.name.Equals(dupName, System.StringComparison.OrdinalIgnoreCase)
            && searchRoot != canonical)
        {
            // Flytt alle barn til canonical
            var children = new List<Transform>();
            foreach (Transform child in searchRoot.transform)
                children.Add(child);

            foreach (Transform child in children)
            {
                Undo.SetTransformParent(child, canonical.transform, $"Merge {dupName}→{canonical.name}");
            }

            // Slett tom duplikat
            Undo.DestroyObjectImmediate(searchRoot);
            count++;
            return;
        }

        // Søk nedover i hierarkiet
        var toSearch = new List<Transform>();
        foreach (Transform child in searchRoot.transform)
            toSearch.Add(child);
        foreach (Transform child in toSearch)
            if (child != null)
                MergeGroupRecursive(child.gameObject, dupName, canonical, ref count);
    }

    // ──────────────────── DUPLICATE CAMERAS ──────────────────────────────────
    [MenuItem("CartoonZombies/Fix/Remove Duplicate Cameras", false, 62)]
    public static void RemoveDuplicateCamerasMenu() =>
        Debug.Log($"[Polish] Kameras fjernet: {RemoveDuplicateCameras()}");

    private static int RemoveDuplicateCameras()
    {
        int count = 0;
        Scene active = SceneManager.GetActiveScene();

        // Finn alle kameraer i aktiv scene
        var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(c => c != null && c.gameObject.scene == active)
            .ToList();

        if (cameras.Count <= 1) return 0;

        // Behold kameraet med CameraFollow (hoved-spillkamera)
        Camera keepCam = cameras.FirstOrDefault(c => c.GetComponent<CameraFollow>() != null);

        // Fallback: behold det som er tagget MainCamera
        if (keepCam == null)
            keepCam = cameras.FirstOrDefault(c => c.CompareTag("MainCamera"));

        // Fallback: behold det første
        if (keepCam == null)
            keepCam = cameras[0];

        // Tag det vi beholder som MainCamera
        if (!keepCam.CompareTag("MainCamera"))
        {
            Undo.RecordObject(keepCam.gameObject, "Set MainCamera tag");
            keepCam.tag = "MainCamera";
        }

        // Fjern alle duplikatkameraer
        foreach (Camera cam in cameras)
        {
            if (cam == keepCam) continue;

            // Ikke slett CameraFollow kameraer — de er nødvendige
            if (cam.GetComponent<CameraFollow>() != null) continue;

            Debug.Log($"[Polish] Sletter duplikat-kamera: {cam.gameObject.name}");
            Undo.DestroyObjectImmediate(cam.gameObject);
            count++;
        }

        return count;
    }

    // ──────────────────── UNFIX MISTAKE TRIGGERS ─────────────────────────────
    [MenuItem("CartoonZombies/Fix/Unfix Mistake Triggers (restore walkable props)", false, 63)]
    public static void UnfixMistakeTriggersMenu() =>
        Debug.Log($"[Polish] Gjenopprettet: {UnfixMistakeTriggers()}");

    /// <summary>
    /// Eldre FIX EVERYTHING-kjøringer satte sm_-prefiks-objekter som triggers (inkl. rør, ramper,
    /// gresstiles, slide osv.). Denne metoden angrer det for alle kjente «walk-on»-objekter.
    /// </summary>
    private static int UnfixMistakeTriggers()
    {
        // Alt som inneholder disse ordene i navn ELLER foreldrenavn skal være solid
        var makeSolidKeywords = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "pipe", "ramp", "slide", "platform", "tile", "flat",
            "escarpment", "mast", "stairs", "step", "ledge", "walkway",
            "catwalk", "floor", "ground", "terrain", "bridge",
            "block_play", "block_question", "lollipop",
            // Synty-spesifikke navnemønstre for terreng/etasjer
            "natures_grass", "grass_tile", "grass_flat", "grass_bar",
            "natures_house_flo", "natures_cube", "house_floor",
            "terrain_grass", "terrain_mountain", "terrain_escarpment",
            "prop_bridge", "prop_pipe", "prop_slide", "prop_block",
        };

        int count = 0;
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (MeshCollider mc in root.GetComponentsInChildren<MeshCollider>(true))
            {
                if (mc == null) continue;
                if (!mc.isTrigger && !mc.convex) continue; // ingenting å angre

                if (!ShouldBeSolid(mc.gameObject, makeSolidKeywords)) continue;

                bool changed = false;
                if (mc.isTrigger)
                {
                    Undo.RecordObject(mc, "Unfixed trigger");
                    mc.isTrigger = false;
                    changed = true;
                }
                // Fjern convex på statiske objekter — de trenger det ikke og gir PhysX-feil
                bool hasRigidbody = mc.GetComponentInParent<Rigidbody>() != null;
                if (mc.convex && !hasRigidbody)
                {
                    Undo.RecordObject(mc, "Unfixed convex");
                    mc.convex = false;
                    changed = true;
                }
                if (changed)
                {
                    count++;
                    Debug.Log($"[Polish] Gjenopprettet solid: {mc.gameObject.name}");
                }
            }

            foreach (BoxCollider bc in root.GetComponentsInChildren<BoxCollider>(true))
            {
                if (bc == null || !bc.isTrigger) continue;
                if (!ShouldBeSolid(bc.gameObject, makeSolidKeywords)) continue;
                Undo.RecordObject(bc, "Unfixed box trigger");
                bc.isTrigger = false;
                count++;
                Debug.Log($"[Polish] Gjenopprettet solid (box): {bc.gameObject.name}");
            }
        }

        if (count > 0)
            Debug.Log($"[Polish] {count} objekter gjenopprettet til solid kollisjon (angret feilaktige triggers)");
        return count;
    }

    private static bool ShouldBeSolid(GameObject go, HashSet<string> keywords)
    {
        // Sjekk objektets navn
        string goName = go.name.ToLowerInvariant();
        if (keywords.Any(k => goName.Contains(k))) return true;

        // Sjekk foreldre-hierarki
        Transform t = go.transform.parent;
        while (t != null)
        {
            string lower = t.name.ToLowerInvariant();
            if (keywords.Any(k => lower.Contains(k))) return true;
            t = t.parent;
        }
        return false;
    }

    // ──────────────────── HELPERS ─────────────────────────────────────────────
    private struct MissionData
    {
        public string    text;
        public int       trigger;
        public Transform target;
        public int       killCount;
    }

    private static void SetMissions(SerializedProperty prop, MissionData[] data)
    {
        prop.arraySize = data.Length;
        for (int i = 0; i < data.Length; i++)
        {
            var elem = prop.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("descriptionText").stringValue     = data[i].text;
            elem.FindPropertyRelative("trigger").enumValueIndex          = data[i].trigger;
            elem.FindPropertyRelative("arrowTarget").objectReferenceValue = data[i].target;
            int kc = data[i].killCount > 0 ? data[i].killCount : 12;
            elem.FindPropertyRelative("killCountRequired").intValue =
                data[i].trigger == 4 ? Mathf.Max(1, kc) : 12;
        }
    }

    private struct MissionUIResult
    {
        public TextMeshProUGUI text;
        public Image           panel;
    }

    private static MissionUIResult EnsureMissionUI(Canvas canvas)
    {
        var result = new MissionUIResult();

        Transform existing = canvas.transform.Find("MissionPanel");
        if (existing != null)
        {
            result.panel = existing.GetComponent<Image>();
            result.text  = existing.GetComponentInChildren<TextMeshProUGUI>();
            return result;
        }

        var panelGo = new GameObject("MissionPanel");
        Undo.RegisterCreatedObjectUndo(panelGo, "Create MissionPanel");
        panelGo.transform.SetParent(canvas.transform, false);

        var panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 0.55f);
        panelRt.anchorMax = new Vector2(0.45f, 0.78f);
        panelRt.offsetMin = new Vector2(12, 0);
        panelRt.offsetMax = new Vector2(-12, 0);

        var img = panelGo.AddComponent<Image>();
        img.color    = new Color(0.04f, 0.06f, 0.10f, 0.82f);
        result.panel = img;

        var textGo = new GameObject("MissionText");
        Undo.RegisterCreatedObjectUndo(textGo, "Create MissionText");
        textGo.transform.SetParent(panelGo.transform, false);

        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10, 6);
        textRt.offsetMax = new Vector2(-10, -6);

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize           = 18;
        tmp.color              = Color.white;
        tmp.richText           = true;
        tmp.textWrappingMode   = TextWrappingModes.Normal;
        tmp.alignment          = TextAlignmentOptions.TopLeft;
        tmp.outlineWidth       = 0.2f;
        tmp.outlineColor       = new Color32(0, 0, 0, 180);
        result.text = tmp;

        return result;
    }

    private static Canvas FindHudCanvas()
    {
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.gameObject.name.IndexOf("HUD", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                c.gameObject.name.IndexOf("Hud", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return c;
        return null;
    }

    private static Transform FindTransformByName(params string[] names)
    {
        foreach (string n in names)
        {
            GameObject go = GameObject.Find(n);
            if (go != null) return go.transform;
        }
        return null;
    }

    private static bool TrySnapToGround(Vector3 pos, out Vector3 result)
    {
        result = pos;
        Vector3 origin = new Vector3(pos.x, pos.y + 500f, pos.z);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1500f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            result = hit.point;
            return true;
        }
        return false;
    }

    private static void EnsureTriggerCollider(GameObject go, float radius = 1f)
    {
        PrepareGameObjectForWeaponPickup(go);
        var sc = go.GetComponent<SphereCollider>();
        if (sc != null && sc.radius < radius * 0.5f)
        {
            Undo.RecordObject(sc, "Pickup sphere radius");
            sc.radius = radius;
        }
    }

    /// <summary>
    /// Sikrer at <paramref name="go"/> har minst én kollider som kan brukes som trigger
    /// (for <see cref="WeaponPickup"/> / RequireComponent). Konkav MeshCollider brukes ikke;
    /// i så fall legges det til en <see cref="SphereCollider"/>.
    /// </summary>
    private static void PrepareGameObjectForWeaponPickup(GameObject go)
    {
        foreach (var c in go.GetComponents<Collider>())
        {
            if (!c.enabled) continue;
            if (c is MeshCollider mc && !mc.convex) continue;
            Undo.RecordObject(c, "Weapon pickup trigger");
            c.isTrigger = true;
            EditorUtility.SetDirty(c);
            return;
        }

        if (go.GetComponent<SphereCollider>() != null) return;

        var sc = Undo.AddComponent<SphereCollider>(go);
        sc.isTrigger = true;
        sc.radius = 0.45f;
        var rend = go.GetComponent<Renderer>() ?? go.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Bounds b = rend.bounds;
            sc.center = go.transform.InverseTransformPoint(b.center);
            float r = Mathf.Max(b.extents.x, b.extents.y, b.extents.z);
            sc.radius = Mathf.Clamp(r * 0.75f, 0.2f, 1.5f);
        }

        EditorUtility.SetDirty(sc);
    }

    /// <summary>Mynter: ikke bruk weapon-pickup-helper (den kan ødelegge mesh-kollisjon). Kun trigger + ev. sfære.</summary>
    private static void PrepareGameObjectForCoin(GameObject go)
    {
        var cols = go.GetComponents<Collider>();
        if (cols == null || cols.Length == 0)
        {
            var sc = Undo.AddComponent<SphereCollider>(go);
            sc.isTrigger = true;
            sc.radius = 0.65f;
            EditorUtility.SetDirty(sc);
            return;
        }

        bool anyTrigger = false;
        foreach (var c in cols)
        {
            if (c == null) continue;
            if (c is MeshCollider mc && !mc.convex)
            {
                Undo.RecordObject(mc, "Coin disable concave MeshCollider");
                mc.enabled = false;
                EditorUtility.SetDirty(mc);
                continue;
            }
            Undo.RecordObject(c, "Coin trigger");
            c.enabled = true;
            c.isTrigger = true;
            EditorUtility.SetDirty(c);
            anyTrigger = true;
        }

        if (!anyTrigger)
        {
            var sc = Undo.AddComponent<SphereCollider>(go);
            sc.isTrigger = true;
            sc.radius = 0.65f;
            EditorUtility.SetDirty(sc);
        }
    }
}
#endif
