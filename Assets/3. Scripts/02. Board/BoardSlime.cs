using System.Collections.Generic;

// 판 위의 슬라임 하나. 값을 바꾸는 것은 BoardModel만 함 (그래야 이벤트가 빠지지 않음).
public sealed class BoardSlime
{
    public int Id { get; }
    public int Number { get; internal set; }
    public SlimeType Type { get; internal set; }
    public int BaseScore { get; internal set; }
    public IReadOnlyList<int> Cells { get; internal set; }   // 보통 1칸, 합체는 4칸 (칸 번호가 작은 순)
    public float JokerTimeLeft { get; internal set; }        // 조커가 다음 이동까지 남은 시간

    // 합체의 왼쪽 아래 칸. 칸 번호 = y * Columns + x 이므로 가장 작은 번호가 왼쪽 아래.
    public int Anchor => Cells[0];

    internal BoardSlime(int id, int number, SlimeType type, int baseScore, IReadOnlyList<int> cells)
    {
        Id = id;
        Number = number;
        Type = type;
        BaseScore = baseScore;
        Cells = cells;
    }
}
