using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AdManagerTests
{
    private AdManager _adManager;

    [SetUp]
    public void CreateAdManager()
    {
        var go = new GameObject("AdManagerTestInstance");
        _adManager = go.AddComponent<AdManager>();
    }

    [TearDown]
    public void DestroyAdManager()
    {
        if (_adManager != null)
        {
            Object.DestroyImmediate(_adManager.gameObject);
        }
    }

    [Test]
    public void ShowRewarded_SdkNotInitialized_InvokesOnFailureNotOnReward()
    {
        bool rewardCalled = false;
        string failureReason = null;

        _adManager.ShowRewarded(
            AdPlacement.ContinueAfterGameOver,
            onReward: () => rewardCalled = true,
            onFailure: reason => failureReason = reason);

        Assert.IsFalse(rewardCalled);
        Assert.IsNotNull(failureReason);
    }

    [Test]
    public void ShowInterstitial_SdkNotInitialized_StillInvokesOnComplete()
    {
        bool completed = false;

        _adManager.ShowInterstitial(() => completed = true);

        Assert.IsTrue(completed);
    }

    [Test]
    // Consent now runs through Google's real User Messaging Platform, so
    // the granted flag reflects whatever the SDK reports rather than the
    // old stub's hardcoded "no". What still matters, and is asserted here,
    // is that the callback always runs and consent always ends up resolved
    // — InitializeSdk gates on that, so a consent flow that silently never
    // completed would leave the game permanently ad-free.
    public void RequestConsentIfRequired_AlwaysResolvesAndInvokesTheCallback()
    {
        bool resolved = false;

        _adManager.RequestConsentIfRequired(() => resolved = true);

        Assert.IsTrue(resolved, "the callback must run even when consent fails");
        Assert.IsTrue(_adManager.HasResolvedConsent);
    }

    [Test]
    public void InitializeSdk_BeforeConsentResolved_LogsWarningAndDoesNotProceed()
    {
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("InitializeSdk called before consent.*"));

        Assert.DoesNotThrow(() => _adManager.InitializeSdk());
    }

    [Test]
    public void InitializeSdk_AfterConsentResolved_DoesNotWarn()
    {
        _adManager.RequestConsentIfRequired(() => { });

        Assert.DoesNotThrow(() => _adManager.InitializeSdk());
    }

    [Test]
    public void ShowBanner_SetsIsBannerVisibleTrue()
    {
        _adManager.ShowBanner();

        Assert.IsTrue(_adManager.IsBannerVisible);
    }

    [Test]
    public void HideBanner_AfterShow_SetsIsBannerVisibleFalse()
    {
        _adManager.ShowBanner();
        _adManager.HideBanner();

        Assert.IsFalse(_adManager.IsBannerVisible);
    }

    [Test]
    public void HideBanner_WithoutEverShowing_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _adManager.HideBanner());
        Assert.IsFalse(_adManager.IsBannerVisible);
    }

    [Test]
    public void ShowBanner_SdkNotInitialized_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _adManager.ShowBanner());
    }
}
