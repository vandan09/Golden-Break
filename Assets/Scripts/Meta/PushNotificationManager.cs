using System;
using UnityEngine;

/// <summary>
/// Daily-streak push notification (CLAUDE.md §4.1: "Push notification at
/// 7pm local if streak >=3 and app not opened today. Permission requested
/// day 3."). Not a MonoBehaviour singleton — same reasoning as
/// HapticManager/ReviewManager: not on BUILD_PLAN Part 1's permitted-
/// singleton list, no Unity lifecycle needed beyond a platform call.
/// Gating logic is pure and unit-testable; the actual permission
/// request/notification scheduling is compiled out entirely off-device.
///
/// "Permission requested day 3" is interpreted as the 3rd app *session*
/// (SaveData.TotalSessions, already tracked for other purposes) — not a
/// literal calendar day, since a player could open the app 3 times in
/// one day. This is the standard mobile UX pattern of asking after the
/// player has had a little time with the app, not on first launch.
/// Documented interpretation, not a literal spec reading (see
/// PROGRESS.md).
/// </summary>
public static class PushNotificationManager
{
    private const int PermissionRequestSessionNumber = 3;
    private const int MinStreakForReminder = 3;

    public static bool ShouldRequestPermission(SaveData saveData)
    {
        return saveData != null && !saveData.NotificationAsked && saveData.TotalSessions >= PermissionRequestSessionNumber;
    }

    // "App not opened today" == the streak's own last-played date isn't
    // today — StreakManager already tracks exactly this, no separate
    // "last opened" field needed.
    public static bool ShouldScheduleReminder(SaveData saveData, string todayIso)
    {
        return saveData != null
            && saveData.NotificationGranted
            && saveData.StreakCount >= MinStreakForReminder
            && saveData.StreakLastDate != todayIso;
    }

    public static void RequestPermission(SaveData saveData, Action<bool> onResult)
    {
        if (saveData == null)
        {
            onResult?.Invoke(false);
            return;
        }

        saveData.NotificationAsked = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        // TODO(notifications-setup): real Android 13+ POST_NOTIFICATIONS
        // runtime permission request via AndroidJavaObject goes here.
        Debug.Log("PushNotificationManager: would request POST_NOTIFICATIONS permission here.");
#endif
        // No real request made yet — conservative default until wired.
        saveData.NotificationGranted = false;
        onResult?.Invoke(saveData.NotificationGranted);
    }

    public static void ScheduleDailyReminderIfEligible(SaveData saveData, string todayIso)
    {
        if (!ShouldScheduleReminder(saveData, todayIso))
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // TODO(notifications-setup): schedule a local 7pm-local-time
        // notification ("keep your streak alive!") via AlarmManager/
        // WorkManager here. No real scheduling exists yet.
        Debug.Log("PushNotificationManager: would schedule a 7pm streak-reminder notification here.");
#else
        Debug.Log("PushNotificationManager: local notifications are Android-only; no-op on this platform.");
#endif
    }
}
