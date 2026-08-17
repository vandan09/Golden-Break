using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Full local save schema (CLAUDE.md §7.3). Field names carry the exact
/// snake_case JSON keys the spec defines, via <see cref="JsonPropertyAttribute"/>,
/// so the on-disk format matches the spec byte-for-byte regardless of C#
/// naming conventions.
/// </summary>
[System.Serializable]
public sealed class SaveData
{
    public const int CurrentSaveVersion = 1;

    [JsonProperty("save_version")]
    public int SaveVersion;

    [JsonProperty("best_score")]
    public int BestScore;

    [JsonProperty("total_games")]
    public int TotalGames;

    [JsonProperty("total_lines_cleared")]
    public int TotalLinesCleared;

    [JsonProperty("coins")]
    public int Coins;

    [JsonProperty("current_ceramic")]
    public CeramicProgressData CurrentCeramic;

    [JsonProperty("gallery")]
    public List<GalleryEntryData> Gallery;

    [JsonProperty("milestones_claimed")]
    public List<int> MilestonesClaimed;

    [JsonProperty("streak_count")]
    public int StreakCount;

    [JsonProperty("streak_last_date")]
    public string StreakLastDate;

    [JsonProperty("daily_completed")]
    public List<string> DailyCompleted;

    [JsonProperty("daily_best_scores")]
    public Dictionary<string, int> DailyBestScores;

    [JsonProperty("interstitial_counter")]
    public int InterstitialCounter;

    [JsonProperty("interstitial_today_count")]
    public int InterstitialTodayCount;

    [JsonProperty("interstitial_today_date")]
    public string InterstitialTodayDate;

    [JsonProperty("iap_remove_ads")]
    public bool IapRemoveAds;

    [JsonProperty("iap_themes_owned")]
    public List<string> IapThemesOwned;

    [JsonProperty("settings")]
    public SaveSettingsData Settings;

    [JsonProperty("dda_avg_score")]
    public float DdaAvgScore;

    [JsonProperty("dda_last_10_scores")]
    public List<int> DdaLast10Scores;

    [JsonProperty("notification_asked")]
    public bool NotificationAsked;

    [JsonProperty("notification_granted")]
    public bool NotificationGranted;

    [JsonProperty("first_launch_date")]
    public string FirstLaunchDate;

    [JsonProperty("total_sessions")]
    public int TotalSessions;

    public static SaveData CreateFresh(string todayIsoDate)
    {
        return new SaveData
        {
            SaveVersion = CurrentSaveVersion,
            BestScore = 0,
            TotalGames = 0,
            TotalLinesCleared = 0,
            Coins = 0,
            CurrentCeramic = CeramicProgressData.CreateFresh(),
            Gallery = new List<GalleryEntryData>(),
            MilestonesClaimed = new List<int>(),
            StreakCount = 0,
            StreakLastDate = null,
            DailyCompleted = new List<string>(),
            DailyBestScores = new Dictionary<string, int>(),
            InterstitialCounter = 0,
            InterstitialTodayCount = 0,
            InterstitialTodayDate = todayIsoDate,
            IapRemoveAds = false,
            IapThemesOwned = new List<string>(),
            Settings = SaveSettingsData.CreateDefault(),
            DdaAvgScore = 0f,
            DdaLast10Scores = new List<int>(),
            NotificationAsked = false,
            NotificationGranted = false,
            FirstLaunchDate = todayIsoDate,
            TotalSessions = 0
        };
    }
}
