using UnityEngine;
using UnityEngine.UI;

// Attach to each volume slider. Event wiring and saved-value synchronization are automatic.
[RequireComponent(typeof(Slider))]
[DisallowMultipleComponent]
public sealed class AudioVolumeSlider : MonoBehaviour
{
    public enum VolumeChannel { Master, BGM, SFX }
    [SerializeField] private VolumeChannel channel = VolumeChannel.Master;
    private Slider slider;
    private SoundManager soundManager;

    public static void Bind(Slider target, VolumeChannel targetChannel)
    {
        if (target == null) return;
        AudioVolumeSlider binding = target.GetComponent<AudioVolumeSlider>();
        if (binding == null) binding = target.gameObject.AddComponent<AudioVolumeSlider>();
        binding.channel = targetChannel;
        if (binding.soundManager != null) binding.RefreshValue();
    }

    private void Awake()
    {
        slider = GetComponent<Slider>();
        slider.wholeNumbers = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
    }

    private void OnEnable()
    {
        soundManager = SoundManager.Instance;
        if (soundManager == null) return;
        RefreshValue();
        soundManager.OnVolumeChanged += RefreshValue;
        slider.onValueChanged.AddListener(ChangeVolume);
    }

    private void RefreshValue()
    {
        float value = channel switch
        {
            VolumeChannel.BGM => soundManager.MusicVolume,
            VolumeChannel.SFX => soundManager.EffectsVolume,
            _ => soundManager.MasterVolume
        };
        slider.SetValueWithoutNotify(value);
    }

    private void ChangeVolume(float value)
    {
        if (soundManager == null) return;
        switch (channel)
        {
            case VolumeChannel.BGM: soundManager.SetMusicVolume(value); break;
            case VolumeChannel.SFX: soundManager.SetEffectsVolume(value); break;
            default: soundManager.SetMasterVolume(value); break;
        }
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(ChangeVolume);
        if (soundManager != null)
        {
            soundManager.OnVolumeChanged -= RefreshValue;
            soundManager.SaveVolumeSettings();
        }
        soundManager = null;
    }
}
