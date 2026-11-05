using System;
using System.Collections.Generic;

// 판 위의 사각형 하나 (양 끝 칸 포함).
public readonly struct BoardRect
{
    public readonly int MinX;
    public readonly int MinY;
    public readonly int MaxX;
    public readonly int MaxY;

    public BoardRect(int minX, int minY, int maxX, int maxY)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
    }

    public bool Contains(int x, int y)
    {
        return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
    }

    public List<int> GetCells()
    {
        List<int> result = new List<int>((MaxX - MinX + 1) * (MaxY - MinY + 1));
        for (int y = MinY; y <= MaxY; y++)
        {
            for (int x = MinX; x <= MaxX; x++)
            {
                result.Add(GameRules.ToIndex(x, y));
            }
        }
        return result;
    }
}

// 목표 합을 만드는 사각형 찾기. 사각형은 8,415개뿐이라 칸 누적합으로 전부 봐도 충분히 빠름.
// 같은 슬라임 묶음이라도 빈칸을 더 덮어 사각형이 다르면 따로 셈.
public static class MatchFinder
{
    public static bool HasAnyMatch(BoardModel board, int targetSum)
    {
        return FindFirst(board, targetSum, out _);
    }

    public static bool FindFirst(BoardModel board, int targetSum, out BoardRect match)
    {
        int[,] prefixSums = BuildPrefixSums(board, out List<BoardSlime> mergedSlimes);

        for (int minX = 0; minX < GameRules.Columns; minX++)
        {
            for (int minY = 0; minY < GameRules.Rows; minY++)
            {
                for (int maxX = minX; maxX < GameRules.Columns; maxX++)
                {
                    for (int maxY = minY; maxY < GameRules.Rows; maxY++)
                    {
                        BoardRect rect = new BoardRect(minX, minY, maxX, maxY);
                        int sum = GetSum(prefixSums, mergedSlimes, rect);

                        // 위로 넓혀도 합은 줄지 않으므로 넘치면 더 볼 필요 없음
                        if (sum > targetSum) break;

                        if (sum == targetSum)
                        {
                            match = rect;
                            return true;
                        }
                    }
                }
            }
        }

        match = default;
        return false;
    }

    public static List<BoardRect> FindAll(BoardModel board, int targetSum)
    {
        List<BoardRect> result = new List<BoardRect>();
        int[,] prefixSums = BuildPrefixSums(board, out List<BoardSlime> mergedSlimes);

        for (int minX = 0; minX < GameRules.Columns; minX++)
        {
            for (int minY = 0; minY < GameRules.Rows; minY++)
            {
                for (int maxX = minX; maxX < GameRules.Columns; maxX++)
                {
                    for (int maxY = minY; maxY < GameRules.Rows; maxY++)
                    {
                        BoardRect rect = new BoardRect(minX, minY, maxX, maxY);
                        int sum = GetSum(prefixSums, mergedSlimes, rect);

                        if (sum > targetSum) break;
                        if (sum == targetSum) result.Add(rect);
                    }
                }
            }
        }
        return result;
    }

    // cell을 포함하는 사각형마다 visit(사각형, 숫자 합). 합이 maxSum을 넘으면 거기서 넓히기를 멈춤.
    // 칸 하나만 바뀌는 아이템(파괴, 숫자변경)으로 새로 생기는 조합은 그 칸을 꼭 포함하므로 AI가 이것으로 찾음.
    public static void ForEachContaining(BoardModel board, int cell, int maxSum, Action<BoardRect, int> visit)
    {
        int[,] prefixSums = BuildPrefixSums(board, out List<BoardSlime> mergedSlimes);
        int cellX = GameRules.IndexToX(cell);
        int cellY = GameRules.IndexToY(cell);

        for (int minX = 0; minX <= cellX; minX++)
        {
            for (int minY = 0; minY <= cellY; minY++)
            {
                for (int maxX = cellX; maxX < GameRules.Columns; maxX++)
                {
                    bool anyFits = false;
                    for (int maxY = cellY; maxY < GameRules.Rows; maxY++)
                    {
                        BoardRect rect = new BoardRect(minX, minY, maxX, maxY);
                        int sum = GetSum(prefixSums, mergedSlimes, rect);
                        if (sum > maxSum) break;

                        anyFits = true;
                        visit(rect, sum);
                    }

                    // 가장 낮은 사각형부터 넘쳤으면 오른쪽으로 더 넓혀도 넘침
                    if (!anyFits) break;
                }
            }
        }
    }

    // 슬라임들의 숫자 합. 드래그 판정과 화면 강조에서 씀.
    public static int SumNumbers(IReadOnlyList<BoardSlime> slimes)
    {
        int sum = 0;
        foreach (BoardSlime slime in slimes)
        {
            sum += slime.Number;
        }
        return sum;
    }

    // 합체 슬라임의 숫자는 Anchor 칸에만 넣음 (네 칸에 다 넣으면 여러 번 더해짐).
    private static int[,] BuildPrefixSums(BoardModel board, out List<BoardSlime> mergedSlimes)
    {
        mergedSlimes = new List<BoardSlime>();
        int[,] prefixSums = new int[GameRules.Columns + 1, GameRules.Rows + 1];

        for (int x = 0; x < GameRules.Columns; x++)
        {
            for (int y = 0; y < GameRules.Rows; y++)
            {
                int cell = GameRules.ToIndex(x, y);
                BoardSlime slime = board.GetSlimeAt(cell);

                int value = 0;
                if (slime != null && slime.Anchor == cell)
                {
                    value = slime.Number;
                    if (slime.Type == SlimeType.Merged) mergedSlimes.Add(slime);
                }

                prefixSums[x + 1, y + 1] = value + prefixSums[x, y + 1]
                    + prefixSums[x + 1, y] - prefixSums[x, y];
            }
        }
        return prefixSums;
    }

    private static int GetSum(int[,] prefixSums, List<BoardSlime> mergedSlimes, BoardRect rect)
    {
        int sum = prefixSums[rect.MaxX + 1, rect.MaxY + 1] - prefixSums[rect.MinX, rect.MaxY + 1]
            - prefixSums[rect.MaxX + 1, rect.MinY] + prefixSums[rect.MinX, rect.MinY];

        // 합체 네 칸 중 일부만 걸치고 Anchor를 안 덮으면 누적합에 빠져 있으므로 여기서 더함
        foreach (BoardSlime merged in mergedSlimes)
        {
            int anchorX = GameRules.IndexToX(merged.Anchor);
            int anchorY = GameRules.IndexToY(merged.Anchor);
            if (rect.Contains(anchorX, anchorY)) continue;

            foreach (int cell in merged.Cells)
            {
                if (rect.Contains(GameRules.IndexToX(cell), GameRules.IndexToY(cell)))
                {
                    sum += merged.Number;
                    break;
                }
            }
        }
        return sum;
    }
}
