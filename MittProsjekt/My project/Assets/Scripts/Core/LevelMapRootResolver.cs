using UnityEngine;

// LevelMapRootResolver — GameObject.Find mot liste av mulige rot-navn (by/strand) (PG2202-01 scene-struktur).
// Pensum: unngår én hardkodet streng når kart bygges på nytt i editor.
// Ekstra: legg nye rot-navn øverst i tabellene ved behov; dokumenter i rapport hvis dere endrer procedural output.
public static class LevelMapRootResolver
{
    // Rot etter «Rebuild procedural CITY» (editor-verktøy).
    public const string ProceduralCityRootName = "CityMap";

    // Rot etter «Rebuild procedural BEACH».
    public const string ProceduralBeachRootName = "BeachMap";

    // Første treff med Find — rekkefølge betyr noe.
    public static readonly string[] CityMapRootCandidates =
    {
        "CityMap",
        "ByKart",
        "Level01_Map",
        "CityMap_Zone2",
    };

    public static readonly string[] BeachMapRootCandidates =
    {
        "BeachMap",
        "StrandKart",
        "Level02_Map",
        "IslandMap",
        "BeachMap_Zone3",
    };

    public static GameObject FindCityMapRoot() => FindFirst(CityMapRootCandidates);

    public static GameObject FindBeachMapRoot() => FindFirst(BeachMapRootCandidates);

    public static string CityCandidatesHint =>
        string.Join(", ", CityMapRootCandidates);

    public static string BeachCandidatesHint =>
        string.Join(", ", BeachMapRootCandidates);

    static GameObject FindFirst(string[] names)
    {
        if (names == null) return null;
        foreach (string n in names)
        {
            if (string.IsNullOrEmpty(n)) continue;
            GameObject go = GameObject.Find(n);
            if (go != null) return go;
        }
        return null;
    }
}
