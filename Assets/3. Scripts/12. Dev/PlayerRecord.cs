using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

// 개발용 기록 모드(DebugCheat)에서 내가 둔 판 하나의 행동 기록. 순수 C#. 시간은 판 시작부터 지난 초.
// 쌓인 기록으로 AI 난이도를 내 실력에 맞춰 나눔 (TestRecords/player_games.csv, player_sessions.csv).
public sealed class PlayerGameLog
{
    public int TargetSum;
    public int Seed;
    public string Item = "";
    public int Removed;      // 판이 끝났을 때 비워진 칸 (아이템으로 없앤 것 포함)
    public int ItemsUsed;
    public int Misses;       // 합이 안 맞게 끈 횟수
    public int Angels, Devils, Kings;   // 끌어서 터뜨린 특수 슬라임

    public readonly List<float> PopTimes = new List<float>();
    public readonly List<int> PopSizes = new List<int>();

    public void AddPop(float time, IReadOnlyList<BoardSlime> slimes)
    {
        PopTimes.Add(time);
        PopSizes.Add(slimes.Count);
        foreach (BoardSlime slime in slimes)
        {
            if (slime.Type == SlimeType.Angel) Angels++;
            else if (slime.Type == SlimeType.Devil) Devils++;
            else if (slime.Type == SlimeType.King) Kings++;
        }
    }

    public int Moves => PopTimes.Count;
    public double AverageSize => Moves > 0 ? PopSizes.Average() : 0;
    public double PairShare => Moves > 0 ? (double)PopSizes.Count(n => n == 2) / Moves : 0;

    // 터뜨린 사이의 간격 (첫 번째는 시작부터)
    public List<float> Gaps()
    {
        List<float> gaps = new List<float>();
        float last = 0f;
        foreach (float time in PopTimes)
        {
            gaps.Add(time - last);
            last = time;
        }
        return gaps;
    }

    public double AverageGap => Moves > 0 ? Gaps().Average() : 0;
    public double MedianGap => Median(Gaps());

    // 가장 오래 못 찾은 시간 (마지막 수부터 판 끝까지 포함)
    public double LongestGap(float duration)
    {
        List<float> gaps = Gaps();
        gaps.Add(duration - (Moves > 0 ? PopTimes[Moves - 1] : 0f));
        return gaps.Max();
    }

    // 이 시간대에 끌어서 없앤 슬라임 수
    public int RemovedBetween(float from, float to)
    {
        int count = 0;
        for (int i = 0; i < Moves; i++)
        {
            if (PopTimes[i] >= from && PopTimes[i] < to) count += PopSizes[i];
        }
        return count;
    }

    public const string CsvHeader =
        "날짜,세션,목표,판,시드,아이템,제거,수,실수,평균 조합,2마리 비율,평균 간격(초),간격 중앙값(초),가장 긴 공백(초)," +
        "0~60초 제거,60~120초 제거,120~180초 제거,천사,악마,킹,아이템 사용";

    public string ToCsv(string date, string session, int gameNumber, float duration)
    {
        float third = duration / 3f;
        return string.Join(",", new[]
        {
            date, session, I(TargetSum), I(gameNumber), I(Seed), Item, I(Removed), I(Moves), I(Misses),
            F(AverageSize), F(PairShare), F(AverageGap), F(MedianGap), F(LongestGap(duration)),
            I(RemovedBetween(0f, third)), I(RemovedBetween(third, third * 2f)), I(RemovedBetween(third * 2f, float.MaxValue)),
            I(Angels), I(Devils), I(Kings), I(ItemsUsed)
        });
    }

    internal static double Median(List<float> values)
    {
        if (values.Count == 0) return 0;
        List<float> sorted = values.OrderBy(v => v).ToList();
        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }

    internal static string I(int value) => value.ToString(CultureInfo.InvariantCulture);
    internal static string F(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}

// 같은 목표 합으로 둔 판 몇 개(보통 10판)를 묶어 요약하고 행동 패턴을 글로 풀어 줌
public sealed class PlayerSessionReport
{
    public readonly int TargetSum;
    public readonly IReadOnlyList<PlayerGameLog> Games;
    private readonly float duration;

    public PlayerSessionReport(int targetSum, IReadOnlyList<PlayerGameLog> games, float duration)
    {
        TargetSum = targetSum;
        Games = games;
        this.duration = duration;
    }

    public double AverageRemoved => Games.Average(g => g.Removed);
    public int MinRemoved => Games.Min(g => g.Removed);
    public int MaxRemoved => Games.Max(g => g.Removed);
    public double StdDevRemoved
    {
        get
        {
            double avg = AverageRemoved;
            return Math.Sqrt(Games.Average(g => (g.Removed - avg) * (g.Removed - avg)));
        }
    }

    // 모든 판의 간격을 한데 모아서 (수가 많은 판이 더 많이 반영됨)
    public double AverageGap => AllGaps().DefaultIfEmpty(0f).Average();
    public double MedianGap => PlayerGameLog.Median(AllGaps());
    private List<float> AllGaps() => Games.SelectMany(g => g.Gaps()).ToList();

