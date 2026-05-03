using UnityEngine;

// Singleton AudioManager - håndterer all lyd i spillet (PG2202-10)
// DontDestroyOnLoad gjør at musikken ikke stopper ved sceneskifte (PG2202-12)
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music — assign GameAudioSettings (Assets/ScriptableObjects/GameAudioSettings)")]
    [SerializeField] private GameAudioSettings musicLibrary;

    // AudioSourcene hentes automatisk fra GameObject - trenger ikke dras inn manuelt
    // Første AudioSource = BGM, andre AudioSource = SFX
    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private float _bgmTrackGain = 1f;

    private void Awake()
    {
        // Singleton-mønster (PG2202-12)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);

        // Henter begge AudioSource-komponentene automatisk fra samme GameObject
        AudioSource[] sources = GetComponents<AudioSource>();

        if (sources.Length < 2)
        {
            Debug.LogError("AudioManager needs two AudioSource components on the same GameObject.");
            return;
        }

        if (musicLibrary == null)
            Debug.LogError("AudioManager: Assign the GameAudioSettings asset (Project → Assets/ScriptableObjects/GameAudioSettings).");

        bgmSource      = sources[0]; // første = musikk
        sfxSource      = sources[1]; // andre = lydeffekter
        bgmSource.loop = true;       // musikk looper alltid

        // Henter lagret volum fra PlayerPrefs ved oppstart
        SetVolume(SaveSystem.GetVolume());
    }

    private void ApplyBgmVolume()
    {
        if (bgmSource == null) return;
        float master = SaveSystem.GetVolume();
        bgmSource.volume = Mathf.Clamp01(master * 0.5f * _bgmTrackGain);
    }

    private void PlayBGM(AudioClip clip, float trackGain)
    {
        if (clip == null || bgmSource == null) return;
        _bgmTrackGain = Mathf.Max(0f, trackGain);

        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            ApplyBgmVolume();
            return;
        }

        bgmSource.clip = clip;
        ApplyBgmVolume();
        bgmSource.Play();
    }

    public void StopBGM() => bgmSource.Stop();

    // PlayOneShot lar lyder overlappe seg selv - bra for skudd-lyder (PG2202-10)
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        float libScale = musicLibrary != null ? musicLibrary.sfxGameplayScale : 1f;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(libScale));
    }

    public void SetVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (sfxSource != null) sfxSource.volume = volume;
        SaveSystem.SaveVolume(volume);
        ApplyBgmVolume();
    }

    // Offentlige metoder for å spille musikk per sone (PG2202-10)
    public void PlayMenuMusic()  => PlayBGM(musicLibrary?.menuMusic,  musicLibrary != null ? musicLibrary.menuMusicVolume  : 1f);
    public void PlayCityMusic()  => PlayBGM(musicLibrary?.cityMusic,  musicLibrary != null ? musicLibrary.cityMusicVolume  : 1f);
    public void PlayBeachMusic() => PlayBGM(musicLibrary?.beachMusic, musicLibrary != null ? musicLibrary.beachMusicVolume : 1f);

    public float CurrentVolume => bgmSource.volume;
}
