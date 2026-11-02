using System.Collections.Generic;

// seed → 판. 같은 seed면 모든 참가자의 판이 완전히 같음 (기획 [4]).
public static class BoardGenerator
{
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
        return rng.Next(1, 10); // 1~9를 같은 확률로 선택.
    }
}
