using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine.AI;

// Procedural by-kart (Kenney + Synty + …). FARE: sletter kartrot + Environment i aktiv scene.
// Bruk kun på tom scene eller med Git-backup. Spillscenen heter nå Level01_By.
// Nytt kart får roten «CityMap» (se LevelMapRootResolver).
public static class Zone2CityMapBuilder
{
    // ── Ressursstier ──────────────────────────────────────────────────────────
    private const string Roads    = "Assets/ThirdParty/Kenney/CityRoads/";
    private const string Comm     = "Assets/ThirdParty/Kenney/CityCommercial/";
    private const string Burb     = "Assets/ThirdParty/Kenney/CitySuburban/";
    private const string VStatic  = "Assets/ThirdParty/SimplePoly City - Low Poly Assets/Prefab/Vehicles/Vehicle with Static Wheels/";
    private const string Ithappy  = "Assets/ThirdParty/ithappy/Creative_Characters_FREE/Prefabs/";

    // Kenney CityRoads tile er 2 m — skalert ×2 → 4 m per felt i Unity-scenen
    private const float TileSize = 4f;
    private const int   Grid     = 7;   // 7×7 → 28 m × 28 m veikryss-rutenett

    [MenuItem("CartoonZombies/Level Art/⚠ Rebuild procedural CITY map (destructive)", false, 100)]
    public static void Rebuild()
    {
        if (!EditorUtility.DisplayDialog("FARE — procedural by",
            "Dette SLETTER «" + LevelMapRootResolver.ProceduralCityRootName + "» / «CityMap_Zone2» (hvis de finnes) og «Environment».\n\n" +
            "Ikke kjør på ferdig bygget Level01_By uten backup.\n\nFortsette?",
            "Ja, bygg!", "Avbryt"))
            return;

        EnsureMaterialsFolder();
        FixFloorToAsphalt();

        DestroyIfExists(LevelMapRootResolver.ProceduralCityRootName);
        DestroyIfExists("CityMap_Zone2");
        DestroyIfExists("Environment");

        GameObject env   = new GameObject("Environment");
        GameObject mapGo = new GameObject(LevelMapRootResolver.ProceduralCityRootName);
        mapGo.transform.SetParent(env.transform, false);
        Transform map = mapGo.transform;

        BuildRoadGrid(map);
        BuildCommercialDistrict(map);
        BuildSuburbanDistrict(map);
        PlaceParkedCars(map);
        BuildDrivableCar(map);
        SpawnCivilians(map);
        PlaceStreetProps(map);
        EnsureNavMeshSurface();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Procedural by",
            "By-kart bygget!\n\n" +
            "Neste steg:\n" +
            "1. CartoonZombies → Scenes → Re-Bake NavMesh (active scene)\n" +
            "2. Ctrl+S", "OK");
    }

    /// <summary>Legger kun inn DrivableCar — ødelegger ikke CityMap / Environment.</summary>
    [MenuItem("CartoonZombies/Level Art/Add DrivableCar only (safe — keeps your map)", false, 102)]
    public static void MenuAddDrivableCarOnly()
    {
        if (Application.isPlaying) return;

        if (GameObject.Find("DrivableCar") != null)
        {
            EditorUtility.DisplayDialog("DrivableCar",
                "Scenen har allerede et objekt som heter «DrivableCar». Slett eller gi nytt navn først.", "OK");
            return;
        }

        GameObject city = LevelMapRootResolver.FindCityMapRoot();
        Transform parent;
        if (city != null)
        {
            parent = city.transform;
        }
        else
        {
            if (!EditorUtility.DisplayDialog("By-kartrot ikke funnet",
                "Fant ikke noe by-kartrot (prøvde: " + LevelMapRootResolver.CityCandidatesHint + ").\n\n" +
                "Legge DrivableCar i scenens rot (tom forelder «GameplayVehicles»)?",
                "Ja", "Avbryt"))
                return;
            GameObject holder = new GameObject("GameplayVehicles");
            Undo.RegisterCreatedObjectUndo(holder, "GameplayVehicles");
            parent = holder.transform;
        }

        BuildDrivableCar(parent);
        Transform carT = parent.Find("DrivableCar");
        if (carT != null)
            Undo.RegisterCreatedObjectUndo(carT.gameObject, "DrivableCar");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("DrivableCar lagt inn",
            "DrivableCar er opprettet (samme oppsett som procedural-byggeren).\n\n" +
            "Flytt den i Scene-view om nødvendig, bake NavMesh om AI henger, lagre (Ctrl+S).", "OK");
    }

    [MenuItem("CartoonZombies/Level Art/Add DrivableCar only (safe — keeps your map)", true)]
    private static bool MenuAddDrivableCarOnlyValidate() => !Application.isPlaying;

    // ── 1. Veirutenett (7×7 Kenney CityRoads) ────────────────────────────────
    private static void BuildRoadGrid(Transform map)
    {
        GameObject root = Child(map, "Roads");
        Transform  r    = root.transform;

        float half = (Grid - 1) * TileSize * 0.5f;

        GameObject straight   = Fbx(Roads + "road-straight.fbx");
        GameObject crossroad  = Fbx(Roads + "road-crossroad.fbx");
        GameObject roundabout = Fbx(Roads + "road-roundabout.fbx");
        GameObject bend       = Fbx(Roads + "road-bend.fbx");

        int midIdx = Grid / 2;

        for (int row = 0; row < Grid; row++)
        {
            for (int col = 0; col < Grid; col++)
            {
                float x = col * TileSize - half;
                float z = row * TileSize - half;

                bool isCenter = row == midIdx && col == midIdx;
                bool isEdge   = row == 0 || row == Grid - 1 || col == 0 || col == Grid - 1;

                GameObject pfb;
                float yRot = 0f;

                if (isCenter && roundabout != null)
                {
                    pfb = roundabout;
                }
                else if (isEdge)
                {
                    pfb  = straight ?? crossroad;
                    yRot = (col == 0 || col == Grid - 1) ? 90f : 0f;
                }
                else
                {
                    pfb = crossroad ?? straight;
                }

                if (pfb == null) continue;
                GameObject tile = PlaceScaled(r, pfb, new Vector3(x, 0.01f, z), yRot, TileSize / 2f);
                if (tile != null) tile.name = $"road_{row}_{col}";
            }
        }

        // Radiale forlengelse-gater mot bygningene (N/S/Ø/V)
        if (straight != null)
        {
            for (int i = 1; i <= 4; i++)
            {
                float d = half + i * TileSize;
                PlaceScaled(r, straight, new Vector3(0f,  0.01f,  d), 0f,   TileSize / 2f);
                PlaceScaled(r, straight, new Vector3(0f,  0.01f, -d), 0f,   TileSize / 2f);
                PlaceScaled(r, straight, new Vector3( d,  0.01f,  0f), 90f, TileSize / 2f);
                PlaceScaled(r, straight, new Vector3(-d,  0.01f,  0f), 90f, TileSize / 2f);
            }
        }
    }

    // ── 2. Kommersiell distrikt — nord/øst: kontorer, skyskrapere ────────────
    private static void BuildCommercialDistrict(Transform map)
    {
        GameObject root = Child(map, "CommercialDistrict");
        Transform  d    = root.transform;

        float edge = (Grid - 1) * TileSize * 0.5f + TileSize * 2f;

        // Skyskrapere i bakgrunnen nord
        string[] scrapers = {
            "building-skyscraper-a.fbx", "building-skyscraper-b.fbx",
            "building-skyscraper-c.fbx", "building-skyscraper-d.fbx",
            "building-skyscraper-e.fbx",
        };
        for (int i = 0; i < scrapers.Length; i++)
            Building(d, Comm + scrapers[i], new Vector3((i - 2) * 10f, 0f, edge + 14f), 180f, 1.8f);

        // Kommersielle bygninger nord — rad 1 (nærmere vei)
        string[] northRow = {
            "building-a.fbx", "building-b.fbx", "building-c.fbx",
            "building-d.fbx", "building-e.fbx", "building-f.fbx",
        };
        for (int i = 0; i < northRow.Length; i++)
            Building(d, Comm + northRow[i], new Vector3((i - 2.5f) * 9f, 0f, edge + 5f), 180f, 1.3f);

        // Kommersielle bygninger øst
        string[] eastRow = {
            "building-g.fbx", "building-h.fbx", "building-i.fbx",
            "building-j.fbx", "building-k.fbx", "building-l.fbx",
        };
        for (int i = 0; i < eastRow.Length; i++)
            Building(d, Comm + eastRow[i], new Vector3(edge + 7f, 0f, (i - 2.5f) * 9f), 270f, 1.3f);

        // Lavere handelsbygg vest
        string[] westRow = {
            "building-m.fbx", "building-n.fbx",
            "low-detail-building-a.fbx", "low-detail-building-b.fbx",
            "low-detail-building-wide-a.fbx",
        };
        for (int i = 0; i < westRow.Length; i++)
            Building(d, Comm + westRow[i], new Vector3(-edge - 6f, 0f, (i - 2) * 10f), 90f, 1.2f);

        // Detaljer: parasoll, markise foran butikker
        Decor(d, Comm + "detail-parasol-a.fbx",   new Vector3(-edge - 3f, 0f,  8f), 90f);
        Decor(d, Comm + "detail-parasol-b.fbx",   new Vector3(-edge - 3f, 0f, -4f), 90f);
        Decor(d, Comm + "detail-awning.fbx",       new Vector3( edge + 4f, 0f, 10f), 270f);
        Decor(d, Comm + "detail-awning-wide.fbx",  new Vector3( edge + 4f, 0f, -6f), 270f);
        Decor(d, Comm + "detail-overhang.fbx",     new Vector3( edge + 4f, 0f,  2f), 270f);
    }

    // ── 3. Forstadsdistrikt — sør: hus, gjerder, stier, trær ────────────────
    private static void BuildSuburbanDistrict(Transform map)
    {
        GameObject root = Child(map, "SuburbanDistrict");
        Transform  s    = root.transform;

        float edge = (Grid - 1) * TileSize * 0.5f + TileSize * 2f;

        // Hustyper a–l, fordelt sørover i to rader
        string[] types = {
            "building-type-a.fbx", "building-type-b.fbx", "building-type-c.fbx",
            "building-type-d.fbx", "building-type-e.fbx", "building-type-f.fbx",
            "building-type-g.fbx", "building-type-h.fbx", "building-type-i.fbx",
            "building-type-j.fbx", "building-type-k.fbx", "building-type-l.fbx",
        };

        for (int i = 0; i < 5; i++)   // Rad 1 (nær vei)
            Building(s, Burb + types[i % types.Length], new Vector3((i - 2) * 12f, 0f, -edge - 5f), 0f, 1.0f);

        for (int i = 0; i < 4; i++)   // Rad 2 (bak)
            Building(s, Burb + types[(i + 5) % types.Length], new Vector3((i - 1.5f) * 14f, 0f, -edge - 16f), 0f, 1.0f);

        // Gjerder langs fortauskanten
        string[] fences = { "fence.fbx", "fence-1x2.fbx", "fence-1x3.fbx", "fence-low.fbx", "fence-2x2.fbx" };
        for (int i = 0; i < 6; i++)
            Decor(s, Burb + fences[i % fences.Length], new Vector3(-20f + i * 8f, 0f, -edge - 1.5f), 0f);

        // Innkjørsler og hageveier
        Decor(s, Burb + "driveway-short.fbx",       new Vector3(-12f, 0f, -edge - 2f), 0f);
        Decor(s, Burb + "driveway-long.fbx",         new Vector3(  0f, 0f, -edge - 2f), 0f);
        Decor(s, Burb + "path-stones-long.fbx",      new Vector3( 12f, 0f, -edge - 2f), 0f);
        Decor(s, Burb + "path-stones-messy.fbx",     new Vector3( -6f, 0f, -edge - 3f), 0f);
        Decor(s, Burb + "path-short.fbx",            new Vector3( 20f, 0f, -edge - 2f), 0f);

        // Plantekasser og trær
        Decor(s, Burb + "planter.fbx",    new Vector3(-20f, 0f, -edge - 4f), 0f);
        Decor(s, Burb + "planter.fbx",    new Vector3( 20f, 0f, -edge - 4f), 90f);
        Decor(s, Burb + "tree-large.fbx", new Vector3(-24f, 0f, -edge - 8f),  0f);
        Decor(s, Burb + "tree-large.fbx", new Vector3( 24f, 0f, -edge - 10f), 45f);
        Decor(s, Burb + "tree-small.fbx", new Vector3(-10f, 0f, -edge - 13f),  0f);
        Decor(s, Burb + "tree-small.fbx", new Vector3(  8f, 0f, -edge - 16f),  0f);
        Decor(s, Burb + "tree-small.fbx", new Vector3( 16f, 0f, -edge - 6f),   0f);
    }

    // ── 4. Parkerte biler langs gatene ────────────────────────────────────────
    private static void PlaceParkedCars(Transform map)
    {
        GameObject root = Child(map, "ParkedCars");
        Transform  c    = root.transform;

        string[] carPfbs = {
            VStatic + "Vehicle_Car_color01.prefab",
            VStatic + "Vehicle_Car_color03.prefab",
            VStatic + "Vehicle_SUV_color01.prefab",
            VStatic + "Vehicle_SUV_color02.prefab",
            VStatic + "Vehicle_Taxi.prefab",
            VStatic + "Vehicle_Police Car.prefab",
            VStatic + "Vehicle_Pick up Truck_color01.prefab",
            VStatic + "Vehicle_Pick up Truck_color03.prefab",
        };

        float edge = (Grid - 1) * TileSize * 0.5f + 2.5f;

        // Øst- og vestside
        for (int i = 0; i < 5; i++)
        {
            float z = (i - 2) * 7f;
            Parked(c, carPfbs[i % carPfbs.Length],           new Vector3( edge + 0.5f, 0f, z),  90f);
            Parked(c, carPfbs[(i + 2) % carPfbs.Length],     new Vector3(-edge - 0.5f, 0f, z), 270f);
        }
        // Nord- og sørside
        for (int i = 0; i < 4; i++)
        {
            float x = (i - 1.5f) * 9f;
            Parked(c, carPfbs[(i + 4) % carPfbs.Length], new Vector3(x, 0f,  edge + 0.5f), 180f);
            Parked(c, carPfbs[(i + 6) % carPfbs.Length], new Vector3(x, 0f, -edge - 0.5f),   0f);
        }

        // Spesielle kjøretøy
        Parked(c, VStatic + "Vehicle_Ambulance.prefab",   new Vector3( 22f, 0f,  20f), 180f);
        Parked(c, VStatic + "Vehicle_Bus_color01.prefab", new Vector3(-24f, 0f,   0f), 270f);
        Parked(c, VStatic + "Vehicle_Truck_color01.prefab", new Vector3(26f, 0f,  -8f),  90f);
    }

    // ── 5. Kjørbar bil — tomt chassis-root + visuelt barn (PG2202-04) ──────────
    // Riktig Unity-arkitektur: fysikk-komponenter på rent tomt GameObject,
    // SimplePoly-prefab som ren visuell barn → ingen prefab-override-konflikt
    /// <summary>Kall fra trygg meny — sletter ikke kartet. Parent bør være by-kartroten eller et tomt rot-objekt.</summary>
    public static void BuildDrivableCar(Transform map)
    {
        // ── Tomt fysikk-chassis (ingen prefab-tilknytning) ────────────────────
        GameObject car = new GameObject("DrivableCar");
        car.transform.SetParent(map, false);
        car.transform.SetPositionAndRotation(new Vector3(0f, 0.3f, -6f), Quaternion.identity);

        // BoxCollider på rent GameObject — ingen MissingComponentException (PG2202-04)
        BoxCollider bc = car.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.55f, 0f);
        bc.size   = new Vector3(1.8f, 1.1f, 4.2f);

        // Rigidbody — Rigidbody-basert kjøretøyfysikk (PG2202-04)
        Rigidbody rb = car.AddComponent<Rigidbody>();
        rb.mass           = 1200f;
        rb.linearDamping  = 0.5f;
        rb.angularDamping = 3f;
        rb.centerOfMass   = new Vector3(0f, -0.3f, 0f);

        // ── Visuelt bil-mesh som barn (SimplePoly prefab, kun rendering) ──────
        string visPath = VStatic + "Vehicle_Car_color02.prefab";
        GameObject visPfb = AssetDatabase.LoadAssetAtPath<GameObject>(visPath);
        if (visPfb != null)
        {
            GameObject vis = (GameObject)PrefabUtility.InstantiatePrefab(visPfb);
            vis.transform.SetParent(car.transform, false);
            vis.transform.localPosition = Vector3.zero;
            vis.name = "CarVisual";
        }

        // ── DriverSeat: tom GameObject inni bilen ────────────────────────────
        GameObject seat = new GameObject("DriverSeat");
        seat.transform.SetParent(car.transform, false);
        seat.transform.localPosition = new Vector3(-0.4f, 0.6f, 0.2f);

        // ── CarController (styring via Rigidbody, WASD) ───────────────────────
        CarController cc = car.AddComponent<CarController>();

        // ── CarInteraction (F-tast inn/ut, kamera-bytte) ─────────────────────
        CarInteraction ci = car.AddComponent<CarInteraction>();
        var so = new SerializedObject(ci);
        so.FindProperty("driverSeat").objectReferenceValue    = seat.transform;
        so.FindProperty("carController").objectReferenceValue = cc;
        so.ApplyModifiedProperties();

        // NavMesh — bilen blokkerer gangbar flate
        var nm = car.AddComponent<NavMeshModifier>();
        nm.overrideArea = true; nm.area = 1;
    }

    // ── 6. Sivile NPC-er — CivilianAI state machine (PG2202-AI, PG2202-03) ──
    private static void SpawnCivilians(Transform map)
    {
        GameObject root = Child(map, "Civilians");
        Transform  n    = root.transform;

        string basePath = Ithappy + "Base_Mesh.prefab";
        GameObject basePfb = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
        if (basePfb == null)
        {
            Debug.LogWarning("[Zone2Builder] ithappy Base_Mesh.prefab ikke funnet: " + basePath);
            return;
        }

        // 7 NPC-er spredt rundt byruter og torg
        Vector3[] positions = {
            new Vector3(  5f, 0f,  6f),
            new Vector3( -6f, 0f,  9f),
            new Vector3(  9f, 0f, -5f),
            new Vector3( -8f, 0f, -6f),
            new Vector3(  2f, 0f, 12f),
            new Vector3(-11f, 0f,  2f),
            new Vector3(  4f, 0f, -12f),
        };

        foreach (Vector3 pos in positions)
        {
            GameObject npc = (GameObject)PrefabUtility.InstantiatePrefab(basePfb);
            npc.transform.SetParent(n, false);
            npc.transform.position = pos;
            npc.name = "Civilian";

            // NavMeshAgent (PG2202-AI): lar CivilianAI bruke NavMesh-navigasjon
            NavMeshAgent agent = npc.GetComponent<NavMeshAgent>() ?? npc.AddComponent<NavMeshAgent>();
            agent.speed        = 2.5f;
            agent.angularSpeed = 180f;
            agent.radius       = 0.4f;
            agent.height       = 1.8f;

            // CivilianAI — Idle → Wander → Flee state machine (PG2202-03)
            if (npc.GetComponent<CivilianAI>() == null)
                npc.AddComponent<CivilianAI>();
        }
    }

    // ── 7. Gaterekvisitter: lykter, kjegler, skilt ───────────────────────────
    private static void PlaceStreetProps(Transform map)
    {
        GameObject root = Child(map, "StreetProps");
        Transform  p    = root.transform;

        float edge = (Grid - 1) * TileSize * 0.5f;

        // Gatelykter i hjørner og midt på kanter av rutenett
        var lights = new (string fbx, Vector3 pos, float rot)[] {
            ("light-square.fbx",        new Vector3( edge, 0f,  edge),  135f),
            ("light-square.fbx",        new Vector3(-edge, 0f,  edge),  225f),
            ("light-square.fbx",        new Vector3( edge, 0f, -edge),   45f),
            ("light-square.fbx",        new Vector3(-edge, 0f, -edge),  315f),
            ("light-square-double.fbx", new Vector3( edge, 0f,   0f),    90f),
            ("light-square-double.fbx", new Vector3(-edge, 0f,   0f),   270f),
            ("light-square-double.fbx", new Vector3(  0f,  0f,  edge),  180f),
            ("light-square-double.fbx", new Vector3(  0f,  0f, -edge),    0f),
            ("light-square-cross.fbx",  new Vector3(  0f,  0f,   0f),    0f),
        };
        foreach (var (fbx, pos, rot) in lights)
            Decor(p, Roads + fbx, pos, rot);

        // Anleggssone med kjegler og barrier (visuell POI)
        Decor(p, Roads + "construction-barrier.fbx", new Vector3( 8f, 0f, 14f),   0f);
        Decor(p, Roads + "construction-barrier.fbx", new Vector3(12f, 0f, 14f),   0f);
        Decor(p, Roads + "construction-barrier.fbx", new Vector3(10f, 0f, 14f),  90f);
        Decor(p, Roads + "construction-cone.fbx",    new Vector3( 7f, 0f, 13f),   0f);
        Decor(p, Roads + "construction-cone.fbx",    new Vector3( 9f, 0f, 13f),   0f);
        Decor(p, Roads + "construction-cone.fbx",    new Vector3(11f, 0f, 13f),   0f);
        Decor(p, Roads + "construction-cone.fbx",    new Vector3(13f, 0f, 13f),   0f);
        Decor(p, Roads + "construction-light.fbx",   new Vector3(10f, 0f, 12f),   0f);

        // Motorveiskilt
        Decor(p, Roads + "sign-highway.fbx",          new Vector3(-15f, 0f,  20f), 180f);
        Decor(p, Roads + "sign-highway-wide.fbx",     new Vector3( 16f, 0f, -20f),   0f);
        Decor(p, Roads + "sign-highway-detailed.fbx", new Vector3(-20f, 0f, -14f),  90f);

        // Sidegate-sperring (barrier)
        Decor(p, Roads + "road-end-barrier.fbx", new Vector3(0f, 0f,  edge + TileSize * 4f), 180f);
        Decor(p, Roads + "road-end-barrier.fbx", new Vector3(0f, 0f, -edge - TileSize * 4f),   0f);
    }

    // ── NavMesh ───────────────────────────────────────────────────────────────
    private static void EnsureNavMeshSurface()
    {
        GameObject floor = GameObject.Find("Floor");
        if (floor == null) return;
        if (floor.GetComponent<NavMeshSurface>() == null)
        {
            var surf = floor.AddComponent<NavMeshSurface>();
            surf.collectObjects = CollectObjects.All;
        }
    }

    // ── Hjelpere ──────────────────────────────────────────────────────────────

    private static void EnsureMaterialsFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
    }

    private static void FixFloorToAsphalt()
    {
        GameObject floor = GameObject.Find("Floor");
        if (floor == null) return;
        Renderer rend = floor.GetComponent<Renderer>();
        if (rend == null) return;
        const string matPath = "Assets/Materials/FloorAsphalt_Zone2.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(rend.sharedMaterial?.shader ?? Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.15f, 0.15f, 0.17f, 1f);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        rend.sharedMaterial = mat;
    }

    // Bygning — MeshCollider + NavMeshModifier Not Walkable
    private static void Building(Transform parent, string fbxPath, Vector3 pos, float yRot, float scale)
    {
        GameObject pfb = Fbx(fbxPath);
        if (pfb == null) return;
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(pfb);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yRot, 0));
        go.transform.localScale = Vector3.one * scale;
        go.name = pfb.name;
        EnsureMeshCollidersOnObject(go);
        var nm = go.AddComponent<NavMeshModifier>();
        nm.overrideArea = true; nm.area = 1;
    }

    // Parkert bil — SimplePoly City-prefaber har allerede MeshCollider på barn
    // Legger IKKE til BoxCollider på roten (unngår MissingComponentException)
    private static void Parked(Transform parent, string prefabPath, Vector3 pos, float yRot)
    {
        GameObject pfb = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (pfb == null) return;
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(pfb);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yRot, 0));
        go.name = pfb.name + "_parked";
        // Blokkerer NavMesh-baking — bilen er ikke gangbar (PG2202-AI)
        var nm = go.AddComponent<NavMeshModifier>();
        nm.overrideArea = true; nm.area = 1;
    }

    // Dekor — ingen kollisjon, ignoreFromBuild for NavMesh
    private static void Decor(Transform parent, string fbxPath, Vector3 pos, float yRot)
    {
        GameObject pfb = Fbx(fbxPath);
        if (pfb == null) return;
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(pfb);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yRot, 0));
        go.name = pfb.name;
        var nm = go.AddComponent<NavMeshModifier>();
        nm.ignoreFromBuild = true;
    }

    private static GameObject PlaceScaled(Transform parent, GameObject pfb, Vector3 pos, float yRot, float scale)
    {
        if (pfb == null) return null;
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(pfb);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yRot, 0));
        go.transform.localScale = Vector3.one * scale;
        // Kenney road FBX usually has no colliders — without this the player falls through the whole grid.
        EnsureMeshCollidersOnObject(go);
        return go;
    }

    private static void EnsureMeshCollidersOnObject(GameObject go)
    {
        if (go == null) return;
        SceneSetupTool.EnsureMeshCollidersUnderRoot(go, SceneSetupTool.ShouldSkipMeshColliderForBeachVegetation);
    }

    /// <summary>Hele by-kartroten — veier og props uten collider (fikser fall-through).</summary>
    public static void EnsureMeshCollidersForCityRoads()
    {
        GameObject map = LevelMapRootResolver.FindCityMapRoot();
        if (map == null) return;
        SceneSetupTool.EnsureMeshCollidersUnderRoot(map, SceneSetupTool.ShouldSkipMeshColliderForBeachVegetation);
    }

    private static GameObject Fbx(string fullPath) =>
        AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);

    private static GameObject Child(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void DestroyIfExists(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    [MenuItem("CartoonZombies/Scenes/Fix CITY mesh colliders (city map root)", false, 35)]
    private static void MenuEnsureRoadColliders()
    {
        GameObject map = LevelMapRootResolver.FindCityMapRoot();
        if (map == null)
        {
            EditorUtility.DisplayDialog("City map",
                "Fant ikke by-kartrot. Prøvde: " + LevelMapRootResolver.CityCandidatesHint, "OK");
            return;
        }

        EnsureMeshCollidersForCityRoads();
        var scene = SceneManager.GetActiveScene();
        if (scene.isLoaded && !string.IsNullOrEmpty(scene.path))
            EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog("Done",
            "MeshColliders lagt til under «" + map.name + "» der de manglet.\nLagre scenen (Ctrl+S).", "OK");
    }

    [MenuItem("CartoonZombies/Scenes/Fix CITY mesh colliders (city map root)", true)]
    private static bool MenuEnsureRoadCollidersValidate() => !Application.isPlaying;
}
