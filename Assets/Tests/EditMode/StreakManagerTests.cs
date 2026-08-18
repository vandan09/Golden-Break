using System;
using NUnit.Framework;

public class StreakManagerTests
{
    [TestCase(1, 10, null)]
    [TestCase(2, 20, null)]
    [TestCase(3, 30, null)]
    [TestCase(4, 40, null)]
    [TestCase(5, 50, null)]
    [TestCase(6, 60, null)]
    [TestCase(7, 100, "streak_day7")]
    [TestCase(8, 10, null)]
    [TestCase(13, 60, null)]
    [TestCase(14, 150, "streak_day14")]
    [TestCase(20, 60, null)]
    [TestCase(21, 200, "streak_day21_rare")]
    [TestCase(22, 10, null)]
    [TestCase(27, 60, null)]
    public void GetReward_FirstCycle_MatchesSpecTable(int day, int expectedCoins, string expectedFrame)
    {
        (int coins, string frame) = StreakManager.GetReward(day);

        Assert.AreEqual(expectedCoins, coins);
        Assert.AreEqual(expectedFrame, frame);
    }

    [Test]
    public void GetReward_Day28_StartsSecondCycleWithTwentyFivePercentBonus()
    {
        (int coins, string frame) = StreakManager.GetReward(28);

        Assert.AreEqual(13, coins, "day 1's base 10 coins * 1.25 = 12.5, rounded away from zero to 13");
        Assert.IsNull(frame);
    }

    [Test]
    public void GetReward_Day34_SecondCycleMilestoneAlsoGetsBonus()
    {
        // Day 34 = cycle-1's day 7 (28 + 6).
        (int coins, string frame) = StreakManager.GetReward(34);

        Assert.AreEqual(125, coins, "day 7's base 100 * 1.25 = 125 exactly");
        Assert.AreEqual("streak_day7", frame);
    }

    [Test]
    public void GetReward_ThirdCycle_GetsFiftyPercentBonus()
    {
        // Day 55 = cycle-2's day 1 (28 + 27).
        (int coins, string frame) = StreakManager.GetReward(55);

        Assert.AreEqual(15, coins, "day 1's base 10 * 1.5 = 15 exactly");
        Assert.IsNull(frame);
    }

    [Test]
    public void RecordGameCompleted_FirstEverGame_StartsStreakAtOne()
    {
        var streak = new StreakManager(initialStreakCount: 0, lastPlayedDateIso: null);

        StreakManager.StreakResult result = streak.RecordGameCompleted(new DateTime(2026, 8, 18));

        Assert.IsTrue(result.StreakAdvanced);
        Assert.AreEqual(1, result.StreakCount);
        Assert.AreEqual(10, result.CoinsAwarded);
        Assert.AreEqual(1, streak.StreakCount);
        Assert.AreEqual("2026-08-18", streak.LastPlayedDateIso);
    }

    [Test]
    public void RecordGameCompleted_SecondGameSameDay_DoesNotAdvanceOrAwardAgain()
    {
        var streak = new StreakManager(initialStreakCount: 0, lastPlayedDateIso: null);
        streak.RecordGameCompleted(new DateTime(2026, 8, 18));

        StreakManager.StreakResult result = streak.RecordGameCompleted(new DateTime(2026, 8, 18));

        Assert.IsFalse(result.StreakAdvanced);
        Assert.AreEqual(0, result.CoinsAwarded);
        Assert.AreEqual(1, streak.StreakCount, "streak should still be 1, not double-counted");
    }

    [Test]
    public void RecordGameCompleted_NextConsecutiveDay_IncrementsStreak()
    {
        var streak = new StreakManager(initialStreakCount: 3, lastPlayedDateIso: "2026-08-17");

        StreakManager.StreakResult result = streak.RecordGameCompleted(new DateTime(2026, 8, 18));

        Assert.IsTrue(result.StreakAdvanced);
        Assert.AreEqual(4, result.StreakCount);
        Assert.AreEqual(40, result.CoinsAwarded);
    }

    [Test]
    public void RecordGameCompleted_MissedADay_ResetsStreakToOne()
    {
        var streak = new StreakManager(initialStreakCount: 5, lastPlayedDateIso: "2026-08-15");

        StreakManager.StreakResult result = streak.RecordGameCompleted(new DateTime(2026, 8, 18));

        Assert.IsTrue(result.StreakAdvanced);
        Assert.AreEqual(1, result.StreakCount, "a missed day breaks the streak");
        Assert.AreEqual(10, result.CoinsAwarded);
    }

    [Test]
    public void RecordGameCompleted_ReachingDaySeven_UnlocksGalleryFrame()
    {
        var streak = new StreakManager(initialStreakCount: 6, lastPlayedDateIso: "2026-08-17");

        StreakManager.StreakResult result = streak.RecordGameCompleted(new DateTime(2026, 8, 18));

        Assert.AreEqual(7, result.StreakCount);
        Assert.AreEqual(100, result.CoinsAwarded);
        Assert.AreEqual("streak_day7", result.GalleryFrameUnlocked);
    }
}
