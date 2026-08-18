using NUnit.Framework;

public class InterstitialControllerTests
{
    private SaveData _saveData;
    private int _showCount;

    [SetUp]
    public void CreateSaveData()
    {
        _saveData = SaveData.CreateFresh("2026-08-18");
        _showCount = 0;
    }

    private InterstitialController CreateController()
    {
        return new InterstitialController(_saveData, () => _showCount++);
    }

    [Test]
    public void Constructor_NullSaveData_ThrowsArgumentNullException()
    {
        Assert.Throws<System.ArgumentNullException>(() => new InterstitialController(null, () => { }));
    }

    [Test]
    public void Constructor_NullShowCallback_ThrowsArgumentNullException()
    {
        Assert.Throws<System.ArgumentNullException>(() => new InterstitialController(_saveData, null));
    }

    [Test]
    public void RecordResolvedGameOver_FirstThreeGames_NeverShowsAnAd()
    {
        var controller = CreateController();

        controller.RecordResolvedGameOver("2026-08-18");
        controller.RecordResolvedGameOver("2026-08-18");
        controller.RecordResolvedGameOver("2026-08-18");

        Assert.AreEqual(0, _showCount, "CLAUDE.md §5.1: none in the first 3 games");
        Assert.AreEqual(3, _saveData.InterstitialCounter);
    }

    [Test]
    public void RecordResolvedGameOver_SixthGameOver_ShowsAnAd()
    {
        var controller = CreateController();

        for (int i = 0; i < 6; i++)
        {
            controller.RecordResolvedGameOver("2026-08-18");
        }

        Assert.AreEqual(1, _showCount, "game 6 is the first multiple of 3 after the exempt first 3");
    }

    [Test]
    public void RecordResolvedGameOver_FourthAndFifthGames_DoNotShowAnAd()
    {
        var controller = CreateController();

        for (int i = 0; i < 5; i++)
        {
            controller.RecordResolvedGameOver("2026-08-18");
        }

        Assert.AreEqual(0, _showCount);
    }

    [Test]
    public void RecordResolvedGameOver_NinthGameOver_ShowsASecondAd()
    {
        var controller = CreateController();

        for (int i = 0; i < 9; i++)
        {
            controller.RecordResolvedGameOver("2026-08-18");
        }

        Assert.AreEqual(2, _showCount, "games 6 and 9 both qualify");
    }

    [Test]
    public void RecordResolvedGameOver_DailyCapOfSix_StopsShowingFurtherAdsSameDay()
    {
        var controller = CreateController();

        // Every 3rd game-over from 6 through 6+3*6=24 would normally be
        // eligible (6,9,12,15,18,21,24 = 7 potential shows) — the cap
        // should stop it at 6.
        for (int i = 0; i < 24; i++)
        {
            controller.RecordResolvedGameOver("2026-08-18");
        }

        Assert.AreEqual(6, _showCount, "daily cap of 6 must not be exceeded");
        Assert.AreEqual(6, _saveData.InterstitialTodayCount);
    }

    [Test]
    public void RecordResolvedGameOver_NewDay_ResetsDailyCountAndAllowsMoreAds()
    {
        var controller = CreateController();
        for (int i = 0; i < 24; i++)
        {
            controller.RecordResolvedGameOver("2026-08-18");
        }
        Assert.AreEqual(6, _showCount, "sanity: daily cap hit on day 1");

        controller.RecordResolvedGameOver("2026-08-19"); // counter=25, not a multiple of 3 -> no show, but resets the day
        Assert.AreEqual(0, _saveData.InterstitialTodayCount);

        controller.RecordResolvedGameOver("2026-08-19"); // counter=26
        controller.RecordResolvedGameOver("2026-08-19"); // counter=27, multiple of 3 -> eligible again on the new day

        Assert.AreEqual(7, _showCount, "a new day should allow at least one more ad past the previous day's cap");
    }

    [Test]
    public void RecordResolvedGameOver_AlwaysIncrementsLifetimeCounterRegardlessOfEligibility()
    {
        var controller = CreateController();

        controller.RecordResolvedGameOver("2026-08-18");

        Assert.AreEqual(1, _saveData.InterstitialCounter);
    }
}
