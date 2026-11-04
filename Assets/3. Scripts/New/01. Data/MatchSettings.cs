using System.Collections.Generic;

// 한 판의 설정. 방장이 정해서 방에 쓰면 모든 참가자가 같은 값을 받음. 판 도중에는 바뀌지 않음.
public sealed class MatchSettings
{
    public int Seed { get; }
    public int TargetSum { get; }
    public long StartAt { get; }     // 시작 시각 (서버 시각, 1970년부터의 밀리초)
    public int SlotCount { get; }    // 참가 자리 수 (사람 + AI)

    // 이번 판에 쓰는 아이템(스킬) 3개. 뽑힌 순서 그대로 (= 단축키 A, S, D). seed로 정하므로 모두 같음.
    public IReadOnlyList<SkillType> Skills { get; }

    public MatchSettings(int seed, int targetSum, long startAt, int slotCount)
    {
        Seed = seed;
        TargetSum = targetSum;
        StartAt = startAt;
        SlotCount = slotCount;
        Skills = PickSkills(seed);
    }

    // 스킬 6종을 seed로 섞어 앞에서 3개. 판 생성·스킬 난수와 겹치지 않게 seed를 바꿔서 씀.
    public static SkillType[] PickSkills(int seed)
    {
        List<SkillType> all = new List<SkillType>(SkillSet.All);
        new GameRandom(unchecked(seed * 7919 + 17)).Shuffle(all);
        return all.GetRange(0, GameRules.SkillsPerMatch).ToArray();
    }

    // 방장이 새 판을 시작할 때: seed와 목표 합을 새로 뽑음.
    public static MatchSettings CreateNew(long startAt, int slotCount)
    {
        int seed = GameRandom.NewSeed();
        int targetSum = new GameRandom(seed).Next(GameRules.MinTargetSum, GameRules.MaxTargetSum + 1);
        return new MatchSettings(seed, targetSum, startAt, slotCount);
    }
}
