using System;
using System.Collections.Generic;

/// <summary>
/// Wires live gameplay events to <see cref="SaveData"/> mutation and
/// persistence (BUILD_PLAN Phase 4 "save triggers": game-over, ceramic
/// completion, coin change, settings change, OnApplicationPause).
/// Ceramic-completion and OnApplicationPause triggers already live in
/// <see cref="CeramicController"/> and <see cref="SaveManager"/>
/// respectively — this class owns the two that don't otherwise have a
/// natural home: game-over (best score, total games, lines cleared, DDA
/// history, game-over coin award, daily streak, score milestones) and any
/// coin balance change.
///
/// Depends on <see cref="SaveData"/> directly, not <see cref="SaveManager"/>
/// — SaveManager's <c>Current</c>/<c>Save()</c> only work once its own
/// Awake() has run (it lazily creates <c>_persistence</c> there), which
/// EditMode tests can't reliably trigger via AddComponent (see
/// PROGRESS.md; SaveManagerTests itself only ever exercises the static
/// LoadFrom for this exact reason). Taking the plain SaveData plus a
/// save-request delegate keeps this class fully unit-testable and matches
/// BUILD_PLAN Part 1's "plain C# for pure logic" rule — GameplayController
/// wires it to the real SaveManager with <c>saveManager.Current</c> and
/// <c>saveManager.Save</c>. The date is injected the same way (defaults to
/// real UtcNow at the production call site) so streak-day-boundary logic
/// is deterministically testable, unlike CeramicController's own inline
/// DateTime.UtcNow call — that class is never unit-tested directly
/// (an orchestrator, same as InputHandler), this one is.
///
/// Not explicitly unsubscribed on teardown, consistent with every other
/// event subscription already in this codebase (ScoreManager.OnNewBest in
/// PieceController, PieceController.OnLinesCleared in CeramicController) —
/// owned for the lifetime of one Gameplay scene instance, which tears down
/// as a whole.
/// </summary>
public sealed class GameplaySaveTriggers
{
    private readonly PieceController _pieceController;
    private readonly CoinManager _coinManager;
    private readonly SaveData _saveData;
    private readonly Action _requestSave;
    private readonly Func<DateTime> _nowProvider;

    // Exposed for the game-over screen (later UI slice) to show "you
    // earned X coins" and offer "double coins" against exactly that base
    // amount — CLAUDE.md §4.5's "Rewarded double coins | 2x game-over
    // amount" means the base game-over earning specifically, not streak/
    // milestone bonuses from the same game-over (those are separate
    // reward moments with their own UI beat).
    public int LastGameOverCoinsAwarded { get; private set; }
    public StreakManager.StreakResult? LastStreakResult { get; private set; }
    public IReadOnlyList<MilestoneManager.MilestoneResult> LastMilestoneResults { get; private set; } = new List<MilestoneManager.MilestoneResult>();
    public DailyChallengeManager.CompletionResult? LastDailyChallengeCompletionResult { get; private set; }

    public GameplaySaveTriggers(PieceController pieceController, CoinManager coinManager, SaveData saveData, Action requestSave, Func<DateTime> nowProvider = null)
    {
        _pieceController = pieceController ?? throw new ArgumentNullException(nameof(pieceController));
        _coinManager = coinManager ?? throw new ArgumentNullException(nameof(coinManager));
        _saveData = saveData ?? throw new ArgumentNullException(nameof(saveData));
        _requestSave = requestSave ?? throw new ArgumentNullException(nameof(requestSave));
        _nowProvider = nowProvider ?? (() => DateTime.UtcNow);

        _pieceController.OnLinesCleared += OnLinesCleared;
        _pieceController.OnGameOver += OnGameOver;
        _coinManager.OnBalanceChanged += OnCoinBalanceChanged;
    }

    // Not itself one of BUILD_PLAN's five named trigger points, so this
    // only updates the in-memory lifetime counter — it rides along to disk
    // at the next real trigger (a coin change is the common case, since
    // clearing lines almost always precedes a game-over anyway).
    private void OnLinesCleared(LineClearDetector.ClearResult result, int pointsAwarded)
    {
        if (!result.AnyCleared)
        {
            return;
        }

        _saveData.TotalLinesCleared += result.TotalLinesCleared;
    }