    public double AverageSize => Avg(g => g.PopSizes.Sum()) / Math.Max(1.0, Avg(g => g.Moves));
    public double PairShare => (double)Games.Sum(g => g.PopSizes.Count(n => n == 2)) / Math.Max(1, Games.Sum(g => g.Moves));
    public double Third(int index) => Avg(g => g.RemovedBetween(duration / 3f * index, index == 2 ? float.MaxValue : duration / 3f * (index + 1)));

    private double Avg(Func<PlayerGameLog, double> pick) => Games.Average(pick);

    public const string CsvHeader =
        "날짜,목표,판 수,평균 제거,최소,최대,표준편차,평균 간격(초),간격 중앙값(초),평균 조합,2마리 비율,판당 실수," +
        "0~60초 제거,60~120초 제거,120~180초 제거,판당 천사,판당 악마,판당 킹,판당 아이템";

    public string ToCsv(string date)
    {
        return string.Join(",", new[]
        {
            date, PlayerGameLog.I(TargetSum), PlayerGameLog.I(Games.Count),
            PlayerGameLog.F(AverageRemoved), PlayerGameLog.I(MinRemoved), PlayerGameLog.I(MaxRemoved), PlayerGameLog.F(StdDevRemoved),
            PlayerGameLog.F(AverageGap), PlayerGameLog.F(MedianGap), PlayerGameLog.F(AverageSize), PlayerGameLog.F(PairShare),
            PlayerGameLog.F(Avg(g => g.Misses)),
            PlayerGameLog.F(Third(0)), PlayerGameLog.F(Third(1)), PlayerGameLog.F(Third(2)),
            PlayerGameLog.F(Avg(g => g.Angels)), PlayerGameLog.F(Avg(g => g.Devils)), PlayerGameLog.F(Avg(g => g.Kings)),
            PlayerGameLog.F(Avg(g => g.ItemsUsed))
        });
    }

    // history: 예전 세션들의 (목표 합, 평균 제거). 이번 세션은 넣지 않음.
    public string Describe(IReadOnlyList<KeyValuePair<int, double>> history)
    {
        StringBuilder text = new StringBuilder();
        text.AppendLine($"목표 {TargetSum} · {Games.Count}판 결과: 평균 {AverageRemoved:0.0}마리 ({MinRemoved} ~ {MaxRemoved}, 표준편차 {StdDevRemoved:0.0})");

        List<double> sameTarget = history.Where(h => h.Key == TargetSum).Select(h => h.Value).ToList();
        if (sameTarget.Count > 0)
        {
            double before = sameTarget.Average();
            text.AppendLine($"  지난 목표 {TargetSum} 기록 {sameTarget.Count}회 평균 {before:0.0}마리 → 이번 {AverageRemoved - before:+0.0;-0.0;0}마리");
        }

        text.AppendLine($"  빠르기: 평균 {AverageGap:0.0}초에 한 번 터뜨림 (중앙값 {MedianGap:0.0}초), 판당 {Avg(g => g.Moves):0.0}수");
        text.AppendLine($"  조합: 평균 {AverageSize:0.0}마리, 2마리 조합 {PairShare:P0}");

        double early = Third(0), late = Third(2);
        string stamina = early <= 0 ? "" :
            late < early * 0.85 ? $" → 후반에 {1 - late / early:P0} 줄어듦 (판이 비거나 지침)" :
            late > early * 1.15 ? $" → 후반에 {late / early - 1:P0} 늘어남 (몸이 풀림)" : " → 처음부터 끝까지 고름";
        text.AppendLine($"  시간대별 제거 (끌어서): 0~60초 {early:0.0} · 60~120초 {Third(1):0.0} · 120~180초 {late:0.0}{stamina}");

        text.AppendLine($"  실수: 판당 {Avg(g => g.Misses):0.0}번 합이 안 맞게 끎");
        text.AppendLine($"  특수 슬라임 (판당 터뜨림): 천사 {Avg(g => g.Angels):0.0} · 킹 {Avg(g => g.Kings):0.0} · 악마 {Avg(g => g.Devils):0.0}");
        text.AppendLine($"  아이템: 판당 {Avg(g => g.ItemsUsed):0.0}번, 안 쓴 판 {Games.Count(g => g.ItemsUsed == 0)}판");

        if (Games.Count >= 4)
        {
            int half = Games.Count / 2;
            double first = Games.Take(half).Average(g => g.Removed), second = Games.Skip(half).Average(g => g.Removed);
            text.AppendLine($"  흐름: 앞 {half}판 {first:0.0}마리 → 뒤 {Games.Count - half}판 {second:0.0}마리");
        }

        // 목표 합마다 지금까지의 내 평균 (이번 세션 포함)
        List<KeyValuePair<int, double>> all = new List<KeyValuePair<int, double>>(history) { new KeyValuePair<int, double>(TargetSum, AverageRemoved) };
        List<string> perTarget = new List<string>();
        for (int target = GameRules.MinTargetSum; target <= GameRules.MaxTargetSum; target++)
        {
            List<double> values = all.Where(h => h.Key == target).Select(h => h.Value).ToList();
            perTarget.Add(values.Count > 0 ? $"{target}: {values.Average():0.0}마리 ({values.Count}회)" : $"{target}: -");
        }
        text.Append("  목표별 내 평균: " + string.Join(" · ", perTarget));
        return text.ToString();
    }
}
