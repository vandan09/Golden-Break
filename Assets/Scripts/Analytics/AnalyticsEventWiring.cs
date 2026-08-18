using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fires the CLAUDE.md §6.2 analytics events from the game/retention
/// events that already exist, rather than scattering LogEvent calls
/// across every controller. Plain C#, takes the actual logging call as a
/// delegate (defaulting to the real AnalyticsManager singleton) for the
/// same testability reason as GameplaySaveTriggers/RewardedAdController.
///
/// Not every §6.2 field is populated with full precision — this is an
/// offline-first project with no real backend receiving this telemetry
/// yet (AnalyticsManager itself only Debug.Logs until a real
/// GameAnalytics key exists), so a few fields are honest, documented
/// approximations rather than new tracking infrastructure built purely
/// for an event nothing consumes yet:
///   - line_clear's streak_length is derived from ScoreManager's own
///     multiplier (multiplier = 1 + 0.5*(length-1), capped at 3 for
///     length >= 5) — ScoreManager doesn't separately track a raw streak
///     count, only the multiplier it produces.
///   - game_over's pieces_placed/max_combo/seconds are tracked locally
///     here per game (reset on OnGameStarted), using Time.realtimeSinceStartup
///     for elapsed time.
///   - ceramic_complete's games_to_complete isn't tracked anywhere in the
///     save schema and is omitted (logged as 0) rather than guessed.
/// continue_used/undo_used/refresh_used/iap_purchased are logged directly
/// at their own call sites (GameOverScreen, GameplayHUD, IapManager) where
/// the contextual data (score at the moment, coin-vs-ad source) is
/// naturally at hand, not routed through here.
/// </summary>
public sealed class AnalyticsEventWiring
{
    private readonly PieceController _pieceController;
    private readonly GameplaySaveTriggers _saveTriggers;
    private readonly SaveData _saveData;
    private readonly CeramicManager _ceramicManager;
    private readonly Action<string, IReadOnlyDictionary<string, object>> _logEvent;

    private int _piecesPlacedThisGame;
    private int _maxComboThisGame;
    private float _gameStartRealtime;
    private int _previousBestScoreShadow;

    public AnalyticsEventWiring(
        PieceController pieceController,
        GameplaySaveTriggers saveTriggers,
        SaveData saveData,
        CeramicManager ceramicManager,
        Action<string, IReadOnlyDictionary<string, object>> logEvent = null)
    {
        _pieceController = pieceController ?? throw new ArgumentNullException(nameof(pieceController));
        _saveTriggers = saveTriggers ?? throw new ArgumentNullException(nameof(saveTriggers));
        _saveData = saveData ?? throw new ArgumentNullException(nameof(saveData));
        _ceramicManager = ceramicManager;
        _logEvent = logEvent ?? ((name, parameters) => AnalyticsManager.Instance?.LogEvent(name, parameters));

        _previousBestScoreShadow = _pieceController.Score.BestScore;

        _pieceController.OnGameStarted += OnGameStarted;
        _pieceController.OnGameOver += OnGameOver;
        _pieceController.OnLinesCleared += OnLinesCleared;
        _pieceController.Score.OnNewBest += OnNewBest;

        if (_ceramicManager != null)
        {
            _ceramicManager.OnCeramicCompleted += OnCeramicCompleted;
        }

        AdManager.OnRewardedOffered += placement => Log("rewarded_offered", ("placement", placement.ToString()));
        AdManager.OnRewardedWatched += placement => Log("rewarded_watched", ("placement", placement.ToString()));
        AdManager.OnRewardedFailed += (placement, error) => Log("rewarded_failed", ("placement", placement.ToString()), ("error", error));
        AdManager.OnInterstitialShown += () => Log("interstitial_shown", ("game_count", _saveData.InterstitialCounter), ("daily_count", _saveData.InterstitialTodayCount));
    }

    private void OnGameStarted()
    {
        _piecesPlacedThisGame = 0;
        _maxComboThisGame = 0;
        _gameStartRealtime = Time.realtimeSinceStartup;

        Log("game_start", ("session_number", _saveData.TotalGames + 1), ("ceramic_tier", _saveData.CurrentCeramic.Tier));
    }

    private void OnLinesCleared(LineClearDetector.ClearResult result, int pointsAwarded)
    {
        _piecesPlacedThisGame++;
        if (result.TotalLinesCleared > _maxComboThisGame)
        {
            _maxComboThisGame = result.TotalLinesCleared;
        }

        if (!result.AnyCleared)
        {
            return;
        }

        // ScoreManager.ApplyLineClear already advances StreakMultiplier to
        // the *next* clear's value before this event fires — reading the
        // live property here would log "what the next clear will use,"
        // not what this one actually did. pointsAwarded was computed
        // with the multiplier this clear actually used, so back it out
        // from that instead: multiplierUsed = pointsAwarded / basePoints.
        int basePoints = ScoreManager.CalculateBasePoints(result.TotalLinesCleared);
        float multiplierUsed = basePoints > 0 ? pointsAwarded / (float)basePoints : 1f;
        int approximateStreakLength = Mathf.RoundToInt((multiplierUsed - 1f) / Constants.StreakMultiplierStep) + 1;
        Log("line_clear",
            ("lines_in_move", result.TotalLinesCleared),
            ("combo_multiplier", multiplierUsed),
            ("streak_length", approximateStreakLength));
    }

    private void OnGameOver()
    {
        int seconds = Mathf.RoundToInt(Time.realtimeSinceStartup - _gameStartRealtime);
        Log("game_over",
            ("score", _pieceController.Score.CurrentScore),
            ("lines_cleared", _saveData.TotalLinesCleared),
            ("pieces_placed", _piecesPlacedThisGame),
            ("max_combo", _maxComboThisGame),
            ("ceramics_completed", _saveData.Gallery.Count),
            ("seconds", seconds));

        if (_saveTriggers.LastStreakResult.HasValue)
        {
            Log("daily_streak", ("streak_count", _saveTriggers.LastStreakResult.Value.StreakCount));
        }

        if (_saveTriggers.LastDailyChallengeCompletionResult.HasValue)
        {
            int score = _pieceController.Score.CurrentScore;
            int ghostScore = DailyChallengeManager.ComputeGhostScore(DateTime.UtcNow);
            string rankVsGhosts = score >= ghostScore ? "above_ghost" : "below_ghost";
            Log("daily_challenge_complete", ("score", score), ("rank_vs_ghosts", rankVsGhosts));
        }

        foreach (MilestoneManager.MilestoneResult milestone in _saveTriggers.LastMilestoneResults)
        {
            Log("milestone_reached", ("milestone_score", milestone.MilestoneScore));
        }
    }

    private void OnNewBest()
    {
        int newBest = _pieceController.Score.BestScore;
        Log("personal_best", ("new_best_score", newBest), ("previous_best", _previousBestScoreShadow));
        _previousBestScoreShadow = newBest;
    }

    private void OnCeramicCompleted()
    {
        // games_to_complete isn't tracked in the save schema — see class
        // doc comment. Logged as 0 rather than a guessed value.
        Log("ceramic_complete", ("ceramic_tier", _ceramicManager.Progress.Tier), ("games_to_complete", 0));
    }

    private void Log(string eventName, params (string key, object value)[] parameters)
    {
        var dict = new Dictionary<string, object>();
        foreach ((string key, object value) in parameters)
        {
            dict[key] = value;
        }

        _logEvent(eventName, dict);
    }
}
