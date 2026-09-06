using UnityEngine;

// Attach to an object, assign a clip, then connect Play to a UnityEvent/animation event.
public sealed class SoundPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    [SerializeField] private SoundEffect effect = SoundEffect.Click;
    [SerializeField] private bool playOnEnable;

    private void OnEnable()
    {
        if (playOnEnable) Play();
    }

    public void Play()
    {
        if (clip != null)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayClip(clip);
        }
        else SoundManager.Play(effect);
    }

    public void SetMusicVolume(float value)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetMusicVolume(value);
    }

    public void SetMasterVolume(float value)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetMasterVolume(value);
    }

    public void SetEffectsVolume(float value)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetEffectsVolume(value);
    }
}
