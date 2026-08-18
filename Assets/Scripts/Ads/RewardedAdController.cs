using System;

/// <summary>
/// Coordinates the 4 rewarded-ad placements (CLAUDE.md §5.1): continue,
/// double coins, free undo, free piece refresh. Plain C# — takes the
/// AdManager.ShowRewarded call as a delegate rather than depending on the
/// MonoBehaviour singleton directly, so it's fully unit-testable with a
/// fake that immediately invokes onReward or onFailure, matching
/// GameplaySaveTriggers' same reasoning for depending on delegates over
/// concrete Unity types.
///
/// Deliberately does no eligibility gating of its own beyond what
/// PieceController already exposes (CanContinue/CanUndo/CanRefresh) —
/// this class's only job is "watch an ad, then perform the already-gated
/// action," not re-deciding whether the action is allowed.
/// </summary>
public sealed class RewardedAdController
{
    private readonly Action<AdPlacement, Action, Action<string>> _showRewarded;
    private readonly PieceController _pieceController;
    private readonly CoinManager _coinManager;

    public RewardedAdController(Action<AdPlacement, Action, Action<string>> showRewarded, PieceController pieceController, CoinManager coinManager)
    {
        _showRewarded = showRewarded ?? throw new ArgumentNullException(nameof(showRewarded));
        _pieceController = pieceController ?? throw new ArgumentNullException(nameof(pieceController));
        _coinManager = coinManager ?? throw new ArgumentNullException(nameof(coinManager));
    }

    public void RequestContinue(Action<bool> onResult)
    {
        if (!_pieceController.CanContinue)
        {
            onResult?.Invoke(false);
            return;
        }

        _showRewarded(
            AdPlacement.ContinueAfterGameOver,
            () => onResult?.Invoke(_pieceController.TryContinue()),
            _ => onResult?.Invoke(false));
    }

    // CLAUDE.md §4.5: "Rewarded 'double coins' | 2x game-over amount" —
    // baseAmount is the caller's already-known game-over earning (the
    // game-over screen already displays this, and
    // GameplaySaveTriggers.LastGameOverCoinsAwarded is the authoritative
    // source), doubled by earning it a second time on top of what was
    // already credited.
    public void RequestDoubleCoins(int baseAmount, Action<bool> onResult)
    {
        if (baseAmount <= 0)
        {
            onResult?.Invoke(false);
            return;
        }

        _showRewarded(
            AdPlacement.DoubleCoins,
            () =>
            {
                _coinManager.Earn(baseAmount);
                onResult?.Invoke(true);
            },
            _ => onResult?.Invoke(false));
    }

    public void RequestFreeUndo(Action<bool> onResult)
    {
        if (!_pieceController.CanUndo)
        {
            onResult?.Invoke(false);
            return;
        }

        _showRewarded(
            AdPlacement.FreeUndo,
            () => onResult?.Invoke(_pieceController.TryUndo()),
            _ => onResult?.Invoke(false));
    }

    public void RequestFreeRefresh(Action<bool> onResult)
    {
        if (!_pieceController.CanRefresh)
        {
            onResult?.Invoke(false);
            return;
        }

        _showRewarded(
            AdPlacement.FreePieceRefresh,
            () => onResult?.Invoke(_pieceController.TryRefresh()),
            _ => onResult?.Invoke(false));
    }
}
