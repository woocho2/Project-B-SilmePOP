using System.Collections.Generic;
using NUnit.Framework;

public class MatchFinderTest
{
    private static int Cell(int x, int y) => GameRules.ToIndex(x, y);

    // (2,2)를 왼쪽 아래로 하는 숫자 7의 합체 슬라임 + (4,3)의 숫자 3
    private static BoardModel MergedBoard()
    {
        BoardModel board = new BoardModel();
        board.AddSlime(3, SlimeType.Basic, Cell(2, 2));
        board.AddSlime(4, SlimeType.Basic, Cell(3, 3));
        Assert.IsNotNull(board.Merge(Cell(2, 2), out _));
        board.AddSlime(3, SlimeType.Basic, Cell(4, 3));
        return board;
    }

    // 정답 대조용: 사각형 8,415개를 칸 하나하나 세어 합을 구함.
    private static HashSet<BoardRect> BruteForce(BoardModel board, int targetSum)
    {
        HashSet<BoardRect> result = new HashSet<BoardRect>();
        for (int minX = 0; minX < GameRules.Columns; minX++)
        {
            for (int minY = 0; minY < GameRules.Rows; minY++)
            {
                for (int maxX = minX; maxX < GameRules.Columns; maxX++)
                {
                    for (int maxY = minY; maxY < GameRules.Rows; maxY++)
                    {
                        BoardRect rect = new BoardRect(minX, minY, maxX, maxY);
                        if (MatchFinder.SumNumbers(board.GetSlimesInCells(rect.GetCells())) == targetSum)
                        {
                            result.Add(rect);
                        }
                    }
                }
            }
        }
        return result;
    }

    [TestCase(10)]
    [TestCase(15)]
    public void FindAll_SameAsCountingEveryCell(int targetSum)
    {
        for (int seed = 0; seed < 10; seed++)
        {
            BoardModel board = BoardGenerator.Generate(seed);
            GameRandom rng = new GameRandom(seed);

            // 빈칸을 만들고 합체도 하나 넣은 판까지 대조
            List<BoardSlime> toRemove = new List<BoardSlime>();
            for (int cell = 0; cell < GameRules.CellCount; cell++)
            {
                if (rng.Chance(0.5f)) toRemove.Add(board.GetSlimeAt(cell));
            }
            board.Remove(toRemove);
            for (int cell = 0; cell < GameRules.CellCount; cell++)
            {
                if (board.CanMerge(cell) && board.Merge(cell, out _) != null) break;
            }

            CollectionAssert.AreEquivalent(BruteForce(board, targetSum), MatchFinder.FindAll(board, targetSum));
        }
    }

    [Test]
    public void Merged_CountsWhenRectTouchesOneCell()
    {
        // 합체의 오른쪽 위 칸(3,3)만 걸침: 7 + 3 = 10
        Assert.Contains(new BoardRect(3, 3, 4, 3), MatchFinder.FindAll(MergedBoard(), 10));
    }

    [Test]
    public void Merged_CountsOnceWhenRectCoversAllCells()
    {
        // 합체 네 칸 + 숫자 3: 28 + 3이 아니라 7 + 3 = 10
        Assert.Contains(new BoardRect(2, 2, 4, 3), MatchFinder.FindAll(MergedBoard(), 10));
    }

    [Test]
    public void GetSlimesInCells_ReturnsMergedOnce()
    {
        BoardModel board = MergedBoard();
        List<BoardSlime> slimes = board.GetSlimesInCells(new[] { Cell(2, 2), Cell(3, 2), Cell(2, 3), Cell(3, 3) });

        Assert.AreEqual(1, slimes.Count);
        Assert.AreEqual(SlimeType.Merged, slimes[0].Type);
    }

    [Test]
    public void EmptyBoard_HasNoMatch()
    {
        Assert.IsFalse(MatchFinder.HasAnyMatch(new BoardModel(), 10));
    }

    [Test]
    public void BlockedBoard_HasNoMatch()
    {
        BoardModel board = new BoardModel();
        board.AddSlime(9, SlimeType.Basic, Cell(0, 0));
        board.AddSlime(9, SlimeType.Basic, Cell(16, 9));

        Assert.IsFalse(MatchFinder.HasAnyMatch(board, 10));
    }

    [Test]
    public void FindFirst_ReturnsRectWithTargetSum()
    {
        BoardModel board = BoardGenerator.Generate(42);

        Assert.IsTrue(MatchFinder.FindFirst(board, 10, out BoardRect rect));
        Assert.AreEqual(10, MatchFinder.SumNumbers(board.GetSlimesInCells(rect.GetCells())));
    }

    [Test]
    public void Merge_ZeroDigit_RemovesSlimes()
    {
        BoardModel board = new BoardModel();
        board.AddSlime(2, SlimeType.Basic, Cell(0, 0));
        board.AddSlime(3, SlimeType.Basic, Cell(1, 0));
        board.AddSlime(7, SlimeType.Basic, Cell(0, 1));
        board.AddSlime(8, SlimeType.Basic, Cell(1, 1));

        Assert.IsNull(board.Merge(Cell(0, 0), out int mergedCount));
        Assert.AreEqual(4, mergedCount);
        Assert.IsNull(board.GetSlimeAt(Cell(0, 0)));
    }

    [Test]
    public void CanMerge_RejectsJokerEdgeAndSingleSlime()
    {
        BoardModel board = new BoardModel();
        BoardSlime joker = board.AddSlime(2, SlimeType.Basic, Cell(0, 0));
        board.AddSlime(3, SlimeType.Basic, Cell(1, 0));
        board.AddSlime(4, SlimeType.Basic, Cell(16, 9));

        Assert.IsTrue(board.CanMerge(Cell(0, 0)));
        Assert.IsFalse(board.CanMerge(Cell(15, 9)), "판 밖으로 나감");
        Assert.IsFalse(board.CanMerge(Cell(15, 8)), "슬라임 1마리");

        board.MakeJoker(joker);
        Assert.IsFalse(board.CanMerge(Cell(0, 0)), "조커 포함");
    }

    [Test]
    public void Joker_MovesToEmptyCellOnlyAfterInterval()
    {
        BoardModel board = BoardGenerator.Generate(7);
        BoardSlime joker = null;
        for (int cell = 0; joker == null; cell++)
        {
            if (board.GetSlimeAt(cell).Type == SlimeType.Basic) joker = board.GetSlimeAt(cell);
        }
        int from = joker.Anchor;
        board.MakeJoker(joker);

        // 빈자리가 없으면 시간이 지나도 그대로
        board.Tick(GameRules.JokerMoveInterval);
        Assert.AreEqual(from, joker.Anchor);

        // 빈자리가 생기면 그 칸으로
        int emptyCell = from == 0 ? 1 : 0;
        board.Remove(new[] { board.GetSlimeAt(emptyCell) });
        board.Tick(GameRules.JokerMoveInterval - 0.5f);
        Assert.AreEqual(from, joker.Anchor, "5초 전에는 안 움직임");

        board.Tick(1f);
        Assert.AreEqual(emptyCell, joker.Anchor);
        Assert.IsNull(board.GetSlimeAt(from));
        Assert.AreSame(joker, board.GetSlimeAt(emptyCell));
    }
}
