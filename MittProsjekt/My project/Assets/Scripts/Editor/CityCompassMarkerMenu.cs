using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// CityCompassMarkerMenu — legger inn CompassObjectiveMarker for utgangspil (PG2202-08 HUD-støtte).
// Pensum: ett tomt objekt + script; flyttes i Scene view.
// Ekstra: meny for rask plassering; DisplayDialog på norsk her er OK for intern gruppehjelp.
public static class CityCompassMarkerMenu
{
    [MenuItem("CartoonZombies/Level Art/Add Compass exit marker (HUD arrow target)", false, 103)]
    public static void AddCompassMarker()
    {
        if (Application.isPlaying) return;

        GameObject go = new GameObject("CompassExitMarker");
        go.AddComponent<CompassObjectiveMarker>();
        Undo.RegisterCreatedObjectUndo(go, "Compass exit marker");
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("CompassExitMarker",
            "Objektet er opprettet. Flytt det dit du vil at pilen skal peke når alle zombie-bølger er ferdig " +
            "(f.eks. ved utgang / heis).\n\nDu kan legge et tomt barn foran døren og dra det inn i " +
            "«Target Override» på komponenten.", "OK");
    }

    [MenuItem("CartoonZombies/Level Art/Add Compass exit marker (HUD arrow target)", true)]
    private static bool AddCompassMarkerValidate() => !Application.isPlaying;
}
