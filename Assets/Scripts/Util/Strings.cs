/// <summary>
/// Every player-facing string lives here — no hardcoded UI text in
/// controller/screen code (BUILD_PLAN Phase 4 task list). Dynamic text
/// uses composite-format templates (<c>{0}</c>, <c>{1}</c>...) consumed
/// via <c>string.Format</c>, since interpolated C# strings can't be
/// centralized as constants.
/// </summary>
public static class Strings
{
    // Home screen
    public const string AppTitle = "Golden Break";
    public const string HomePlayButton = "Play";
    public const string HomeDailyChallengeButton = "Daily Challenge";
    public const string HomeGalleryButton = "Gallery";
    public const string HomeSettingsButton = "Settings";
    public const string HomeStreakStartPrompt = "Start your streak today";
    public const string HomeStreakActiveFormat = "{0} day streak";
    public const string CeramicProgressFormat = "Ceramic progress: {0}/{1} cracks repaired";
    public const string HomeBestScoreFormat = "Best score · {0}";
    public const string HomeDailyStreakBadgeFormat = "{0}";
    public const string HomeCrackFractionFormat = "{0}/{1} cracks";
    public const string PushNotificationStreakReminderBody = "Keep your streak alive! Play today before it resets.";

    // Game-over screen
    public const string GameOverTitle = "GAME OVER";
    public const string GameOverBestScoreFormat = "Best {0}";
    public const string GameOverCeramicProgressFormat = "Ceramic: {0}/{1} cracks repaired";
    public const string GameOverMilestoneReachedFormat = "Milestone {0} reached! +{1} coins";
    public const string GameOverContinueButton = "Watch ad to continue";
    public const string GameOverDoubleCoinsButton = "Watch ad: double coins";
    public const string GameOverPlayAgainButton = "Play again";

    // Gameplay HUD
    public const string HudBestFormat = "BEST {0}";
    public const string HudCoinsFormat = "{0} coins";
    public const string NotEnoughCoinsFormat = "Need {0} coins";
    public const string HudUndoButtonFormat = "Undo ({0})";
    public const string HudRefreshButtonFormat = "Refresh ({0})";
    public const string HudStreakMultiplierFormat = "×{0} streak";
    public const string HudNewBest = "New best!";

    // Streak popup
    public const string StreakPopupFormat = "Day {0} streak! +{1} coins";
    public const string StreakPopupFrameUnlockedSuffix = "\nNew gallery frame unlocked!";

    // Gallery screen
    public const string GalleryTitle = "Gallery";
    public const string GalleryEmptyState = "No completed ceramics yet - repair your first one!";
    public const string GalleryCardTierFormat = "Tier {0} - {1}";
    public const string GalleryCardCompletedFormat = "Completed {0}";
    public const string GalleryCardScoreFormat = "{0} pts";
    public const string GalleryNextPiece = "next piece";

    // Settings screen
    public const string SettingsTitle = "Settings";
    public const string SettingsSoundLabel = "Sound";
    public const string SettingsMusicLabel = "Music";
    public const string SettingsHapticsLabel = "Haptics";
    public const string SettingsHighContrastLabel = "High contrast";
    public const string SettingsRemoveAdsLabel = "Remove ads";
    public const string SettingsAdsRemovedLabel = "Ads removed";
    public const string SettingsCrossPromoLabel = "More cozy puzzles";

    // Daily challenge screen
    public const string DailyChallengeTitle = "Daily Challenge";
    public const string DailyChallengeNotPlayedToday = "Not played today";
    public const string DailyChallengeBestTodayFormat = "Best today: {0}";
    public const string DailyChallengeGhostScoreFormat = "Beat: {0}";
    public const string DailyChallengePlayButton = "Play";
    public const string DailyChallengeGameOverTitle = "DAILY CHALLENGE COMPLETE";
    public const string DailyChallengeNewBestTodayLabel = "New best today!";
    public const string DailyChallengeCoinsEarnedFormat = "+{0} coins";
    public const string DailyChallengeCloseButton = "Close";
    public const string DailyChallengePerfectRunLabel = "Perfect run! Medallion filled";

    // Shared
    public const string CloseButtonSymbol = "X";
    public const string MenuButtonLabel = "Menu";
    public const string AdUnavailableToast = "Ad unavailable";
}
