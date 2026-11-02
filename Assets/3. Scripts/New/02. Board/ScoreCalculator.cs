using System.Collections.Generic;

// 점수 계산 (기획 [4-2]). 숫자는 GameRules에서 읽음.
public static class ScoreCalculator
{
    // 점수 = B + Σ천사(B x 2) + Σ악마(B x -1) + Σ킹(10) + Σ조커(목표 합 x 2). B = 기본점수 합.
    public static int Calculate(IReadOnlyList<BoardSlime> slimes, int targetSum)
    {
        int baseSum = 0;
        foreach (BoardSlime slime in slimes)
        {
            baseSum += slime.BaseScore;
        }

        int score = baseSum;
        foreach (BoardSlime slime in slimes)
        {
            switch (slime.Type)
            {
                case SlimeType.Angel:
                    score += baseSum * GameRules.AngelMultiplier;
                    break;
                case SlimeType.Devil:
                    score += baseSum * GameRules.DevilMultiplier;
                    break;
                case SlimeType.King:
                    score += GameRules.KingBonus;
                    break;
                case SlimeType.Joker:
                    score += targetSum * GameRules.JokerTargetMultiplier;
                    break;
            }
        }
        return score;
    }

    // 파괴 스킬: 기본점수만 (특수점수 없음).
    public static int DestroyScore(BoardSlime slime)
    {
        return slime.BaseScore;
    }

    // 합체 결과가 0이라 바로 터졌을 때.
    public static int MergeZeroScore(int mergedCount)
    {
        return mergedCount * GameRules.MergeZeroScorePerSlime;
    }
}
