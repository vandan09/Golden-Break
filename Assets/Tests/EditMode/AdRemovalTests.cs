using NUnit.Framework;

/// <summary>
/// A player who paid to remove ads must never be shown one.
///
/// Worth locking down because the gap was invisible: IapManager stored the
/// entitlement, the save schema persisted it, IsAdsRemoved exposed it — and
/// no ad code read it. Buying "Remove ads" would have taken the money and
/// changed nothing, which is a billing complaint rather than a bug report.
/// Harmless only while IAP is stubbed and nobody can buy it.
/// </summary>
public class AdRemovalTests
{
    private const string Today = "2026-08-30";

    private static SaveData FreshSaveWithAdsRemoved(bool removed)
    {
        SaveData data = SaveData.CreateFresh(Today);
        data.IapRemoveAds = removed;
        return data;
    }

    [Test]
    public void Interstitial_NeverShownOnceAdsAreRemoved()
    {
        SaveData data = FreshSaveWithAdsRemoved(true);
        int shown = 0;
        var controller = new InterstitialController(data, () => shown++);

        // Well past the first-3-games grace period and landing on the
        // cadence interval every time, so only the entitlement can be
        // suppressing it.
        for (int i = 0; i < 20; i++)
        {
            controller.RecordResolvedGameOver(Today);
        }

        Assert.AreEqual(0, shown, "a player who removed ads must never see an interstitial");
    }

    [Test]
    public void Interstitial_StillShownWhenAdsAreNotRemoved()
    {
        SaveData data = FreshSaveWithAdsRemoved(false);
        int shown = 0;
        var controller = new InterstitialController(data, () => shown++);

        for (int i = 0; i < 20; i++)
        {
            controller.RecordResolvedGameOver(Today);
        }

        // Proves the test above is measuring the entitlement rather than a
        // controller that never fires at all.
        Assert.Greater(shown, 0, "interstitials should still run for a non-paying player");
    }

    [Test]
    public void Interstitial_RespectsTheFirstGamesGracePeriodAndDailyCap()
    {
        SaveData data = FreshSaveWithAdsRemoved(false);
        int shown = 0;
        var controller = new InterstitialController(data, () => shown++);

        // CLAUDE.md §5.1: none in the first 3 games, then every 3rd
        // game-over, capped at 6 a day.
        for (int i = 0; i < 3; i++)
        {
            controller.RecordResolvedGameOver(Today);
        }

        Assert.AreEqual(0, shown, "the first three games must stay ad-free");

        for (int i = 0; i < 100; i++)
        {
            controller.RecordResolvedGameOver(Today);
        }

        Assert.LessOrEqual(shown, 6, "daily cap of 6 must hold");
    }
}
