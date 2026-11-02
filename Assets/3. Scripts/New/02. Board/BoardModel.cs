using System;
using System.Collections.Generic;

// 판의 진짜 상태. 화면(SlimeManager)은 이벤트를 듣고 그리기만 함.
// 내 판, AI 판, 시뮬레이션, 테스트가 모두 이 클래스를 씀 (MonoBehaviour가 아니라 화면 없이 돌아감).
public sealed class BoardModel
{
    public event Action<IReadOnlyList<BoardSlime>> SlimesRemoved;            // 터짐·파괴
    public event Action<BoardSlime> SlimeChanged;                            // 숫자·종류 바뀜 (변신, 숫자변경, 조커 지정)
    public event Action<BoardSlime, int, int> SlimeMoved;                    // 조커 이동 (슬라임, 이전 칸, 새 칸)
    public event Action<BoardSlime, IReadOnlyList<BoardSlime>> SlimesMerged; // 합체 (새 슬라임, 없어진 슬라임들)

    private readonly BoardSlime[] cells = new BoardSlime[GameRules.CellCount];
    private readonly List<BoardSlime> jokers = new List<BoardSlime>();
    private int nextId;

    public BoardSlime GetSlimeAt(int cell)
    {
        return cells[cell];
    }

    // 칸 목록 → 슬라임 목록. 합체 슬라임은 네 칸 중 몇 칸이 들어와도 1번만.
    public List<BoardSlime> GetSlimesInCells(IEnumerable<int> cellIndexes)
    {
        List<BoardSlime> result = new List<BoardSlime>();
        foreach (int cell in cellIndexes)
        {
            BoardSlime slime = cells[cell];
            if (slime != null && !result.Contains(slime))
            {
                result.Add(slime);
            }
        }
        return result;
    }

    // 판 생성(BoardGenerator)과 테스트, 개발용 치트에서 씀.
    public BoardSlime AddSlime(int number, SlimeType type, int cell)
    {
        if (cells[cell] != null)
        {
            throw new InvalidOperationException($"칸 {cell}에 이미 슬라임이 있습니다.");
        }

        BoardSlime slime = new BoardSlime(nextId++, number, type, GameRules.BaseScore(type), new[] { cell });
        cells[cell] = slime;
        return slime;
    }

    public void Remove(IReadOnlyList<BoardSlime> slimes)
    {
        foreach (BoardSlime slime in slimes)
        {
            ClearCells(slime);
        }
        SlimesRemoved?.Invoke(slimes);
    }

    public void SetNumber(BoardSlime slime, int number)
    {
        slime.Number = number;
        SlimeChanged?.Invoke(slime);
    }

    // 조커 스킬: 기본 슬라임만 조커가 됨 (기획 [4-1] 5번).
    public bool MakeJoker(BoardSlime slime)
    {
        if (slime.Type != SlimeType.Basic) return false;

        slime.Type = SlimeType.Joker;
        slime.BaseScore = GameRules.BaseScore(SlimeType.Joker);
        slime.JokerTimeLeft = GameRules.JokerMoveInterval;
        jokers.Add(slime);
        SlimeChanged?.Invoke(slime);
        return true;
    }

    // anchorCell을 왼쪽 아래로 하는 2 x 2에 합체를 쓸 수 있는가. 합체는 항상 네 칸을 차지하므로 판 안에 다 들어와야 함.
    public bool CanMerge(int anchorCell)
    {
        if (!TryGetMergeCells(anchorCell, out int[] area)) return false;

        List<BoardSlime> slimes = GetSlimesInCells(area);
        if (slimes.Count < GameRules.MergeMinSlimes) return false;

        foreach (BoardSlime slime in slimes)
        {
            if (slime.Type == SlimeType.Joker || slime.Type == SlimeType.Merged) return false;
        }
        return true;
    }

    // 합체 (기획 [4-1] 4번). 숫자 합의 일의 자리가 0이면 바로 터지고 null을 돌려줌. 점수는 부르는 쪽(MergeSkill)이 계산.
    public BoardSlime Merge(int anchorCell, out int mergedCount)
    {
        mergedCount = 0;
        if (!CanMerge(anchorCell)) return null;

        TryGetMergeCells(anchorCell, out int[] area);
        List<BoardSlime> removed = GetSlimesInCells(area);
        mergedCount = removed.Count;

        int numberSum = 0;
        int baseScoreSum = 0;
        foreach (BoardSlime slime in removed)
        {
            numberSum += slime.Number;
            baseScoreSum += slime.BaseScore;
            ClearCells(slime);
        }

        int number = numberSum % 10;
        if (number == 0)
        {
            SlimesRemoved?.Invoke(removed);
            return null;
        }

        BoardSlime merged = new BoardSlime(nextId++, number, SlimeType.Merged, baseScoreSum, area);
        foreach (int cell in area)
        {
            cells[cell] = merged;
        }
        SlimesMerged?.Invoke(merged, removed);
        return merged;
    }

    // 매 프레임 부름. 조커는 5초마다 랜덤한 빈자리로 이동하고, 빈자리가 없으면 그대로 있음.
    public void Tick(float deltaTime, GameRandom rng)
    {
        foreach (BoardSlime joker in jokers)
        {
            joker.JokerTimeLeft -= deltaTime;
            if (joker.JokerTimeLeft > 0f) continue;

            joker.JokerTimeLeft += GameRules.JokerMoveInterval;
            MoveJoker(joker, rng);
        }
    }

    private void MoveJoker(BoardSlime joker, GameRandom rng)
    {
        List<int> emptyCells = new List<int>();
        for (int cell = 0; cell < cells.Length; cell++)
        {
            if (cells[cell] == null) emptyCells.Add(cell);
        }
        if (emptyCells.Count == 0) return;

        int from = joker.Anchor;
        int to = rng.Pick(emptyCells);
        cells[from] = null;
        cells[to] = joker;
        joker.Cells = new[] { to };
        SlimeMoved?.Invoke(joker, from, to);
    }

    private void ClearCells(BoardSlime slime)
    {
        foreach (int cell in slime.Cells)
        {
            cells[cell] = null;
        }
        jokers.Remove(slime);
    }

    private static bool TryGetMergeCells(int anchorCell, out int[] area)
    {
        area = null;
        int x = GameRules.IndexToX(anchorCell);
        int y = GameRules.IndexToY(anchorCell);
        if (!GameRules.IsInside(x, y) || !GameRules.IsInside(x + 1, y + 1)) return false;

        area = new[]
        {
            GameRules.ToIndex(x, y),
            GameRules.ToIndex(x + 1, y),
            GameRules.ToIndex(x, y + 1),
            GameRules.ToIndex(x + 1, y + 1)
        };
        return true;
    }
}
