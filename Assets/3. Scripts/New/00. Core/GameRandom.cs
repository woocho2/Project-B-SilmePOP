using System;
using System.Collections.Generic;

// 시드로 만드는 랜덤. 판 생성과 스킬은 같은 경기 시드로 같은 난수 순서를 사용함.
// 조커 이동은 BoardModel에서 별도의 일반 난수를 사용함.
//
// System.Random을 감싸는 이유
//   - 같은 빌드에서 같은 시드 = 같은 결과. 방장이 seed 숫자 하나만 보내면 모든 참가자가 같은 판을 만듦.
//   - UnityEngine.Random은 전역 하나라서, 다른 코드(연출 등)가 한 번만 불러도 순서가 어긋남.
//   - 판 생성과 스킬에 자주 쓰는 기능(Chance, Pick, Shuffle)을 한곳에 모음.
//
// 판 생성과 스킬은 각각 별도의 new GameRandom(seed)를 사용함.
// 플레이어 자리 번호는 섞지 않으며, 같은 시드와 같은 호출 순서면 같은 결과가 나옴.
public sealed class GameRandom
{
    private readonly Random random;

    public GameRandom(int seed)
    {
        random = new Random(seed);
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

        return random.Next(minInclusive, maxExclusive);
    }

    // 0 이상 1 미만.
    public double NextDouble()
    {
        return random.NextDouble();
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
}
