using System;
using System.Collections.Generic;

// AI가 아이템을 쓸 만한 곳과 그 가치(점수로 환산)를 찾음. 순수 C#.
//   가치 = 바로 얻는 점수 + 새로 생기는 조합의 점수 − 없어지거나 바뀌는 슬라임이 앞으로 벌어 줄 점수
//   조커     : 무작위 기본 슬라임이 조커가 됨 → 기본 슬라임마다 조커가 되면 바로 얻는 이득의 평균
//   합체     : 2 x 2 숫자 합의 일의 자리가 0이면 바로 터져 마리 수 x 4점 (막혔을 때는 합쳐서 조합이 생기는 곳도)
//   파괴     : 없애면 남은 슬라임으로 조합이 생기는 칸 (특히 조합을 0점으로 만드는 악마)
//   숫자변경 : 바뀔 수 있는 8가지 숫자마다 생기는 조합의 평균 점수
//   변신     : 막혔을 때만. 슬라임이 많은 곳 몇 군데를 미리 몇 번 굴려 보고 평균
//   돋보기   : 조합이 거의 안 남았을 때 (다음 수를 바로 찾게 해 줌)
// 결과가 랜덤인 아이템도 실제 결과(스킬 난수)는 보지 않음. 사람처럼 가능성만 따짐.
public static class AiSkillAdvisor
{
    public const int FewMoves = 2;              // 이만큼 이하로 남으면 돋보기를 씀
    private const float StuckThreshold = 0.5f;  // 막혔을 때는 조금이라도 도움이 되면 씀
    private const int TransformCenters = 5;
    private const int TransformSamples = 4;

    private static readonly MergeSkill Merge = new MergeSkill();
    private static readonly TransformSkill Transform = new TransformSkill();
    private static readonly int[] AllCells = CreateAllCells();

    private static int[] CreateAllCells()
    {
        int[] cells = new int[GameRules.CellCount];
        for (int c = 0; c < cells.Length; c++) cells[c] = c;
        return cells;
    }

    public static bool TryFindBest(BoardModel board, int targetSum, SkillType skill, IReadOnlyList<BoardRect> candidates,
                                   Random random, out int cell, out float value)
    {
        bool stuck = candidates.Count == 0;
        switch (skill)
        {
            case SkillType.Joker: return FindJoker(board, targetSum, candidates, random, out cell, out value);
            case SkillType.Merge: return FindMerge(board, targetSum, stuck, out cell, out value);
            case SkillType.Destroy: return FindDestroy(board, targetSum, out cell, out value);
            case SkillType.NumberChange: return FindNumberChange(board, targetSum, candidates, out cell, out value);
            case SkillType.Transform: return FindTransform(board, targetSum, stuck, random, out cell, out value);
            case SkillType.Magnifier:
                cell = -1;
                value = 1f;
                return !stuck && candidates.Count <= FewMoves;
            default:
                cell = -1;
                value = 0f;
                return false;
        }
    }

    // 이만큼 이득이어야 씀
    public static float Threshold(SkillType skill, float timeLeft, bool stuck, AiStrategy strategy)
    {
        if (stuck) return StuckThreshold;

        float threshold;
        switch (skill)
        {
            case SkillType.Joker: threshold = strategy.JokerThreshold; break;
            case SkillType.Merge: threshold = strategy.MergeThreshold; break;
            case SkillType.Destroy: threshold = strategy.DestroyThreshold; break;
            case SkillType.NumberChange: threshold = strategy.NumberChangeThreshold; break;
            case SkillType.Magnifier: return StuckThreshold;
            default: return float.MaxValue;   // 변신은 막혔을 때만
        }
        return timeLeft <= strategy.EndgameTime ? threshold * strategy.EndgameFactor : threshold;
    }

    // 슬라임 하나가 앞으로 벌어 줄 것으로 보는 점수 (아이템으로 없애거나 바꿀 때의 손해)
    public static int Potential(BoardSlime slime)
    {
        switch (slime.Type)
        {
            case SlimeType.King: return slime.BaseScore + GameRules.KingBonus;
            case SlimeType.Angel: return slime.BaseScore + 6;   // 조합 기본점수 합(보통 3 ~ 4) x 2
            case SlimeType.Devil: return -3;                    // 들어간 조합을 0점으로 만드는 방해꾼
            default: return slime.BaseScore;
        }
    }

