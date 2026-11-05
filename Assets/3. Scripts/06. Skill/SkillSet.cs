using System;
using System.Collections.Generic;

// 참가자 한 명의 스킬과 남은 횟수. 사람(SkillManager)과 AI(AiPlayer)가 같이 씀. 판마다 새로 만듦.
// 스킬마다 경기 시드로 만든 난수를 따로 둠 → 모든 참가자가 같은 스킬을 같은 순서로 쓰면 같은 결과.
// available을 주면 그 스킬만 쓸 수 있음 (판마다 뽑은 3개). 나머지는 횟수 0.
public sealed class SkillSet
{
    public static readonly SkillType[] All =
    {
        SkillType.Magnifier, SkillType.Destroy, SkillType.Transform,
        SkillType.Merge, SkillType.Joker, SkillType.NumberChange
    };

    public event Action<SkillType, int> UsesChanged;   // 스킬, 남은 횟수

    private readonly Dictionary<SkillType, SkillBase> skills = new Dictionary<SkillType, SkillBase>();
    private readonly Dictionary<SkillType, int> usesLeft = new Dictionary<SkillType, int>();
    private readonly Dictionary<SkillType, GameRandom> randoms = new Dictionary<SkillType, GameRandom>();

    public SkillSet(int matchSeed, IReadOnlyList<SkillType> available = null)
    {
        Available = available ?? All;
        foreach (SkillType type in All)
        {
            SkillBase skill = Create(type);
            skills[type] = skill;
            usesLeft[type] = Contains(Available, type) ? skill.MaxUses : 0;
            randoms[type] = new GameRandom(unchecked(matchSeed * 31 + (int)type));
        }
    }

    // 이번 판에 쓸 수 있는 스킬 (순서 = 단축키 순서)
    public IReadOnlyList<SkillType> Available { get; }

    private static bool Contains(IReadOnlyList<SkillType> list, SkillType type)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == type) return true;
        }
        return false;
    }

    public SkillBase Get(SkillType type) => skills[type];
    public int UsesLeft(SkillType type) => usesLeft.TryGetValue(type, out int left) ? left : 0;

    public bool HasAnyLeft
    {
        get
        {
            foreach (int left in usesLeft.Values)
            {
                if (left > 0) return true;
            }
            return false;
        }
    }

    public bool CanUse(SkillType type, BoardModel board, int cell, int targetSum)
    {
        return UsesLeft(type) > 0 && skills[type].CanApply(board, cell, targetSum);
    }

    // 쓸 수 있으면 쓰고 횟수를 줄임. 쓸 수 없으면 아무것도 바뀌지 않음 (횟수도 그대로).
    public bool TryUse(SkillType type, BoardModel board, int cell, int targetSum, out int score)
    {
        score = 0;
        if (!CanUse(type, board, cell, targetSum)) return false;

        score = skills[type].Apply(board, cell, randoms[type], targetSum);
        usesLeft[type]--;
        UsesChanged?.Invoke(type, usesLeft[type]);
        return true;
    }

    private static SkillBase Create(SkillType type)
    {
        switch (type)
        {
            case SkillType.Magnifier: return new MagnifierSkill();
            case SkillType.Destroy: return new DestroySkill();
            case SkillType.Transform: return new TransformSkill();
            case SkillType.Merge: return new MergeSkill();
            case SkillType.Joker: return new JokerSkill();
            default: return new NumberChangeSkill();
        }
    }
}
