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
    public void Rescue_EasyUsesNoSkill_NormalUsesNumberChangeFirst()
    {
        BoardModel board = new BoardModel();
        board.AddSlime(9, SlimeType.Basic, Cell(0, 0));   // 막힌 판

        Assert.IsFalse(AiBrain.TryChooseRescue(board, Target, AiDifficulty.Easy, new SkillSet(1), new Random(1), out _, out _));
        Assert.IsTrue(AiBrain.TryChooseRescue(board, Target, AiDifficulty.Normal, new SkillSet(1), new Random(1), out SkillType skill, out int cell));
        Assert.AreEqual(SkillType.NumberChange, skill);
        Assert.AreEqual(Cell(0, 0), cell);
    }
}
