#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// IslandTreeTerrainWarningFix — fjerner Tree Creator-komponent fra Island-prefabs for å slippe Soft Occlusion-varsel (URP).
// Pensum: forståelse for prefab vs terrain; bygg ikke inn i standalone.
// Ekstra: løser importeringsstøy fra tredjeparts øy-assets; dialogtekst kan være norsk eller engelsk — her bevares forklaring til gruppa.
// Tree_a / Tree_b: etter strip brukes MeshFilter/MeshRenderer som før når trær ikke males på terrain.
public static class IslandTreeTerrainWarningFix
{
    const string TreeA = "Assets/ThirdParty/Island/Prefabs/Tree_a.prefab";
    const string TreeB = "Assets/ThirdParty/Island/Prefabs/Tree_b.prefab";

    [MenuItem("CartoonZombies/Fix/Island palms – fjern Tree Creator (stopp Soft Occlusion-varsel)", false, 51)]
    static void StripTreeComponentFromIslandPrefabs()
    {
        if (!EditorUtility.DisplayDialog(
                "Island palmer og Terrain",
                "Unity viser varsel fordi Tree Creator + Terrain er laget for gamle «Soft Occlusion»-shaders. " +
                "URP Lit er riktig for farger, men motoren klager fortsatt så lenge Tree-komponenten finnes.\n\n" +
                "Denne kommandoen fjerner Tree-komponenten fra Tree_a og Tree_b. Da forsvinner varselet.\n\n" +
                "VIKTIG: På Terrain → Paint Trees: slett alle Tree_a/Tree_b (eller bytt prototype) FØRST. " +
                "Deretter plasser trær som vanlige prefab-instanser der du vil ha dem.\n\n" +
                "Fortsette?",
                "Ja", "Avbryt"))
            return;

        foreach (string path in new[] { TreeA, TreeB })
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                EditorUtility.DisplayDialog("Island", "Fant ikke prefab:\n" + path, "OK");
                return;
            }
        }

        foreach (string path in new[] { TreeA, TreeB })
        {
            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var tree = scope.prefabContentsRoot.GetComponent<Tree>();
                if (tree != null)
                    Undo.DestroyObjectImmediate(tree);
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Ferdig",
            "Tree-komponent er fjernet fra Tree_a og Tree_b. Åpne scenen på nytt om nødvendig, og sjekk Game-visning.",
            "OK");
    }
}
#endif
