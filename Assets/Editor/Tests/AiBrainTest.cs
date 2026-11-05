using System;
using System.Collections.Generic;
using NUnit.Framework;

public class AiBrainTest
{
    private const int Target = 10;

    private static int Cell(int x, int y) => GameRules.ToIndex(x, y);

    [Test]
    public void Hell_AlwaysPicksBestMove()
    {
        BoardModel board = new BoardModel();
        board.AddSlime(4, SlimeType.Basic, Cell(0, 0));
        board.AddSlime(6, SlimeType.Basic, Cell(0, 1));    // 기본 + 기본 = 2점
        board.AddSlime(3, SlimeType.King, Cell(10, 5));
        board.AddSlime(7, SlimeType.Basic, Cell(11, 5));   // 킹 + 기본 = 12점

        for (int seed = 0; seed < 20; seed++)
        {
            Assert.IsTrue(AiBrain.TryChooseMove(board, Target, AiDifficulty.Hell, new Random(seed), out List<BoardSlime> move));
            Assert.IsTrue(move.Exists(slime => slime.Type == SlimeType.King));
        }
    }

    [Test]
    public void SameScore_PrefersFewerSlimes()
    {
        BoardModel board = new BoardModel();
        board.AddSlime(1, SlimeType.Basic, Cell(0, 0));
        board.AddSlime(9, SlimeType.Basic, Cell(1, 0));    // 2마리 (큰 숫자를 먼저 없앰)
        board.AddSlime(2, SlimeType.Basic, Cell(5, 5));
        board.AddSlime(3, SlimeType.Basic, Cell(6, 5));
        board.AddSlime(5, SlimeType.Basic, Cell(7, 5));    // 3마리

        Assert.IsTrue(AiBrain.TryChooseMove(board, Target, AiDifficulty.Hell, new Random(1), out List<BoardSlime> move));
        Assert.AreEqual(2, move.Count);
    }

    [Test]
    public void Easy_NeverMakesWrongMove()
    {
        BoardModel board = BoardGenerator.Generate(5);
        Random random = new Random(5);

        for (int i = 0; i < 30 && AiBrain.TryChooseMove(board, Target, AiDifficulty.Easy, random, out List<BoardSlime> move); i++)
        {
            Assert.AreEqual(Target, MatchFinder.SumNumbers(move));
            board.Remove(move);
        }
    }

    [Test]
    public void Joker_GoesIntoAMatchThatCanPopNow()
    {
        BoardModel board = new BoardModel();
        board.AddSlime(4, SlimeType.Basic, Cell(0, 0));
        board.AddSlime(6, SlimeType.Basic, Cell(1, 0));
        List<BoardRect> candidates = MatchFinder.FindAll(board, Target);

        Assert.IsTrue(AiSkillAdvisor.TryFindBest(board, Target, SkillType.Joker, candidates, new Random(1), out int cell, out float value));
        Assert.IsTrue(cell == Cell(0, 0) || cell == Cell(1, 0));
        Assert.AreEqual(Target * GameRules.JokerTargetMultiplier + 2, value);   // 2점 → 기본점수 1 + 3, 목표 합 x 2
    }

    [Test]
    public void Merge_PicksTheSquareThatPopsWithMostSlimes()
    {
        BoardModel board = new BoardModel();
        board.AddSlime(3, SlimeType.Basic, Cell(0, 0));
        board.AddSlime(7, SlimeType.Basic, Cell(1, 0));
        board.AddSlime(4, SlimeType.Basic, Cell(0, 1));
        board.AddSlime(6, SlimeType.Basic, Cell(1, 1));   // 3 + 7 + 4 + 6 = 20 → 0이라 바로 터짐 (4마리 x 4점)
        List<BoardRect> candidates = MatchFinder.FindAll(board, Target);

        Assert.IsTrue(AiSkillAdvisor.TryFindBest(board, Target, SkillType.Merge, candidates, new Random(1), out int cell, out float value));
        Assert.AreEqual(Cell(0, 0), cell);
        Assert.AreEqual(4 * GameRules.MergeZeroScorePerSlime - 4, value);
    }

    [Test]
    public void Destroy_RemovesTheDevilBlockingAMatch()
    {
        BoardModel board = new BoardModel();
        board.AddSlime(4, SlimeType.Basic, Cell(0, 0));
        board.AddSlime(5, SlimeType.Devil, Cell(1, 0));   // 4 + 5, 5 + 6, 4 + 5 + 6 모두 10이 아님
        board.AddSlime(6, SlimeType.Basic, Cell(2, 0));   // 악마를 없애면 4 + 6

        Assert.IsTrue(AiSkillAdvisor.TryFindBest(board, Target, SkillType.Destroy, new List<BoardRect>(), new Random(1), out int cell, out _));
        Assert.AreEqual(Cell(1, 0), cell);
    }

    [Test]
    public void Stuck_HellUsesHelpfulSkill_EasyUsesAnything_NoHelpMeansNoSkill()
    {
        BoardModel blocked = new BoardModel();
        blocked.AddSlime(4, SlimeType.Basic, Cell(0, 0));
        blocked.AddSlime(5, SlimeType.Devil, Cell(1, 0));
        blocked.AddSlime(6, SlimeType.Basic, Cell(2, 0));
        List<BoardRect> none = new List<BoardRect>();
        SkillSet destroyOnly = new SkillSet(1, new[] { SkillType.Destroy });

        Assert.IsTrue(AiBrain.TryChooseSkill(blocked, Target, AiDifficulty.Hell, destroyOnly, none, 60f, new Random(1), AiStrategy.Best,
                                             out SkillType skill, out int cell));
        Assert.AreEqual(SkillType.Destroy, skill);
        Assert.AreEqual(Cell(1, 0), cell);

        // 숫자 하나뿐인 판: 무엇을 해도 조합이 안 생김 → 따져 보는 난이도는 아끼고, 쉬움은 아무거나 씀
        BoardModel hopeless = new BoardModel();
        hopeless.AddSlime(9, SlimeType.Basic, Cell(0, 0));
        Assert.IsFalse(AiBrain.TryChooseSkill(hopeless, Target, AiDifficulty.Hell, new SkillSet(1), none, 60f, new Random(1), AiStrategy.Best, out _, out _));
        Assert.IsTrue(AiBrain.TryChooseSkill(hopeless, Target, AiDifficulty.Easy, new SkillSet(1), none, 60f, new Random(1), AiStrategy.Best, out _, out _));
    }

    [Test]
    public void Simulation_HarderDifficultyRemovesMoreSlimes()
    {
        int[] seeds = { 11, 12, 13, 14, 15 };
        Assert.Greater(AverageRemoved(AiDifficulty.Hell, seeds), AverageRemoved(AiDifficulty.Hard, seeds));
        Assert.Greater(AverageRemoved(AiDifficulty.Hard, seeds), AverageRemoved(AiDifficulty.Easy, seeds));
    }

    private static double AverageRemoved(AiDifficulty difficulty, int[] seeds)
    {
        double total = 0;
        foreach (int seed in seeds) total += AiSimulator.Play(seed, difficulty, seed).Removed;
        return total / seeds.Length;
    }
}
