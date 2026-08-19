using System;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

public class DailyChallengeSaveTriggersTests
{
    private static readonly DateTime FixedDate = new DateTime(2026, 8, 18);

    private GridManager _grid;
    private PieceTrayController _tray;
    private PieceController _controller;
    private PieceDefinition _singleCellPiece;
    private CoinManager _coins;
    private SaveData _saveData;
    private DailyChallengeSaveTriggers _saveTriggers;
    private int _saveRequestCount;

    [SetUp]
    public void CreateAll()
    {
        _grid = new GameObject("DailyGrid").AddComponent<GridManager>();
        _grid.BuildGrid();

        _tray = new GameObject("DailyTray").AddComponent<PieceTrayController>();
        _tray.BuildSlots();

        _singleCellPiece = ScriptableObject.CreateInstance<PieceDefinition>();
        _singleCellPiece.pieceId = "single";
        _singleCellPiece.cells = new[] { new Vector2Int(0, 0) };
        _singleCellPiece.spawnWeight = 1;

        var spawner = new PieceSpawner(new[] { _singleCellPiece }, new Random(1));

        _controller = new GameObject("DailyController").AddComponent<PieceController>();
        _controller.Configure(_grid, _tray, spawner, new ScoreManager(initialBestScore: 0));

        _coins = new CoinManager(initialBalance: 0);
        _saveData = SaveData.CreateFresh("2026-08-18");
        _saveRequestCount = 0;

        _saveTriggers = new DailyChallengeSaveTriggers(_controller, _coins, _saveData, () => _saveRequestCount++, () => FixedDate);
    }

    [TearDown]
    public void DestroyAll()
    {
        UnityEngine.Object.DestroyImmediate(_controller.gameObject);
        UnityEngine.Object.DestroyImmediate(_tray.gameObject);
        UnityEngine.Object.DestroyImmediate(_grid.gameObject);
    }

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

    [Test]
    public void Constructor_NullPieceController_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DailyChallengeSaveTriggers(null, _coins, _saveData, () => { }));
    }

    [Test]
    public void Constructor_NullCoinManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DailyChallengeSaveTriggers(_controller, null, _saveData, () => { }));
    }

    [Test]
    public void Constructor_NullSaveData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DailyChallengeSaveTriggers(_controller, _coins, null, () => { }));
    }

    [Test]
    public void Constructor_NullRequestSaveCallback_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DailyChallengeSaveTriggers(_controller, _coins, _saveData, null));
    }

    [Test]
    public void GameOver_FirstCompletionToday_AwardsCoinsAndRecordsBestAndSaves()
    {
        FillRemainingCellsAndForceGameOver();

        Assert.IsTrue(_saveTriggers.LastCompletionResult.HasValue);
        Assert.IsTrue(_saveTriggers.LastCompletionResult.Value.IsFirstCompletionToday);
        Assert.AreEqual(1, _saveData.DailyCompleted.Count);
        Assert.AreEqual(0, _saveData.DailyBestScores["2026-08-18"]);
        Assert.AreEqual(DailyChallengeManager.CompletionRewardCoins, _coins.Balance);
        Assert.GreaterOrEqual(_saveRequestCount, 1);
    }

    [Test]
    public void GameOver_SecondAttemptSameDay_DoesNotReAwardCoins()
    {
        FillRemainingCellsAndForceGameOver();
        int balanceAfterFirst = _coins.Balance;

        _controller.RestartGame();
        FillRemainingCellsAndForceGameOver();

        Assert.IsFalse(_saveTriggers.LastCompletionResult.Value.IsFirstCompletionToday);
        Assert.AreEqual(balanceAfterFirst, _coins.Balance, "a same-day replay must not re-pay the daily completion reward");
    }

    [Test]
    public void GameOver_DoesNotTouchRegularPlayState()
    {
        FillRemainingCellsAndForceGameOver();

        Assert.AreEqual(0, _saveData.TotalGames, "Daily Challenge must never touch regular play's total_games");
        Assert.AreEqual(0, _saveData.BestScore, "Daily Challenge must never touch regular play's best_score");
        Assert.AreEqual(0, _saveData.StreakCount, "Daily Challenge must never touch the regular daily-streak counter");
        Assert.AreEqual(0f, _saveData.DdaAvgScore, "Daily Challenge must never feed DDA history");
    }
}
