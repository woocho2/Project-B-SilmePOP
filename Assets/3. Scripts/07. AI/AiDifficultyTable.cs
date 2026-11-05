// AI 난이도별 수치 (기획 [5]). 수 고르기·아이템 쓰기는 모두 같은 전략(AiStrategy.Best)을 쓰고 정확도와 반응 시간만 다름.
// 정확도 = 생각할 때마다 최선의 수를 고를 확률이자, 아이템을 쓸 좋은 때를 알아챌 확률. 나머지는 보이는 수 아무거나.
// 반응 시간: 처음에는 120초 판에서 목표 제거 수(쉬움 60 ~ 80, 보통 80 ~ 100, 어려움 100 ~ 120)에 맞췄고,
// 직접 해 보니 목표 합 11 ~ 15와 특수 슬라임·아이템까지 챙기기엔 사람이 훨씬 바빠서, 판을 180초로 늘리고 반응 시간을 2배로 함.
// 아이템도 판마다 1개로 줄임. 기록 모드(목표 10·11 각 10판, 나 평균 108 / 88마리)에 맞춰 다시 정하고, 11 ~ 15는 반응 시간 1.2배 (ReactionScale, 아래 숫자는 1.25배일 때).
//                반응 시간(랜덤)   정확도   시뮬레이션 평균 (180초 판, 아이템 1개, 200판) 목표 10 / 11 / 15
//   쉬움        : 4.8 ~ 9.6초       0%      64 / 55 / 67마리  (보이는 대로 아무 수, 막히면 아무 아이템이나 아무 데나)
//   보통        : 3.6 ~ 7.2초       33%     82 / 68 / 83마리
//   어려움      : 2.8 ~ 5.6초       67%    100 / 85 / 103마리
//   지옥        : 2.4 ~ 4.8초       100%   110 / 118 / 151마리  (늘 같은 속도)
// 직접 해 보고 너무 쉽거나 어려우면 반응 시간(또는 HighSumReactionScale)만 고치면 됨.
public static class AiDifficultyTable
{
    public static float MinReaction(AiDifficulty difficulty)
    {
        switch (difficulty)
        {
            case AiDifficulty.Easy: return 4.8f;
            case AiDifficulty.Normal: return 3.6f;
            case AiDifficulty.Hard: return 2.8f;
            case AiDifficulty.Hell: return 2.4f;
            default: return 1f;
        }
    }

    public static float MaxReaction(AiDifficulty difficulty)
    {
        switch (difficulty)
        {
            case AiDifficulty.Easy: return 9.6f;
            case AiDifficulty.Normal: return 7.2f;
            case AiDifficulty.Hard: return 5.6f;
            case AiDifficulty.Hell: return 4.8f;
            default: return 2f;
        }
    }

    // 목표 합 11 ~ 15는 사람이 훨씬 느려서 쉬움·보통·어려움은 반응 시간을 이만큼 늘림 (10은 그대로, 지옥은 늘 그대로)
    public const float HighSumReactionScale = 1.2f;

    public static float ReactionScale(AiDifficulty difficulty, int targetSum)
    {
        if (difficulty == AiDifficulty.Hell || targetSum <= GameRules.MinTargetSum) return 1f;
        return HighSumReactionScale;
    }

    public static float Accuracy(AiDifficulty difficulty)
    {
        switch (difficulty)
        {
            case AiDifficulty.Easy: return 0f;
            case AiDifficulty.Normal: return 1/3f;
            case AiDifficulty.Hard: return 2/3f;
            default: return 1f;
        }
    }

    // 아이템을 따져 보고 쓰는가. 아니면(쉬움) 막혔을 때 아무거나 씀.
    public static bool PlansSkills(AiDifficulty difficulty)
    {
        return difficulty != AiDifficulty.Easy;
    }
}
