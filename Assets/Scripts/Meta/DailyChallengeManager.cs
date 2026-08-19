using System;
using System.Collections.Generic;
using Vector2Int = UnityEngine.Vector2Int;

/// <summary>
/// Daily challenge seed (CLAUDE.md §4.2): one fixed piece sequence per
/// day, seeded deterministically from the date so every player sees the
/// same sequence regardless of device/OS/.NET runtime.
/// <c>string.GetHashCode()</c> is NOT stable across platforms — this
/// integer formula is the spec's own documented, deterministic
/// replacement. Verified cross-platform-stable by
/// <c>DailyChallengeManagerTests.ComputeSeed_KnownDate_MatchesSpecExample</c>
/// and <c>DailyChallengeSeedTest</c>.
/// </summary>
public static class DailyChallengeSeed
{
    public static int ComputeSeed(DateTime date)
    {
        int y = date.Year;
        int m = date.Month;
        int d = date.Day;
        return (y * 10000) + (m * 100) + d;
    }
}

/// <summary>
/// Daily challenge completion tracking and reward (CLAUDE.md §4.2). Plain
/// C#, static — no per-instance state of its own; the actual state
/// (daily_completed, daily_best_scores) already lives in SaveData.
/// </summary>
public static class DailyChallengeManager
{
    public const int CompletionRewardCoins = Constants.CoinsForDailyChallengeCompletion;

    // No DDA weighting: §4.2's "same piece sequence for all players"
    // would break the moment two players had different DDA histories —
    // the daily challenge is deliberately the one place DDA never applies.
    //
    // Draws only from the "large" DDA size tier (confirmed with the
    // player: Daily Challenge must be a genuinely harder, structurally
    // separate mode, not regular play with a badge on it) — every attempt
    // deals only the bigger, harder-to-place pieces, never the small ones
    // DDA would normally hand a struggling regular-play session.
    public static PieceSpawner CreateSpawner(PieceDefinition[] pool, DateTime date)
    {
        int seed = DailyChallengeSeed.ComputeSeed(date);
        return new PieceSpawner(GetHardPool(pool), new Random(seed));
    }

    public static PieceDefinition[] GetHardPool(PieceDefinition[] pool)
    {
        var large = new List<PieceDefinition>();
        foreach (PieceDefinition piece in pool)
        {
            if (DDAManager.GetSizeTier(piece.pieceId) == DDAManager.PieceSizeTier.Large)
            {
                large.Add(piece);
            }
        }

        return large.ToArray();
    }

    // Hard-mode mechanic 2 (confirmed with the player): a fixed set of
    // cells starts already filled before the player's first move, seeded
    // from the same date so every player faces the identical layout — the
    // ghost-score/leaderboard comparison (ComputeGhostScore) stays fair
    // only if every attempt starts from the same board, not a random one.
    public static Vector2Int[] GetObstacleCells(DateTime date)
    {
        int seed = DailyChallengeSeed.ComputeSeed(date);
        var random = new Random(seed);
        var cells = new HashSet<Vector2Int>();

        while (cells.Count < Constants.DailyChallengeObstacleCellCount)
        {
            int x = random.Next(0, Constants.GridSize);
            int y = random.Next(0, Constants.GridSize);
            cells.Add(new Vector2Int(x, y));
        }

        var result = new Vector2Int[cells.Count];
        cells.CopyTo(result);
        return result;
    }

    public static bool IsCompletedToday(List<string> dailyCompleted, string todayIso)
    {
        return dailyCompleted != null && dailyCompleted.Contains(todayIso);
    }

    public readonly struct CompletionResult
    {
        public readonly bool IsFirstCompletionToday;
        public readonly int CoinsAwarded;
        public readonly bool IsNewBestForToday;

        public CompletionResult(bool isFirstCompletionToday, int coinsAwarded, bool isNewBestForToday)
        {
            IsFirstCompletionToday = isFirstCompletionToday;
            CoinsAwarded = coinsAwarded;
            IsNewBestForToday = isNewBestForToday;
        }
    }

    // Records one completed daily-challenge attempt. Coins (§4.2: 30) are
    // only awarded on the FIRST completion of the day — replaying the
    // same day's sequence is explicitly allowed (BUILD_PLAN's own QA gate:
    // "play daily challenge twice in one day — second attempt shows same
    // piece sequence") and keeps the best score updated, but doesn't
    // re-pay the reward.
    public static CompletionResult RecordCompletion(
        List<string> dailyCompleted,
        Dictionary<string, int> dailyBestScores,
        string todayIso,
        int score)
    {
        bool isFirst = !dailyCompleted.Contains(todayIso);
        if (isFirst)
        {
            dailyCompleted.Add(todayIso);
        }

        bool isNewBest = !dailyBestScores.TryGetValue(todayIso, out int existingBest) || score > existingBest;
        if (isNewBest)
        {
            dailyBestScores[todayIso] = score;
        }

        int coinsAwarded = isFirst ? CompletionRewardCoins : 0;
        return new CompletionResult(isFirst, coinsAwarded, isNewBest);
    }

    // CLAUDE.md §4.2: "Weekly leaderboard against AI ghost scores (no
    // server needed for v1)." With no backend there's no real population
    // of other players' scores to compare against — this generates one
    // deterministic synthetic "ghost" score from the same date seed, so
    // every player on every device sees the identical target to beat
    // without needing a server. Documented interpretation, not a literal
    // reading — there's no real "AI" here, just a stable per-day number.
    public static int ComputeGhostScore(DateTime date)
    {
        int seed = DailyChallengeSeed.ComputeSeed(date);
        var random = new Random(seed);
        return random.Next(400, 1200);
    }
}
