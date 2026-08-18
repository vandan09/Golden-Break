using System.Collections.Generic;
using NUnit.Framework;

public class MilestoneManagerTests
{
    [Test]
    public void CheckNewlyReached_ScoreBelowFirstMilestone_ReturnsEmpty()
    {
        var claimed = new List<int>();

        List<MilestoneManager.MilestoneResult> results = MilestoneManager.CheckNewlyReached(499, claimed);

        Assert.AreEqual(0, results.Count);
        Assert.AreEqual(0, claimed.Count);
    }

    [Test]
    public void CheckNewlyReached_ExactlyFiveHundred_ReturnsFirstMilestoneWithGalleryFrame()
    {
        var claimed = new List<int>();

        List<MilestoneManager.MilestoneResult> results = MilestoneManager.CheckNewlyReached(500, claimed);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(500, results[0].MilestoneScore);
        Assert.AreEqual(50, results[0].CoinsAwarded);
        Assert.AreEqual("milestone_500", results[0].GalleryFrameUnlocked);
        Assert.IsNull(results[0].ThemeUnlocked);
        Assert.Contains(500, claimed);
    }

    [Test]
    public void CheckNewlyReached_OneThousand_GrantsThemeNotFrame()
    {
        var claimed = new List<int> { 500 };

        List<MilestoneManager.MilestoneResult> results = MilestoneManager.CheckNewlyReached(1000, claimed);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(1000, results[0].MilestoneScore);
        Assert.AreEqual(100, results[0].CoinsAwarded);
        Assert.IsNull(results[0].GalleryFrameUnlocked);
        Assert.AreEqual("milestone_1000", results[0].ThemeUnlocked);
    }

    [Test]
    public void CheckNewlyReached_HighScoreOnFirstGame_ClaimsAllQualifyingMilestonesAtOnce()
    {
        var claimed = new List<int>();

        List<MilestoneManager.MilestoneResult> results = MilestoneManager.CheckNewlyReached(3000, claimed);

        Assert.AreEqual(3, results.Count, "3000 clears 500, 1000, and 2500, but not 5000/10000");
        Assert.AreEqual(500, results[0].MilestoneScore);
        Assert.AreEqual(1000, results[1].MilestoneScore);
        Assert.AreEqual(2500, results[2].MilestoneScore);
        CollectionAssert.AreEquivalent(new[] { 500, 1000, 2500 }, claimed);
    }

    [Test]
    public void CheckNewlyReached_AllFiveAtOnce_ReturnsAllInAscendingOrder()
    {
        var claimed = new List<int>();

        List<MilestoneManager.MilestoneResult> results = MilestoneManager.CheckNewlyReached(50000, claimed);

        Assert.AreEqual(5, results.Count);
        CollectionAssert.AreEqual(new[] { 500, 1000, 2500, 5000, 10000 }, results.ConvertAll(r => r.MilestoneScore));
    }

    [Test]
    public void CheckNewlyReached_AlreadyClaimedMilestone_IsNotReturnedAgain()
    {
        var claimed = new List<int> { 500 };

        List<MilestoneManager.MilestoneResult> results = MilestoneManager.CheckNewlyReached(500, claimed);

        Assert.AreEqual(0, results.Count, "milestones trigger once ever, per CLAUDE.md §4.3");
        Assert.AreEqual(1, claimed.Count, "should not be duplicated in the claimed list");
    }

    [Test]
    public void CheckNewlyReached_TenThousand_GrantsRareGalleryFrame()
    {
        var claimed = new List<int> { 500, 1000, 2500, 5000 };

        List<MilestoneManager.MilestoneResult> results = MilestoneManager.CheckNewlyReached(10000, claimed);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(300, results[0].CoinsAwarded);
        Assert.AreEqual("milestone_10000_rare", results[0].GalleryFrameUnlocked);
    }
}
