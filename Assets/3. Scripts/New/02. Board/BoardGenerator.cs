using System.Collections.Generic;

// seed → 판. 같은 seed면 모든 참가자의 판이 완전히 같음 (기획 [4]).
public static class BoardGenerator
{
    // 숫자 확률표: 낮은 수가 조금 더 많음 (SlimeController.SetRandomNumber에서 그대로 옮김).
    private const double MidPercent = 100.0 / 9.0;
    private const double LowPercent = MidPercent + 0.25;
    private const double HighPercent = MidPercent - 0.25;

    private static readonly double[] NumberPercents =
    {
        LowPercent, LowPercent, LowPercent,     // 1, 2, 3
        MidPercent, MidPercent, MidPercent,     // 4, 5, 6
        HighPercent, HighPercent, HighPercent   // 7, 8, 9
    };

    public static BoardModel Generate(int seed)
    {
        GameRandom rng = new GameRandom(seed);
        SlimeType[] types = DrawTypes(rng);

        BoardModel board = new BoardModel();
        for (int cell = 0; cell < GameRules.CellCount; cell++)
        {
            board.AddSlime(DrawNumber(rng), types[cell], cell);
        }
        return board;
    }

    // 칸 번호를 섞어 앞쪽 칸부터 킹, 천사, 악마를 놓음. 나머지는 기본.
    private static SlimeType[] DrawTypes(GameRandom rng)
    {
        List<int> shuffledCells = new List<int>(GameRules.CellCount);
        for (int cell = 0; cell < GameRules.CellCount; cell++)
        {
            shuffledCells.Add(cell);
        }
        rng.Shuffle(shuffledCells);

        SlimeType[] types = new SlimeType[GameRules.CellCount];
        int next = 0;
        next = PlaceType(types, shuffledCells, next, GameRules.KingCount, SlimeType.King);
        next = PlaceType(types, shuffledCells, next, GameRules.AngelCount, SlimeType.Angel);
        PlaceType(types, shuffledCells, next, GameRules.DevilCount, SlimeType.Devil);
        return types;
    }

    private static int PlaceType(SlimeType[] types, List<int> shuffledCells, int start, int count, SlimeType type)
    {
        for (int i = start; i < start + count; i++)
        {
            types[shuffledCells[i]] = type;
        }
        return start + count;
    }

    private static int DrawNumber(GameRandom rng)
    {
        double value = rng.NextDouble() * 100.0;
        double threshold = 0.0;
        for (int i = 0; i < NumberPercents.Length - 1; i++)
        {
            threshold += NumberPercents[i];
            if (value < threshold) return i + 1;
        }
        return NumberPercents.Length;
    }
}
