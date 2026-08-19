using System;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
using Unity.Notifications.Android;
#endif

/// <summary>
/// Daily-streak push notification (CLAUDE.md §4.1: "Push notification at
/// 7pm local if streak >=3 and app not opened today. Permission requested
/// day 3."). Not a MonoBehaviour singleton — same reasoning as
/// HapticManager/ReviewManager: not on BUILD_PLAN Part 1's permitted-
/// singleton list, no Unity lifecycle needed beyond a platform call.
/// Gating logic is pure and unit-testable; the real permission request
/// (Unity's own <see cref="Permission"/> API) and scheduling (Unity's
/// official `com.unity.mobile.notifications` package) both compile out
/// entirely off-device, same as before — only the `#if` bodies changed
/// from TODO stubs to real calls, so every existing EditMode test still
/// exercises the exact same conservative-default/no-op behaviour it did
/// before this was wired up for real.
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

#if UNITY_ANDROID && !UNITY_EDITOR
    private const string PostNotificationsPermission = "android.permission.POST_NOTIFICATIONS";
    private const string ReminderChannelId = "daily_streak_reminder";
    private const int ReminderHourLocal = 19; // CLAUDE.md §4.1: "7pm local"
#endif

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
        // Below Android 13, posting a notification never required a
        // runtime permission at all — HasUserAuthorizedPermission reports
        // true unconditionally on those OS versions, so this one check
        // covers "already on/not needed" and "already granted" the same
        // way without a separate API-level branch.
        if (Permission.HasUserAuthorizedPermission(PostNotificationsPermission))
        {
            saveData.NotificationGranted = true;
            onResult?.Invoke(true);
            return;
        }

        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += _ =>
        {
            saveData.NotificationGranted = true;
            onResult?.Invoke(true);
        };
        callbacks.PermissionDenied += _ =>
        {
            saveData.NotificationGranted = false;
            onResult?.Invoke(false);
        };
        callbacks.PermissionDeniedAndDontAskAgain += _ =>
        {
            saveData.NotificationGranted = false;
            onResult?.Invoke(false);
        };
        Permission.RequestUserPermission(PostNotificationsPermission, callbacks);
#else
        // No real permission system off-device (Editor/other platforms) —
        // conservative default, same as before.
        saveData.NotificationGranted = false;
        onResult?.Invoke(saveData.NotificationGranted);
#endif
    }

    public static void ScheduleDailyReminderIfEligible(SaveData saveData, string todayIso)
    {
        if (!ShouldScheduleReminder(saveData, todayIso))
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        EnsureReminderChannelRegistered();

        // Re-run every eligible session (HomeScreen calls this on every
        // app open) — cancel-then-reschedule keeps exactly one pending
        // reminder outstanding rather than piling up a duplicate for
        // every session in between. This is the only local notification
        // this app ever schedules, so cancelling all of them is exactly
        // cancelling "ours."
        AndroidNotificationCenter.CancelAllScheduledNotifications();

        DateTime now = DateTime.Now;
        var sevenPmToday = new DateTime(now.Year, now.Month, now.Day, ReminderHourLocal, 0, 0);
        DateTime fireTime = now < sevenPmToday ? sevenPmToday : sevenPmToday.AddDays(1);

        var notification = new AndroidNotification
        {
            Title = Strings.AppTitle,
            Text = Strings.PushNotificationStreakReminderBody,
            FireTime = fireTime,
        };

        AndroidNotificationCenter.SendNotification(notification, ReminderChannelId);
#else
        Debug.Log("PushNotificationManager: local notifications are Android-only; no-op on this platform.");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void EnsureReminderChannelRegistered()
    {
        var channel = new AndroidNotificationChannel
        {
            Id = ReminderChannelId,
            Name = "Daily streak reminders",
            Importance = Importance.Default,
            Description = "Reminds you to keep your Golden Break streak alive.",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
    }
#endif
}