    // 지금 둘 수 있는 수 중 가장 높은 점수. 없으면 0.
    public static int BestMoveScore(BoardModel board, int targetSum)
    {
        int best = 0;
        foreach (BoardRect rect in MatchFinder.FindAll(board, targetSum))
        {
            best = Math.Max(best, ScoreCalculator.Calculate(board.GetSlimesInCells(rect.GetCells()), targetSum));
        }
        return best;
    }

    private static bool IsTargetable(BoardSlime slime)
    {
        return slime != null && slime.Type != SlimeType.Joker && slime.Type != SlimeType.Merged;
    }

    // 조커는 무작위 기본 슬라임이 됨 (자리를 못 고름). 가치 = 기본 슬라임 아무거나 조커가 됐을 때 바로 얻는 이득의 평균
    // (그 슬라임이 든 조합 중 가장 큰 이득, 조합에 안 든 슬라임은 0). 막혔으면 조커가 움직이다 짝을 만날 수 있으니 조금이라도 씀.
    private static bool FindJoker(BoardModel board, int targetSum, IReadOnlyList<BoardRect> candidates, Random random,
                                  out int cell, out float value)
    {
        cell = -1;
        value = 0f;

        Dictionary<BoardSlime, int> bestGain = new Dictionary<BoardSlime, int>();
        int basics = 0;
        foreach (BoardSlime slime in board.GetSlimesInCells(AllCells))
        {
            if (slime.Type != SlimeType.Basic) continue;
            basics++;
            bestGain[slime] = 0;
        }
        if (basics == 0) return false;

        // 막혔으면: 조커가 움직이다 짝(더하면 목표 합)을 만날 수 있는 기본 슬라임이 하나라도 있을 때만
        if (candidates.Count == 0)
        {
            int[] numberCount = new int[10];
            foreach (BoardSlime slime in board.GetSlimesInCells(AllCells)) numberCount[slime.Number]++;
            foreach (BoardSlime slime in bestGain.Keys)
            {
                int partner = targetSum - slime.Number;
                if (partner < 1 || partner > 9 || numberCount[partner] <= (partner == slime.Number ? 1 : 0)) continue;
                value = StuckThreshold;
                return true;
            }
            return false;
        }

        int jokerBase = GameRules.BaseScore(SlimeType.Joker);
        foreach (BoardRect rect in candidates)
        {
            List<BoardSlime> slimes = board.GetSlimesInCells(rect.GetCells());
            int now = ScoreCalculator.Calculate(slimes, targetSum);
            foreach (BoardSlime slime in slimes)
            {
                if (slime.Type != SlimeType.Basic) continue;
                int gain = ScoreCalculator.CalculateAs(slimes, targetSum, slime, SlimeType.Joker, jokerBase) - now;
                if (gain > bestGain[slime]) bestGain[slime] = gain;
            }
        }

        float total = 0f;
        foreach (int gain in bestGain.Values) total += gain;
        value = total / basics;
        return true;
    }

    private static bool FindMerge(BoardModel board, int targetSum, bool stuck, out int cell, out float value)
    {
        cell = -1;
        value = 0f;

        for (int anchor = 0; anchor < GameRules.CellCount; anchor++)
        {
            if (!board.CanMerge(anchor)) continue;

            List<BoardSlime> slimes = board.GetSlimesInCells(Merge.GetArea(anchor));
            bool popsNow = MatchFinder.SumNumbers(slimes) % 10 == 0;
            if (!popsNow && !stuck) continue;   // 그냥 합치기만 하는 것은 막혔을 때만 봄

            float gain;
            if (popsNow)
            {
                gain = ScoreCalculator.MergeZeroScore(slimes.Count);
                foreach (BoardSlime slime in slimes) gain -= Potential(slime);
            }
            else
            {
                gain = 0f;   // 기본점수는 합체 슬라임이 그대로 가짐. 킹·천사·악마의 효과만 사라짐
                foreach (BoardSlime slime in slimes) gain -= Potential(slime) - slime.BaseScore;
            }

            if (stuck)
            {
                BoardModel after = board.Clone();
                after.Merge(anchor, out _);
                gain += BestMoveScore(after, targetSum);
            }

            if (cell < 0 || gain > value)
            {
                cell = anchor;
                value = gain;
            }
        }
        return cell >= 0;
    }

