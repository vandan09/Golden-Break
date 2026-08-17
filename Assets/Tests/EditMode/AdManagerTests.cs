using NUnit.Framework;
using UnityEngine;

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
}
