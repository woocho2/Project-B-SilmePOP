using System.Collections.Generic;

// 스킬 하나의 규칙 (기획 [4-1]). 사람(SkillManager)과 AI(AiBrain)가 같은 클래스를 씀.
// 순수 C#이라 화면 없이 테스트됨. cell은 마우스(또는 AI)가 고른 칸. 대상이 없는 스킬(돋보기)은 cell을 쓰지 않음.
public abstract class SkillBase
{
    public abstract SkillType Type { get; }
    public int MaxUses => GameRules.SkillUses(Type);

    // false면 고르는 순간 바로 씀 (돋보기)
    public virtual bool NeedsTarget => true;

    // 적용 범위 칸. 판 밖은 잘림. 미리보기 표시와 적용에 같이 씀.
    public virtual List<int> GetArea(int cell)
    {
        return new List<int> { cell };
    }

    public abstract bool CanApply(BoardModel board, int cell, int targetSum);

    // 적용하고 얻은 점수를 돌려줌. CanApply가 true일 때만 부름.
    // random은 이 스킬 전용 난수 (모든 참가자가 같은 시드·같은 사용 순서로 같은 결과).
    public abstract int Apply(BoardModel board, int cell, GameRandom random, int targetSum);

    // 공통 규칙: 조커·합체 슬라임은 어떤 스킬의 대상도 아님
    protected static bool IsTargetable(BoardSlime slime)
    {
        return slime != null && slime.Type != SlimeType.Joker && slime.Type != SlimeType.Merged;
    }

    // cell을 기준으로 한 size x size 범위. centered면 cell이 가운데, 아니면 cell이 왼쪽 아래. 판 밖은 잘림.
    protected static List<int> SquareArea(int cell, int size, bool centered)
    {
        List<int> area = new List<int>();
        if (cell < 0) return area;

        int offset = centered ? size / 2 : 0;
        int startX = GameRules.IndexToX(cell) - offset;
        int startY = GameRules.IndexToY(cell) - offset;
        for (int y = startY; y < startY + size; y++)
        {
            for (int x = startX; x < startX + size; x++)
            {
                if (GameRules.IsInside(x, y)) area.Add(GameRules.ToIndex(x, y));
            }
        }
        return area;
    }

    protected static BoardSlime SlimeAt(BoardModel board, int cell)
    {
        return cell >= 0 && cell < GameRules.CellCount ? board.GetSlimeAt(cell) : null;
    }
}

// 1. 돋보기: 가능한 조합 1개를 표시. 고르는 순간 바로 씀. 표시는 부르는 쪽(SlimeManager.ShowHint(LastHint))이 함.
//    최고의 수가 아니라 아무 조합 하나 (일찍 써서 킹 조합을 찾는 것처럼 쓰는 때에 따라 이득이 크게 달라지지 않게).
//    악마가 든 조합만 피함. 고르는 난수는 경기 시드라 모든 참가자가 같은 판·같은 순서면 같은 조합.
public sealed class MagnifierSkill : SkillBase
{
    public override SkillType Type => SkillType.Magnifier;
    public override bool NeedsTarget => false;

    // 마지막으로 Apply가 고른 조합 (화면 표시용)
    public BoardRect LastHint { get; private set; }

    // 알려 줄 조합: 악마가 가장 적은 조합들 중 무작위 하나 (악마 없는 조합이 있으면 악마는 절대 안 끼움).
    // 같은 슬라임 묶음을 빈칸만 더 덮은 사각형은 하나로 셈. hint는 슬라임이 있는 곳까지 좁힌 사각형.
    public static bool FindHint(BoardModel board, int targetSum, GameRandom random, out BoardRect hint)
    {
        hint = default;
        int fewestDevils = int.MaxValue;
        List<BoardRect> best = new List<BoardRect>();
        HashSet<BoardRect> seen = new HashSet<BoardRect>();
        foreach (BoardRect rect in MatchFinder.FindAll(board, targetSum))
        {
            BoardRect tight = Tighten(board, rect);
            if (!seen.Add(tight)) continue;

            int devils = 0;
            foreach (BoardSlime slime in board.GetSlimesInCells(tight.GetCells()))
                if (slime.Type == SlimeType.Devil) devils++;

            if (devils < fewestDevils)
            {
                fewestDevils = devils;
                best.Clear();
            }
            if (devils == fewestDevils) best.Add(tight);
        }
        if (best.Count == 0) return false;

        hint = best[random.Next(0, best.Count)];
        return true;
    }

    // 사각형을 실제로 슬라임이 있는 칸까지 좁힘 (안에 든 슬라임은 같음)
    private static BoardRect Tighten(BoardModel board, BoardRect rect)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (int cell in rect.GetCells())
        {
            if (board.GetSlimeAt(cell) == null) continue;
            int x = GameRules.IndexToX(cell);
            int y = GameRules.IndexToY(cell);
            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }
        return minX == int.MaxValue ? rect : new BoardRect(minX, minY, maxX, maxY);
    }

    public override bool CanApply(BoardModel board, int cell, int targetSum)
    {
        return MatchFinder.HasAnyMatch(board, targetSum);
    }

    public override int Apply(BoardModel board, int cell, GameRandom random, int targetSum)
    {
        if (FindHint(board, targetSum, random, out BoardRect hint)) LastHint = hint;
        return 0;
    }
}

// 2. 파괴: 슬라임 1개 제거, 기본점수만 얻음
public sealed class DestroySkill : SkillBase
{
    public override SkillType Type => SkillType.Destroy;

