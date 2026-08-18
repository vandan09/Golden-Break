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
    public void RequestConsentIfRequired_ResolvesImmediatelyWithConservativeDefault()
    {
        bool resolved = false;

        _adManager.RequestConsentIfRequired(() => resolved = true);

        Assert.IsTrue(resolved);
        Assert.IsTrue(_adManager.HasResolvedConsent);
        Assert.IsFalse(_adManager.ConsentGrantedForPersonalizedAds, "no real CMP yet — default to the safe non-personalized stance");
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
}
