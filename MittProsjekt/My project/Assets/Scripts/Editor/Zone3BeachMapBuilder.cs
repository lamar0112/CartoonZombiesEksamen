using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

// Zone3BeachMapBuilder — procedural strand/øy. FARE: sletter kartrot + Environment — backup først.
// Pensum: terreng/prefab-layout, NavMesh, kobling til BeachParkourMission (PG2202-01, PG2202-03).
// Ekstra: destruktiv rebuild-knapp — kun for utvikling; dialog på norsk for tydelig gruppevarsel.
// Nytt kart får roten «BeachMap» (se LevelMapRootResolver). Spillscene: Level02_StrandSkog.
public static class Zone3BeachMapBuilder
{
    private const string NK      = "Assets/ThirdParty/Kenney/NatureKit/";
    private const string Proxy   = "Assets/ThirdParty/Proxy Games/Stylized Nature Kit Lite/Prefabs/";
    private const string Forest  = "Assets/ThirdParty/LowPoly_ForestPack/Prefabs/";

    [MenuItem("CartoonZombies/Level Art/⚠ Rebuild procedural BEACH map (destructive)", false, 101)]
    public static void Rebuild()
    {
        if (!EditorUtility.DisplayDialog("FARE — procedural strand",
            "Dette SLETTER «" + LevelMapRootResolver.ProceduralBeachRootName + "» / «BeachMap_Zone3» (hvis de finnes) og «Environment».\n\n" +
            "Ikke kjør på ferdig Level02_StrandSkog uten backup.\n\nFortsette?",
            "Ja, bygg!", "Avbryt"))
            return;

        EnsureMaterialsFolder();
        FixFloorToSand();

        DestroyIfExists(LevelMapRootResolver.ProceduralBeachRootName);
        DestroyIfExists("BeachMap_Zone3");
        DestroyIfExists("Environment");

        GameObject env   = new GameObject("Environment");
        GameObject mapGo = new GameObject(LevelMapRootResolver.ProceduralBeachRootName);
        mapGo.transform.SetParent(env.transform, false);
        Transform map = mapGo.transform;

        PlaceOcean(map);           // Hav rundt hele øya
        BuildEastCliffs(map);      // Klippevegg øst
        BuildWestRocks(map);       // Stein-formasjoner vest
        PlantPalms(map);           // Palmeskog
        BuildCampArea(map);        // Bålplass (sentral-vest)
        BuildNorthDock(map);       // Trebryggje nord over vann
        BuildTentCamp(map);        // Teltleir nord-øst
        PlaceGroundCover(map);     // Grasdekke og blomster
        BuildParkourCourse(map);   // Hoppeplattformer sør (over vann)
        PlaceForestEdge(map);      // Skogkant sør-vest
        EnsureNavMeshSurface();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Procedural strand — øy-layout",
            "Strand-øy bygget!\n\n" +
            "• Hav omringer hele øya\n" +
            "• Trebryggje mot nord + kano\n" +
            "• Parkour-hoppebane mot sør\n" +
            "• Klippevegg øst, stein-formasjoner vest\n\n" +
            "Neste: Re-Bake NavMesh → Ctrl+S", "OK");
    }

    // ── 1. Havflate — OMRINGER hele øya (stor, sentrert) ────────────────────
    private static void PlaceOcean(Transform map)
    {
        string waterPath = Proxy + "Water/Flat Water.prefab";
        GameObject waterPfb = AssetDatabase.LoadAssetAtPath<GameObject>(waterPath);
        if (waterPfb == null)
        {
            Debug.LogWarning("[Zone3Builder] Flat Water.prefab ikke funnet: " + waterPath);
            return;
        }
        // Stor plane sentrert under øya — dekker alt rundt
        GameObject water = (GameObject)PrefabUtility.InstantiatePrefab(waterPfb);
        water.transform.SetParent(map, false);
        water.transform.SetPositionAndRotation(new Vector3(0f, -0.35f, 0f), Quaternion.identity);
        water.transform.localScale = new Vector3(30f, 1f, 30f);
        water.name = "Ocean";
        var nm = water.AddComponent<NavMeshModifier>();
        nm.overrideArea = true; nm.area = 1;
    }

    // ── 2. Klippevegg øst (kortere enn før — 6 søyler, Z=-10 til +18) ───────
    private static void BuildEastCliffs(Transform map)
    {
        GameObject root = Child(map, "EastCliffs");
        Transform  c    = root.transform;
        const float LayerH = 3f;
        const int   Layers = 3;

        for (int i = 0; i < 6; i++)
        {
            float z = -10f + i * 5f;
            for (int layer = 0; layer < Layers; layer++)
            {
                float y = layer * LayerH;
                CliffBlock(c, NK + "cliff_block_stone.fbx", new Vector3(22f, y, z),         0f,   1.5f);
                if (layer < 2)
                    CliffBlock(c, NK + "cliff_block_rock.fbx",  new Vector3(19f, y, z + 2f), i * 43f, 1.2f);
            }
            CliffBlock(c, NK + "cliff_top_stone.fbx", new Vector3(22f, Layers * LayerH, z), 0f, 1.5f);
        }
        // Hjørnestykker
        for (int layer = 0; layer < Layers; layer++)
        {
            float y = layer * LayerH;
            CliffBlock(c, NK + "cliff_corner_stone.fbx", new Vector3(22f, y, -12f),  90f, 1.5f);
            CliffBlock(c, NK + "cliff_corner_stone.fbx", new Vector3(22f, y,  18f), 180f, 1.5f);
        }
        CliffBlock(c, NK + "cliff_cornerTop_stone.fbx", new Vector3(22f, Layers * LayerH, -12f),  90f, 1.5f);
        CliffBlock(c, NK + "cliff_cornerTop_stone.fbx", new Vector3(22f, Layers * LayerH,  18f), 180f, 1.5f);

        // Klippe-trapp mot stranden
        CliffBlock(c, NK + "cliff_steps_stone.fbx", new Vector3(18f, 0f,  4f), 270f, 1.4f);

        // Cave-inngang som visuell interest
        CliffBlock(c, NK + "cliff_cave_stone.fbx",       new Vector3(21f, 0f,  0f), 270f, 1.5f);
        CliffBlock(c, NK + "cliff_blockCave_stone.fbx",  new Vector3(21f, LayerH, 0f), 270f, 1.5f);
    }

    // ── 3. Steinformasjoner vest ─────────────────────────────────────────────
    private static void BuildWestRocks(Transform map)
    {
        GameObject root = Child(map, "WestRocks");
        Transform  r    = root.transform;

        var largeRocks = new (string fbx, Vector3 pos, float rot, float sc)[] {
            ("rock_largeA.fbx", new Vector3(-22f, 0f,  -6f),   0f, 2.0f),
            ("rock_largeB.fbx", new Vector3(-20f, 0f,   8f),  45f, 1.7f),
            ("rock_largeC.fbx", new Vector3(-24f, 0f,  18f),  90f, 2.2f),
            ("rock_largeD.fbx", new Vector3(-18f, 0f, -16f), 130f, 1.5f),
        };
        foreach (var (fbx, pos, rot, sc) in largeRocks)
            Rock(r, NK + fbx, pos, rot, sc);

        var tallRocks = new (string fbx, Vector3 pos, float rot)[] {
            ("rock_tallA.fbx", new Vector3(-12f, 0f, -12f),  20f),
            ("rock_tallB.fbx", new Vector3(-14f, 0f,  16f), 140f),
            ("rock_tallC.fbx", new Vector3(-10f, 0f,  -2f), 250f),
        };
        foreach (var (fbx, pos, rot) in tallRocks)
            Rock(r, NK + fbx, pos, rot, 1.2f);

        var smallRocks = new (string fbx, Vector3 pos, float rot)[] {
            ("rock_smallA.fbx",     new Vector3(-6f,  0f,  4f),  10f),
            ("rock_smallB.fbx",     new Vector3(-8f,  0f, 12f),  80f),
            ("rock_smallFlatA.fbx", new Vector3(-4f,  0f, -6f),   0f),
            ("rock_smallFlatB.fbx", new Vector3(-14f, 0f,  6f),  90f),
        };
        foreach (var (fbx, pos, rot) in smallRocks)
            Decor(r, NK + fbx, pos, rot);
    }

    // ── 4. Palmeskog fordelt over øya ────────────────────────────────────────
    private static void PlantPalms(Transform map)
    {
        GameObject root = Child(map, "Palms");
        Transform  p    = root.transform;

        var palms = new (string fbx, Vector3 pos, float rot, float sc)[] {
            // Nord-øst — ved teltleiren
            ("tree_palmTall.fbx",          new Vector3( 10f, 0f, 16f),  20f, 2.0f),
            ("tree_palmDetailedTall.fbx",  new Vector3( 14f, 0f, 12f),  70f, 1.9f),
            ("tree_palmBend.fbx",          new Vector3(  6f, 0f, 18f), 110f, 1.8f),
            ("tree_palm.fbx",              new Vector3( 12f, 0f, 20f), 200f, 2.2f),
            // Sentrum
            ("tree_palmShort.fbx",         new Vector3(  2f, 0f,  4f), 150f, 1.7f),
            ("tree_palmBend.fbx",          new Vector3( -4f, 0f,  6f),  40f, 1.9f),
            ("tree_palm.fbx",              new Vector3(  4f, 0f, -4f), 280f, 2.0f),
            ("tree_palmDetailedShort.fbx", new Vector3( -6f, 0f,  2f),  90f, 1.8f),
            // Syd-vest — nær camp
            ("tree_palmShort.fbx",         new Vector3(-10f, 0f, -2f),   0f, 1.7f),
            ("tree_palmBend.fbx",          new Vector3(-14f, 0f,  4f),  90f, 1.9f),
            ("tree_palmTall.fbx",          new Vector3( -8f, 0f, 10f), 320f, 2.1f),
            // Nord-strand
            ("tree_palmBend.fbx",          new Vector3(  2f, 0f, 17f), 260f, 1.8f),
            ("tree_palmShort.fbx",         new Vector3( -4f, 0f, 15f),  50f, 1.7f),
            // Syd — kantpalmer
            ("tree_palmTall.fbx",          new Vector3(  6f, 0f, -8f), 180f, 2.0f),
            ("tree_palmDetailedTall.fbx",  new Vector3( -8f, 0f,-12f),  60f, 1.8f),
        };
        foreach (var (fbx, pos, rot, sc) in palms)
            PlaceScaled(p, Fbx(NK + fbx), pos, rot, sc, ignoreNavMesh: true);
    }

    // ── 5. Bålplass (sentral-vest) — visuell POI ────────────────────────────
    private static void BuildCampArea(Transform map)
    {
        GameObject root = Child(map, "CampArea");
        Transform  c    = root.transform;

        // Plattformer
        PlaceStatic(c, NK + "platform_beach.fbx", new Vector3(-8f, 0f, 2f),  0f, 1.0f);
        PlaceStatic(c, NK + "platform_beach.fbx", new Vector3(-8f, 0f, 6f),  0f, 1.0f);

        // Bål
        PlaceStatic(c, NK + "campfire_stones.fbx", new Vector3(-8f, 0f, 4f), 0f, 1.0f);
        PlaceStatic(c, NK + "campfire_logs.fbx",   new Vector3(-8f, 0.05f, 4f), 45f, 1.0f);

        // Vedstabel og utstyr
        Decor(c, NK + "log_stack.fbx",     new Vector3(-11f, 0f, 4f),  45f);
        Decor(c, NK + "log_stack.fbx",     new Vector3( -5f, 0f, 4f), 315f);

        // Gjerde rundt
        Decor(c, NK + "fence_planks.fbx",       new Vector3(-12f, 0f,  0f), 90f);
        Decor(c, NK + "fence_planks.fbx",       new Vector3(-12f, 0f,  4f), 90f);
        Decor(c, NK + "fence_planks.fbx",       new Vector3(-12f, 0f,  8f), 90f);
        Decor(c, NK + "fence_gate.fbx",         new Vector3( -8f, 0f, -2f),  0f);
        Decor(c, NK + "fence_planksDouble.fbx", new Vector3( -4f, 0f,  4f), 180f);

        // Blomster og gress
        Decor(c, NK + "flower_yellowA.fbx",  new Vector3(-6f, 0f,  6f),  0f);
        Decor(c, NK + "flower_redA.fbx",     new Vector3(-10f, 0f, 6f), 45f);
        Decor(c, NK + "grass_leafs.fbx",     new Vector3(-5f,  0f, 2f),  0f);
        Decor(c, NK + "plant_bush.fbx",      new Vector3(-12f, 0f, 2f),  0f);
        Decor(c, NK + "sign.fbx",            new Vector3(-7f,  0f, -2f), 270f);
    }

    // ── 6. Trebryggje mot nord — over vannet (hoveddock) ────────────────────
    // Synlig orienteringspunkt + kano ved enden
    private static void BuildNorthDock(Transform map)
    {
        GameObject root = Child(map, "NorthDock");
        Transform  d    = root.transform;

        // Bybygd trebru — fem seksjoner langs Z-aksen
        for (int i = 0; i < 5; i++)
        {
            float z = 22f + i * 4.2f;
            PlaceStatic(d, NK + "bridge_wood.fbx", new Vector3(0f, 0.08f, z), 0f, 1.3f);
        }

        // Sideplattformer ved bryggen
        PlaceStatic(d, NK + "bridge_woodNarrow.fbx", new Vector3( 3.5f, 0.08f, 27f),  90f, 1.1f);
        PlaceStatic(d, NK + "bridge_woodNarrow.fbx", new Vector3(-3.5f, 0.08f, 31f), 270f, 1.1f);

        // Kano og årer ved enden av bryggen
        Decor(d, NK + "canoe.fbx",        new Vector3( 2.5f, -0.1f, 42f),  90f);
        Decor(d, NK + "canoe_paddle.fbx", new Vector3( 2.5f,  0.2f, 41f),   0f);
        Decor(d, NK + "canoe.fbx",        new Vector3(-2.5f, -0.1f, 40f), 270f);
        Decor(d, NK + "canoe_paddle.fbx", new Vector3(-2.5f,  0.2f, 39f), 180f);

        // Liten bålstue på syd-enden av bryggen (orienteringspunkt)
        Decor(d, NK + "campfire_planks.fbx", new Vector3(3f, 0f, 21f), 0f);
    }

    // ── 7. Teltleir nord-øst ────────────────────────────────────────────────
    private static void BuildTentCamp(Transform map)
    {
        GameObject root = Child(map, "TentCamp");
        Transform  t    = root.transform;

        // Telt
        PlaceStatic(t, NK + "tent_detailedClosed.fbx", new Vector3( 10f, 0f, 10f),  30f, 1.2f);
        PlaceStatic(t, NK + "tent_detailedOpen.fbx",   new Vector3( 14f, 0f,  8f), 350f, 1.2f);
        PlaceStatic(t, NK + "tent_smallClosed.fbx",    new Vector3(  8f, 0f, 14f),  80f, 1.0f);
        PlaceStatic(t, NK + "tent_smallOpen.fbx",      new Vector3( 12f, 0f, 14f), 200f, 1.0f);

        // Bål i leiren
        PlaceStatic(t, NK + "campfire_bricks.fbx", new Vector3(11f, 0f, 11f), 0f, 0.8f);

        // Senger
        PlaceStatic(t, NK + "bed.fbx",       new Vector3( 8f, 0f, 10f),  90f, 1.0f);
        PlaceStatic(t, NK + "bed_floor.fbx", new Vector3(10f, 0f, 14f), 270f, 1.0f);

        // Gjerde
        Decor(t, NK + "fence_simple.fbx",     new Vector3(16f, 0f, 10f),  90f);
        Decor(t, NK + "fence_simple.fbx",     new Vector3(16f, 0f, 14f),  90f);
        Decor(t, NK + "fence_simpleHigh.fbx", new Vector3(12f, 0f, 17f),   0f);
        Decor(t, NK + "fence_gate.fbx",       new Vector3( 8f, 0f, 17f),   0f);

        // Blomster og planter
        Decor(t, NK + "flower_purpleB.fbx",  new Vector3( 9f, 0f,  8f),   0f);
        Decor(t, NK + "flower_yellowB.fbx",  new Vector3(15f, 0f, 12f),   0f);
        Decor(t, NK + "plant_bushLarge.fbx", new Vector3(17f, 0f,  8f),   0f);
        Decor(t, NK + "mushroom_tanGroup.fbx", new Vector3(9f, 0f, 16f),  0f);
    }

    // ── 8. Grasdekke og blomster ─────────────────────────────────────────────
    private static void PlaceGroundCover(Transform map)
    {
        GameObject root = Child(map, "GroundCover");
        Transform  g    = root.transform;

        var grassSpots = new Vector3[] {
            new Vector3( 8f, 0f,  4f), new Vector3(-4f, 0f, 12f),
            new Vector3( 2f, 0f, -6f), new Vector3(14f, 0f,  4f),
            new Vector3(-14f, 0f, 8f), new Vector3( 4f, 0f, -2f),
            new Vector3(18f, 0f,  6f), new Vector3( 0f, 0f,  8f),
        };
        string[] grassTypes = { "grass.fbx", "grass_large.fbx", "grass_leafs.fbx", "grass_leafsLarge.fbx" };
        for (int i = 0; i < grassSpots.Length; i++)
            Decor(g, NK + grassTypes[i % grassTypes.Length], grassSpots[i], i * 47f);

        var flowers = new (string fbx, Vector3 pos, float rot)[] {
            ("flower_purpleC.fbx", new Vector3(  6f, 0f,  6f),  10f),
            ("flower_redB.fbx",    new Vector3( -6f, 0f,  8f),  60f),
            ("flower_yellowC.fbx", new Vector3( 12f, 0f, -2f), 120f),
            ("flower_redC.fbx",    new Vector3( -2f, 0f,-10f), 190f),
            ("flower_purpleA.fbx", new Vector3( 16f, 0f, 10f), 250f),
            ("flower_yellowA.fbx", new Vector3(-16f, 0f, 12f), 310f),
        };
        foreach (var (fbx, pos, rot) in flowers)
            Decor(g, NK + fbx, pos, rot);

        Decor(g, NK + "plant_bushDetailed.fbx",       new Vector3(  4f, 0f, -12f),   0f);
        Decor(g, NK + "plant_bushLargeTriangle.fbx",  new Vector3(-10f, 0f,  14f),  45f);
        Decor(g, NK + "plant_bushTriangle.fbx",       new Vector3( 12f, 0f,  -6f),   0f);
        Decor(g, NK + "mushroom_redGroup.fbx",        new Vector3(-16f, 0f, -18f),   0f);
        Decor(g, NK + "mushroom_tanTall.fbx",         new Vector3(-20f, 0f,  10f),   0f);
    }

    // ── 9. Hoppeplattformer sør (over vannet) — PARKOUR-BANE ────────────────
    // Spilleren hopper mellom klippe-halvblokker for å nå FINISH
    // BeachParkourMission håndterer timer og respawn (PG2202-03)
    private static void BuildParkourCourse(Transform map)
    {
        GameObject root = Child(map, "ParkourCourse");
        Transform  k    = root.transform;

        // Trapp fra stranden ned til første plattform (Z=-17)
        CliffBlock(k, NK + "cliff_steps_stone.fbx", new Vector3(0f, 0f, -17f), 180f, 1.3f);

        // cliff_blockHalf_stone er ~1m høy ved scale=1.2 — nåbar med hopp (Space)
        // Sikksakk-mønster over vannet (Z: -20 til -36)
        var platforms = new (Vector3 pos, float rot)[] {
            (new Vector3(  0f, 0f, -21f),   0f),   // P0: start (ved stranden)
            (new Vector3(  6f, 0f, -26f),  90f),   // P1: hopp høyre+frem
            (new Vector3( 12f, 0f, -22f), 180f),   // P2: hopp høyre+tilbake
            (new Vector3( 16f, 0f, -28f), 270f),   // P3: hopp frem
            (new Vector3( 10f, 0f, -33f),   0f),   // P4: hopp venstre+frem
            (new Vector3(  3f, 0f, -30f),  90f),   // P5: FINISH — hopp venstre
        };

        var cpTransforms = new Transform[platforms.Length];

        for (int i = 0; i < platforms.Length; i++)
        {
            var (pos, rot) = platforms[i];

            // Plattform-blokk
            CliffBlock(k, NK + "cliff_blockHalf_stone.fbx", pos, rot, 1.2f);

            // Tom sjekkpunkt-GameObject litt over plattformen
            GameObject cp = new GameObject("Checkpoint_" + i);
            cp.transform.SetParent(k, false);
            cp.transform.position = pos + new Vector3(0f, 2f, 0f);
            cpTransforms[i] = cp.transform;
        }

        // FINISH-markør: campfire på topp av siste plattform
        Vector3 finPos = platforms[platforms.Length - 1].pos;
        Decor(k, NK + "campfire_stones.fbx", finPos + new Vector3(0f, 1.5f, 0f), 0f);

        // Sett opp BeachParkourMission og knytt sjekkpunktene
        GameObject mgr = new GameObject("ParkourMissionManager");
        mgr.transform.SetParent(k, false);
        BeachParkourMission mission = mgr.AddComponent<BeachParkourMission>();

        var so = new SerializedObject(mission);
        SerializedProperty cpProp = so.FindProperty("checkpoints");
        if (cpProp != null)
        {
            cpProp.arraySize = cpTransforms.Length;
            for (int i = 0; i < cpTransforms.Length; i++)
                cpProp.GetArrayElementAtIndex(i).objectReferenceValue = cpTransforms[i];
            so.ApplyModifiedProperties();
        }

        // Respawn-punkt: tilbake til startplattform
        SerializedProperty respProp = so.FindProperty("respawnPoint");
        if (respProp != null)
        {
            respProp.objectReferenceValue = cpTransforms[0];
            so.ApplyModifiedProperties();
        }
    }

    // ── 10. Skogkant syd-vest ────────────────────────────────────────────────
    private static void PlaceForestEdge(Transform map)
    {
        GameObject root = Child(map, "ForestEdge");
        Transform  f    = root.transform;

        var edgeTrees = new (string pfbPath, Vector3 pos, float rot, float sc)[] {
            (Forest + "Tree_Summer.prefab",            new Vector3(-22f, 0f, -18f),   0f, 1.5f),
            (Forest + "Tree_AutumnLight.prefab",       new Vector3(-16f, 0f, -20f),  30f, 1.3f),
            (Forest + "CircleTree_Summer.prefab",      new Vector3( -8f, 0f, -18f),  70f, 1.4f),
            (Forest + "Tree_Autumn.prefab",            new Vector3(  2f, 0f, -16f), 160f, 1.6f),
            (Forest + "Dead_Tree.prefab",              new Vector3( 18f, 0f, -16f), 280f, 1.4f),
        };
        foreach (var (pfbPath, pos, rot, sc) in edgeTrees)
        {
            GameObject pfb = AssetDatabase.LoadAssetAtPath<GameObject>(pfbPath);
            if (pfb == null) continue;
            PlaceScaled(f, pfb, pos, rot, sc, ignoreNavMesh: true);
        }
        Decor(f, Forest + "Bush.prefab",       new Vector3(-20f, 0f, -16f),  0f);
        Decor(f, Forest + "Bush_Flat.prefab",  new Vector3( -6f, 0f, -16f), 45f);
        Decor(f, Forest + "Tree_Stump.prefab", new Vector3(-12f, 0f, -16f),  0f);
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

    private static void FixFloorToSand()
    {
        GameObject floor = GameObject.Find("Floor");
        if (floor == null) return;
        Renderer rend = floor.GetComponent<Renderer>();
        if (rend == null) return;
        const string matPath = "Assets/Materials/FloorSand_Zone3.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(rend.sharedMaterial?.shader ?? Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.82f, 0.75f, 0.54f, 1f);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        rend.sharedMaterial = mat;
    }

    // Klippe/stein: MeshCollider + NavMeshModifier Not Walkable
    private static void CliffBlock(Transform parent, string fbxPath, Vector3 pos, float yRot, float scale)
    {
        GameObject pfb = Fbx(fbxPath);
        if (pfb == null) return;
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(pfb);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yRot, 0));
        go.transform.localScale = Vector3.one * scale;
        go.name = pfb.name;
        foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            if (mf.GetComponent<MeshCollider>() == null)
                mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }
        var nm = go.AddComponent<NavMeshModifier>();
        nm.overrideArea = true; nm.area = 1;
    }

    private static void Rock(Transform parent, string fbxPath, Vector3 pos, float yRot, float scale)
        => CliffBlock(parent, fbxPath, pos, yRot, scale);

    // Statisk detalj: MeshCollider, ingen NavMesh-blokkering
    private static void PlaceStatic(Transform parent, string fbxPath, Vector3 pos, float yRot, float scale)
    {
        GameObject pfb = Fbx(fbxPath);
        if (pfb == null) return;
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(pfb);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yRot, 0));
        go.transform.localScale = Vector3.one * scale;
        go.name = pfb.name;
        foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            if (mf.GetComponent<MeshCollider>() == null)
                mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }
    }

    // Dekor: ingen kollisjon, ignoreFromBuild for NavMesh
    private static void Decor(Transform parent, string path, Vector3 pos, float yRot)
    {
        GameObject pfb = path.EndsWith(".prefab")
            ? AssetDatabase.LoadAssetAtPath<GameObject>(path)
            : Fbx(path);
        if (pfb == null) return;
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(pfb);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yRot, 0));
        go.name = pfb.name;
        var nm = go.AddComponent<NavMeshModifier>();
        nm.ignoreFromBuild = true;
    }

    // Skalert plassering (palmer, skogtrær o.l.) — ingen MeshCollider på vegetasjon som standard (spillbar skog).
    private static void PlaceScaled(Transform parent, GameObject pfb, Vector3 pos, float yRot, float scale,
        bool ignoreNavMesh = false, bool addMeshColliders = false)
    {
        if (pfb == null) return;
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(pfb);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yRot, 0));
        go.transform.localScale = Vector3.one * scale;
        go.name = pfb.name;
        var nm = go.AddComponent<NavMeshModifier>();
        if (ignoreNavMesh) nm.ignoreFromBuild = true;
        else { nm.overrideArea = true; nm.area = 1; }
        if (addMeshColliders)
            SceneSetupTool.EnsureMeshCollidersUnderRoot(go);
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

    [MenuItem("CartoonZombies/Scenes/Fix BEACH mesh colliders (beach map root)", false, 36)]
    private static void MenuEnsureBeachColliders()
    {
        GameObject map = LevelMapRootResolver.FindBeachMapRoot();
        if (map == null)
        {
            EditorUtility.DisplayDialog("Beach map",
                "Fant ikke strand-kartrot. Prøvde: " + LevelMapRootResolver.BeachCandidatesHint, "OK");
            return;
        }

        SceneSetupTool.EnsureMeshCollidersUnderRoot(map, SceneSetupTool.ShouldSkipMeshColliderForBeachVegetation);
        var scene = SceneManager.GetActiveScene();
        if (scene.isLoaded && !string.IsNullOrEmpty(scene.path))
            EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog("Done",
            "MeshColliders lagt til der de manglet (terreng + harde mesher).\n" +
            "Trær / palmer / busker under Palms, ForestEdge, GroundCover hoppes over så skogen er spillbar.\n\n" +
            "Lagre scenen (Ctrl+S).", "OK");
    }

    [MenuItem("CartoonZombies/Scenes/Strip BEACH tree/bush MeshColliders (walkable forest)", false, 37)]
    private static void MenuStripBeachVegetationColliders()
    {
        GameObject map = LevelMapRootResolver.FindBeachMapRoot();
        if (map == null)
        {
            EditorUtility.DisplayDialog("Beach map",
                "Fant ikke strand-kartrot. Prøvde: " + LevelMapRootResolver.BeachCandidatesHint, "OK");
            return;
        }

        int removed = 0;
        foreach (var mc in map.GetComponentsInChildren<MeshCollider>(true))
        {
            if (mc == null) continue;
            if (!SceneSetupTool.IsBeachVegetationHierarchy(mc.transform)) continue;
            Undo.DestroyObjectImmediate(mc);
            removed++;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.isLoaded && !string.IsNullOrEmpty(scene.path))
            EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog("Done",
            $"Fjernet {removed} MeshCollider(e) på vegetasjon under «{map.name}».\nLagre scenen (Ctrl+S).", "OK");
    }

    [MenuItem("CartoonZombies/Scenes/Fix BEACH mesh colliders (beach map root)", true)]
    private static bool MenuEnsureBeachCollidersValidate() => !Application.isPlaying;

    [MenuItem("CartoonZombies/Scenes/Strip BEACH tree/bush MeshColliders (walkable forest)", true)]
    private static bool MenuStripBeachVegetationCollidersValidate() => !Application.isPlaying;
}
