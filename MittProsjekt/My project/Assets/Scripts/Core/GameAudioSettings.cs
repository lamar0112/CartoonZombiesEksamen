using UnityEngine;

// GameAudioSettings — ScriptableObject med BGM-klipp og volum per sone (PG2202-08 ScriptableObject; PG2202-10 lyd).
// Pensum: én asset som AudioManager leser; enklere å bytte musikk uten å bygge på nytt.
// Ekstra: sfxGameplayScale — finjustering av SFX vs musikk for balanse på eksamensmaskiner.
[CreateAssetMenu(fileName = "GameAudioSettings", menuName = "CartoonZombies/Game Audio Settings")]
public class GameAudioSettings : ScriptableObject
{
    [Header("Bakgrunnsmusikk per sone")]
    public AudioClip menuMusic;
    public AudioClip cityMusic;
    public AudioClip beachMusic;

    [Header("Volum per sone (× master-slider; 1 = standard, 0 = stille, 2 = høyere)")]
    [Range(0f, 2f)] public float menuMusicVolume  = 1f;
    [Range(0f, 2f)] public float cityMusicVolume  = 1f;
    [Range(0f, 2f)] public float beachMusicVolume = 1f;

    [Header("SFX via AudioManager")]
    [Tooltip("Ekstra multiplikator på toppen av master-volum (SFX-kanal). 0 = stille, 1 = standard.")]
    [Range(0f, 1f)] public float sfxGameplayScale = 1f;
}
