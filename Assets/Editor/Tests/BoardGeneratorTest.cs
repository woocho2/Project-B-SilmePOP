using NUnit.Framework;

public class BoardGeneratorTest
{
    [Test]
    public void SameSeed_MakesSameBoard()
    {
        BoardModel a = BoardGenerator.Generate(12345);
        BoardModel b = BoardGenerator.Generate(12345);

        for (int cell = 0; cell < GameRules.CellCount; cell++)
        {
            Assert.AreEqual(a.GetSlimeAt(cell).Number, b.GetSlimeAt(cell).Number);
            Assert.AreEqual(a.GetSlimeAt(cell).Type, b.GetSlimeAt(cell).Type);
        }
    }

    [Test]
    public void DifferentSeed_MakesDifferentBoard()
    {
        BoardModel a = BoardGenerator.Generate(1);
        BoardModel b = BoardGenerator.Generate(2);

        bool anyDifferent = false;
        for (int cell = 0; cell < GameRules.CellCount; cell++)
        {
            if (a.GetSlimeAt(cell).Number != b.GetSlimeAt(cell).Number) anyDifferent = true;
        }
        Assert.IsTrue(anyDifferent);
    }

    [TestCase(1)]
    [TestCase(-987654)]
    [TestCase(int.MaxValue)]
    public void EverySeed_HasRightCountsAndNumbers(int seed)
    {
        BoardModel board = BoardGenerator.Generate(seed);

        int kings = 0, angels = 0, devils = 0, basics = 0;
        for (int cell = 0; cell < GameRules.CellCount; cell++)
        {
            BoardSlime slime = board.GetSlimeAt(cell);
            Assert.That(slime.Number, Is.InRange(1, 9));

            switch (slime.Type)
            {
                case SlimeType.King: kings++; break;
                case SlimeType.Angel: angels++; break;
                case SlimeType.Devil: devils++; break;
                case SlimeType.Basic: basics++; break;
                default: Assert.Fail($"판 생성에서 나오면 안 되는 종류: {slime.Type}"); break;
            }
        }

        Assert.AreEqual(2, kings);
        Assert.AreEqual(3, angels);
        Assert.AreEqual(5, devils);
        Assert.AreEqual(160, basics);
    }
}
