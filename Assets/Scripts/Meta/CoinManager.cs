using System;

/// <summary>
/// Coin balance, earning, and spending (CLAUDE.md §4.5 economy table).
/// Plain C# — not a MonoBehaviour singleton, same reasoning as
/// <see cref="ScoreManager"/>: not on BUILD_PLAN Part 1's permitted-singleton
/// list, and has genuine per-save state that a static class doesn't fit
/// either. Owned/instantiated by whatever composes a game session
/// (GameplayController), seeded from and written back to SaveData by the
/// caller — this class has no save-file knowledge of its own.
/// </summary>
public sealed class CoinManager
{
    public event Action<int> OnBalanceChanged;

    public int Balance { get; private set; }

    public CoinManager(int initialBalance)
    {
        if (initialBalance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialBalance), "CoinManager: initial balance cannot be negative.");
        }

        Balance = initialBalance;
    }

    public void Earn(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "CoinManager: cannot earn a negative amount.");
        }

        if (amount == 0)
        {
            return;
        }

        Balance += amount;
        OnBalanceChanged?.Invoke(Balance);
    }

    // Spending never drives the balance negative — insufficient funds is a
    // no-op that reports failure, not a partial/clamped spend. Callers
    // (undo/refresh) are expected to check this before offering the action,
    // but this is the actual enforcement point.
    public bool TrySpend(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "CoinManager: cannot spend a negative amount.");
        }

        if (amount > Balance)
        {
            return false;
        }

        Balance -= amount;
        OnBalanceChanged?.Invoke(Balance);
        return true;
    }

    public bool CanAfford(int amount)
    {
        return amount <= Balance;
    }
}
