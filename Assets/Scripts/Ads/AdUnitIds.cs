using UnityEngine;

/// <summary>
/// The AdMob app and ad unit IDs for Golden Break.
///
/// Development builds resolve to Google's public TEST ad units instead of
/// the real ones. This is not a nicety: repeatedly loading and tapping your
/// own live ads is the single most common way to get an AdMob account
/// suspended for invalid traffic, and every build we install for testing
/// would otherwise do exactly that. Live IDs are used only in a release
/// build, which is what ships.
///
/// Ad unit IDs are not secrets — they are embedded in the shipped APK and
/// visible to anyone who unpacks it — so keeping them in source is correct.
/// </summary>
public static class AdUnitIds
{
    // AdMob app ID (note the '~' rather than '/'). Also has to be declared
    // in GoogleMobileAdsSettings, which is what writes it into the Android
    // manifest; the SDK crashes on startup if the manifest entry is absent.
    public const string AppId = "ca-app-pub-2093747168100414~3070242774";

    private const string LiveBanner = "ca-app-pub-2093747168100414/4072333254";
    private const string LiveInterstitial = "ca-app-pub-2093747168100414/1757161106";
    private const string LiveRewardedContinue = "ca-app-pub-2093747168100414/7652485615";
    private const string LiveRewardedDoubleCoins = "ca-app-pub-2093747168100414/1087077265";
    private const string LiveRewardedUndo = "ca-app-pub-2093747168100414/2711150128";
    private const string LiveRewardedRefresh = "ca-app-pub-2093747168100414/5293081430";

    // Google's documented test units — safe to load and click any number of
    // times, and they always fill, which also makes them the only reliable
    // way to exercise the reward path before the app is live.
    private const string TestBanner = "ca-app-pub-3940256099942544/6300978111";
    private const string TestInterstitial = "ca-app-pub-3940256099942544/1033173712";
    private const string TestRewarded = "ca-app-pub-3940256099942544/5224354917";

    /// <summary>
    /// Test ads stay on until the app is actually published.
    ///
    /// This used to be <c>Debug.isDebugBuild</c>, which was wrong in the way
    /// that matters: the APKs built for on-device testing are release
    /// builds, so they asked for the LIVE ad units — and live units do not
    /// fill before the app is on the Play Store, which showed up as "no ad
    /// available" on every placement. Tying it to the build type meant the
    /// one configuration we actually test never exercised ads at all.
    ///
    /// Flip this to false as part of preparing the store release, together
    /// with stripping the gallery's dev catalogue.
    /// </summary>
    private const bool ForceTestAds = true;

    public static bool UsingTestAds => ForceTestAds || Debug.isDebugBuild;

    public static string Banner => UsingTestAds ? TestBanner : LiveBanner;

    public static string Interstitial => UsingTestAds ? TestInterstitial : LiveInterstitial;

    /// <summary>
    /// Each rewarded placement has its own live unit so AdMob reports
    /// revenue per placement — CLAUDE.md §5.1 predicts very different opt-in
    /// rates across the four (55-70% for continue, 25-40% for refresh), and
    /// one shared unit would report them as a single indistinguishable
    /// number. Test builds share one unit because the test units are
    /// interchangeable.
    /// </summary>
    public static string Rewarded(AdPlacement placement)
    {
        if (UsingTestAds)
        {
            return TestRewarded;
        }

        switch (placement)
        {
            case AdPlacement.ContinueAfterGameOver: return LiveRewardedContinue;
            case AdPlacement.DoubleCoins: return LiveRewardedDoubleCoins;
            case AdPlacement.FreeUndo: return LiveRewardedUndo;
            case AdPlacement.FreePieceRefresh: return LiveRewardedRefresh;
            default: return LiveRewardedContinue;
        }
    }
}
