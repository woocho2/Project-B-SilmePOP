// AI 난이도별 수치 (기획 [5] 표).
//                반응 시간(랜덤)   정답확률   막히면 스킬
//   쉬움        : 2 ~ 5초           50%        안 씀
//   보통        : 1 ~ 4초           70%        숫자변경, 파괴
//   어려움      : 0.75 ~ 2초        90%        변신, 합체까지
//   지옥        : 0.5 ~ 1초         100%       변신, 합체까지
public static class AiDifficultyTable
{
    public static float MinReaction(AiDifficulty difficulty)
    {
        switch (difficulty)
        {
            case AiDifficulty.Easy: return 2f;
            case AiDifficulty.Normal: return 1f;
            case AiDifficulty.Hard: return 0.75f;
            default: return 0.5f;
        }
    }

    public static float MaxReaction(AiDifficulty difficulty)
    {
        switch (difficulty)
        {
            case AiDifficulty.Easy: return 5f;
            case AiDifficulty.Normal: return 4f;
            case AiDifficulty.Hard: return 2f;
            default: return 1f;
        }
    }

    // 최선의 수를 고를 확률. 나머지는 가능한 수 중 아무거나.
    public static float Accuracy(AiDifficulty difficulty)
    {
        switch (difficulty)
        {
            case AiDifficulty.Easy: return 0.5f;
            case AiDifficulty.Normal: return 0.7f;
            case AiDifficulty.Hard: return 0.9f;
            default: return 1f;
        }
    }

    // 판이 막혔을 때 써 보는 스킬 (앞에서부터). 쉬움은 쓰지 않음.
    public static SkillType[] RescueSkills(AiDifficulty difficulty)
    {
        switch (difficulty)
        {
            case AiDifficulty.Easy: return new SkillType[0];
            case AiDifficulty.Normal: return new[] { SkillType.NumberChange, SkillType.Destroy };
            default: return new[] { SkillType.Transform, SkillType.NumberChange, SkillType.Merge, SkillType.Destroy };
        }
    }
}
