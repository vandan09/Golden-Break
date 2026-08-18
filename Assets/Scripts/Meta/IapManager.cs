using System;
using System.Collections.Generic;

/// <summary>
/// In-app purchase entitlements (CLAUDE.md §5.2). Plain C#, not a
/// MonoBehaviour singleton — not in CLAUDE.md §7.2's file tree at all
/// (unlike AdManager/AnalyticsManager, which are explicitly listed as
/// singletons) and not on BUILD_PLAN Part 1's permitted-singleton list
/// either, so this follows CoinManager/StreakManager's plain-class
/// pattern instead.
///
/// The actual store purchase flow (<c>showPurchaseFlow</c>) is a
/// delegate, not a direct Unity IAP / Play Billing call — no real store
/// product IDs exist yet (same placeholder category as AppLovin MAX's ad
/// unit IDs and GameAnalytics' game key; see PROGRESS.md), so this always
/// reports "unavailable" until a real SDK is wired in, matching
/// AdManager's own no-SDK failure behavior (§5.1: never block the
/// player, never grant a fake entitlement). The entitlement bookkeeping
/// itself (remove_ads flag, themes owned, coin bundle crediting) is
/// fully real and tested — ready to fire the moment a real purchase
/// succeeds.
/// </summary>
public enum IapItem
{
    RemoveAds,
    ThemePack,
    Coins500,
    Coins2000
}

public sealed class IapManager
{
    // Real theme catalog is Phase 5 art scope (same as gallery frame
    // art) — this single placeholder id stands in until real named theme
    // packs exist.
    private const string ThemePackPlaceholderId = "theme_pack_default";

    private readonly SaveData _saveData;
    private readonly CoinManager _coinManager;
    private readonly Action _requestSave;
    private readonly Action<string, Action, Action<string>> _showPurchaseFlow;

    public IapManager(SaveData saveData, CoinManager coinManager, Action requestSave, Action<string, Action, Action<string>> showPurchaseFlow)
    {
        _saveData = saveData ?? throw new ArgumentNullException(nameof(saveData));
        _coinManager = coinManager ?? throw new ArgumentNullException(nameof(coinManager));
        _requestSave = requestSave ?? throw new ArgumentNullException(nameof(requestSave));
        _showPurchaseFlow = showPurchaseFlow ?? throw new ArgumentNullException(nameof(showPurchaseFlow));
    }

    public bool IsAdsRemoved => _saveData.IapRemoveAds;
    public bool IsThemePackOwned => _saveData.IapThemesOwned.Contains(ThemePackPlaceholderId);

    public void Purchase(IapItem item, Action<bool> onResult)
    {
        string storeItemId = ResolveStoreItemId(item);
        _showPurchaseFlow(
            storeItemId,
            () =>
            {
                ApplyEntitlement(item);
                AnalyticsManager.Instance?.LogEvent("iap_purchased", new Dictionary<string, object>
                {
                    { "item_id", storeItemId },
                    { "price_usd", ResolvePriceUsd(item) }
                });
                onResult?.Invoke(true);
            },
            _ => onResult?.Invoke(false));
    }

    // TODO(iap-setup): a real restore flow queries the store for prior
    // purchases and re-applies each entitlement. No store SDK exists yet,
    // so this reports "unavailable" without touching any entitlement —
    // never silently grants, never silently revokes.
    public void RestorePurchases(Action<bool> onResult)
    {
        onResult?.Invoke(false);
    }

    private void ApplyEntitlement(IapItem item)
    {
        switch (item)
        {
            case IapItem.RemoveAds:
                _saveData.IapRemoveAds = true;
                break;

            case IapItem.ThemePack:
                if (!_saveData.IapThemesOwned.Contains(ThemePackPlaceholderId))
                {
                    _saveData.IapThemesOwned.Add(ThemePackPlaceholderId);
                }
                break;

            case IapItem.Coins500:
                _coinManager.Earn(500);
                break;

            case IapItem.Coins2000:
                _coinManager.Earn(2000);
                break;
        }

        // Coin-bundle purchases already trigger their own save via
        // CoinManager.OnBalanceChanged (wherever that's wired, e.g.
        // GameplaySaveTriggers) — this explicit call is still made
        // unconditionally, same reasoning as GameplaySaveTriggers.OnGameOver:
        // keeps "a purchase always saves" true by construction, not as an
        // incidental side effect of which item happened to be bought.
        _requestSave();
    }

    // CLAUDE.md §5.2's USD prices, for the iap_purchased analytics event's
    // price_usd field. Reference values only, not a real store-quoted
    // price — no store integration exists yet to fetch the actual localized
    // price at purchase time.
    private static double ResolvePriceUsd(IapItem item)
    {
        switch (item)
        {
            case IapItem.RemoveAds:
                return 2.99;
            case IapItem.ThemePack:
                return 2.99;
            case IapItem.Coins500:
                return 0.99;
            case IapItem.Coins2000:
                return 2.99;
            default:
                return 0d;
        }
    }

    private static string ResolveStoreItemId(IapItem item)
    {
        switch (item)
        {
            case IapItem.RemoveAds:
                return "com.goldenbreak.remove_ads";
            case IapItem.ThemePack:
                return "com.goldenbreak.theme_pack";
            case IapItem.Coins500:
                return "com.goldenbreak.coins_500";
            case IapItem.Coins2000:
                return "com.goldenbreak.coins_2000";
            default:
                throw new ArgumentOutOfRangeException(nameof(item), item, null);
        }
    }
}
