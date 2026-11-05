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

// 1. 돋보기: 가능한 조합 1개를 표시. 고르는 순간 바로 씀. 표시는 부르는 쪽(SlimeManager.ShowHint)이 함.
public sealed class MagnifierSkill : SkillBase
{
    public override SkillType Type => SkillType.Magnifier;
    public override bool NeedsTarget => false;

    // 알려 줄 조합: 악마가 적은 것, 그중 점수가 높은 것 (악마 없는 조합이 있으면 악마는 절대 안 끼움)
    public static bool FindHint(BoardModel board, int targetSum, out BoardRect hint)
    {
        hint = default;
        int bestDevils = int.MaxValue, bestScore = int.MinValue;
        foreach (BoardRect rect in MatchFinder.FindAll(board, targetSum))
        {
            List<BoardSlime> slimes = board.GetSlimesInCells(rect.GetCells());
            int devils = 0;
            foreach (BoardSlime slime in slimes)
                if (slime.Type == SlimeType.Devil) devils++;
            int score = ScoreCalculator.Calculate(slimes, targetSum);

            if (devils < bestDevils || (devils == bestDevils && score > bestScore))
            {
                hint = rect;
                bestDevils = devils;
                bestScore = score;
            }
        }
        return bestDevils != int.MaxValue;
    }

    public override bool CanApply(BoardModel board, int cell, int targetSum)
    {
        return MatchFinder.HasAnyMatch(board, targetSum);
    }

    public override int Apply(BoardModel board, int cell, GameRandom random, int targetSum)
    {
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

// 5. 조커: 기본 슬라임 1개를 조커로 (5초마다 빈자리로 이동)
public sealed class JokerSkill : SkillBase
{
    public override SkillType Type => SkillType.Joker;

    public override bool CanApply(BoardModel board, int cell, int targetSum)
    {
        BoardSlime slime = SlimeAt(board, cell);
        return slime != null && slime.Type == SlimeType.Basic;
    }

    public override int Apply(BoardModel board, int cell, GameRandom random, int targetSum)
    {
        board.MakeJoker(SlimeAt(board, cell));
        return 0;
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
