using NUnit.Framework;

public class PushNotificationManagerTests
{
    [Test]
    public void ShouldRequestPermission_BeforeThirdSession_ReturnsFalse()
    {
        SaveData data = SaveData.CreateFresh("2026-08-18");
        data.TotalSessions = 2;

        Assert.IsFalse(PushNotificationManager.ShouldRequestPermission(data));
    }

    [Test]
    public void ShouldRequestPermission_OnThirdSession_ReturnsTrue()
    {
        SaveData data = SaveData.CreateFresh("2026-08-18");
        data.TotalSessions = 3;

        Assert.IsTrue(PushNotificationManager.ShouldRequestPermission(data));
    }

    [Test]
    public void ShouldRequestPermission_AlreadyAsked_ReturnsFalse()
    {
        SaveData data = SaveData.CreateFresh("2026-08-18");
        data.TotalSessions = 5;
        data.NotificationAsked = true;

        Assert.IsFalse(PushNotificationManager.ShouldRequestPermission(data));
    }

    [Test]
    public void RequestPermission_MarksAskedAndReportsConservativeDefault()
    {
        SaveData data = SaveData.CreateFresh("2026-08-18");
        bool? granted = null;

        PushNotificationManager.RequestPermission(data, g => granted = g);

        Assert.IsTrue(data.NotificationAsked);
        Assert.IsFalse(data.NotificationGranted, "no real permission dialog exists yet — conservative default");
        Assert.IsFalse(granted);
    }

    [Test]
    public void ShouldScheduleReminder_NotGranted_ReturnsFalse()
    {
        SaveData data = SaveData.CreateFresh("2026-08-18");
        data.StreakCount = 5;
        data.NotificationGranted = false;

        Assert.IsFalse(PushNotificationManager.ShouldScheduleReminder(data, "2026-08-18"));
    }

    [Test]
    public void ShouldScheduleReminder_StreakBelowThree_ReturnsFalse()
    {
        SaveData data = SaveData.CreateFresh("2026-08-18");
        data.NotificationGranted = true;
        data.StreakCount = 2;
        data.StreakLastDate = "2026-08-17";

        Assert.IsFalse(PushNotificationManager.ShouldScheduleReminder(data, "2026-08-18"));
    }

    [Test]
    public void ShouldScheduleReminder_AlreadyPlayedToday_ReturnsFalse()
    {
        SaveData data = SaveData.CreateFresh("2026-08-18");
        data.NotificationGranted = true;
        data.StreakCount = 5;
        data.StreakLastDate = "2026-08-18";

        Assert.IsFalse(PushNotificationManager.ShouldScheduleReminder(data, "2026-08-18"));
    }

    [Test]
    public void ShouldScheduleReminder_EligibleConditions_ReturnsTrue()
    {
        SaveData data = SaveData.CreateFresh("2026-08-18");
        data.NotificationGranted = true;
        data.StreakCount = 3;
        data.StreakLastDate = "2026-08-17";

        Assert.IsTrue(PushNotificationManager.ShouldScheduleReminder(data, "2026-08-18"));
    }

    [Test]
    public void ScheduleDailyReminderIfEligible_NotEligible_DoesNotThrow()
    {
        SaveData data = SaveData.CreateFresh("2026-08-18");

        Assert.DoesNotThrow(() => PushNotificationManager.ScheduleDailyReminderIfEligible(data, "2026-08-18"));
    }

    [Test]
    public void ScheduleDailyReminderIfEligible_Eligible_DoesNotThrow()
    {
        SaveData data = SaveData.CreateFresh("2026-08-18");
        data.NotificationGranted = true;
        data.StreakCount = 4;
        data.StreakLastDate = "2026-08-17";

        Assert.DoesNotThrow(() => PushNotificationManager.ScheduleDailyReminderIfEligible(data, "2026-08-18"));
    }
}
