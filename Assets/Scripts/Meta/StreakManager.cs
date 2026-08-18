using System;

/// <summary>
/// Daily streak tracking and reward calculation (CLAUDE.md §4.1). Plain
/// C# — mirrors CoinManager/ScoreManager's pattern: genuine per-save
/// state that doesn't fit a static class, and not on BUILD_PLAN Part 1's
/// permitted-singleton list.
///
/// §4.1's reward table only names days 1-6, 7, 14, and 21 explicitly,
/// with "28+: cycle repeats, +25% coins" implying the pattern continues
/// forever — not a literal spec, an interpretation (documented in
/// PROGRESS.md). Modeled as a 27-day cycle: three 7-day "weeks" where
/// days 1-6/8-13/15-20 ramp 10/20/30/40/50/60 and days 7/14/21 are
/// escalating milestones (100/150/200 + a gallery-frame unlock), then
/// days 22-27 are a final un-milestoned 1-6 ramp before the cycle
/// repeats at day 28 — the only reading of the table with no gaps.
/// Every additional full cycle adds a flat +25% to every reward in it
/// (not compounding), so day 28 (cycle 1, day 1 of the cycle) pays 12
/// coins instead of 10.
/// </summary>
public sealed class StreakManager
{
    private const int CycleLengthDays = 27;
    private const int WeekLengthDays = 7;
    private const float PerCycleBonus = 0.25f;

    public int StreakCount { get; private set; }
    public string LastPlayedDateIso { get; private set; }

    public StreakManager(int initialStreakCount, string lastPlayedDateIso)
    {
        StreakCount = initialStreakCount;
        LastPlayedDateIso = lastPlayedDateIso;
    }

    public readonly struct StreakResult
    {
        public readonly int StreakCount;
        public readonly int CoinsAwarded;
        public readonly string GalleryFrameUnlocked;
        public readonly bool StreakAdvanced;

        public StreakResult(int streakCount, int coinsAwarded, string galleryFrameUnlocked, bool streakAdvanced)
        {
            StreakCount = streakCount;
            CoinsAwarded = coinsAwarded;
            GalleryFrameUnlocked = galleryFrameUnlocked;
            StreakAdvanced = streakAdvanced;
        }
    }

    // Call once per completed game. If this is the first completed game
    // today, advances the streak — continuing it if yesterday was the
    // last played day, resetting to 1 otherwise (a missed day breaks the
    // streak per §4.1) — and returns today's reward. A second call later
    // the same day is a safe no-op (StreakAdvanced false, no reward):
    // §4.1 is "at least 1 game per day," not a per-game bonus.
    public StreakResult RecordGameCompleted(DateTime todayUtc)
    {
        string todayIso = FormatDate(todayUtc);
        if (LastPlayedDateIso == todayIso)
        {
            return new StreakResult(StreakCount, 0, null, false);
        }

        bool isConsecutiveDay = LastPlayedDateIso != null && IsExactlyOneDayAfter(LastPlayedDateIso, todayIso);
        StreakCount = isConsecutiveDay ? StreakCount + 1 : 1;
        LastPlayedDateIso = todayIso;

        (int coins, string frame) = GetReward(StreakCount);
        return new StreakResult(StreakCount, coins, frame, true);
    }

    public static (int coins, string galleryFrameId) GetReward(int streakDay)
    {
        int zeroBased = streakDay - 1;
        int cycleNumber = zeroBased / CycleLengthDays;
        int dayInCycle = (zeroBased % CycleLengthDays) + 1; // 1..27
        int weekPosition = ((dayInCycle - 1) % WeekLengthDays) + 1; // 1..7

        int baseCoins;
        string frame = null;

        if (weekPosition < WeekLengthDays)
        {
            baseCoins = weekPosition * 10;
        }
        else if (dayInCycle == 7)
        {
            baseCoins = 100;
            frame = "streak_day7";
        }
        else if (dayInCycle == 14)
        {
            baseCoins = 150;
            frame = "streak_day14";
        }
        else
        {
            // Only dayInCycle == 21 remains: weekPosition == 7 within a
            // 27-day cycle occurs only at 7, 14, and 21 (28 would be
            // weekPosition 7 too, but that's cycle 1's day 1, not day 28
            // of cycle 0 — see the modulo above).
            baseCoins = 200;
            frame = "streak_day21_rare";
        }

        float multiplier = 1f + (PerCycleBonus * cycleNumber);
        int finalCoins = (int)Math.Round(baseCoins * multiplier, MidpointRounding.AwayFromZero);
        return (finalCoins, frame);
    }

    private static string FormatDate(DateTime date)
    {
        return date.ToString("yyyy-MM-dd");
    }

    private static bool IsExactlyOneDayAfter(string previousIso, string currentIso)
    {
        if (!DateTime.TryParse(previousIso, out DateTime previous) || !DateTime.TryParse(currentIso, out DateTime current))
        {
            return false;
        }

        return (current.Date - previous.Date).Days == 1;
    }
}
