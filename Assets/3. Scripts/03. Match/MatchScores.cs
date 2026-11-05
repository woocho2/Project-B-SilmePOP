using System.Collections.Generic;

// 한 판의 모든 자리 점수와 순위. 점수판과 결과 화면은 이것만 보면 됨.
public sealed class MatchScores
{
    private readonly List<ScoreEntry> entries = new List<ScoreEntry>();

    public void SetEntries(IReadOnlyList<ScoreEntry> newEntries)
    {
        entries.Clear();
        entries.AddRange(newEntries);
    }

    public void Clear()
    {
        entries.Clear();
    }

    public ScoreEntry GetEntry(int slot)
    {
        foreach (ScoreEntry entry in entries)
        {
            if (entry.slot == slot) return entry;
        }
        return null;
    }

    // 1위부터 차례로.
    public List<ScoreEntry> GetRanking()
    {
        List<ScoreEntry> ranking = new List<ScoreEntry>(entries);
        ranking.Sort(CompareRank);
        return ranking;
    }

    // 점수 높은 순 → 동점이면 그 점수에 먼저 도달한 순 → 기권은 맨 아래, 먼저 나간 사람이 더 아래.
    private static int CompareRank(ScoreEntry a, ScoreEntry b)
    {
        if (a.forfeited != b.forfeited) return a.forfeited ? 1 : -1;

        if (a.forfeited)
        {
            int byForfeitTime = b.forfeitTime.CompareTo(a.forfeitTime);
            return byForfeitTime != 0 ? byForfeitTime : a.slot.CompareTo(b.slot);
        }

        if (a.score != b.score) return b.score.CompareTo(a.score);

        int byScoreTime = a.lastScoreTime.CompareTo(b.lastScoreTime);
        return byScoreTime != 0 ? byScoreTime : a.slot.CompareTo(b.slot);
    }
}
