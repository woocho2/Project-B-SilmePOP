using System.Collections.Generic;
using NUnit.Framework;

public class PlayerRecordTest
{
    private static PlayerGameLog Game(int removed, params float[] popTimes)
    {
        BoardModel board = new BoardModel();
        PlayerGameLog log = new PlayerGameLog { TargetSum = 10, Removed = removed };
        int cell = 0;
        foreach (float time in popTimes)
        {
            List<BoardSlime> pair = new List<BoardSlime>
            {
                board.AddSlime(3, SlimeType.Basic, cell++),
                board.AddSlime(7, cell == 1 ? SlimeType.Angel : SlimeType.Basic, cell++)
            };
            log.AddPop(time, pair);
        }
        return log;
    }

    [Test]
    public void GameLog_GapsStartFromZero_AndThirdsSplitByTime()
    {
        PlayerGameLog log = Game(6, 10f, 70f, 130f);

        CollectionAssert.AreEqual(new[] { 10f, 60f, 60f }, log.Gaps());
        Assert.AreEqual(60.0, log.MedianGap, 1e-6);
        Assert.AreEqual(60.0, log.LongestGap(180f), 1e-6);   // 마지막 수부터 끝까지는 50초
        Assert.AreEqual(2, log.RemovedBetween(0f, 60f));
        Assert.AreEqual(2, log.RemovedBetween(120f, float.MaxValue));
        Assert.AreEqual(1, log.Angels);
        Assert.AreEqual(1.0, log.PairShare, 1e-6);
        Assert.AreEqual(PlayerGameLog.CsvHeader.Split(',').Length, log.ToCsv("d", "s", 1, 180f).Split(',').Length);
    }

    [Test]
    public void SessionReport_AveragesGames_AndComparesWithHistory()
    {
        PlayerSessionReport report = new PlayerSessionReport(10, new[] { Game(80, 5f), Game(100, 5f, 10f) }, 180f);

        Assert.AreEqual(90.0, report.AverageRemoved, 1e-6);
        Assert.AreEqual(10.0, report.StdDevRemoved, 1e-6);
        Assert.AreEqual(PlayerSessionReport.CsvHeader.Split(',').Length, report.ToCsv("d").Split(',').Length);

        string text = report.Describe(new[] { new KeyValuePair<int, double>(10, 85.0), new KeyValuePair<int, double>(12, 70.0) });
        StringAssert.Contains("+5.0마리", text);
        StringAssert.Contains("10: 87.5마리 (2회)", text);
        StringAssert.Contains("12: 70.0마리 (1회)", text);
    }
}
