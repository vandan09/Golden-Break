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
    // v2 grants Constants.StartingCoins to saves written before undo and
    // refresh were coin-gated (see SaveManager.Migrate).
    public const int CurrentSaveVersion = 2;

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

    // Not in CLAUDE.md §7.3's example JSON — a genuine gap discovered
    // only once Phase 3 needed it. §3.5 requires the gallery to show
    // "the total cumulative score earned across ALL games played while
    // working on that ceramic," which needs somewhere durable to
    // accumulate across game-overs (the same way current_ceramic itself
    // persists), not just live in memory for one session.
    [JsonProperty("ceramic_cumulative_score")]
    public int CeramicCumulativeScore;

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

    // Not in CLAUDE.md §7.3's example JSON — a genuine gap discovered
    // implementing §4.1's streak milestones ("day 7: gallery frame
    // unlock", "day 14: gallery frame", "day 21: rare gallery frame") and
    // §4.5's purchasable "Gallery frame (cosmetic border)" — nothing in
    // the example JSON records which frames a player has unlocked/bought.
    // Frame IDs are opaque strings (e.g. "streak_day7"); actual frame
    // rendering is Phase 5 art/polish scope, same as every other cosmetic
    // asset — this only tracks ownership.
    [JsonProperty("gallery_frames_owned")]
    public List<string> GalleryFramesOwned;

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

    // Not in CLAUDE.md §7.3's example JSON — needed to implement "Google
    // Play In-App Review API" (BUILD_PLAN Phase 4) without ever
    // re-prompting: Google's own guidelines require not asking again
    // once a review flow has been shown, regardless of whether the
    // player actually left a review (the OS itself throttles/hides the
    // dialog after a quota, so there is no real "did they review"
    // signal to check — only "did we ask").
    [JsonProperty("review_requested")]
    public bool ReviewRequested;

    public static SaveData CreateFresh(string todayIsoDate)
    {
        return new SaveData
        {
            SaveVersion = CurrentSaveVersion,
            BestScore = 0,
            TotalGames = 0,
            TotalLinesCleared = 0,
            Coins = Constants.StartingCoins,
            CurrentCeramic = CeramicProgressData.CreateFresh(),
            CeramicCumulativeScore = 0,
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
            GalleryFramesOwned = new List<string>(),
            Settings = SaveSettingsData.CreateDefault(),
            DdaAvgScore = 0f,
            DdaLast10Scores = new List<int>(),
            NotificationAsked = false,
            NotificationGranted = false,
            FirstLaunchDate = todayIsoDate,
            TotalSessions = 0,
            ReviewRequested = false
        };
    }
}
