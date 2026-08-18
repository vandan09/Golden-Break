using System;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

public class DailyChallengeSessionTests
{
    private GridManager _grid;
    private PieceTrayController _tray;
    private PieceController _controller;
    private PieceDefinition _singleCellPiece;
    private PieceDefinition _dominoPiece;

    [SetUp]
    public void CreateController()
    {
        _grid = new GameObject("Grid").AddComponent<GridManager>();
        _grid.BuildGrid();

        _tray = new GameObject("Tray").AddComponent<PieceTrayController>();
        _tray.BuildSlots();

        _singleCellPiece = ScriptableObject.CreateInstance<PieceDefinition>();
        _singleCellPiece.pieceId = "single";
        _singleCellPiece.cells = new[] { new Vector2Int(0, 0) };
        _singleCellPiece.spawnWeight = 1;

        _dominoPiece = ScriptableObject.CreateInstance<PieceDefinition>();
        _dominoPiece.pieceId = "domino";
        _dominoPiece.cells = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) };
        _dominoPiece.spawnWeight = 1;

        // DDA/regular pool is dominoes only; the daily pool is single-cell
        // only, so hand composition alone proves which spawner is active.
        var ddaSpawner = new PieceSpawner(new[] { _dominoPiece }, new Random(1));
        _controller = new GameObject("Controller").AddComponent<PieceController>();
        _controller.Configure(_grid, _tray, ddaSpawner, new ScoreManager(0));
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

    [Test]
    public void IsDailyChallengeSession_BeforeStarting_IsFalse()
    {
        Assert.IsFalse(_controller.IsDailyChallengeSession);
    }

    [Test]
    public void StartDailyChallenge_DealsFromTheSuppliedSpawnerNotTheRegularOne()
    {
        var dailySpawner = new PieceSpawner(new[] { _singleCellPiece }, new Random(1));

        _controller.StartDailyChallenge(dailySpawner);

        Assert.IsTrue(_controller.IsDailyChallengeSession);
        foreach (PieceDefinition piece in _controller.Hand)
        {
            Assert.AreEqual("single", piece.pieceId);
        }
    }

    [Test]
    public void StartDailyChallenge_NullSpawner_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _controller.StartDailyChallenge(null));
    }

    [Test]
    public void StartDailyChallenge_RefreshStaysWithinTheSeededPool_NotTheDdaPool()
    {
        var dailySpawner = new PieceSpawner(new[] { _singleCellPiece }, new Random(1));
        _controller.StartDailyChallenge(dailySpawner);

        bool refreshed = _controller.TryRefresh();

        Assert.IsTrue(refreshed);
        foreach (PieceDefinition piece in _controller.Hand)
        {
            Assert.AreEqual("single", piece.pieceId, "refresh mid-daily-challenge must stay within the seeded pool");
        }
    }

    [Test]
    public void StartDailyChallenge_SubsequentHandAfterFullPlacement_StaysWithinSeededPool()
    {
        var dailySpawner = new PieceSpawner(new[] { _singleCellPiece }, new Random(1));
        _controller.StartDailyChallenge(dailySpawner);

        PlaceAt(0, 0, 0);
        PlaceAt(1, 2, 2);
        PlaceAt(2, 4, 4);

        foreach (PieceDefinition piece in _controller.Hand)
        {
            Assert.AreEqual("single", piece.pieceId, "the second hand of a daily-challenge session must also come from the seeded pool");
        }
    }

    [Test]
    public void CanContinue_DuringDailyChallengeGameOver_IsFalse()
    {
        var dailySpawner = new PieceSpawner(new[] { _singleCellPiece }, new Random(1));
        _controller.StartDailyChallenge(dailySpawner);

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

        Assert.IsTrue(_controller.IsGameOver, "sanity: forced game-over");
        Assert.IsFalse(_controller.CanContinue, "continue must be unavailable during a daily-challenge session");
    }

    [Test]
    public void RestartGame_AfterDailyChallenge_RevertsToTheRegularDdaSpawner()
    {
        var dailySpawner = new PieceSpawner(new[] { _singleCellPiece }, new Random(1));
        _controller.StartDailyChallenge(dailySpawner);
        Assert.IsTrue(_controller.IsDailyChallengeSession);

        _controller.RestartGame();

        Assert.IsFalse(_controller.IsDailyChallengeSession);
        foreach (PieceDefinition piece in _controller.Hand)
        {
            Assert.AreEqual("domino", piece.pieceId, "regular play should be back on the DDA spawner");
        }
    }

    [Test]
    public void RestartGame_AfterDailyChallenge_ContinueBecomesAvailableAgainOnGameOver()
    {
        var dailySpawner = new PieceSpawner(new[] { _singleCellPiece }, new Random(1));
        _controller.StartDailyChallenge(dailySpawner);
        _controller.RestartGame();

        for (int y = 0; y < Constants.GridSize; y++)
        {
            for (int x = 0; x < Constants.GridSize; x++)
            {
                _grid.Board.Place(_singleCellPiece, x, y, colourId: 0);
            }
        }
        _controller.DealNewHand();

        Assert.IsTrue(_controller.IsGameOver);
        Assert.IsTrue(_controller.CanContinue, "regular play after leaving a daily-challenge session should allow continue again");
    }
}
