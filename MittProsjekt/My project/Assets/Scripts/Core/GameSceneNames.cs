// GameSceneNames — const-strenger for Build Settings og SceneManager.LoadScene (PG2202-12).
// Pensum: én sann kilde unngår «magic strings» spredt i prosjektet.
// Ekstra: norske scene-filer kan ha ulike navn i prosjektet — disse må matche eksakt det som står i File → Build Settings.
public static class GameSceneNames
{
    public const string MainMenu = "MainMenu";
    public const string Level01By = "Level01_By";
    public const string Level02StrandSkog = "Level02_StrandSkog";
    public const string GameOver = "GameOver";
    public const string Win = "Win";
}
