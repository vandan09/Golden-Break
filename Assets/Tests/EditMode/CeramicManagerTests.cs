using NUnit.Framework;

public class CeramicManagerTests
{
    private static CeramicProgressData MakeProgress(int tier, int totalCracks, int cracksRepaired)
    {
        return new CeramicProgressData
        {
            Tier = tier,
            TotalCracks = totalCracks,
            CracksRepaired = cracksRepaired,
            ColourVariant = 0
        };
    }

    [Test]
    public void RepairCracks_FewerThanRemaining_RepairsExactlyThatManyAndDoesNotComplete()
    {
        var manager = new CeramicManager(MakeProgress(1, totalCracks: 4, cracksRepaired: 0), 0);

        bool completed = manager.RepairCracks(2);

        Assert.IsFalse(completed);
        Assert.AreEqual(2, manager.Progress.CracksRepaired);
    }

    [Test]
    public void RepairCracks_FiresOnCracksRepairedWithActualRepairedCount()
    {
        var manager = new CeramicManager(MakeProgress(1, totalCracks: 4, cracksRepaired: 0), 0);
        int? repairedCount = null;
        manager.OnCracksRepaired += n => repairedCount = n;

        manager.RepairCracks(2);

        Assert.AreEqual(2, repairedCount);
    }

    [Test]
    public void RepairCracks_ExactlyReachesTotalCracks_CompletesAndFiresOnCeramicCompleted()
    {
        var manager = new CeramicManager(MakeProgress(1, totalCracks: 4, cracksRepaired: 3), 0);
        bool completedFired = false;
        manager.OnCeramicCompleted += () => completedFired = true;

        bool completed = manager.RepairCracks(1);

        Assert.IsTrue(completed);
        Assert.IsTrue(completedFired);
        Assert.AreEqual(4, manager.Progress.CracksRepaired);
    }

    [Test]
    public void RepairCracks_MoreThanRemaining_CapsAtTotalCracksWithoutOverflow()
    {
        var manager = new CeramicManager(MakeProgress(1, totalCracks: 4, cracksRepaired: 3), 0);

        bool completed = manager.RepairCracks(5);

        Assert.IsTrue(completed);
        Assert.AreEqual(4, manager.Progress.CracksRepaired, "should cap at totalCracks, not overflow to 8");
    }

    [Test]
    public void RepairCracks_ZeroLines_DoesNothingAndReturnsFalse()
    {
        var manager = new CeramicManager(MakeProgress(1, totalCracks: 4, cracksRepaired: 1), 0);
        bool repairedFired = false;
        manager.OnCracksRepaired += _ => repairedFired = true;

        bool completed = manager.RepairCracks(0);

        Assert.IsFalse(completed);
        Assert.IsFalse(repairedFired);
        Assert.AreEqual(1, manager.Progress.CracksRepaired);
    }

    [Test]
    public void RepairCracks_AlreadyComplete_DoesNothingAndDoesNotRefireCompletion()
    {
        var manager = new CeramicManager(MakeProgress(1, totalCracks: 4, cracksRepaired: 4), 0);
        bool completedFired = false;
        manager.OnCeramicCompleted += () => completedFired = true;

        bool completed = manager.RepairCracks(2);

        Assert.IsFalse(completed);
        Assert.IsFalse(completedFired);
        Assert.AreEqual(4, manager.Progress.CracksRepaired);
    }

    [Test]
    public void AddScore_Accumulates()
    {
        var manager = new CeramicManager(MakeProgress(1, 4, 0), 100);

        manager.AddScore(50);
        manager.AddScore(25);

        Assert.AreEqual(175, manager.CumulativeScoreThisCeramic);
    }

    [Test]
    public void AdvanceToNextTier_IncrementsTierAndResetsCracksAndScore()
    {
        var manager = new CeramicManager(MakeProgress(1, totalCracks: 4, cracksRepaired: 4), 250);

        manager.AdvanceToNextTier(nextTierTotalCracks: 5);

        Assert.AreEqual(2, manager.Progress.Tier);
        Assert.AreEqual(5, manager.Progress.TotalCracks);
        Assert.AreEqual(0, manager.Progress.CracksRepaired);
        Assert.AreEqual(0, manager.CumulativeScoreThisCeramic);
    }

    [Test]
    public void AdvanceToNextTier_PastNine_SetsCorrectColourVariant()
    {
        var manager = new CeramicManager(MakeProgress(9, totalCracks: 12, cracksRepaired: 12), 0);

        manager.AdvanceToNextTier(nextTierTotalCracks: 9);

        Assert.AreEqual(10, manager.Progress.Tier);
        Assert.AreEqual(CeramicTierResolver.ResolveColourVariant(10), manager.Progress.ColourVariant);
    }

    [Test]
    public void Constructor_NullProgress_ThrowsArgumentNullException()
    {
        Assert.Throws<System.ArgumentNullException>(() => new CeramicManager(null, 0));
    }
}
