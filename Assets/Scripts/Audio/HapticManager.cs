using System;
using UnityEngine;

/// <summary>
/// Not a MonoBehaviour singleton — BUILD_PLAN Part 1's permitted-singleton
/// list is GameManager/AudioManager/SaveManager/AdManager/AnalyticsManager
/// only, and haptics need no Unity lifecycle beyond a static settings flag,
/// so a static class is the correct fit rather than stretching that list.
/// Pattern selection (<see cref="GetPatternMilliseconds"/>) is a pure
/// function so it's unit-testable without any Android/JNI dependency; the
/// actual platform call is compiled out entirely off-device.
/// </summary>
public static class HapticManager
{
    public static bool Enabled { get; set; } = true;

    public static void Trigger(HapticPattern pattern)
    {
        if (!Enabled)
        {
            return;
        }

        Vibrate(GetPatternMilliseconds(pattern));
    }

    internal static long[] GetPatternMilliseconds(HapticPattern pattern)
    {
        switch (pattern)
        {
            case HapticPattern.Pickup:
                return new long[] { 5 };
            case HapticPattern.Place:
                return new long[] { 15 };
            case HapticPattern.Clear:
                return new long[] { 25 };
            case HapticPattern.Combo:
                return new long[] { 40 };
            case HapticPattern.GoldFlow:
                return new long[] { 20 };
            case HapticPattern.CeramicComplete:
                return new long[] { 50 };
            case HapticPattern.GameOver:
                // CLAUDE.md §8.3 specifies "30ms double-pulse" but not the
                // gap between pulses — 60ms chosen as clearly distinct
                // without feeling sluggish; revisit on-device in Phase 5.
                return new long[] { 30, 60, 30 };
            default:
                Debug.LogWarning($"HapticManager: unhandled pattern {pattern}, no vibration triggered.");
                return Array.Empty<long>();
        }
    }

    private static void Vibrate(long[] pattern)
    {
        if (pattern.Length == 0)
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                if (pattern.Length == 1)
                {
                    vibrator.Call("vibrate", pattern[0]);
                }
                else
                {
                    long[] fullPattern = new long[pattern.Length + 1];
                    fullPattern[0] = 0;
                    Array.Copy(pattern, 0, fullPattern, 1, pattern.Length);
                    vibrator.Call("vibrate", fullPattern, -1);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"HapticManager: vibration failed — {e.Message}");
        }
#endif
    }
}
