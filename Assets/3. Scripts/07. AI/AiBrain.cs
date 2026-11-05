using System;
using System.Collections.Generic;

// AI가 한 번에 무엇을 할지 고름 (기획 [5]). 순수 C#이라 화면 없이 테스트됨.
// 수 고르기: 목표 합이 되는 사각형을 모두 찾고, 정확도만큼은 전략(AiStrategy)으로 가장 좋은 수, 나머지는 아무 수.
//           실패 드래그(합이 틀린 수)는 하지 않음.
// 아이템: 정확도만큼은 쓸 좋은 때를 알아채고(AiSkillAdvisor), 막혔을 때는 늘 도움이 되는 것을 찾아 씀.
//        계획하지 않는 난이도(쉬움)는 막혔을 때 아무 아이템이나 아무 데나 씀.
public static class AiBrain
{
    // 둘 수 있는 수가 없으면 false
    public static bool TryChooseMove(BoardModel board, int targetSum, AiDifficulty difficulty, Random random,
                                     out List<BoardSlime> move)
    {
        List<BoardRect> candidates = MatchFinder.FindAll(board, targetSum);
        move = candidates.Count > 0
            ? ChooseMove(board, targetSum, difficulty, candidates, random, false, AiStrategy.Best)
            : null;
        return move != null;
    }

    // focus면 정확도와 상관없이 최선의 수 (조커·돋보기를 쓴 바로 다음)
    public static List<BoardSlime> ChooseMove(BoardModel board, int targetSum, AiDifficulty difficulty,
                                              IReadOnlyList<BoardRect> candidates, Random random, bool focus,
                                              AiStrategy strategy)
    {
        if (!focus && random.NextDouble() >= AiDifficultyTable.Accuracy(difficulty))
        {
            return board.GetSlimesInCells(candidates[random.Next(candidates.Count)].GetCells());
        }

        // 바로 얻는 점수로 줄 세운 뒤, 앞의 몇 개는 둔 뒤의 판까지 봄
        List<KeyValuePair<double, BoardRect>> scored = new List<KeyValuePair<double, BoardRect>>(candidates.Count);
        foreach (BoardRect rect in candidates)
        {
            List<BoardSlime> slimes = board.GetSlimesInCells(rect.GetCells());
            scored.Add(new KeyValuePair<double, BoardRect>(Evaluate(slimes, targetSum, strategy), rect));
        }
        scored.Sort((a, b) => b.Key.CompareTo(a.Key));

        double bestValue = double.MinValue;
        BoardRect best = scored[0].Value;
        for (int i = 0; i < scored.Count && i < Math.Max(1, strategy.Lookahead); i++)
        {
            double value = scored[i].Key;
            if (strategy.Lookahead > 0) value += strategy.FutureWeight * Math.Log(1 + MovesAfter(board, scored[i].Value, targetSum));
            if (value > bestValue)
            {
                bestValue = value;
                best = scored[i].Value;
            }
        }
        return board.GetSlimesInCells(best.GetCells());
    }

    // 수 하나의 바로 얻는 가치 (악마가 들어간 수는 손해, 킹·천사·조커가 들어간 수는 이득) + 마리 수 보정
    public static double Evaluate(IReadOnlyList<BoardSlime> slimes, int targetSum, AiStrategy strategy)
    {
        return ScoreCalculator.Calculate(slimes, targetSum) + strategy.CountWeight * slimes.Count;
    }

    private static int MovesAfter(BoardModel board, BoardRect rect, int targetSum)
    {
        BoardModel after = board.Clone();
        after.Remove(after.GetSlimesInCells(rect.GetCells()));
        return MatchFinder.FindAll(after, targetSum).Count;
    }

    // 지금 쓸 아이템과 칸. 쓰지 않으면 false.
    public static bool TryChooseSkill(BoardModel board, int targetSum, AiDifficulty difficulty, SkillSet skills,
                                      IReadOnlyList<BoardRect> candidates, float timeLeft, Random random,
                                      AiStrategy strategy, out SkillType skill, out int cell)
    {
        skill = SkillType.None;
        cell = -1;
        bool stuck = candidates.Count == 0;

        if (!AiDifficultyTable.PlansSkills(difficulty))
        {
            return stuck && TryChooseAnySkill(board, targetSum, skills, random, out skill, out cell);
        }
        if (!stuck && random.NextDouble() >= AiDifficultyTable.Accuracy(difficulty)) return false;

        float bestMargin = float.MinValue;
        foreach (SkillType type in skills.Available)
        {
            if (skills.UsesLeft(type) <= 0) continue;
            if (!AiSkillAdvisor.TryFindBest(board, targetSum, type, candidates, random, out int target, out float value)) continue;

            float margin = value - AiSkillAdvisor.Threshold(type, timeLeft, stuck, strategy);
            if (margin < 0f || margin <= bestMargin || !skills.CanUse(type, board, target, targetSum)) continue;

            bestMargin = margin;
            skill = type;
            cell = target;
        }
        return skill != SkillType.None;
    }

    // 계획 없이: 남은 아이템 중 아무거나, 쓸 수 있는 칸 중 아무 곳
    private static bool TryChooseAnySkill(BoardModel board, int targetSum, SkillSet skills, Random random,
                                          out SkillType skill, out int cell)
    {
        List<SkillType> types = new List<SkillType>();
        foreach (SkillType type in skills.Available)
        {
            if (skills.UsesLeft(type) > 0) types.Add(type);
        }

        while (types.Count > 0)
        {
            SkillType type = types[random.Next(types.Count)];
            types.Remove(type);

            List<int> usable = new List<int>();
            for (int c = 0; c < GameRules.CellCount; c++)
            {
                if (skills.CanUse(type, board, c, targetSum)) usable.Add(c);
            }
            if (usable.Count == 0) continue;

            skill = type;
            cell = usable[random.Next(usable.Count)];
            return true;
        }

        skill = SkillType.None;
        cell = -1;
        return false;
    }
}
