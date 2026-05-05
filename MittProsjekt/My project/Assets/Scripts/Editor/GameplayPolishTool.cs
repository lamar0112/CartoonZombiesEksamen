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

// ──────────────────────────────────────────────────────────────────────────────
// CartoonZombies → Fix → Fix Everything
// Spawn, NavMesh, vegetation, car, missions, HUD for the active scene.
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
        HierarchyLevelCleanupTool.OrganizeRoots(SceneManager.GetActiveScene());
        fixes++;

        fixes += FixSpawnPoints();
        fixes += FixVegetationColliders();
        fixes += FixCarConvex();
        fixes += FixCarSetup();
        fixes += DisablePlayerShootingAtStart();
        fixes += PlaceWeaponPickupIfMissing();
        fixes += FixCityZombieSpawnerAutoload();
        fixes += EnsureZombieSpawnerHordeDefaults();
        fixes += SetupMissionManagerInScene();
        fixes += FixHUDWiring();
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
        var vegWords = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "bush", "shrub", "grass", "weed", "fern", "plant", "flower", "hedge",
            "undergrowth", "foliage", "gress", "busk", "ugress",
            "palm", "pine", "birch", "oak", "tree", "leaf", "leaves",
            "branch", "vegetation", "natur", "frond",
            // Vanlige asset-prefiks (Synty/Kenney m.fl.)
            "sm_", "sm-", "forest", "jungle", "canopy", "sapling",
            "stump", "ivy", "moss", "reed", "rush", "kelp", "seaweed", "dune", "crop",
            "_veg", "nature_", "foliage_", "tree_", "-tree", "_tree", "trees"
        };

        // Ord som ALDRI skal gjøres til trigger
        var neverTrigger = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "wall", "floor", "road", "building", "roof", "ground", "terrain",
            "platform", "collider", "barrier", "fence", "water", "vann",
            "ocean", "sea", "player", "zombie", "car", "vehicle",
            "rock", "boulder", "stone", "cliff", "bridge", "concrete", "brick", "window", "glass",
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

                // Unity: trigger krever konveks MeshCollider — ikke sett isTrigger på konkav mesh
                if (!mc.convex)
                {
                    Undo.RecordObject(mc, "Vegetation MeshCollider convex");
                    mc.convex = true;
                }

                if (!mc.convex)
                {
                    Debug.LogWarning($"[Polish] Skip vegetation trigger (mesh stays concave): {mc.gameObject.name}");
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
        string[] hints =
        {
            "sm_", "veg", "grass", "tree", "bush", "palm", "fern", "flower", "plant",
            "leaf", "moss", "ivy", "stump", "log", "crop", "branch", "jungle", "forest", "reed", "foliage"
        };

        foreach (MeshCollider mc in env.GetComponentsInChildren<MeshCollider>(true))
        {
            if (mc == null || mc.isTrigger) continue;
            if (mc.GetComponent<CharacterController>() != null) continue;

            string goName = mc.gameObject.name.ToLowerInvariant();
            if (neverTrigger.Any(w => goName.Contains(w))) continue;
            if (LooksLikePlaygroundOrSolidProp(goName)) continue;
            if (!hints.Any(h => goName.Contains(h))) continue;

            if (!mc.convex)
            {
                Undo.RecordObject(mc, "EnvArt MeshCollider convex");
                mc.convex = true;
            }

            if (!mc.convex) continue;

            Undo.RecordObject(mc, "EnvArt vegetation trigger");
            mc.isTrigger = true;
            count++;
            Debug.Log($"[Polish] Trigger (env art): {mc.gameObject.name}");
        }

        foreach (BoxCollider bc in env.GetComponentsInChildren<BoxCollider>(true))
        {
            if (bc == null || bc.isTrigger) continue;
            string goName = bc.gameObject.name.ToLowerInvariant();
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
        foreach (Rigidbody rb in Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
        {
            foreach (MeshCollider mc in rb.GetComponentsInChildren<MeshCollider>(true))
            {
                if (mc.convex) continue;
                Undo.RecordObject(mc, "Car MeshCollider Convex");
                mc.convex = true;
                count++;
            }
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

        string[] names =
        {
            "DrivableCar", "Car", "CarRoot", "Vehicle", "CarVisual", "PlayerCar", "Sedan", "Truck",
            "PoliceCar", "SportCar", "Veh_", "CityCar", "Kenney_Car", "Car_"
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
            SerializedProperty rep = so.FindProperty("repeatWavesForever");
            if (rep != null && !rep.boolValue) { rep.boolValue = true; dirty = true; }
            SerializedProperty rad = so.FindProperty("randomSpawnScatterRadius");
            if (rad != null && rad.floatValue < 18f) { rad.floatValue = 26f; dirty = true; }
            SerializedProperty wch = so.FindProperty("wideMapScatterChance");
            if (wch != null && wch.floatValue < 0.25f) { wch.floatValue = 0.42f; dirty = true; }
            SerializedProperty wsr = so.FindProperty("wideMapScatterSampleRadius");
            if (wsr != null && wsr.floatValue < 40f) { wsr.floatValue = 88f; dirty = true; }
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
                    text    = "Find and pick up the pistol\n<size=80%>Follow the orange compass arrow</size>",
                    trigger = 0,
                    target  = weaponTarget
                },
                new MissionData
                {
                    text    = "Eliminate 12 zombies\n<size=80%>Waves keep looping — this step completes after <b>12 kills</b>.</size>",
                    trigger = 4,
                    target  = null,
                    killCount = 12
                },
                new MissionData
                {
                    text    = "Reach the drivable car\n<size=80%>Stand nearby and press F to enter</size>",
                    trigger = 0,
                    target  = carTarget
                },
                new MissionData
                {
                    text    = "Complete both parkour zones\n<size=80%>Collect the coins in each zone</size>",
                    trigger = 2,
                    target  = null
                },
                new MissionData
                {
                    text    = "Use the exit portal\n<size=80%>Drive or run to the green marker</size>",
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
                    text    = "Find the pistol\n<size=80%>Follow the orange compass arrow</size>",
                    trigger = 0,
                    target  = weaponTarget
                },
                new MissionData
                {
                    text    = "Eliminate 14 zombies\n<size=80%>Horde keeps coming — complete after <b>14 kills</b>, then head for the boat.</size>",
                    trigger = 4,
                    target  = null,
                    killCount = 14
                },
                new MissionData
                {
                    text    = "Reach the boat\n<size=80%>Follow the arrow once it unlocks</size>",
                    trigger = 3,
                    target  = boatTarget
                },
                new MissionData
                {
                    text    = "Sail to the island and open the chest\n<size=80%>Interact with the gold chest to win</size>",
                    trigger = 0,
                    target  = chestTarget
                },
            });
        }

        mso.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(mm);
        return 1;
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
}
#endif
