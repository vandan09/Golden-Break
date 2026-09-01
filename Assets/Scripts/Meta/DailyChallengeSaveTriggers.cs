using System;
using System.Collections.Generic;

/// <summary>
/// Save-trigger wiring for a Daily Challenge attempt (CLAUDE.md §4.2) —
/// the Daily Challenge equivalent of <see cref="GameplaySaveTriggers"/>,
/// deliberately its own separate class rather than a branch inside that
/// one. A Daily Challenge game-over only ever touches
/// daily_completed/daily_best_scores and the coin balance; it must never
/// touch best_score, total_games, DDA history, streak, or milestones —
/// those belong exclusively to regular play (confirmed with the player:
/// "shouldn't touch the regular game at all"). Keeping this as a
/// structurally separate class makes that guarantee obvious by
/// construction instead of relying on an if-branch to remember it.
/// </summary>
public sealed class DailyChallengeSaveTriggers
{
    private readonly PieceController _pieceController;
    private readonly CoinManager _coinManager;
    private readonly SaveData _saveData;
    private readonly Action _requestSave;
    private readonly Func<DateTime> _nowProvider;

    public DailyChallengeManager.CompletionResult? LastCompletionResult { get; private set; }

    public DailyChallengeSaveTriggers(PieceController pieceController, CoinManager coinManager, SaveData saveData, Action requestSave, Func<DateTime> nowProvider = null)
    {
        _pieceController = pieceController ?? throw new ArgumentNullException(nameof(pieceController));
        _coinManager = coinManager ?? throw new ArgumentNullException(nameof(coinManager));
        _saveData = saveData ?? throw new ArgumentNullException(nameof(saveData));
        _requestSave = requestSave ?? throw new ArgumentNullException(nameof(requestSave));
        _nowProvider = nowProvider ?? (() => DateTime.UtcNow);

        _pieceController.OnGameOver += OnGameOver;
    }

    /// <summary>
    /// Called when a fresh attempt begins, so the completion analytics fire
    /// once per attempt rather than once per game-over.
    ///
    /// Needed because an attempt can now reach game-over more than once: a
    /// rewarded "continue" resumes the same run, and OnGameOver then fires
    /// again when it finally ends. RecordCompletion itself is safely
    /// idempotent — coins award only on the first completion of the day and
    /// the best score takes the maximum, so a higher post-continue score
    /// still wins — but the analytics event is not, and double-counting it
    /// would inflate completions and skew every funnel built on them.
    /// </summary>
    public void BeginAttempt()
    {
        _loggedCompletionThisAttempt = false;
    }

    private bool _loggedCompletionThisAttempt;

    private void OnGameOver()
    {
        string todayIso = _nowProvider().ToString("yyyy-MM-dd");
        int score = _pieceController.Score.CurrentScore;

        DailyChallengeManager.CompletionResult result = DailyChallengeManager.RecordCompletion(
            _saveData.DailyCompleted, _saveData.DailyBestScores, todayIso, score);
        LastCompletionResult = result;

        if (result.CoinsAwarded > 0)
        {
            _coinManager.Earn(result.CoinsAwarded);
        }

        int ghostScore = DailyChallengeManager.ComputeGhostScore(_nowProvider());
        string rankVsGhosts = score >= ghostScore ? "above_ghost" : "below_ghost";

        if (!_loggedCompletionThisAttempt)
        {
            _loggedCompletionThisAttempt = true;
            AnalyticsManager.Instance?.LogEvent("daily_challenge_complete", new Dictionary<string, object>
            {
                { "score", score },
                { "rank_vs_ghosts", rankVsGhosts },
                { "is_first_completion_today", result.IsFirstCompletionToday },
                { "is_new_best_today", result.IsNewBestForToday }
            });
        }

        // Earn() above only requests a save when coins were actually
        // awarded (first completion of the day) — a replay that only
        // updates daily_best_scores still needs to persist, so this is
        // called unconditionally rather than relying on that side effect.
        _requestSave();
    }
}
