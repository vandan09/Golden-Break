using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class AnalyticsManagerTests
{
    private AnalyticsManager _analyticsManager;

    [SetUp]
    public void CreateAnalyticsManager()
    {
        var go = new GameObject("AnalyticsManagerTestInstance");
        _analyticsManager = go.AddComponent<AnalyticsManager>();
    }

    [TearDown]
    public void DestroyAnalyticsManager()
    {
        if (_analyticsManager != null)
        {
            Object.DestroyImmediate(_analyticsManager.gameObject);
        }
    }

    [Test]
    public void LogEvent_SdkNotInitialized_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _analyticsManager.LogEvent("game_start"));
    }

    [Test]
    public void LogError_SdkNotInitialized_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _analyticsManager.LogError("save_corruption", "bad json"));
    }

    [Test]
    public void SetCustomDimensions_SdkNotInitialized_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _analyticsManager.SetCustomDimensions("US", "Normal", 12));
    }

    [Test]
    public void FormatParameters_NullOrEmpty_ReturnsEmptyString()
    {
        Assert.AreEqual(string.Empty, AnalyticsManager.FormatParameters(null));
        Assert.AreEqual(string.Empty, AnalyticsManager.FormatParameters(new Dictionary<string, object>()));
    }

    [Test]
    public void FormatParameters_WithEntries_IncludesEachKeyAndValue()
    {
        var parameters = new Dictionary<string, object>
        {
            { "score", 4280 },
            { "ceramic_tier", 5 }
        };

        string formatted = AnalyticsManager.FormatParameters(parameters);

        StringAssert.Contains("score=4280", formatted);
        StringAssert.Contains("ceramic_tier=5", formatted);
    }
}
