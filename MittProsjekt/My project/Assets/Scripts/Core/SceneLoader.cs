using UnityEngine;
using UnityEngine.SceneManagement; // SceneManager ligger i dette namespace (PG2202-12)

// Statisk wrapper rundt SceneManager
// Samler all scene-lasting på ett sted - enklere å endre scene-navn hvis nødvendig
public static class SceneLoader
{
    // Laster en navngitt scene - scenen MÅ være lagt til i Build Settings
    public static void Load(string sceneName)
    {
        // Sjekker at scenen finnes i Build Settings før vi prøver å laste
        bool found = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            // path er på formen "Assets/Scenes/Level01_By.unity"
            if (path.Contains(sceneName))
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogError(
                $"[SceneLoader] Scene '{sceneName}' is not in Build Settings.\n" +
                "Fix: CartoonZombies → Project → Add scenes to Build Settings");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    // Laster inn den aktive scenen på nytt - brukes til "prøv igjen"-funksjon
    public static void Reload()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
