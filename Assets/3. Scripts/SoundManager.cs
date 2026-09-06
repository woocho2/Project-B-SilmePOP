using UnityEngine;

// Resources/SoundData is loaded once; audio survives scene changes.
public sealed class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    private SoundData data;
    private AudioSource musicSource;
    private AudioSource[] effectSources;
    private int nextVoice;
    private const int VoiceCount = 12;
    public float MasterVolume { get; private set; } = 1f;
    public float MusicVolume { get; private set; } = 0.5f;
    public float EffectsVolume { get; private set; } = 1f;
    public event System.Action OnVolumeChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null) new GameObject("SoundManager").AddComponent<SoundManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        data = Resources.Load<SoundData>("SoundData");
        musicSource = CreateSource();
        musicSource.loop = true;
        effectSources = new AudioSource[VoiceCount];
        for (int i = 0; i < VoiceCount; i++) effectSources[i] = CreateSource();
        SetMasterVolume(PlayerPrefs.GetFloat("Audio.MasterVolume", 1f));
        SetMusicVolume(PlayerPrefs.GetFloat("Audio.MusicVolume", data != null ? data.musicVolume : 0.5f));
        SetEffectsVolume(PlayerPrefs.GetFloat("Audio.EffectsVolume", data != null ? data.effectsVolume : 1f));
        if (data != null) PlayMusic(data.backgroundMusic);
    }

    private AudioSource CreateSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    public static void Play(SoundEffect effect)
    {
        if (Instance != null && Instance.data != null)
            Instance.PlayClip(Instance.data.GetClip(effect));
    }

    public void PlayClip(AudioClip clip)
    {
        if (clip == null) return;
        // Reuse idle voices and cap simultaneous playback without allocating objects.
        for (int i = 0; i < effectSources.Length; i++)
        {
            int index = (nextVoice + i) % effectSources.Length;
            if (effectSources[index].isPlaying) continue;
            nextVoice = index;
            break;
        }
        AudioSource source = effectSources[nextVoice];
        source.clip = clip;
        source.Play();
        nextVoice = (nextVoice + 1) % effectSources.Length;
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || (musicSource.clip == clip && musicSource.isPlaying)) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic() => musicSource.Stop();

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);
        MusicVolume = value;
        PlayerPrefs.SetFloat("Audio.MusicVolume", value);
        ApplyVolumes();
    }

    public void SetEffectsVolume(float value)
    {
        value = Mathf.Clamp01(value);
        EffectsVolume = value;
        PlayerPrefs.SetFloat("Audio.EffectsVolume", value);
        ApplyVolumes();
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("Audio.MasterVolume", MasterVolume);
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        musicSource.volume = MasterVolume * MusicVolume;
        foreach (AudioSource source in effectSources) source.volume = MasterVolume * EffectsVolume;
        OnVolumeChanged?.Invoke();
    }

    public void SaveVolumeSettings() => PlayerPrefs.Save();

    private void OnApplicationQuit() => SaveVolumeSettings();

    private void OnApplicationPause(bool paused)
    {
        if (paused) PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
