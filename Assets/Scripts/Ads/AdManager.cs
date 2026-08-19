using System;
using UnityEngine;

/// <summary>
/// Wraps ad-mediation SDK calls behind a stable API so the rest of the game
/// never touches AppLovin MAX directly. No SDK is imported and no real ad
/// unit IDs exist yet (placeholder — see PROGRESS.md deviations); every
/// call below fails to "unavailable" until both are wired in, which is
/// exactly CLAUDE.md §5.1's required failure behavior (no reward granted,
/// player never blocked) — not a fake stand-in success path.
/// </summary>
public sealed class AdManager : MonoBehaviour
{
    // TODO(ads-setup): replace with real AppLovin MAX ad unit IDs once the account exists.
    private const string RewardedAdUnitIdPlaceholder = "TODO_REWARDED_AD_UNIT_ID";
    private const string InterstitialAdUnitIdPlaceholder = "TODO_INTERSTITIAL_AD_UNIT_ID";

    public static AdManager Instance { get; private set; }

    public static event Action<AdPlacement> OnRewardedOffered;
    public static event Action<AdPlacement> OnRewardedWatched;
    public static event Action<AdPlacement, string> OnRewardedFailed;
    public static event Action OnInterstitialShown;
    public static event Action OnBannerShown;
    public static event Action OnBannerHidden;

    private bool _sdkInitialized;
    private bool _bannerVisible;

    // CLAUDE.md §5.1: "Banner: Home screen and gallery screen only. Never
    // during gameplay." Exposed so a caller could assert the banner isn't
    // left showing behind an overlay it forgot to hide, though nothing
    // reads it yet.
    public bool IsBannerVisible => _bannerVisible;

    // CLAUDE.md §5.4/§9.3: "GDPR CMP via MAX" — AppLovin MAX's own Consent
    // Management Platform (MaxCmpService) handles the actual dialog once
    // the SDK is imported. Living here rather than a separate
    // ConsentManager class: BUILD_PLAN Part 1 permits exactly 5
    // singletons (GameManager/AudioManager/SaveManager/AdManager/
    // AnalyticsManager), and CMP is squarely an ad-SDK concern — the real
    // implementation will literally be a MAX SDK call from inside this
    // class, not a standalone system.
    public bool HasResolvedConsent { get; private set; }
    public bool ConsentGrantedForPersonalizedAds { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // TODO(ads-setup): replace with MaxCmpService.ShowCmpForExistingUser /
    // HasSupportedCmp once AppLovin MAX SDK is imported. Until then,
    // resolves immediately with the conservative default (no personalized
    // ads) so InitializeSdk has a stable gate to check without blocking
    // on real CMP integration.
    public void RequestConsentIfRequired(Action onResolved)
    {
        HasResolvedConsent = true;
        ConsentGrantedForPersonalizedAds = false;
        onResolved?.Invoke();
    }

    public void InitializeSdk()
    {
        if (!HasResolvedConsent)
        {
            Debug.LogWarning("AdManager: InitializeSdk called before consent was resolved — call RequestConsentIfRequired first.");
            return;
        }

        // TODO(ads-setup): call AppLovin MAX's MaxSdk.InitializeSdk() here
        // once the SDK plugin is imported and RewardedAdUnitIdPlaceholder /
        // InterstitialAdUnitIdPlaceholder are replaced with real IDs.
        _sdkInitialized = false;
        Debug.Log("AdManager: SDK not yet integrated — deferred until AppLovin MAX account and ad unit IDs exist.");
    }

    public void ShowRewarded(AdPlacement placement, Action onReward, Action<string> onFailure)
    {
        OnRewardedOffered?.Invoke(placement);

        if (!_sdkInitialized)
        {
            const string reason = "SDK not initialized (placeholder ad unit IDs)";
            OnRewardedFailed?.Invoke(placement, reason);
            onFailure?.Invoke(reason);
            return;
        }

        try
        {
            // TODO(ads-setup): real MaxSdk.ShowRewardedAd call + reward
            // callback wiring goes here. OnRewardedWatched/onReward should
            // only fire from the SDK's actual "ad fully watched" callback.
        }
        catch (Exception e)
        {
            Debug.LogError($"AdManager: rewarded ad threw for placement {placement} — {e.Message}");
            OnRewardedFailed?.Invoke(placement, e.Message);
            onFailure?.Invoke(e.Message);
        }
    }

    // CLAUDE.md §5.1: Home/Gallery show a banner; every other screen
    // (gameplay, Settings, Daily Challenge, Game Over) simply never calls
    // this. _bannerVisible guards against double-firing OnBannerShown
    // when Gallery.Show() -> Home.Show() both call this in the same
    // navigation step (Gallery.Hide() re-shows Home).
    public void ShowBanner()
    {
        if (_bannerVisible)
        {
            return;
        }

        _bannerVisible = true;

        if (!_sdkInitialized)
        {
            Debug.Log("AdManager: banner requested — SDK not yet integrated, no-op until AppLovin MAX account and ad unit IDs exist.");
            return;
        }

        // TODO(ads-setup): real MaxSdk.ShowBanner call goes here.
        OnBannerShown?.Invoke();
    }

    public void HideBanner()
    {
        if (!_bannerVisible)
        {
            return;
        }

        _bannerVisible = false;

        if (!_sdkInitialized)
        {
            return;
        }

        // TODO(ads-setup): real MaxSdk.HideBanner call goes here.
        OnBannerHidden?.Invoke();
    }

    public void ShowInterstitial(Action onComplete)
    {
        if (!_sdkInitialized)
        {
            onComplete?.Invoke();
            return;
        }

        try
        {
            // TODO(ads-setup): real MaxSdk.ShowInterstitial call goes here.
            OnInterstitialShown?.Invoke();
            onComplete?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"AdManager: interstitial threw — {e.Message}");
            onComplete?.Invoke();
        }
    }
}
