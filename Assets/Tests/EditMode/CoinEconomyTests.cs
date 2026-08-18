using NUnit.Framework;

public class CoinEconomyTests
{
    [Test]
    public void Constructor_SeedsInitialBalance()
    {
        var coins = new CoinManager(initialBalance: 245);

        Assert.AreEqual(245, coins.Balance);
    }

    [Test]
    public void Constructor_NegativeInitialBalance_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new CoinManager(initialBalance: -5));
    }

    [Test]
    public void Earn_PositiveAmount_IncreasesBalance()
    {
        var coins = new CoinManager(initialBalance: 10);

        coins.Earn(5);

        Assert.AreEqual(15, coins.Balance);
    }

    [Test]
    public void Earn_FiresOnBalanceChangedWithNewBalance()
    {
        var coins = new CoinManager(initialBalance: 0);
        int? reportedBalance = null;
        coins.OnBalanceChanged += b => reportedBalance = b;

        coins.Earn(25);

        Assert.AreEqual(25, reportedBalance);
    }

    [Test]
    public void Earn_ZeroAmount_DoesNotFireOnBalanceChanged()
    {
        var coins = new CoinManager(initialBalance: 10);
        bool fired = false;
        coins.OnBalanceChanged += _ => fired = true;

        coins.Earn(0);

        Assert.IsFalse(fired);
        Assert.AreEqual(10, coins.Balance);
    }

    [Test]
    public void Earn_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        var coins = new CoinManager(initialBalance: 10);

        Assert.Throws<System.ArgumentOutOfRangeException>(() => coins.Earn(-1));
    }

    [Test]
    public void TrySpend_SufficientBalance_DeductsAndReturnsTrue()
    {
        var coins = new CoinManager(initialBalance: 100);

        bool result = coins.TrySpend(50);

        Assert.IsTrue(result);
        Assert.AreEqual(50, coins.Balance);
    }

    [Test]
    public void TrySpend_ExactBalance_DeductsToZeroAndReturnsTrue()
    {
        var coins = new CoinManager(initialBalance: 50);

        bool result = coins.TrySpend(50);

        Assert.IsTrue(result);
        Assert.AreEqual(0, coins.Balance);
    }

    [Test]
    public void TrySpend_InsufficientBalance_ReturnsFalseAndLeavesBalanceUnchanged()
    {
        var coins = new CoinManager(initialBalance: 30);

        bool result = coins.TrySpend(50);

        Assert.IsFalse(result);
        Assert.AreEqual(30, coins.Balance, "balance must never go negative from a failed spend");
    }

    [Test]
    public void TrySpend_InsufficientBalance_DoesNotFireOnBalanceChanged()
    {
        var coins = new CoinManager(initialBalance: 10);
        bool fired = false;
        coins.OnBalanceChanged += _ => fired = true;

        coins.TrySpend(50);

        Assert.IsFalse(fired);
    }

    [Test]
    public void TrySpend_FiresOnBalanceChangedWithNewBalance()
    {
        var coins = new CoinManager(initialBalance: 100);
        int? reportedBalance = null;
        coins.OnBalanceChanged += b => reportedBalance = b;

        coins.TrySpend(30);

        Assert.AreEqual(70, reportedBalance);
    }

    [Test]
    public void TrySpend_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        var coins = new CoinManager(initialBalance: 10);

        Assert.Throws<System.ArgumentOutOfRangeException>(() => coins.TrySpend(-1));
    }

    [Test]
    public void CanAfford_AmountLessThanOrEqualToBalance_ReturnsTrue()
    {
        var coins = new CoinManager(initialBalance: 50);

        Assert.IsTrue(coins.CanAfford(50));
        Assert.IsTrue(coins.CanAfford(49));
    }

    [Test]
    public void CanAfford_AmountGreaterThanBalance_ReturnsFalse()
    {
        var coins = new CoinManager(initialBalance: 50);

        Assert.IsFalse(coins.CanAfford(51));
    }

    [Test]
    public void MultipleSpendsAndEarns_BalanceNeverGoesNegative()
    {
        var coins = new CoinManager(initialBalance: 60);

        Assert.IsTrue(coins.TrySpend(50)); // 10 left
        Assert.IsFalse(coins.TrySpend(50)); // insufficient, still 10
        coins.Earn(45); // 55
        Assert.IsTrue(coins.TrySpend(50)); // 5 left

        Assert.AreEqual(5, coins.Balance);
    }
}
