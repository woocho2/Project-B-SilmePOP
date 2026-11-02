using System.Collections.Generic;
using NUnit.Framework;

// 기획 [4-2]의 예시를 그대로 고정함.
public class ScoreCalculatorTest
{
    private static List<BoardSlime> Slimes(params SlimeType[] types)
    {
        BoardModel board = new BoardModel();
        List<BoardSlime> slimes = new List<BoardSlime>();
        for (int i = 0; i < types.Length; i++)
        {
            slimes.Add(board.AddSlime(1, types[i], i));
        }
        return slimes;
    }

    [TestCase(6, SlimeType.Angel, SlimeType.Devil)]
    [TestCase(19, SlimeType.Angel, SlimeType.King)]
    [TestCase(2, SlimeType.Basic, SlimeType.Basic)]
    [TestCase(0, SlimeType.Basic, SlimeType.Devil)]
    [TestCase(9, SlimeType.Basic, SlimeType.Angel)]
    [TestCase(12, SlimeType.Basic, SlimeType.King)]
    [TestCase(-2, SlimeType.Devil, SlimeType.Devil)]
    [TestCase(20, SlimeType.Angel, SlimeType.Angel)]
    [TestCase(22, SlimeType.King, SlimeType.King)]
    public void Examples_WithoutJoker(int expected, params SlimeType[] types)
    {
        Assert.AreEqual(expected, ScoreCalculator.Calculate(Slimes(types), 10));
    }

    [TestCase(24, SlimeType.Basic, SlimeType.Joker)]
    [TestCase(25, SlimeType.Basic, SlimeType.Basic, SlimeType.Joker)]
    [TestCase(35, SlimeType.Angel, SlimeType.Joker)]
    [TestCase(20, SlimeType.Devil, SlimeType.Joker)]
    [TestCase(34, SlimeType.King, SlimeType.Joker)]
    public void Examples_WithJoker_Target10(int expected, params SlimeType[] types)
    {
        Assert.AreEqual(expected, ScoreCalculator.Calculate(Slimes(types), 10));
    }

    [Test]
    public void Joker_BonusFollowsTargetSum()
    {
        // B = 4, 조커 +30
        Assert.AreEqual(34, ScoreCalculator.Calculate(Slimes(SlimeType.Basic, SlimeType.Joker), 15));
    }

    [TestCase(SlimeType.Basic, 1)]
    [TestCase(SlimeType.Devil, 1)]
    [TestCase(SlimeType.Angel, 2)]
    [TestCase(SlimeType.King, 1)]
    public void DestroyScore_IsBaseScoreOnly(SlimeType type, int expected)
    {
        Assert.AreEqual(expected, ScoreCalculator.DestroyScore(Slimes(type)[0]));
    }

    [Test]
    public void MergeZeroScore_IsCountTimesFour()
    {
        Assert.AreEqual(16, ScoreCalculator.MergeZeroScore(4));
    }

    [Test]
    public void Merged_HasSumOfBaseScores_AndNoSpecialScore()
    {
        // 기본 + 기본 + 악마 + 천사 → 기본점수 5, 특수점수 없음
        BoardModel board = new BoardModel();
        board.AddSlime(1, SlimeType.Basic, GameRules.ToIndex(0, 0));
        board.AddSlime(1, SlimeType.Basic, GameRules.ToIndex(1, 0));
        board.AddSlime(1, SlimeType.Devil, GameRules.ToIndex(0, 1));
        board.AddSlime(2, SlimeType.Angel, GameRules.ToIndex(1, 1));

        BoardSlime merged = board.Merge(GameRules.ToIndex(0, 0), out _);

        Assert.AreEqual(5, merged.BaseScore);
        Assert.AreEqual(5, merged.Number);
        Assert.AreEqual(5, ScoreCalculator.Calculate(new[] { merged }, 10));
    }
}
