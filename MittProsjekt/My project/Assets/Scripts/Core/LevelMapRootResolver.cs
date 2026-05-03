using UnityEngine;

/// <summary>
/// Finner rot-objekt for by- og strand-nivå uten hardkoding til gamle procedural-navn.
/// Legg egne hierarchy-navn øverst i kandidatlistene.
/// </summary>
public static class LevelMapRootResolver
{
    /// <summary>Navn som brukes når «Rebuild procedural CITY» kjøres (nytt kart).</summary>
    public const string ProceduralCityRootName = "CityMap";

    /// <summary>Navn som brukes når «Rebuild procedural BEACH» kjøres (nytt kart).</summary>
    public const string ProceduralBeachRootName = "BeachMap";

    /// <summary>Første <see cref="GameObject.Find"/> som treffer.</summary>
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
