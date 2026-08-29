using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

/// <summary>
/// Wraps the Google Mobile Ads SDK behind a stable API so the rest of the
/// game never touches AdMob directly.
///
/// CLAUDE.md §5.1's failure rule holds throughout: when an ad is
/// unavailable for any reason, no reward is granted and the player is never
/// blocked. Every path below either reaches the SDK's real "ad was fully
/// watched" callback or fails cleanly to the caller's onFailure — there is
/// no path that grants a reward without an ad actually completing.
///
/// AppLovin MAX was the original plan (§5.4) but AppLovin will not activate
/// an account until the app is already published, so AdMob is what can be
/// integrated pre-launch. AdMob later becomes a mediated network inside MAX
/// if that migration happens, so none of this account setup is wasted.
/// </summary>
public sealed class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    public static event Action<AdPlacement> OnRewardedOffered;
    public static event Action<AdPlacement> OnRewardedWatched;
    public static event Action<AdPlacement, string> OnRewardedFailed;
    public static event Action OnInterstitialShown;
    public static event Action OnBannerShown;
    public static event Action OnBannerHidden;

    private bool _sdkInitialized;
    private bool _bannerVisible;

    private BannerView _bannerView;
    private InterstitialAd _interstitial;
    private RewardedAd _rewarded;
    private AdPlacement _rewardedPlacement;

    // CLAUDE.md §5.1: "Banner: Home screen and gallery screen only. Never
    // during gameplay." Exposed so a caller could assert the banner isn't
    // left showing behind an overlay it forgot to hide.
    public bool IsBannerVisible => _bannerVisible;

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

    private void OnDestroy()
    {
        DestroyBanner();
        _interstitial?.Destroy();
        _rewarded?.Destroy();
    }

    /// <summary>
    /// CLAUDE.md §5.4/§9.3's GDPR requirement, via Google's User Messaging
    /// Platform. Consent failing to resolve must not stop the game or the
    /// SDK: the callback always runs, and a failure simply leaves consent
    /// ungranted, which the SDK treats as non-personalised ads rather than
    /// no ads at all.
    /// </summary>
    public void RequestConsentIfRequired(Action onResolved)
    {
        var request = new ConsentRequestParameters();

        ConsentInformation.Update(request, updateError =>
        {
            if (updateError != null)
            {
                Debug.LogWarning($"AdManager: consent update failed — {updateError.Message}");
                ResolveConsent(false, onResolved);
                return;
            }

            ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
            {
                if (formError != null)
                {
                    Debug.LogWarning($"AdManager: consent form failed — {formError.Message}");
                    ResolveConsent(false, onResolved);
                    return;
                }

                ResolveConsent(ConsentInformation.CanRequestAds(), onResolved);
            });
        });
    }

    private void ResolveConsent(bool granted, Action onResolved)
    {
        HasResolvedConsent = true;
        ConsentGrantedForPersonalizedAds = granted;
        onResolved?.Invoke();
    }

    public void InitializeSdk()
    {
        if (!HasResolvedConsent)
        {
            Debug.LogWarning("AdManager: InitializeSdk called before consent was resolved — call RequestConsentIfRequired first.");
            return;
        }

        if (_sdkInitialized)
        {
            return;
        }

        // MobileAds.Initialize spawns a DontDestroyOnLoad executor, which
        // Unity rejects outside play mode. Initialising an ad SDK from an
        // editor context is never correct anyway, so this is a real guard
        // rather than a test accommodation — it also keeps EditMode tests
        // able to exercise the consent gate above.
        if (!Application.isPlaying)
        {
            return;
        }

        MobileAds.Initialize(status =>
        {
            _sdkInitialized = true;
            Debug.Log($"AdManager: SDK initialized (test ads: {AdUnitIds.UsingTestAds}).");

            // Preload so the first continue/interstitial is not a wait.
            LoadInterstitial();
        });
    }

    // ---- Rewarded -------------------------------------------------------

    public void ShowRewarded(AdPlacement placement, Action onReward, Action<string> onFailure)
    {
        OnRewardedOffered?.Invoke(placement);

        if (!_sdkInitialized)
        {
            FailRewarded(placement, "SDK not initialized", onFailure);
            return;
        }

        _rewardedPlacement = placement;

        // A RewardedAd is single-use, so each request loads its own. Loading
        // on demand rather than caching keeps the reward tied to the
        // placement that asked for it — caching one ad and reusing it across
        // placements is how the wrong reward gets granted.
        RewardedAd.Load(AdUnitIds.Rewarded(placement), BuildRequest(), (ad, loadError) =>
        {
            if (loadError != null || ad == null)
            {
                FailRewarded(placement, loadError?.GetMessage() ?? "no fill", onFailure);
                return;
            }

            _rewarded?.Destroy();
            _rewarded = ad;

            bool rewardGranted = false;

            ad.OnAdFullScreenContentFailed += error =>
            {
                if (!rewardGranted)
                {
                    FailRewarded(placement, error.GetMessage(), onFailure);
                }
            };

            // Only the SDK's own reward callback grants anything. Closing
            // the ad early lands here with rewardGranted still false, which
            // is exactly §5.1's "no reward granted, player never blocked".
            ad.Show(_ =>
            {
                rewardGranted = true;
                OnRewardedWatched?.Invoke(placement);
                onReward?.Invoke();
            });
        });
    }

    private void FailRewarded(AdPlacement placement, string reason, Action<string> onFailure)
    {
        Debug.Log($"AdManager: rewarded unavailable for {placement} — {reason}");
        OnRewardedFailed?.Invoke(placement, reason);
        onFailure?.Invoke(reason);
    }

    // ---- Interstitial ---------------------------------------------------

    private void LoadInterstitial()
    {
        if (!_sdkInitialized)
        {
            return;
        }

        InterstitialAd.Load(AdUnitIds.Interstitial, BuildRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.Log($"AdManager: interstitial failed to load — {error?.GetMessage() ?? "no fill"}");
                return;
            }

            _interstitial?.Destroy();
            _interstitial = ad;
        });
    }

    public void ShowInterstitial(Action onComplete)
    {
        // The caller continues regardless — an interstitial must never be
        // able to strand the player between games.
        if (!_sdkInitialized || _interstitial == null || !_interstitial.CanShowAd())
        {
            onComplete?.Invoke();
            LoadInterstitial();
            return;
        }

        try
        {
            InterstitialAd shown = _interstitial;
            _interstitial = null;

            shown.OnAdFullScreenContentClosed += () =>
            {
                shown.Destroy();
                LoadInterstitial();
                onComplete?.Invoke();
            };

            shown.OnAdFullScreenContentFailed += error =>
            {
                Debug.Log($"AdManager: interstitial failed to show — {error.GetMessage()}");
                shown.Destroy();
                LoadInterstitial();
                onComplete?.Invoke();
            };

            shown.Show();
            OnInterstitialShown?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"AdManager: interstitial threw — {e.Message}");
            onComplete?.Invoke();
        }
    }

    // ---- Banner ---------------------------------------------------------

    public void ShowBanner()
    {
        if (_bannerVisible)
        {
            return;
        }

        _bannerVisible = true;

        if (!_sdkInitialized)
        {
            return;
        }

        if (_bannerView == null)
        {
            _bannerView = new BannerView(
                AdUnitIds.Banner, AdSize.Banner, AdPosition.Bottom);
        }

        _bannerView.LoadAd(BuildRequest());
        _bannerView.Show();
        OnBannerShown?.Invoke();
    }

    public void HideBanner()
    {
        if (!_bannerVisible)
        {
            return;
        }

        _bannerVisible = false;

        if (!_sdkInitialized || _bannerView == null)
        {
            return;
        }

        _bannerView.Hide();
        OnBannerHidden?.Invoke();
    }

    private void DestroyBanner()
    {
        if (_bannerView == null)
        {
            return;
        }

        _bannerView.Destroy();
        _bannerView = null;
    }

    private static AdRequest BuildRequest()
    {
        return new AdRequest();
    }
}
