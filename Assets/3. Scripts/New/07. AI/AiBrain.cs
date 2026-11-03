using System;
using System.Collections.Generic;

// AI가 한 번에 무엇을 할지 고름 (기획 [5]). 순수 C#이라 화면 없이 테스트됨.
// 수 고르기: 목표 합이 되는 사각형을 모두 찾고, 정답확률만큼은 점수가 가장 높은 수, 나머지는 아무 수.
//           실패 드래그(합이 틀린 수)는 하지 않음.
// 막혔을 때: 난이도별 스킬을 앞에서부터, 쓸 수 있는 칸 중 아무 곳에 씀.
public static class AiBrain
{
    // 둘 수 있는 수가 없으면 false
    public static bool TryChooseMove(BoardModel board, int targetSum, AiDifficulty difficulty, Random random,
                                     out List<BoardSlime> move)
    {
        move = null;
        List<BoardRect> candidates = MatchFinder.FindAll(board, targetSum);
        if (candidates.Count == 0) return false;

        if (random.NextDouble() >= AiDifficultyTable.Accuracy(difficulty))
        {
            move = board.GetSlimesInCells(candidates[random.Next(candidates.Count)].GetCells());
            return true;
        }

        int bestScore = int.MinValue;
        foreach (BoardRect rect in candidates)
        {
            List<BoardSlime> slimes = board.GetSlimesInCells(rect.GetCells());
            int score = Evaluate(slimes, targetSum);
            if (score > bestScore)
            {
                bestScore = score;
                move = slimes;
            }
        }
        return true;
    }

    // 수 하나의 가치. 지금은 바로 얻는 점수 (악마가 들어간 수는 손해, 킹·천사·조커가 들어간 수는 이득).
    // 같은 점수면 슬라임을 적게 쓰는 수 (남은 판에 숫자가 더 남아 다음 수가 잘 막히지 않음).
    public static int Evaluate(IReadOnlyList<BoardSlime> slimes, int targetSum)
    {
        return ScoreCalculator.Calculate(slimes, targetSum) * 10 - slimes.Count;
    }

    // 막혔을 때 쓸 스킬과 칸. 쓸 것이 없으면 false.
    public static bool TryChooseRescue(BoardModel board, int targetSum, AiDifficulty difficulty, SkillSet skills,
                                       Random random, out SkillType skill, out int cell)
    {
        foreach (SkillType type in AiDifficultyTable.RescueSkills(difficulty))
        {
            if (skills.UsesLeft(type) <= 0) continue;

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
