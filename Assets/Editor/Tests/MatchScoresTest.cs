using System.Collections.Generic;
using NUnit.Framework;

public class MatchScoresTest
{
    private static List<int> RankedSlots(params ScoreEntry[] entries)
    {
        MatchScores scores = new MatchScores();
        scores.SetEntries(entries);

        List<int> slots = new List<int>();
        foreach (ScoreEntry entry in scores.GetRanking())
        {
            slots.Add(entry.slot);
        }
        return slots;
    }

    [Test]
    public void HigherScore_RanksFirst()
    {
        CollectionAssert.AreEqual(new[] { 1, 2, 0 }, RankedSlots(
            new ScoreEntry { slot = 0, score = 10 },
            new ScoreEntry { slot = 1, score = 30 },
            new ScoreEntry { slot = 2, score = 20 }));
    }

    [Test]
    public void SameScore_EarlierTimeRanksFirst()
    {
        CollectionAssert.AreEqual(new[] { 1, 0 }, RankedSlots(
            new ScoreEntry { slot = 0, score = 10, lastScoreTime = 50f },
            new ScoreEntry { slot = 1, score = 10, lastScoreTime = 20f }));
    }

    [Test]
    public void NegativeScore_RanksBelowZero()
    {
        CollectionAssert.AreEqual(new[] { 1, 0 }, RankedSlots(
            new ScoreEntry { slot = 0, score = -5 },
            new ScoreEntry { slot = 1, score = 0 }));
    }

    [Test]
    public void Forfeit_RanksLast_EvenWithHighScore()
    {
        CollectionAssert.AreEqual(new[] { 1, 0 }, RankedSlots(
            new ScoreEntry { slot = 0, score = 100, forfeited = true, forfeitTime = 60f },
            new ScoreEntry { slot = 1, score = -5 }));
    }

    [Test]
    public void EarlierForfeit_RanksLower()
    {
        CollectionAssert.AreEqual(new[] { 1, 0 }, RankedSlots(
            new ScoreEntry { slot = 0, score = 50, forfeited = true, forfeitTime = 10f },
            new ScoreEntry { slot = 1, score = 1, forfeited = true, forfeitTime = 40f }));
    }

    [Test]
    public void FullTie_IsStableBySlot()
    {
        CollectionAssert.AreEqual(new[] { 0, 1 }, RankedSlots(
            new ScoreEntry { slot = 1, score = 5, lastScoreTime = 3f },
            new ScoreEntry { slot = 0, score = 5, lastScoreTime = 3f }));
    }
}
