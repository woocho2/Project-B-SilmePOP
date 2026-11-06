using System;
using System.Collections.Generic;

// AI 한 명의 생각 (기획 [5]). 무엇을 할지 정해 판에 적용하고, 다음 행동까지 기다릴 시간을 돌려줌. 순수 C#.
// 시간을 재고 결과를 점수·소리로 보여 주는 것은 AiPlayer. 시뮬레이션도 이것을 그대로 돌림.
public sealed class AiAgent
{
    private const float BurstChance = 0.15f;      // 가끔 연속으로 빠르게 터뜨림 (사람처럼 보이게)
    private const float BurstFactor = 0.4f;
    private const float StuckRetry = 1f;          // 막혔을 때 다시 볼 간격 (조커가 움직이면 수가 생길 수 있음)

    private readonly Random random;
    private readonly AiStrategy strategy;
    private AiDifficulty difficulty;
    private bool focusNext;   // 조커·돋보기 바로 다음: 최선의 수를 빨리 (조커는 5초 뒤 움직임)

    public AiAgent(AiDifficulty difficulty, int seed, AiStrategy strategy = null)
    {
        random = new Random(seed);
        this.strategy = strategy ?? AiStrategy.Best;
        Difficulty = difficulty;
    }

    // 난이도를 바꾸면 반응 시간도 그 난이도의 표 값으로 (시뮬레이션은 바꿔 가며 잼)
    public AiDifficulty Difficulty
    {
        get => difficulty;
        set
        {
            difficulty = value;
            MinReaction = AiDifficultyTable.MinReaction(value);
            MaxReaction = AiDifficultyTable.MaxReaction(value);
        }
    }

    public float MinReaction { get; set; }
    public float MaxReaction { get; set; }

    // 3마리 이상 조합은 1마리마다 이만큼 더 오래 걸림 (사람도 큰 조합은 찾기 어려움)
    public float PerSlimeDelay { get; set; } = 0.08f;

    public float FirstWait(int targetSum) => NextWait(1, targetSum);

    // true면 고르기만 하고 판에는 적용하지 않음 (Score는 0). 화면에서 사각형을 끄는 연출을 보여 준 뒤 부르는 쪽이 적용함.
    public bool PlanOnly { get; set; }

    public AiAction Act(BoardModel board, SkillSet skills, int targetSum, float timeLeft)
    {
        List<BoardRect> candidates = MatchFinder.FindAll(board, targetSum);

        int skillScore = 0;
        if (skills != null
            && AiBrain.TryChooseSkill(board, targetSum, difficulty, skills, candidates, timeLeft, random, strategy,
                                      out SkillType skill, out int cell)
            && (PlanOnly ? skills.CanUse(skill, board, cell, targetSum)
                         : skills.TryUse(skill, board, cell, targetSum, out skillScore)))
        {
            focusNext = skill == SkillType.Joker || skill == SkillType.Magnifier;
            float wait = focusNext ? MinReaction * AiDifficultyTable.ReactionScale(difficulty, targetSum) : NextWait(1, targetSum);
            return AiAction.UsedSkill(skill, cell, skillScore, wait);
        }

        if (candidates.Count == 0) return AiAction.Waited(StuckRetry);

        BoardRect rect = AiBrain.ChooseRect(board, targetSum, difficulty, candidates, random, focusNext, strategy);
        List<BoardSlime> move = board.GetSlimesInCells(rect.GetCells());
        focusNext = false;
        int score = 0;
        if (!PlanOnly)
        {
            score = ScoreCalculator.Calculate(move, targetSum);
            board.Remove(move);
        }
        return AiAction.Moved(move.Count, score, NextWait(move.Count, targetSum), rect);
    }

    // 다음 행동까지 기다릴 시간: 난이도별 반응 시간 + 큰 사각형 보정 (목표 합 11 이상이면 더 느리게), 가끔 빠르게
    private float NextWait(int slimeCount, int targetSum)
    {
        float wait = MinReaction + (float)random.NextDouble() * (MaxReaction - MinReaction);
        wait += PerSlimeDelay * Math.Max(0, slimeCount - 2);
        wait *= AiDifficultyTable.ReactionScale(difficulty, targetSum);
        if (random.NextDouble() < BurstChance) wait *= BurstFactor;
        return wait;
    }
}

public enum AiActionKind
{
    Move,
    Skill,
    Wait
}

// AI 행동 하나의 결과
public readonly struct AiAction
{
    public readonly AiActionKind Kind;
    public readonly int Score;         // 이 행동으로 얻은 점수
    public readonly int SlimeCount;    // 수: 터뜨린 슬라임 수
    public readonly SkillType Skill;   // 아이템: 쓴 아이템과 칸
    public readonly int Cell;
    public readonly BoardRect Rect;    // 수: 고른 사각형
    public readonly float Wait;        // 다음 행동까지

    private AiAction(AiActionKind kind, int score, int slimeCount, SkillType skill, int cell, BoardRect rect, float wait)
    {
        Kind = kind;
        Score = score;
        SlimeCount = slimeCount;
        Skill = skill;
        Cell = cell;
        Rect = rect;
        Wait = wait;
    }

    public static AiAction Moved(int slimeCount, int score, float wait, BoardRect rect) => new AiAction(AiActionKind.Move, score, slimeCount, SkillType.None, -1, rect, wait);
    public static AiAction UsedSkill(SkillType skill, int cell, int score, float wait) => new AiAction(AiActionKind.Skill, score, 0, skill, cell, default, wait);
    public static AiAction Waited(float wait) => new AiAction(AiActionKind.Wait, 0, 0, SkillType.None, -1, default, wait);
}