    public override bool CanApply(BoardModel board, int cell, int targetSum)
    {
        return IsTargetable(SlimeAt(board, cell));
    }

    public override int Apply(BoardModel board, int cell, GameRandom random, int targetSum)
    {
        BoardSlime slime = SlimeAt(board, cell);
        board.Remove(new[] { slime });
        return ScoreCalculator.DestroyScore(slime);
    }
}

// 3. 변신: 5 x 5 안의 슬라임마다 50%로 +1 또는 -1 (9 → 1, 1 → 9). 조커·합체는 빼고 적용.
public sealed class TransformSkill : SkillBase
{
    public override SkillType Type => SkillType.Transform;

    public override List<int> GetArea(int cell)
    {
        return SquareArea(cell, GameRules.TransformSize, centered: true);
    }

    public override bool CanApply(BoardModel board, int cell, int targetSum)
    {
        foreach (BoardSlime slime in board.GetSlimesInCells(GetArea(cell)))
        {
            if (IsTargetable(slime)) return true;
        }
        return false;
    }

    public override int Apply(BoardModel board, int cell, GameRandom random, int targetSum)
    {
        foreach (BoardSlime slime in board.GetSlimesInCells(GetArea(cell)))
        {
            if (!IsTargetable(slime)) continue;

            int number = slime.Number + (random.Chance(0.5f) ? 1 : -1);
            if (number > 9) number = 1;
            if (number < 1) number = 9;
            board.SetNumber(slime, number);
        }
        return 0;
    }
}

// 4. 합체: 2 x 2 (cell이 왼쪽 아래). 숫자는 합의 일의 자리, 0이면 바로 터지고 합쳐진 수 x 4점.
//    조커가 있으면 불가, 슬라임 2마리 이상 (BoardModel.CanMerge).
public sealed class MergeSkill : SkillBase
{
    public override SkillType Type => SkillType.Merge;

    public override List<int> GetArea(int cell)
    {
        return SquareArea(cell, GameRules.MergeSize, centered: false);
    }

    public override bool CanApply(BoardModel board, int cell, int targetSum)
    {
        return cell >= 0 && board.CanMerge(cell);
    }

    public override int Apply(BoardModel board, int cell, GameRandom random, int targetSum)
    {
        BoardSlime merged = board.Merge(cell, out int mergedCount);
        return merged == null ? ScoreCalculator.MergeZeroScore(mergedCount) : 0;
    }
}

// 5. 조커: 누르는 순간 무작위 기본 슬라임 1개가 조커가 됨 (5초마다 빈자리로 이동). 자리를 고를 수 없음.
//    (원하는 곳에 넣고 바로 터뜨리는 고득점 플레이가 너무 쉬워서 바꿈)
//    경기 시드로 칸 하나를 뽑고, 그 칸에서 가장 가까운 기본 슬라임 → 모든 참가자가 같은 순서로 쓰면 같은 자리 근처.
public sealed class JokerSkill : SkillBase
{
    public override SkillType Type => SkillType.Joker;
    public override bool NeedsTarget => false;

    // 마지막으로 조커가 된 칸 (화면에서 잠깐 표시)
    public int LastCell { get; private set; } = -1;

    public override bool CanApply(BoardModel board, int cell, int targetSum)
    {
        for (int c = 0; c < GameRules.CellCount; c++)
        {
            BoardSlime slime = board.GetSlimeAt(c);
            if (slime != null && slime.Type == SlimeType.Basic) return true;
        }
        return false;
    }

    public override int Apply(BoardModel board, int cell, GameRandom random, int targetSum)
    {
        BoardSlime target = NearestBasic(board, random.Next(0, GameRules.CellCount));
        if (target == null) return 0;

        board.MakeJoker(target);
        LastCell = target.Anchor;
        return 0;
    }

    // from에서 가장 가까운 기본 슬라임 (거리가 같으면 칸 번호가 작은 쪽)
    public static BoardSlime NearestBasic(BoardModel board, int from)
    {
        int fromX = GameRules.IndexToX(from);
        int fromY = GameRules.IndexToY(from);
        BoardSlime best = null;
        int bestDistance = int.MaxValue;
        for (int c = 0; c < GameRules.CellCount; c++)
        {
            BoardSlime slime = board.GetSlimeAt(c);
            if (slime == null || slime.Type != SlimeType.Basic) continue;

            int dx = GameRules.IndexToX(c) - fromX;
            int dy = GameRules.IndexToY(c) - fromY;
            int distance = dx * dx + dy * dy;
            if (distance < bestDistance)
            {
                best = slime;
                bestDistance = distance;
            }
        }
        return best;
    }
}

// 6. 숫자변경: 슬라임 1개의 숫자를 1 ~ 9 중 원래 숫자를 뺀 값으로 (랜덤)
public sealed class NumberChangeSkill : SkillBase
{
    public override SkillType Type => SkillType.NumberChange;

    public override bool CanApply(BoardModel board, int cell, int targetSum)
    {
        return IsTargetable(SlimeAt(board, cell));
    }

    public override int Apply(BoardModel board, int cell, GameRandom random, int targetSum)
    {
        BoardSlime slime = SlimeAt(board, cell);
        int number = random.Next(1, 9);          // 1 ~ 8 중 하나를 뽑고
        if (number >= slime.Number) number++;    // 원래 숫자 이상이면 하나 밀어서 원래 숫자를 건너뜀
        board.SetNumber(slime, number);
        return 0;
    }
}
