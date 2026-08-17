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

    private bool _sdkInitialized;

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

    public void InitializeSdk()
    {
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
