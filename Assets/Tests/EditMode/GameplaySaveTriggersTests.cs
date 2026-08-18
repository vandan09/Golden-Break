using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

public class GameplaySaveTriggersTests
{
    private GridManager _grid;
    private PieceTrayController _tray;
    private PieceController _controller;
    private PieceDefinition _singleCellPiece;
    private CoinManager _coins;
    private SaveData _saveData;
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

        new GameplaySaveTriggers(_controller, _coins, _saveData, () => _saveRequestCount++);
    }

    [TearDown]
    public void DestroyAll()
    {
        Object.DestroyImmediate(_controller.gameObject);
        Object.DestroyImmediate(_tray.gameObject);
        Object.DestroyImmediate(_grid.gameObject);
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

    [Test]
    public void Constructor_NullPieceController_ThrowsArgumentNullException()
    {
        Assert.Throws<System.ArgumentNullException>(() => new GameplaySaveTriggers(null, _coins, _saveData, () => { }));
    }

    [Test]
    public void Constructor_NullCoinManager_ThrowsArgumentNullException()
    {
        Assert.Throws<System.ArgumentNullException>(() => new GameplaySaveTriggers(_controller, null, _saveData, () => { }));
    }

    [Test]
    public void Constructor_NullSaveData_ThrowsArgumentNullException()
    {
        Assert.Throws<System.ArgumentNullException>(() => new GameplaySaveTriggers(_controller, _coins, null, () => { }));
    }

    [Test]
    public void Constructor_NullRequestSaveCallback_ThrowsArgumentNullException()
    {
        Assert.Throws<System.ArgumentNullException>(() => new GameplaySaveTriggers(_controller, _coins, _saveData, null));
    }

    [Test]
    public void LineClear_AccumulatesIntoTotalLinesClearedWithoutRequestingASave()
    {
        for (int x = 0; x < Constants.GridSize - 1; x++)
        {
            _grid.Board.Place(_singleCellPiece, x, 0, colourId: 0);
        }

        PlaceAt(0, Constants.GridSize - 1, 0);

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
        Assert.AreEqual(Constants.CoinsForGameOver, _coins.Balance);
        Assert.AreEqual(Constants.CoinsForGameOver, _saveData.Coins);
        Assert.AreEqual(1, _saveData.TotalGames);
        Assert.GreaterOrEqual(_saveRequestCount, 1);
    }

    [Test]
    public void GameOver_WhenScoreExceedsPriorBest_AwardsBonusCoinsAndUpdatesBestScoreAndDdaAverage()
    {
        for (int x = 0; x < Constants.GridSize - 1; x++)
        {
            _grid.Board.Place(_singleCellPiece, x, 0, colourId: 0);
        }
        PlaceAt(0, Constants.GridSize - 1, 0); // +10 points, row 0 clears

        FillRemainingCellsAndForceGameOver();

        Assert.IsTrue(_controller.IsGameOver);
        Assert.AreEqual(Constants.CoinsForGameOver + Constants.CoinsForNewBestBonus, _coins.Balance);
        Assert.AreEqual(10, _saveData.BestScore);
        Assert.AreEqual(10f, _saveData.DdaAvgScore, 0.0001f);
        Assert.AreEqual(1, _saveData.TotalLinesCleared);
    }

    [Test]
    public void GameOver_NoScoreImprovement_AwardsOnlyBaseCoins()
    {
        FillRemainingCellsAndForceGameOver();

        Assert.AreEqual(Constants.CoinsForGameOver, _coins.Balance, "no new best this game, so no bonus should be awarded");
    }

    [Test]
    public void GameOver_AcrossMultipleGames_UpdatesLifetimeAverageIncrementally()
    {
        // Game 1: score exactly 10 points via one clear, then force game over.
        for (int x = 0; x < Constants.GridSize - 1; x++)
        {
            _grid.Board.Place(_singleCellPiece, x, 0, colourId: 0);
        }
        PlaceAt(0, Constants.GridSize - 1, 0);
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
