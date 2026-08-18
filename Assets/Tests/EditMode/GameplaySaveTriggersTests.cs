using System;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

public class GameplaySaveTriggersTests
{
    // Every game-over in these tests happens "on" this same fixed date
    // unless a test explicitly advances it — keeps streak behavior
    // deterministic (first game-over of a fresh SaveData always advances
    // the streak to day 1, awarding its own 10 coins, which every
    // coin-balance assertion below accounts for).
    private static readonly DateTime FixedDate = new DateTime(2026, 8, 18);

    private GridManager _grid;
    private PieceTrayController _tray;
    private PieceController _controller;
    private PieceDefinition _singleCellPiece;
    private CoinManager _coins;
    private SaveData _saveData;
    private GameplaySaveTriggers _saveTriggers;
    private int _saveRequestCount;

    [SetUp]
    public void CreateAll()
    {
        _grid = new GameObject("Grid").AddComponent<GridManager>();
        _grid.BuildGrid();

        _tray = new GameObject("Tray").AddComponent<PieceTrayController>();
        _tray.BuildSlots();

        _singleCellPiece = ScriptableObject.CreateInstance<PieceDefinition>();
        _singleCellPiece.pieceId = "single";
        _singleCellPiece.cells = new[] { new Vector2Int(0, 0) };
        _singleCellPiece.spawnWeight = 1;

        var spawner = new PieceSpawner(new[] { _singleCellPiece }, new Random(1));

        _controller = new GameObject("Controller").AddComponent<PieceController>();
        _controller.Configure(_grid, _tray, spawner, new ScoreManager(initialBestScore: 0));

        _coins = new CoinManager(initialBalance: 0);
        _saveData = SaveData.CreateFresh("2026-08-18");
        _saveRequestCount = 0;

        _saveTriggers = new GameplaySaveTriggers(_controller, _coins, _saveData, () => _saveRequestCount++, () => FixedDate);
    }

    [TearDown]
    public void DestroyAll()
    {
        UnityEngine.Object.DestroyImmediate(_controller.gameObject);
        UnityEngine.Object.DestroyImmediate(_tray.gameObject);
        UnityEngine.Object.DestroyImmediate(_grid.gameObject);
    }

    private void PlaceAt(int slotIndex, int x, int y)
    {
        Vector3 target = _grid.transform.TransformPoint(GridManager.CellToLocalPosition(x, y));
        _controller.BeginDrag(slotIndex, _tray.Slots[slotIndex].transform.position);
        _controller.UpdateDrag(target);
        _controller.EndDrag();
    }

    // Fills whatever cells are still empty (works on an empty board or a
    // partially-filled one) so no piece can fit anywhere, then re-deals to
    // force GameOverDetector to trip and PieceController.OnGameOver to
    // fire — same technique as GameOverDetectorTests' full-board case.
    private void FillRemainingCellsAndForceGameOver()
    {
        for (int y = 0; y < Constants.GridSize; y++)
        {
            for (int x = 0; x < Constants.GridSize; x++)
            {
                if (!_grid.Board.IsFilled(x, y))
                {
                    _grid.Board.Place(_singleCellPiece, x, y, colourId: 0);
                }
            }
        }

        _controller.DealNewHand();
    }

    // Clears row 0 exactly once, worth 10 base points times the current
    // streak multiplier — used to build up a precise, predictable score
    // across repeated calls without ever needing more than one row.
    private void ClearRowZeroOnce(int slotIndex)
    {
        for (int x = 0; x < Constants.GridSize - 1; x++)
        {
            _grid.Board.Place(_singleCellPiece, x, 0, colourId: 0);
        }
        PlaceAt(slotIndex, Constants.GridSize - 1, 0);
    }

