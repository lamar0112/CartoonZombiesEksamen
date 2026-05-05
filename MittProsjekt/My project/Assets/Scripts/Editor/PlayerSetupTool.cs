using UnityEngine;
using UnityEditor;

// PlayerSetupTool — meny som finner ithappy Base_Mesh og setter opp spiller-prefab med våre komponenter.
// Pensum: prefab + komponent-stack (PG2202-02).
// Ekstra: avhenger av tredjepartssti — dokumenter i rapport hvis prefab-navn endres.
public class PlayerSetupTool
{
    [MenuItem("CartoonZombies/Setup/05 Player prefab", false, 14)]
    public static void SetupPlayer()
    {
        // Finner Base_Mesh-prefaben fra ithappy
        string[] guids = AssetDatabase.FindAssets("Base_Mesh t:Prefab");
        string prefabPath = "";
        foreach (string guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.Contains("ithappy")) { prefabPath = p; break; }
        }

        if (string.IsNullOrEmpty(prefabPath))
        {
            EditorUtility.DisplayDialog("Feil", "Fant ikke Base_Mesh.prefab i ithappy-mappen!", "OK");
            return;
        }

        // Kopierer til Prefabs/Player/ som vår egen player-prefab
        string destPath = "Assets/Prefabs/Player/Player.prefab";
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(destPath))
            AssetDatabase.CopyAsset(prefabPath, destPath);

        // Redigerer prefaben
        using (var scope = new PrefabUtility.EditPrefabContentsScope(destPath))
        {
            GameObject root = scope.prefabContentsRoot;
            root.name = "Player";

            // Setter tag til Player
            root.tag = "Player";

            // CharacterController MÅ ligge før PlayerMovement (RequireComponent)
            CharacterController cc = root.GetComponent<CharacterController>();
            if (cc == null) cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.radius = 0.3f;

            // Legger til scripts hvis de mangler
            if (root.GetComponent<PlayerMovement>()  == null) root.AddComponent<PlayerMovement>();
            if (root.GetComponent<PlayerHealth>()    == null) root.AddComponent<PlayerHealth>();
            if (root.GetComponent<PlayerShooting>()  == null) root.AddComponent<PlayerShooting>();
            if (root.GetComponent<CheatMenu>()       == null) root.AddComponent<CheatMenu>();

            // AudioSource for skudd-lyder
            var sources = root.GetComponents<AudioSource>();
            if (sources.Length == 0) root.AddComponent<AudioSource>();

            // Lager MuzzlePoint som child GameObject
            Transform muzzle = root.transform.Find("MuzzlePoint");
            if (muzzle == null)
            {
                GameObject mp = new GameObject("MuzzlePoint");
                mp.transform.SetParent(root.transform);
                mp.transform.localPosition = new Vector3(0f, 1.5f, 0.8f); // foran og litt opp
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Ferdig!",
            "Player-prefab opprettet i Assets/Prefabs/Player/Player.prefab\n\n" +
            "Scripts lagt til: PlayerHealth, PlayerShooting, CheatMenu, AudioSource\n" +
            "MuzzlePoint opprettet som child.\n\n" +
            "Husk: Dra Player-prefaben inn i hver zone-scene!", "OK");
    }
}
