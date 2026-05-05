#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// ThirdPartyFolderSetup — flytter kjente importmapper til Assets/ThirdParty (ryddig rot).
// Pensum: prosjektstruktur og navngiving (PG2202-08).
// Ekstra: trygt å kjøre flere ganger; listen med mappenavn må oppdateres ved nye Asset Store-pakker.
public static class ThirdPartyFolderSetup
{
    private const string ThirdPartyRoot = "Assets/ThirdParty";

    private static readonly string[] ImportedPackageFolderNames =
    {
        "AllSkyFree",
        "Alstra Infinite",
        "ArtStore3D",
        "FSP",
        "IgniteCoders",
        "ithappy",
        "JMO Assets",
        "Kenney",
        "Low Poly Weapons VOL.1",
        "LowPoly_ForestPack",
        "modular_platformer_unity_package",
        "Obstacle Pack",
        "Peaceful Piano - Free Loop Sample Pack",
        "Polytope Studio",
        "Proxy Games",
        "Rose Fantasy World _ Tumblebee\u200b\u200b",
        "SimpleNaturePack",
        "SimplePoly City - Low Poly Assets",
        "SimpleSky",
        "Supercyan Character Pack Zombie Sample",
        "Synty",
        "TextMesh Pro",
        "TutorialInfo",
        "VFXPACK_FIRE_WALLCOEUR",
        "Weapons of Choice FREE - Komposite Sound",
    };

    [MenuItem("CartoonZombies/Project/Move imported packages to ThirdParty", false, 50)]
    public static void MoveImportedPackagesToThirdParty()
    {
        if (!AssetDatabase.IsValidFolder(ThirdPartyRoot))
        {
            string guid = AssetDatabase.CreateFolder("Assets", "ThirdParty");
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError("Could not create Assets/ThirdParty folder.");
                return;
            }
        }

        int movedCount = 0;
        foreach (string folderName in ImportedPackageFolderNames)
        {
            string sourcePath = $"Assets/{folderName}";
            if (!AssetDatabase.IsValidFolder(sourcePath))
                continue;

            string destPath = $"{ThirdPartyRoot}/{folderName}";
            if (AssetDatabase.IsValidFolder(destPath))
            {
                Debug.LogWarning($"Already exists, skipping: {destPath}");
                continue;
            }

            string error = AssetDatabase.MoveAsset(sourcePath, destPath);
            if (string.IsNullOrEmpty(error))
            {
                movedCount++;
                Debug.Log($"Moved: {sourcePath} -> {destPath}");
            }
            else
                Debug.LogError(error);
        }

        AssetDatabase.Refresh();
        Debug.Log($"Done. Moved {movedCount} folder(s); others were already under ThirdParty or missing.");
    }
}
#endif
