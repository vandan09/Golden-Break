using NUnit.Framework;

public class IapManagerTests
{
    private SaveData _saveData;
    private CoinManager _coins;
    private int _saveRequestCount;
    private bool _purchaseSucceeds;
    private string _lastRequestedStoreItemId;

    [SetUp]
    public void CreateAll()
    {
        _saveData = SaveData.CreateFresh("2026-08-18");
        _coins = new CoinManager(initialBalance: 0);
        _saveRequestCount = 0;
        _purchaseSucceeds = true;
        _lastRequestedStoreItemId = null;
    }

    private IapManager CreateManager()
    {
        return new IapManager(_saveData, _coins, () => _saveRequestCount++, (storeItemId, onSuccess, onFailure) =>
        {
            _lastRequestedStoreItemId = storeItemId;
            if (_purchaseSucceeds)
            {
                onSuccess();
            }
            else
            {
                onFailure("simulated failure");
            }
        });
    }

    [Test]
    public void Constructor_NullArguments_ThrowArgumentNullException()
    {
        Assert.Throws<System.ArgumentNullException>(() => new IapManager(null, _coins, () => { }, (a, b, c) => { }));
        Assert.Throws<System.ArgumentNullException>(() => new IapManager(_saveData, null, () => { }, (a, b, c) => { }));
        Assert.Throws<System.ArgumentNullException>(() => new IapManager(_saveData, _coins, null, (a, b, c) => { }));
        Assert.Throws<System.ArgumentNullException>(() => new IapManager(_saveData, _coins, () => { }, null));
    }

    [Test]
    public void Purchase_RemoveAds_SetsFlagAndRequestsSave()
    {
        var iap = CreateManager();
        bool? result = null;

        iap.Purchase(IapItem.RemoveAds, r => result = r);

        Assert.IsTrue(result);
        Assert.IsTrue(_saveData.IapRemoveAds);
        Assert.IsTrue(iap.IsAdsRemoved);
        Assert.GreaterOrEqual(_saveRequestCount, 1);
    }

    [Test]
    public void Purchase_ThemePack_AddsToOwnedThemes()
    {
        var iap = CreateManager();

        iap.Purchase(IapItem.ThemePack, null);

        Assert.IsTrue(iap.IsThemePackOwned);
        Assert.AreEqual(1, _saveData.IapThemesOwned.Count);
    }

    [Test]
    public void Purchase_ThemePackTwice_DoesNotDuplicateEntry()
    {
        var iap = CreateManager();

        iap.Purchase(IapItem.ThemePack, null);
        iap.Purchase(IapItem.ThemePack, null);

        Assert.AreEqual(1, _saveData.IapThemesOwned.Count);
    }

    [Test]
    public void Purchase_Coins500_CreditsFiveHundredCoins()
    {
        var iap = CreateManager();

        iap.Purchase(IapItem.Coins500, null);

        Assert.AreEqual(500, _coins.Balance);
    }

    [Test]
    public void Purchase_Coins2000_CreditsTwoThousandCoins()
    {
        var iap = CreateManager();

        iap.Purchase(IapItem.Coins2000, null);

        Assert.AreEqual(2000, _coins.Balance);
    }

    [Test]
    public void Purchase_UsesDistinctStoreItemIdsPerItem()
    {
        var iap = CreateManager();

        iap.Purchase(IapItem.RemoveAds, null);
        string removeAdsId = _lastRequestedStoreItemId;

        iap.Purchase(IapItem.Coins500, null);
        string coins500Id = _lastRequestedStoreItemId;

        Assert.AreNotEqual(removeAdsId, coins500Id);
    }

    [Test]
    public void Purchase_StoreFlowFails_DoesNotApplyEntitlement()
    {
        _purchaseSucceeds = false;
        var iap = CreateManager();
        bool? result = null;

        iap.Purchase(IapItem.RemoveAds, r => result = r);

        Assert.IsFalse(result);
        Assert.IsFalse(_saveData.IapRemoveAds, "a failed purchase must never grant the entitlement");
    }

    [Test]
    public void Purchase_StoreFlowFails_DoesNotCreditCoins()
    {
        _purchaseSucceeds = false;
        var iap = CreateManager();

        iap.Purchase(IapItem.Coins2000, null);

        Assert.AreEqual(0, _coins.Balance);
    }

    [Test]
    public void RestorePurchases_NoStoreIntegrationYet_ReportsUnavailableWithoutThrowing()
    {
        var iap = CreateManager();
        bool? result = null;

        Assert.DoesNotThrow(() => iap.RestorePurchases(r => result = r));
        Assert.IsFalse(result);
    }

    [Test]
    public void IsAdsRemoved_BeforeAnyPurchase_IsFalse()
    {
        var iap = CreateManager();

        Assert.IsFalse(iap.IsAdsRemoved);
        Assert.IsFalse(iap.IsThemePackOwned);
    }
}
