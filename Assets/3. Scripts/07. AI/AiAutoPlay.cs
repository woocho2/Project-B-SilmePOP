using System;

// 발표용 자동 플레이: 내 자리도 AI가 대신 둠. 대기실에서 T로 켜고 1 ~ 4로 난이도를 고름 (AutoPlayInput, 출시 빌드에도 있음).
// 켜져 있으면 게임 씬에서 AiPlayer가 내 판을 사람처럼 풂 (사각형을 끌어 터뜨리고, 아이템은 범위를 보여 준 뒤 씀).
// 그동안 내 마우스·아이템 단축키는 막힘. 씬이 바뀌어도 유지되고 다시 T를 누를 때까지 켜져 있음.
public static class AiAutoPlay
{
    public static event Action Changed;

    private static bool isOn;
    private static AiDifficulty difficulty = AiDifficulty.Normal;

    public static bool IsOn
    {
        get => isOn;
        set
        {
            if (isOn == value) return;
            isOn = value;
            Changed?.Invoke();
        }
    }

    public static AiDifficulty Difficulty
    {
        get => difficulty;
        set
        {
            if (difficulty == value) return;
            difficulty = value;
            Changed?.Invoke();
        }
    }

    public static string DifficultyName(AiDifficulty value)
    {
        switch (value)
        {
            case AiDifficulty.Easy: return "쉬움";
            case AiDifficulty.Normal: return "보통";
            case AiDifficulty.Hard: return "어려움";
            default: return "지옥";
        }
    }
}
