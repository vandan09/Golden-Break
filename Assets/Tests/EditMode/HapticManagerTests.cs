using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class HapticManagerTests
{
    [TearDown]
    public void ResetEnabled()
    {
        HapticManager.Enabled = true;
    }

    [Test]
    public void GetPatternMilliseconds_SinglePulsePatterns_ReturnExpectedDurations()
    {
        Assert.AreEqual(new long[] { 5 }, HapticManager.GetPatternMilliseconds(HapticPattern.Pickup));
        Assert.AreEqual(new long[] { 15 }, HapticManager.GetPatternMilliseconds(HapticPattern.Place));
        Assert.AreEqual(new long[] { 25 }, HapticManager.GetPatternMilliseconds(HapticPattern.Clear));
        Assert.AreEqual(new long[] { 40 }, HapticManager.GetPatternMilliseconds(HapticPattern.Combo));
        Assert.AreEqual(new long[] { 20 }, HapticManager.GetPatternMilliseconds(HapticPattern.GoldFlow));
        Assert.AreEqual(new long[] { 50 }, HapticManager.GetPatternMilliseconds(HapticPattern.CeramicComplete));
    }

    [Test]
    public void GetPatternMilliseconds_GameOver_ReturnsDoublePulse()
    {
        long[] pattern = HapticManager.GetPatternMilliseconds(HapticPattern.GameOver);

        Assert.AreEqual(3, pattern.Length);
        Assert.AreEqual(30, pattern[0]);
        Assert.AreEqual(30, pattern[2]);
    }

    [Test]
    public void GetPatternMilliseconds_UnhandledValue_LogsWarningAndReturnsEmpty()
    {
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("HapticManager: unhandled pattern.*"));

        long[] pattern = HapticManager.GetPatternMilliseconds((HapticPattern)999);

        Assert.AreEqual(0, pattern.Length);
    }

    [Test]
    public void Trigger_WhenDisabled_DoesNotThrow()
    {
        HapticManager.Enabled = false;

        Assert.DoesNotThrow(() => HapticManager.Trigger(HapticPattern.Place));
    }

    [Test]
    public void Trigger_WhenEnabled_DoesNotThrow()
    {
        HapticManager.Enabled = true;

        Assert.DoesNotThrow(() => HapticManager.Trigger(HapticPattern.GameOver));
    }
}
