using UnityEngine;

public enum SoundEffect { Click, Match, Miss, Hint, Mix, Destroy, GameOver, Countdown, GameStart }

[CreateAssetMenu(fileName = "SoundData", menuName = "SlimeGame/Sound Data")]
public sealed class SoundData : ScriptableObject
{
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float effectsVolume = 1f;
    public AudioClip click;
    public AudioClip match;
    public AudioClip miss;
    public AudioClip hint;
    public AudioClip mix;
    public AudioClip destroy;
    public AudioClip gameOver;
    public AudioClip countdown;
    public AudioClip gameStart;

    public AudioClip GetClip(SoundEffect effect) => effect switch
    {
        SoundEffect.Click => click,
        SoundEffect.Match => match,
        SoundEffect.Miss => miss,
        SoundEffect.Hint => hint,
        SoundEffect.Mix => mix,
        SoundEffect.Destroy => destroy,
        SoundEffect.GameOver => gameOver,
        SoundEffect.Countdown => countdown,
        SoundEffect.GameStart => gameStart,
        _ => null
    };
}
