using System;

/// <summary>
/// Wires live gameplay events to <see cref="SaveData"/> mutation and
/// persistence (BUILD_PLAN Phase 4 "save triggers": game-over, ceramic
/// completion, coin change, settings change, OnApplicationPause).
/// Ceramic-completion and OnApplicationPause triggers already live in
/// <see cref="CeramicController"/> and <see cref="SaveManager"/>
/// respectively — this class owns the two that don't otherwise have a
/// natural home: game-over (best score, total games, lines cleared, DDA
/// history, game-over coin award) and any coin balance change.
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
/// <c>saveManager.Save</c>.
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

    public GameplaySaveTriggers(PieceController pieceController, CoinManager coinManager, SaveData saveData, Action requestSave)
    {
        _pieceController = pieceController ?? throw new ArgumentNullException(nameof(pieceController));
        _coinManager = coinManager ?? throw new ArgumentNullException(nameof(coinManager));
        _saveData = saveData ?? throw new ArgumentNullException(nameof(saveData));
        _requestSave = requestSave ?? throw new ArgumentNullException(nameof(requestSave));

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

        int coinsEarned = Constants.CoinsForGameOver + (isNewBest ? Constants.CoinsForNewBestBonus : 0);
        _coinManager.Earn(coinsEarned);

        // Earn() above already fired OnCoinBalanceChanged synchronously,
        // which requests its own save — this explicit call is deliberate,
        // not redundant cruft: it keeps "game-over always saves" true by
        // construction, not as an incidental side effect of coinsEarned
        // always being positive today.
        _requestSave();
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
