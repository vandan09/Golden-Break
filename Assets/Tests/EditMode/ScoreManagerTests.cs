using NUnit.Framework;

public class ScoreManagerTests
{
    [TestCase(0, 0)]
    [TestCase(1, 10)]
    [TestCase(2, 30)]
    [TestCase(3, 60)]
    [TestCase(4, 100)]
    [TestCase(5, 150)]
    [TestCase(6, 200)]
    [TestCase(8, 300)]
    public void CalculateBasePoints_KnownLineCounts_MatchSpecTable(int linesCleared, int expectedPoints)
    {
        Assert.AreEqual(expectedPoints, ScoreManager.CalculateBasePoints(linesCleared));
    }

    [Test]
    public void ApplyLineClear_FirstClearInStreak_UsesBaseMultiplierOfOne()
    {
        var scoreManager = new ScoreManager(initialBestScore: 0);

        int points = scoreManager.ApplyLineClear(1);

        Assert.AreEqual(10, points);
    }

    [Test]
    public void ApplyLineClear_ConsecutiveClears_StreakMultiplierEscalatesByHalfEachTime()
    {
        var scoreManager = new ScoreManager(initialBestScore: 0);

        int first = scoreManager.ApplyLineClear(1);
        int second = scoreManager.ApplyLineClear(1);
        int third = scoreManager.ApplyLineClear(1);
        int fourth = scoreManager.ApplyLineClear(1);
        int fifth = scoreManager.ApplyLineClear(1);

        Assert.AreEqual(10, first);
        Assert.AreEqual(15, second);
        Assert.AreEqual(20, third);
        Assert.AreEqual(25, fourth);
        Assert.AreEqual(30, fifth);
    }

    [Test]
    public void ApplyLineClear_StreakBeyondFifthClear_CapsAtTripleMultiplier()
    {
        var scoreManager = new ScoreManager(initialBestScore: 0);
        for (int i = 0; i < 5; i++)
        {
            scoreManager.ApplyLineClear(1);
        }

        int sixth = scoreManager.ApplyLineClear(1);
        int seventh = scoreManager.ApplyLineClear(1);

        Assert.AreEqual(30, sixth);
        Assert.AreEqual(30, seventh);
        Assert.AreEqual(3f, scoreManager.StreakMultiplier);
    }

    [Test]
    public void ApplyLineClear_NonClearingPlacement_ResetsStreakToOne()
    {
        var scoreManager = new ScoreManager(initialBestScore: 0);
        scoreManager.ApplyLineClear(1);
        scoreManager.ApplyLineClear(1);

        int noClearPoints = scoreManager.ApplyLineClear(0);
        Assert.AreEqual(1f, scoreManager.StreakMultiplier, "streak should reset immediately on the non-clearing placement");

        int nextClear = scoreManager.ApplyLineClear(1);

        Assert.AreEqual(0, noClearPoints);
        Assert.AreEqual(10, nextClear, "streak should have reset to x1 before this clear");
    }

    [Test]
    public void ApplyLineClear_AccumulatesIntoCurrentScore()
    {
        var scoreManager = new ScoreManager(initialBestScore: 0);

        scoreManager.ApplyLineClear(1);
        scoreManager.ApplyLineClear(2);

        Assert.AreEqual(10 + 45, scoreManager.CurrentScore); // 10 + (30 * 1.5)
    }

    [Test]
    public void ApplyLineClear_FiresOnScoreChangedWithCumulativeScore()
    {
        var scoreManager = new ScoreManager(initialBestScore: 0);
        int? reportedScore = null;
        scoreManager.OnScoreChanged += s => reportedScore = s;

        scoreManager.ApplyLineClear(1);

        Assert.AreEqual(10, reportedScore);
    }

    [Test]
    public void ApplyLineClear_ScoreExceedsBest_UpdatesBestAndFiresOnNewBest()
    {
        var scoreManager = new ScoreManager(initialBestScore: 5);
        bool newBestFired = false;
        scoreManager.OnNewBest += () => newBestFired = true;

        scoreManager.ApplyLineClear(1);

        Assert.IsTrue(newBestFired);
        Assert.AreEqual(10, scoreManager.BestScore);
    }

    [Test]
    public void ApplyLineClear_ScoreStillBelowBest_DoesNotFireOnNewBest()
    {
        var scoreManager = new ScoreManager(initialBestScore: 10000);
        bool newBestFired = false;
        scoreManager.OnNewBest += () => newBestFired = true;

        scoreManager.ApplyLineClear(1);

        Assert.IsFalse(newBestFired);
        Assert.AreEqual(10000, scoreManager.BestScore);
    }

    [Test]
    public void ApplyLineClear_ScoreExactlyEqualsBest_DoesNotFireOnNewBest()
    {
        var scoreManager = new ScoreManager(initialBestScore: 10);
        bool newBestFired = false;
        scoreManager.OnNewBest += () => newBestFired = true;

        scoreManager.ApplyLineClear(1);

        Assert.IsFalse(newBestFired, "matching (not exceeding) the best should not count as a new best");
    }

    [Test]
    public void ResetForNewGame_ClearsCurrentScoreAndStreakButKeepsBest()
    {
        var scoreManager = new ScoreManager(initialBestScore: 0);
        scoreManager.ApplyLineClear(2);
        scoreManager.ApplyLineClear(1);

        scoreManager.ResetForNewGame();

        Assert.AreEqual(0, scoreManager.CurrentScore);
        Assert.AreEqual(1f, scoreManager.StreakMultiplier);
        Assert.AreEqual(45, scoreManager.BestScore, "best score set during the previous game should survive a reset");
    }

    [Test]
    public void Constructor_SeedsInitialBestScore()
    {
        var scoreManager = new ScoreManager(initialBestScore: 4280);

        Assert.AreEqual(4280, scoreManager.BestScore);
        Assert.AreEqual(0, scoreManager.CurrentScore);
    }
}
