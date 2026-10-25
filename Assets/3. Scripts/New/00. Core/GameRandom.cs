using System;
using System.Collections.Generic;

// 시드로 만드는 랜덤. 규칙 코드(판 생성, 스킬, 조커 이동, AI)는 UnityEngine.Random 대신 이것만 씀.
//
// 왜 직접 만드나
//   - 같은 시드 = 같은 결과. 방장이 seed 숫자 하나만 보내면 모든 참가자가 같은 판을 만듦.
//   - UnityEngine.Random은 전역 하나라서, 다른 코드(연출 등)가 한 번만 불러도 순서가 어긋남.
//   - System.Random은 .NET 버전에 따라 알고리즘이 바뀔 수 있음. 아래 SplitMix64는 몇 줄짜리 공식이라 어디서나 같음.
//
// 랜덤의 흐름을 둘로 나눔
//   판 생성       new GameRandom(seed)                → 모두 같은 판
//   스킬·조커     GameRandom.ForPlayer(seed, 자리)     → 같은 판이어도 자리마다 다른 흐름
public sealed class GameRandom
{
    private ulong state;

    public GameRandom(int seed)
    {
        state = (ulong)(uint)seed;
    }

    // 참가자 한 명의 스킬·조커 이동용. 자리 번호를 섞어 판 생성과 다른 흐름을 만듦.
    public static GameRandom ForPlayer(int matchSeed, int slot)
    {
        GameRandom mixer = new GameRandom(matchSeed ^ ((slot + 1) * 0x5BD1E995));
        return new GameRandom((int)mixer.NextUInt());
    }

    // 방장이 새 판의 seed를 정할 때만 씀 (이 값 자체는 재현할 필요가 없음).
    public static int NewSeed()
    {
        return Guid.NewGuid().GetHashCode();
    }

    // min 이상 max 미만. (UnityEngine.Random.Range(int, int)와 같은 범위)
    public int Next(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "max는 min보다 커야 합니다.");
        }

        ulong range = (ulong)((long)maxExclusive - minInclusive);
        return (int)(minInclusive + (long)(NextULong() % range));
    }

    // 0 이상 1 미만.
    public double NextDouble()
    {
        // 상위 53비트만 써서 double의 정밀도에 맞춤
        return (NextULong() >> 11) * (1.0 / (1UL << 53));
    }

    public float NextFloat()
    {
        return (float)NextDouble();
    }

    // probability(0 ~ 1)의 확률로 true. 예: Chance(0.5f) = 50%
    public bool Chance(float probability)
    {
        return NextDouble() < probability;
    }

    public T Pick<T>(IReadOnlyList<T> list)
    {
        return list[Next(0, list.Count)];
    }

    // 피셔-예이츠 섞기: 뒤에서부터 한 칸씩, 아직 안 고른 앞쪽 중 하나와 바꿈.
    public void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Next(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private uint NextUInt()
    {
        return (uint)(NextULong() >> 32);
    }

    // SplitMix64: 상태에 고정값을 더하고 비트를 섞어 다음 수를 만듦.
    private ulong NextULong()
    {
        state += 0x9E3779B97F4A7C15UL;
        ulong z = state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
