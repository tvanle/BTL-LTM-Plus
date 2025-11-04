using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip buttonClick;
    [SerializeField] private AudioClip screenTransition;

    [Header("Game Sounds")]
    [SerializeField] private AudioClip wordCorrect;
    [SerializeField] private AudioClip wordCorrectLast;
    [SerializeField] private AudioClip[] tileSounds;
    [SerializeField] private AudioClip hint;
    [SerializeField] private AudioClip[] hintSteps;

    [Header("Completion Sounds")]
    [SerializeField] private AudioClip levelComplete;
    [SerializeField] private AudioClip packComplete;
    [SerializeField] private AudioClip chapterComplete;

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Settings")]
    [SerializeField] private bool sfxEnabled = true;
    [SerializeField] private bool musicEnabled = true;
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
            this.Initialize();
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void Initialize()
    {
        // Create AudioSources if not assigned
        if (this.sfxSource == null)
        {
            this.sfxSource = this.gameObject.AddComponent<AudioSource>();
            this.sfxSource.playOnAwake = false;
        }

        if (this.musicSource == null)
        {
            this.musicSource = this.gameObject.AddComponent<AudioSource>();
            this.musicSource.playOnAwake = false;
            this.musicSource.loop = true;
        }

        // Load settings
        this.LoadSettings();

        // Apply settings
        this.sfxSource.volume = this.sfxVolume;
        this.musicSource.volume = this.musicVolume;

        // Start background music
        if (this.musicEnabled && this.backgroundMusic != null)
        {
            this.PlayMusic(this.backgroundMusic);
        }
    }

    private void LoadSettings()
    {
        this.sfxEnabled = PlayerPrefs.GetInt("SFX_Enabled", 1) == 1;
        this.musicEnabled = PlayerPrefs.GetInt("Music_Enabled", 1) == 1;
        this.sfxVolume = PlayerPrefs.GetFloat("SFX_Volume", 1f);
        this.musicVolume = PlayerPrefs.GetFloat("Music_Volume", 0.5f);
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt("SFX_Enabled", this.sfxEnabled ? 1 : 0);
        PlayerPrefs.SetInt("Music_Enabled", this.musicEnabled ? 1 : 0);
        PlayerPrefs.SetFloat("SFX_Volume", this.sfxVolume);
        PlayerPrefs.SetFloat("Music_Volume", this.musicVolume);
        PlayerPrefs.Save();
    }

    // ============================================
    // Public API
    // ============================================

    public void PlayButtonClick()
    {
        this.PlaySFX(this.buttonClick);
    }

    public void PlayScreenTransition()
    {
        this.PlaySFX(this.screenTransition);
    }

    public void PlayWordCorrect()
    {
        this.PlaySFX(this.wordCorrect);
    }

    public void PlayWordCorrectLast()
    {
        this.PlaySFX(this.wordCorrectLast);
    }

    public void PlayTileSound(int index)
    {
        if (this.tileSounds != null && this.tileSounds.Length > 0)
        {
            var clip = this.tileSounds[index % this.tileSounds.Length];
            this.PlaySFX(clip);
        }
    }

    public void PlayHint()
    {
        this.PlaySFX(this.hint);
    }

    public void PlayHintStep(int step)
    {
        if (this.hintSteps != null && step < this.hintSteps.Length)
        {
            this.PlaySFX(this.hintSteps[step]);
        }
    }

    public void PlayLevelComplete()
    {
        this.PlaySFX(this.levelComplete);
    }

    public void PlayPackComplete()
    {
        this.PlaySFX(this.packComplete);
    }

    public void PlayChapterComplete()
    {
        this.PlaySFX(this.chapterComplete);
    }

    // ============================================
    // Multiplayer Sounds
    // ============================================

    public void PlayPlayerJoined()
    {
        // Use bling sound for player joined
        this.PlaySFX(this.buttonClick);
    }

    public void PlayPlayerLeft()
    {
        // Use a softer sound for player left
        this.PlaySFX(this.buttonClick, 0.5f);
    }

    public void PlayGameStarted()
    {
        // Use chapter complete for game started
        this.PlaySFX(this.chapterComplete);
    }

    public void PlayRoomCreated()
    {
        this.PlaySFX(this.buttonClick);
    }

    // ============================================
    // Core Sound Methods
    // ============================================

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && this.sfxEnabled && this.sfxSource != null)
        {
            this.sfxSource.PlayOneShot(clip, this.sfxVolume);
        }
    }

    public void PlaySFX(AudioClip clip, float volume)
    {
        if (clip != null && this.sfxEnabled && this.sfxSource != null)
        {
            this.sfxSource.PlayOneShot(clip, volume * this.sfxVolume);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip != null && this.musicEnabled && this.musicSource != null)
        {
            if (this.musicSource.clip != clip)
            {
                this.musicSource.clip = clip;
                this.musicSource.Play();
            }
        }
    }

    public void StopMusic()
    {
        if (this.musicSource != null)
        {
            this.musicSource.Stop();
        }
    }

    // ============================================
    // Settings
    // ============================================

    public void SetSFXEnabled(bool enabled)
    {
        this.sfxEnabled = enabled;
        this.SaveSettings();
    }

    public void SetMusicEnabled(bool enabled)
    {
        this.musicEnabled = enabled;
        if (!enabled)
        {
            this.StopMusic();
        }
        else if (this.backgroundMusic != null)
        {
            this.PlayMusic(this.backgroundMusic);
        }
        this.SaveSettings();
    }

    public void SetSFXVolume(float volume)
    {
        this.sfxVolume = Mathf.Clamp01(volume);
        if (this.sfxSource != null)
        {
            this.sfxSource.volume = this.sfxVolume;
        }
        this.SaveSettings();
    }

    public void SetMusicVolume(float volume)
    {
        this.musicVolume = Mathf.Clamp01(volume);
        if (this.musicSource != null)
        {
            this.musicSource.volume = this.musicVolume;
        }
        this.SaveSettings();
    }

    public bool IsSFXEnabled() => this.sfxEnabled;
    public bool IsMusicEnabled() => this.musicEnabled;
    public float GetSFXVolume() => this.sfxVolume;
    public float GetMusicVolume() => this.musicVolume;
}
