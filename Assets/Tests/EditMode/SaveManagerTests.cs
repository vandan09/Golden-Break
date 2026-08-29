using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SaveManagerTests
{
    private sealed class FakeSavePersistence : ISavePersistence
    {
        private string _stored;
        private bool _hasData;

        public void Seed(string json)
        {
            _stored = json;
            _hasData = true;
        }

        public bool HasSavedData() => _hasData;
        public string Load() => _stored;

        public void Save(string json)
        {
            _stored = json;
            _hasData = true;
        }
    }

    [Test]
    public void LoadFrom_NoSavedData_ReturnsFreshSaveWithDefaults()
    {
        var persistence = new FakeSavePersistence();

        SaveData data = SaveManager.LoadFrom(persistence);

        Assert.AreEqual(SaveData.CurrentSaveVersion, data.SaveVersion);
        Assert.AreEqual(0, data.BestScore);
        Assert.AreEqual(Constants.StartingCoins, data.Coins, "a fresh save is seeded so undo/refresh aren't dead on first run");
        Assert.IsNotNull(data.CurrentCeramic);
        Assert.AreEqual(1, data.CurrentCeramic.Tier);
        Assert.AreEqual(4, data.CurrentCeramic.TotalCracks);
        Assert.IsNotNull(data.Gallery);
        Assert.AreEqual(0, data.Gallery.Count);
        Assert.IsTrue(data.Settings.Sound);
    }

    [Test]
    public void LoadFrom_ValidPreviouslySavedJson_RoundTripsAllFields()
    {
        var persistence = new FakeSavePersistence();
        SaveData original = SaveData.CreateFresh("2026-08-17");
        original.BestScore = 4280;
        original.Coins = 245;
        original.CeramicCumulativeScore = 1875;
        original.CurrentCeramic.Tier = 5;
        original.CurrentCeramic.CracksRepaired = 3;
        original.Gallery.Add(new GalleryEntryData { Tier = 1, Date = "2026-07-28", Score = 2140 });
        persistence.Seed(JsonConvert.SerializeObject(original));

        SaveData loaded = SaveManager.LoadFrom(persistence);

        Assert.AreEqual(4280, loaded.BestScore);
        Assert.AreEqual(245, loaded.Coins);
        Assert.AreEqual(1875, loaded.CeramicCumulativeScore);
        Assert.AreEqual(5, loaded.CurrentCeramic.Tier);
        Assert.AreEqual(3, loaded.CurrentCeramic.CracksRepaired);
        Assert.AreEqual(1, loaded.Gallery.Count);
        Assert.AreEqual(2140, loaded.Gallery[0].Score);
    }

    [Test]
    public void LoadFrom_CorruptedJson_ResetsToFreshSaveWithoutThrowing()
    {
        var persistence = new FakeSavePersistence();
        persistence.Seed("{ this is not valid json ");

        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("SaveManager: save data corrupted.*"));
        SaveData data = null;
        Assert.DoesNotThrow(() => data = SaveManager.LoadFrom(persistence));

        Assert.IsNotNull(data);
        Assert.AreEqual(SaveData.CurrentSaveVersion, data.SaveVersion);
        Assert.AreEqual(0, data.BestScore);
    }

    [Test]
    public void LoadFrom_CorruptedJson_FiresOnSaveCorruptedEvent()
    {
        var persistence = new FakeSavePersistence();
        persistence.Seed("not json at all");
        bool fired = false;
        System.Action handler = () => fired = true;
        SaveManager.OnSaveCorrupted += handler;

        try
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("SaveManager: save data corrupted.*"));
            SaveManager.LoadFrom(persistence);
        }
        finally
        {
            SaveManager.OnSaveCorrupted -= handler;
        }

        Assert.IsTrue(fired);
    }

    [Test]
    public void LoadFrom_OlderSaveVersion_StampsCurrentVersionOnLoad()
    {
        var persistence = new FakeSavePersistence();
        SaveData original = SaveData.CreateFresh("2026-08-17");
        original.SaveVersion = 0;
        original.BestScore = 100;
        persistence.Seed(JsonConvert.SerializeObject(original));

        SaveData loaded = SaveManager.LoadFrom(persistence);

        Assert.AreEqual(SaveData.CurrentSaveVersion, loaded.SaveVersion);
        Assert.AreEqual(100, loaded.BestScore, "migration should not disturb existing field values");
    }

    [Test]
    public void LoadFrom_V1SaveWithNoCoins_IsGrantedStartingCoins()
    {
        var persistence = new FakeSavePersistence();
        SaveData original = SaveData.CreateFresh("2026-08-17");
        original.SaveVersion = 1;
        original.Coins = 0;
        persistence.Seed(JsonConvert.SerializeObject(original));

        SaveData loaded = SaveManager.LoadFrom(persistence);

        Assert.AreEqual(Constants.StartingCoins, loaded.Coins,
            "a v1 save predates undo/refresh being coin-gated, so it must not upgrade into two unaffordable buttons");
    }

    [Test]
    public void LoadFrom_V1SaveWithEarnedCoins_KeepsTheLargerBalance()
    {
        var persistence = new FakeSavePersistence();
        SaveData original = SaveData.CreateFresh("2026-08-17");
        original.SaveVersion = 1;
        original.Coins = Constants.StartingCoins + 500;
        persistence.Seed(JsonConvert.SerializeObject(original));

        SaveData loaded = SaveManager.LoadFrom(persistence);

        Assert.AreEqual(Constants.StartingCoins + 500, loaded.Coins,
            "the grant tops up, it must never overwrite coins the player already earned");
    }

    [Test]
    public void LoadFrom_AlreadyCurrentSaveVersion_LeavesDataUnchanged()
    {
        var persistence = new FakeSavePersistence();
        SaveData original = SaveData.CreateFresh("2026-08-17");
        original.SaveVersion = SaveData.CurrentSaveVersion;
        original.Coins = 77;
        persistence.Seed(JsonConvert.SerializeObject(original));

        SaveData loaded = SaveManager.LoadFrom(persistence);

        Assert.AreEqual(SaveData.CurrentSaveVersion, loaded.SaveVersion);
        Assert.AreEqual(77, loaded.Coins);
    }

    [Test]
    public void LoadFrom_JsonMissingAFieldAddedLaterToV1_BackfillsAnEmptyCollectionNotNull()
    {
        var persistence = new FakeSavePersistence();
        // Simulates a real save written before gallery_frames_owned
        // existed in the schema — Newtonsoft leaves the field null rather
        // than erroring on an unrecognized-missing property.
        persistence.Seed("{\"save_version\":1,\"best_score\":50,\"coins\":10}");

        SaveData loaded = SaveManager.LoadFrom(persistence);

        Assert.IsNotNull(loaded.GalleryFramesOwned);
        Assert.AreEqual(0, loaded.GalleryFramesOwned.Count);
        Assert.IsNotNull(loaded.Gallery);
        Assert.IsNotNull(loaded.MilestonesClaimed);
        Assert.IsNotNull(loaded.DailyCompleted);
        Assert.IsNotNull(loaded.DailyBestScores);
        Assert.IsNotNull(loaded.IapThemesOwned);
        Assert.IsNotNull(loaded.DdaLast10Scores);
        Assert.IsNotNull(loaded.CurrentCeramic);
        Assert.IsNotNull(loaded.Settings);
        Assert.AreEqual(50, loaded.BestScore, "fields actually present in the JSON should still load correctly");
    }
}
