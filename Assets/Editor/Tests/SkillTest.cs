using System.Collections.Generic;
using NUnit.Framework;

public class SkillTest
{
    private const int Target = 10;

    private static int Cell(int x, int y) => GameRules.ToIndex(x, y);

    [Test]
    public void Transform_WrapsNineAndOne_AndSkipsJoker()
    {
        BoardModel board = new BoardModel();
        BoardSlime nine = board.AddSlime(9, SlimeType.Basic, Cell(5, 5));
        BoardSlime one = board.AddSlime(1, SlimeType.Devil, Cell(6, 5));
        BoardSlime joker = board.AddSlime(4, SlimeType.Basic, Cell(5, 6));
        board.MakeJoker(joker);

        new TransformSkill().Apply(board, Cell(5, 5), new GameRandom(7), Target);

        CollectionAssert.Contains(new[] { 8, 1 }, nine.Number);
        CollectionAssert.Contains(new[] { 2, 9 }, one.Number);
        Assert.AreEqual(4, joker.Number);
        Assert.AreEqual(SlimeType.Devil, one.Type);
    }

    [Test]
    public void Transform_AreaIsClippedAtBoardEdge()
    {
        List<int> area = new TransformSkill().GetArea(Cell(0, 0));

        Assert.AreEqual(9, area.Count);   // 5 x 5 중 판 안의 3 x 3
    }

    [Test]
    public void NumberChange_NeverKeepsOriginal_AndStaysInOneToNine()
    {
        BoardModel board = new BoardModel();
        BoardSlime slime = board.AddSlime(5, SlimeType.Angel, Cell(0, 0));
        GameRandom random = new GameRandom(3);
        NumberChangeSkill skill = new NumberChangeSkill();

        for (int i = 0; i < 200; i++)
        {
            int before = slime.Number;
            skill.Apply(board, Cell(0, 0), random, Target);
            Assert.AreNotEqual(before, slime.Number);
            Assert.That(slime.Number, Is.InRange(1, 9));
        }
    }

    [Test]
    public void Merge_ZeroOnesDigitPops_AndScoresFourPerSlime()
    {
        BoardModel board = new BoardModel();
        board.AddSlime(2, SlimeType.Basic, Cell(0, 0));
        board.AddSlime(8, SlimeType.Basic, Cell(1, 1));

        int score = new MergeSkill().Apply(board, Cell(0, 0), new GameRandom(1), Target);

        Assert.AreEqual(2 * GameRules.MergeZeroScorePerSlime, score);
        Assert.IsNull(board.GetSlimeAt(Cell(0, 0)));
        Assert.IsNull(board.GetSlimeAt(Cell(1, 1)));
    }

    [Test]
    public void Merge_RefusesAreaWithJoker()
    {
        BoardModel board = new BoardModel();
        board.AddSlime(2, SlimeType.Basic, Cell(0, 0));
        board.AddSlime(3, SlimeType.Basic, Cell(1, 0));
        board.MakeJoker(board.AddSlime(4, SlimeType.Basic, Cell(0, 1)));

        Assert.IsFalse(new MergeSkill().CanApply(board, Cell(0, 0), Target));
    }

    [Test]
    public void Destroy_GivesBaseScoreOnly_AndJokerIsNotTarget()
    {
        BoardModel board = new BoardModel();
        board.AddSlime(3, SlimeType.Angel, Cell(0, 0));
        board.MakeJoker(board.AddSlime(4, SlimeType.Basic, Cell(1, 0)));
        DestroySkill skill = new DestroySkill();

        Assert.IsFalse(skill.CanApply(board, Cell(1, 0), Target));
        Assert.AreEqual(GameRules.BaseScore(SlimeType.Angel), skill.Apply(board, Cell(0, 0), new GameRandom(1), Target));
        Assert.IsNull(board.GetSlimeAt(Cell(0, 0)));
    }

    [Test]
    public void Joker_OnlyOnBasicSlime()
    {
        BoardModel board = new BoardModel();
        board.AddSlime(3, SlimeType.King, Cell(0, 0));
        board.AddSlime(4, SlimeType.Basic, Cell(1, 0));
        JokerSkill skill = new JokerSkill();

        Assert.IsFalse(skill.CanApply(board, Cell(0, 0), Target));
        Assert.IsTrue(skill.CanApply(board, Cell(1, 0), Target));
    }

    [Test]
    public void SkillSet_CountsUses_AndFailedUseKeepsCount()
    {
        BoardModel board = new BoardModel();
        board.AddSlime(3, SlimeType.Basic, Cell(0, 0));
        board.AddSlime(4, SlimeType.Basic, Cell(1, 0));
        SkillSet skills = new SkillSet(42);

        Assert.IsFalse(skills.TryUse(SkillType.Destroy, board, Cell(5, 5), Target, out _));   // 빈칸
        Assert.AreEqual(2, skills.UsesLeft(SkillType.Destroy));

        Assert.IsTrue(skills.TryUse(SkillType.Destroy, board, Cell(0, 0), Target, out _));
        Assert.IsTrue(skills.TryUse(SkillType.Destroy, board, Cell(1, 0), Target, out _));
        Assert.AreEqual(0, skills.UsesLeft(SkillType.Destroy));
        Assert.IsFalse(skills.CanUse(SkillType.Destroy, board, Cell(0, 0), Target));
    }

    [Test]
    public void SkillSet_SameSeed_SameTransformResult()
    {
        int[] RunOnce()
        {
            BoardModel board = BoardGenerator.Generate(99);
            new SkillSet(99).TryUse(SkillType.Transform, board, Cell(8, 5), Target, out _);
            int[] numbers = new int[GameRules.CellCount];
            for (int c = 0; c < numbers.Length; c++) numbers[c] = board.GetSlimeAt(c).Number;
            return numbers;
        }

        CollectionAssert.AreEqual(RunOnce(), RunOnce());
    }

    [Test]
    public void MatchSkills_SameSeedSameThree_AllDifferent()
    {
        for (int seed = -50; seed < 50; seed++)
        {
            MatchSettings a = new MatchSettings(seed, 10, 0, 2);
            MatchSettings b = new MatchSettings(seed, 15, 123, 4);   // 목표 합·시각이 달라도 아이템은 seed로만

            CollectionAssert.AreEqual(a.Skills, b.Skills);
            Assert.AreEqual(GameRules.SkillsPerMatch, a.Skills.Count);
            CollectionAssert.AllItemsAreUnique(a.Skills);
            CollectionAssert.DoesNotContain(a.Skills, SkillType.None);
        }
    }

    [Test]
    public void MatchSkills_DifferentSeeds_GiveDifferentPicks()
    {
        HashSet<string> seen = new HashSet<string>();
        for (int seed = 0; seed < 200; seed++) seen.Add(string.Join(",", MatchSettings.PickSkills(seed)));

        Assert.Greater(seen.Count, 20);   // 늘 같은 3개가 나오지 않음
    }

    [Test]
    public void SkillSet_OnlyPickedSkillsHaveUses()
    {
        SkillType[] picked = { SkillType.Joker, SkillType.Destroy, SkillType.Merge };
        SkillSet skills = new SkillSet(7, picked);

        CollectionAssert.AreEqual(picked, skills.Available);
        foreach (SkillType type in SkillSet.All)
        {
            bool isPicked = System.Array.IndexOf(picked, type) >= 0;
            Assert.AreEqual(isPicked, skills.UsesLeft(type) > 0, type.ToString());
        }
    }
}
