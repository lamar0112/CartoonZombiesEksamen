using System.Linq;
using UnityEditor;
using UnityEngine;

// Removes outdated scene entries from Build Settings (old Zone-* paths, pre-rename level scenes).
// Menu: CartoonZombies → Project → Strip legacy scenes from Build Settings
public static class BuildSettingsLegacyCleanupTool
{
    private const string Menu = "CartoonZombies/Project/Strip legacy scenes from Build Settings";

    [MenuItem(Menu, false, 20)]
    private static void Run()
    {
        var scenes = EditorBuildSettings.scenes;
        var filtered = scenes.Where(s => !IsLegacyPath(s.path)).ToArray();

        if (filtered.Length == scenes.Length)
        {
            EditorUtility.DisplayDialog("Build Settings", "No legacy scene paths found.", "OK");
            return;
        }

        EditorBuildSettings.scenes = filtered;
        EditorUtility.DisplayDialog("Done",
            $"Removed {scenes.Length - filtered.Length} outdated scene reference(s) from Build Settings.\n\n" +
            "Use CartoonZombies → Project → Add scenes to Build Settings if anything is missing.", "OK");
    }

    private static bool IsLegacyPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        return path.Contains("Zone1_Castle")
            || path.Contains("Zone2_City")
            || path.Contains("Zone3_Beach")
            || path.Contains("Level1Byen")
            || path.Contains("Level2StrandSkog");
    }

    [MenuItem(Menu, true)]
    private static bool Validate() => !Application.isPlaying;
}