    private void OnGameOver()
    {
        ScoreManager score = _pieceController.Score;
        bool isNewBest = score.CurrentScore > _saveData.BestScore;

        // ScoreManager.BestScore already reflects max(the value it was
        // seeded with, any new best reached this game) — writing it back
        // here is exactly PROGRESS.md's flagged Phase 2 gap ("BestScore is
        // read once to seed ScoreManager, but never written back").
        _saveData.BestScore = score.BestScore;
        _saveData.TotalGames++;
        RecordDdaScore(score.CurrentScore);

        LastGameOverCoinsAwarded = Constants.CoinsForGameOver + (isNewBest ? Constants.CoinsForNewBestBonus : 0);
        _coinManager.Earn(LastGameOverCoinsAwarded);

        DateTime today = _nowProvider();
        ApplyStreak(today);
        ApplyMilestones(score.CurrentScore);
        ApplyDailyChallengeCompletion(today, score.CurrentScore);

        // Earn() above already fired OnCoinBalanceChanged synchronously,
        // which requests its own save — this explicit call is deliberate,
        // not redundant cruft: it keeps "game-over always saves" true by
        // construction, not as an incidental side effect of coinsEarned
        // always being positive today.
        _requestSave();
    }

    private void ApplyStreak(DateTime today)
    {
        var streak = new StreakManager(_saveData.StreakCount, _saveData.StreakLastDate);
        StreakManager.StreakResult result = streak.RecordGameCompleted(today);

        _saveData.StreakCount = streak.StreakCount;
        _saveData.StreakLastDate = streak.LastPlayedDateIso;
        LastStreakResult = result.StreakAdvanced ? result : (StreakManager.StreakResult?)null;

        if (!result.StreakAdvanced)
        {
            return;
        }

        if (result.CoinsAwarded > 0)
        {
            _coinManager.Earn(result.CoinsAwarded);
        }

        if (result.GalleryFrameUnlocked != null && !_saveData.GalleryFramesOwned.Contains(result.GalleryFrameUnlocked))
        {
            _saveData.GalleryFramesOwned.Add(result.GalleryFrameUnlocked);
        }
    }

    private void ApplyMilestones(int score)
    {
        List<MilestoneManager.MilestoneResult> results = MilestoneManager.CheckNewlyReached(score, _saveData.MilestonesClaimed);
        LastMilestoneResults = results;

        foreach (MilestoneManager.MilestoneResult milestone in results)
        {
            _coinManager.Earn(milestone.CoinsAwarded);

            if (milestone.GalleryFrameUnlocked != null && !_saveData.GalleryFramesOwned.Contains(milestone.GalleryFrameUnlocked))
            {
                _saveData.GalleryFramesOwned.Add(milestone.GalleryFrameUnlocked);
            }

            if (milestone.ThemeUnlocked != null && !_saveData.IapThemesOwned.Contains(milestone.ThemeUnlocked))
            {
                _saveData.IapThemesOwned.Add(milestone.ThemeUnlocked);
            }
        }
    }

    // CLAUDE.md §4.2: awards the 30-coin daily-challenge reward and
    // records today's best score — only when this game-over ends a
    // session PieceController.StartDailyChallenge actually started.
    // Regular games never touch daily_completed/daily_best_scores.
    private void ApplyDailyChallengeCompletion(DateTime today, int score)
    {
        if (!_pieceController.IsDailyChallengeSession)
        {
            return;
        }

        string todayIso = today.ToString("yyyy-MM-dd");
        DailyChallengeManager.CompletionResult result = DailyChallengeManager.RecordCompletion(_saveData.DailyCompleted, _saveData.DailyBestScores, todayIso, score);
        LastDailyChallengeCompletionResult = result;

        if (result.CoinsAwarded > 0)
        {
            _coinManager.Earn(result.CoinsAwarded);
        }
    }

    private void OnCoinBalanceChanged(int newBalance)
    {
        _saveData.Coins = newBalance;
        _requestSave();
    }

    // CLAUDE.md §3.7: rolling last-10-scores history (for "current form")
    // plus a lifetime average (for "usual form"), the two inputs
    // DDAManager.Classify needs. The lifetime average is updated via the
    // standard incremental-mean formula rather than keeping a separate
    // cumulative-sum field — CLAUDE.md §7.3's save schema only has
    // dda_avg_score itself, no running total to divide.
    private void RecordDdaScore(int score)
    {
        _saveData.DdaLast10Scores.Add(score);
        while (_saveData.DdaLast10Scores.Count > Constants.DdaLast10ScoresCapacity)
        {
            _saveData.DdaLast10Scores.RemoveAt(0);
        }

        _saveData.DdaAvgScore += (score - _saveData.DdaAvgScore) / _saveData.TotalGames;
    }
}
