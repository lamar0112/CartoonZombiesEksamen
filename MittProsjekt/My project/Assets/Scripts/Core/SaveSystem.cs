using UnityEngine;

// Statisk hjelpeklasse for lagring og lasting av spillerdata (PG2202-12)
// Bruker Unity sin innebygde PlayerPrefs - lagrer nøkkel/verdi-par lokalt på maskinen
// Fordel: enkel å bruke fra alle scripts uten å opprette en instans
public static class SaveSystem
{
    // Konstanter for nøkler - unngår skrivefeil ved bruk
    private const string KEY_HIGHSCORE = "HighScore";
    private const string KEY_LAST_RUN  = "LastRunKills";
    private const string KEY_ZONE      = "ZoneUnlocked";
    private const string KEY_VOLUME    = "MasterVolume";

    // Highscore - antall drepte zombier totalt
    public static void SaveHighScore(int score)  => PlayerPrefs.SetInt(KEY_HIGHSCORE, score);
    public static int  GetHighScore()            => PlayerPrefs.GetInt(KEY_HIGHSCORE, 0); // 0 som standard

    /// <summary>Drepte i siste runde som endte i meny (hovedmeny / game over / win).</summary>
    public static void SaveLastRunKills(int kills) => PlayerPrefs.SetInt(KEY_LAST_RUN, kills);
    public static int  GetLastRunKills()          => PlayerPrefs.GetInt(KEY_LAST_RUN, -1);

    // Hvilket nivå som er låst opp (1 = by, 2 = strand) — evt. brukt til fremtidig level select
    public static void SaveZoneUnlocked(int zone) => PlayerPrefs.SetInt(KEY_ZONE, zone);
    public static int  GetZoneUnlocked()          => PlayerPrefs.GetInt(KEY_ZONE, 1);

    // Lydvolum - kobles til slider i innstillinger
    public static void SaveVolume(float vol)  => PlayerPrefs.SetFloat(KEY_VOLUME, vol);
    public static float GetVolume()           => PlayerPrefs.GetFloat(KEY_VOLUME, 0.4f); // 40% som standard

    // Sletter all lagret data - nyttig for testing og "ny spiller"-funksjon
    public static void DeleteAll() => PlayerPrefs.DeleteAll();
}
