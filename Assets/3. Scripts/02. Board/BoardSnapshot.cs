using System;

// 결과 화면에 보여 줄 최종 판. 칸마다 숫자와 종류를 담아 네트워크로 보냄.
[Serializable]
public sealed class BoardSnapshot
{
    public int[] numbers;     // 칸마다 숫자. 빈칸은 0
    public int[] types;       // 칸마다 SlimeType
    public int[] anchors;     // 칸마다 슬라임의 Anchor 칸. 빈칸은 -1. 합체 슬라임은 네 칸이 같은 값
    public int colorIndex;    // 그 참가자가 고른 슬라임 색
    public string themeName;  // 그 참가자가 고른 맵 테마

    public static BoardSnapshot FromBoard(BoardModel board, int colorIndex, string themeName)
    {
        BoardSnapshot snapshot = new BoardSnapshot
        {
            numbers = new int[GameRules.CellCount],
            types = new int[GameRules.CellCount],
            anchors = new int[GameRules.CellCount],
            colorIndex = colorIndex,
            themeName = themeName
        };

        for (int cell = 0; cell < GameRules.CellCount; cell++)
        {
            BoardSlime slime = board.GetSlimeAt(cell);
            snapshot.numbers[cell] = slime != null ? slime.Number : 0;
            snapshot.types[cell] = slime != null ? (int)slime.Type : 0;
            snapshot.anchors[cell] = slime != null ? slime.Anchor : -1;
        }
        return snapshot;
    }
}