    [Test]
    public void Constructor_NullPieceController_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GameplaySaveTriggers(null, _coins, _saveData, () => { }));
    }

    [Test]
    public void Constructor_NullCoinManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GameplaySaveTriggers(_controller, null, _saveData, () => { }));
    }

    [Test]
    public void Constructor_NullSaveData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GameplaySaveTriggers(_controller, _coins, null, () => { }));
    }

    [Test]
    public void Constructor_NullRequestSaveCallback_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GameplaySaveTriggers(_controller, _coins, _saveData, null));
    }

    [Test]
    public void LineClear_AccumulatesIntoTotalLinesClearedWithoutRequestingASave()
    {
        ClearRowZeroOnce(0);

        Assert.AreEqual(1, _saveData.TotalLinesCleared);
        Assert.AreEqual(0, _saveRequestCount, "a bare line clear (no coin change yet) shouldn't request a save on its own");
    }

    [Test]
    public void NonClearingPlacement_DoesNotChangeTotalLinesCleared()
    {
        PlaceAt(0, 3, 3);

        Assert.AreEqual(0, _saveData.TotalLinesCleared);
    }

    [Test]
    public void GameOver_AwardsBaseCoinsAndRequestsASave()
    {
        FillRemainingCellsAndForceGameOver();

        Assert.IsTrue(_controller.IsGameOver, "sanity: the forced fill should have triggered game-over");
        // Base game-over (5) + day-1 streak reward (10), since this is
        // this SaveData's first-ever game-over.
        Assert.AreEqual(Constants.CoinsForGameOver + 10, _coins.Balance);
        Assert.AreEqual(Constants.CoinsForGameOver + 10, _saveData.Coins);
        Assert.AreEqual(1, _saveData.TotalGames);
        Assert.GreaterOrEqual(_saveRequestCount, 1);
        Assert.AreEqual(Constants.CoinsForGameOver, _saveTriggers.LastGameOverCoinsAwarded, "the exposed base amount should exclude streak/milestone bonuses");
    }

    [Test]
    public void GameOver_WhenScoreExceedsPriorBest_AwardsBonusCoinsAndUpdatesBestScoreAndDdaAverage()
    {
        ClearRowZeroOnce(0); // +10 points, row 0 clears

        FillRemainingCellsAndForceGameOver();

        Assert.IsTrue(_controller.IsGameOver);
        Assert.AreEqual(Constants.CoinsForGameOver + Constants.CoinsForNewBestBonus + 10, _coins.Balance, "base + new-best bonus + day-1 streak reward");
        Assert.AreEqual(Constants.CoinsForGameOver + Constants.CoinsForNewBestBonus, _saveTriggers.LastGameOverCoinsAwarded);
        Assert.AreEqual(10, _saveData.BestScore);
        Assert.AreEqual(10f, _saveData.DdaAvgScore, 0.0001f);
        Assert.AreEqual(1, _saveData.TotalLinesCleared);
    }

    [Test]
    public void GameOver_NoScoreImprovement_AwardsOnlyBaseCoinsPlusStreak()
    {
        FillRemainingCellsAndForceGameOver();

        Assert.AreEqual(Constants.CoinsForGameOver + 10, _coins.Balance, "no new best this game, so no bonus — but day-1 streak still applies");
    }

    [Test]
    public void GameOver_FirstEverGameOver_AdvancesStreakToOneAndUnlocksNoFrameYet()
    {
        FillRemainingCellsAndForceGameOver();

        Assert.AreEqual(1, _saveData.StreakCount);
        Assert.AreEqual("2026-08-18", _saveData.StreakLastDate);
        Assert.AreEqual(0, _saveData.GalleryFramesOwned.Count, "day 1 doesn't unlock a frame");
        Assert.IsTrue(_saveTriggers.LastStreakResult.HasValue);
        Assert.AreEqual(1, _saveTriggers.LastStreakResult.Value.StreakCount);
    }

    [Test]
    public void GameOver_SecondGameOverSameDay_DoesNotDoubleAwardStreakCoins()
    {
        FillRemainingCellsAndForceGameOver();
        int balanceAfterFirst = _coins.Balance;

        _controller.RestartGame();
        FillRemainingCellsAndForceGameOver();

        Assert.AreEqual(1, _saveData.StreakCount, "streak should still be 1 — same calendar day");
        Assert.AreEqual(balanceAfterFirst + Constants.CoinsForGameOver, _coins.Balance, "second game-over same day earns only the base game-over coins, no repeat streak reward");
        Assert.IsFalse(_saveTriggers.LastStreakResult.HasValue, "no streak advance to report the second time");
    }

    [Test]
    public void GameOver_ScoreReachesFiveHundred_AwardsMilestoneCoinsAndFrame()
    {
        // Streak multiplier caps at x3 after the 5th consecutive clear
        // (CLAUDE.md §3.2 / ScoreManagerTests): clears 1-5 total
        // 10+15+20+25+30=100, every clear after that is a flat 30. To
        // clear 500 without overshooting into the 1000 milestone: 100 +
        // n*30 >= 500 => n >= 14 (100 + 14*30 = 520). 19 total clears.
        for (int i = 0; i < 19; i++)
        {
            ClearRowZeroOnce(i % Constants.PieceHandSize);
        }

        Assert.AreEqual(520, _controller.Score.CurrentScore, "sanity: precise predicted score before forcing game-over");

        FillRemainingCellsAndForceGameOver();

        Assert.AreEqual(1, _saveTriggers.LastMilestoneResults.Count);
        Assert.AreEqual(500, _saveTriggers.LastMilestoneResults[0].MilestoneScore);
        Assert.AreEqual("milestone_500", _saveTriggers.LastMilestoneResults[0].GalleryFrameUnlocked);
        Assert.Contains("milestone_500", _saveData.GalleryFramesOwned);
        Assert.Contains(500, _saveData.MilestonesClaimed);
        // Base(5) + day-1 streak(10) + milestone(50). No new-best bonus
        // check here — best_score started at 0, so 520 is trivially a new
        // best too, adding CoinsForNewBestBonus on top.
        Assert.AreEqual(Constants.CoinsForGameOver + Constants.CoinsForNewBestBonus + 10 + 50, _coins.Balance);
    }

    [Test]
    public void GameOver_RegularGame_DoesNotTouchDailyChallengeState()
    {
        FillRemainingCellsAndForceGameOver();

        Assert.AreEqual(0, _saveData.DailyCompleted.Count);
        Assert.IsFalse(_saveTriggers.LastDailyChallengeCompletionResult.HasValue);
    }

    [Test]
    public void GameOver_DuringDailyChallengeSession_AwardsCompletionCoinsAndRecordsBest()
    {
        var dailySpawner = new PieceSpawner(new[] { _singleCellPiece }, new Random(1));
        _controller.StartDailyChallenge(dailySpawner);

        FillRemainingCellsAndForceGameOver();

        Assert.IsTrue(_saveTriggers.LastDailyChallengeCompletionResult.HasValue);
        Assert.IsTrue(_saveTriggers.LastDailyChallengeCompletionResult.Value.IsFirstCompletionToday);
        Assert.AreEqual(1, _saveData.DailyCompleted.Count);
        Assert.AreEqual(0, _saveData.DailyBestScores["2026-08-18"]);
        // Base(5) + day-1 streak(10) + daily challenge(30) — no new-best
        // bonus check here since a score of 0 doesn't beat an initial 0 best.
        Assert.AreEqual(Constants.CoinsForGameOver + 10 + DailyChallengeManager.CompletionRewardCoins, _coins.Balance);
    }

    [Test]
    public void GameOver_AcrossMultipleGames_UpdatesLifetimeAverageIncrementally()
    {
        // Game 1: score exactly 10 points via one clear, then force game over.
        ClearRowZeroOnce(0);
        FillRemainingCellsAndForceGameOver();

        Assert.AreEqual(10f, _saveData.DdaAvgScore, 0.0001f);

        _controller.RestartGame();

        // Game 2: no scoring at all, straight to game over.
        FillRemainingCellsAndForceGameOver();

        Assert.AreEqual(5f, _saveData.DdaAvgScore, 0.0001f, "incremental mean of [10, 0] should be 5");
        Assert.AreEqual(2, _saveData.TotalGames);
        CollectionAssert.AreEqual(new[] { 10, 0 }, _saveData.DdaLast10Scores);
    }

    [Test]
    public void GameOver_MoreThanTenGames_KeepsOnlyMostRecentTenScores()
    {
        for (int i = 0; i < 11; i++)
        {
            FillRemainingCellsAndForceGameOver();
            Assert.IsTrue(_controller.IsGameOver);
            _controller.RestartGame();
        }

        Assert.AreEqual(Constants.DdaLast10ScoresCapacity, _saveData.DdaLast10Scores.Count);
        Assert.AreEqual(11, _saveData.TotalGames);
    }

    [Test]
    public void CoinBalanceChange_UnrelatedToGameOver_TriggersASave()
    {
        _coins.Earn(20);

        Assert.AreEqual(1, _saveRequestCount);
        Assert.AreEqual(20, _saveData.Coins);
    }

    [Test]
    public void CoinBalanceChange_FromSpending_AlsoTriggersASaveAndReflectsNewBalance()
    {
        _coins.Earn(100);
        _coins.TrySpend(50);

        Assert.AreEqual(2, _saveRequestCount);
        Assert.AreEqual(50, _saveData.Coins);
    }
}