    private static bool FindDestroy(BoardModel board, int targetSum, out int cell, out float value)
    {
        cell = -1;
        value = 0f;

        for (int c = 0; c < GameRules.CellCount; c++)
        {
            BoardSlime target = board.GetSlimeAt(c);
            if (!IsTargetable(target)) continue;

            // 이 슬라임을 빼면 합이 목표가 되는 사각형 = 없애면 생기는 조합
            int unlocked = 0;
            int sumWithTarget = targetSum + target.Number;
            MatchFinder.ForEachContaining(board, c, sumWithTarget, (rect, sum) =>
            {
                if (sum != sumWithTarget) return;
                List<BoardSlime> rest = board.GetSlimesInCells(rect.GetCells());
                rest.Remove(target);
                if (rest.Count > 0) unlocked = Math.Max(unlocked, ScoreCalculator.Calculate(rest, targetSum));
            });

            float gain = ScoreCalculator.DestroyScore(target) + unlocked - Potential(target);
            if (cell < 0 || gain > value)
            {
                cell = c;
                value = gain;
            }
        }
        return cell >= 0;
    }

    private static bool FindNumberChange(BoardModel board, int targetSum, IReadOnlyList<BoardRect> candidates,
                                         out int cell, out float value)
    {
        cell = -1;
        value = 0f;

        // 지금 둘 수 있는 조합에 든 슬라임은 바꾸지 않음 (있는 조합을 깨뜨림)
        bool[] inMatch = new bool[GameRules.CellCount];
        foreach (BoardRect rect in candidates)
        {
            foreach (int c in rect.GetCells()) inMatch[c] = true;
        }

        int[] bestByNumber = new int[10];
        for (int c = 0; c < GameRules.CellCount; c++)
        {
            BoardSlime target = board.GetSlimeAt(c);
            if (!IsTargetable(target) || inMatch[c]) continue;

            // 숫자가 k로 바뀌면 합이 목표가 되는 사각형마다, k별로 가장 높은 점수
            Array.Clear(bestByNumber, 0, bestByNumber.Length);
            MatchFinder.ForEachContaining(board, c, targetSum - 1 + target.Number, (rect, sum) =>
            {
                int k = targetSum - (sum - target.Number);
                if (k < 1 || k > 9 || k == target.Number) return;
                List<BoardSlime> slimes = board.GetSlimesInCells(rect.GetCells());
                if (slimes.Count >= 2) bestByNumber[k] = Math.Max(bestByNumber[k], ScoreCalculator.Calculate(slimes, targetSum));
            });

            float expected = 0f;
            foreach (int score in bestByNumber) expected += score;
            expected /= 8f;   // 원래 숫자를 뺀 8가지가 같은 확률

            if (cell < 0 || expected > value)
            {
                cell = c;
                value = expected;
            }
        }
        return cell >= 0;
    }

    private static bool FindTransform(BoardModel board, int targetSum, bool stuck, Random random, out int cell, out float value)
    {
        cell = -1;
        value = 0f;
        if (!stuck) return false;

        // 바꿀 수 있는 슬라임이 많은 곳부터 몇 군데
        List<KeyValuePair<int, int>> centers = new List<KeyValuePair<int, int>>();
        for (int c = 0; c < GameRules.CellCount; c++)
        {
            int count = 0;
            foreach (BoardSlime slime in board.GetSlimesInCells(Transform.GetArea(c)))
            {
                if (IsTargetable(slime)) count++;
            }
            if (count >= 2) centers.Add(new KeyValuePair<int, int>(c, count));
        }
        centers.Sort((a, b) => b.Value.CompareTo(a.Value));

        for (int i = 0; i < centers.Count && i < TransformCenters; i++)
        {
            int center = centers[i].Key;
            float total = 0f;
            for (int sample = 0; sample < TransformSamples; sample++)
            {
                BoardModel after = board.Clone();
                Transform.Apply(after, center, new GameRandom(random.Next()), targetSum);
                total += BestMoveScore(after, targetSum);
            }

            float expected = total / TransformSamples;
            if (cell < 0 || expected > value)
            {
                cell = center;
                value = expected;
            }
        }
        return cell >= 0;
    }
}
