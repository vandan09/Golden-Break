using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

public class AnalyticsEventWiringTests
{
    private GridManager _grid;
    private PieceTrayController _tray;
    private PieceController _controller;
    private PieceDefinition _singleCellPiece;
    private CoinManager _coins;
    private SaveData _saveData;
    private GameplaySaveTriggers _saveTriggers;
    private readonly List<(string name, IReadOnlyDictionary<string, object> parameters)> _loggedEvents = new List<(string, IReadOnlyDictionary<string, object>)>();

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
        // Configure() itself fires OnGameStarted — AnalyticsEventWiring
        // isn't constructed yet at that point, so it never observes this
        // very first firing; each test that cares triggers its own
        // RestartGame() afterward instead.
        _controller.Configure(_grid, _tray, spawner, new ScoreManager(initialBestScore: 0));

        _coins = new CoinManager(initialBalance: 0);
        _saveData = SaveData.CreateFresh("2026-08-18");
        _saveTriggers = new GameplaySaveTriggers(_controller, _coins, _saveData, () => { });
        _loggedEvents.Clear();
    }

    [TearDown]
    public void DestroyAll()
    {
        UnityEngine.Object.DestroyImmediate(_controller.gameObject);
        UnityEngine.Object.DestroyImmediate(_tray.gameObject);
        UnityEngine.Object.DestroyImmediate(_grid.gameObject);
    }

    private AnalyticsEventWiring CreateWiring(CeramicManager ceramicManager = null)
    {
        return new AnalyticsEventWiring(_controller, _saveTriggers, _saveData, ceramicManager, (name, parameters) => _loggedEvents.Add((name, parameters)));
    }

    private void PlaceAt(int slotIndex, int x, int y)
    {
        Vector3 target = _grid.transform.TransformPoint(GridManager.CellToLocalPosition(x, y));
        _controller.BeginDrag(slotIndex, _tray.Slots[slotIndex].transform.position);
        _controller.UpdateDrag(target);
        _controller.EndDrag();
    }

    private (string name, IReadOnlyDictionary<string, object> parameters) FindEvent(string name)
    {
        foreach (var entry in _loggedEvents)
        {
            if (entry.name == name)
            {
                return entry;
            }
        }

        Assert.Fail($"Expected event '{name}' was never logged. Logged: {string.Join(", ", _loggedEvents.ConvertAll(e => e.name))}");
        return default;
    }

    [Test]
    public void Constructor_NullPieceController_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AnalyticsEventWiring(null, _saveTriggers, _saveData, null, (n, p) => { }));
    }

    [Test]
    public void Constructor_NullSaveTriggers_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AnalyticsEventWiring(_controller, null, _saveData, null, (n, p) => { }));
    }

    [Test]
    public void Constructor_NullSaveData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AnalyticsEventWiring(_controller, _saveTriggers, null, null, (n, p) => { }));
    }

    [Test]
    public void Constructor_NullCeramicManager_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => CreateWiring(null));
    }

    [Test]
    public void RestartGame_LogsGameStartWithSessionNumberAndCeramicTier()
    {
        CreateWiring();
        _saveData.TotalGames = 4;
        _saveData.CurrentCeramic.Tier = 3;

        _controller.RestartGame();

        var evt = FindEvent("game_start");
        Assert.AreEqual(5, evt.parameters["session_number"]);
        Assert.AreEqual(3, evt.parameters["ceramic_tier"]);
    }

    [Test]
    public void ClearingPlacement_LogsLineClearWithCorrectParams()
    {
        CreateWiring();
        for (int x = 0; x < Constants.GridSize - 1; x++)
        {
            _grid.Board.Place(_singleCellPiece, x, 0, colourId: 0);
        }

        PlaceAt(0, Constants.GridSize - 1, 0);

        var evt = FindEvent("line_clear");
        Assert.AreEqual(1, evt.parameters["lines_in_move"]);
        Assert.AreEqual(1f, evt.parameters["combo_multiplier"], "the first clear's own multiplier, not ScoreManager's already-advanced live value");
        Assert.AreEqual(1, evt.parameters["streak_length"]);
    }

    [Test]
    public void SecondConsecutiveClear_LogsTheMultiplierItActuallyUsed()
    {
        CreateWiring();
        for (int x = 0; x < Constants.GridSize - 1; x++)
        {
            _grid.Board.Place(_singleCellPiece, x, 0, colourId: 0);
        }
        PlaceAt(0, Constants.GridSize - 1, 0); // 1st clear, x1

        for (int x = 0; x < Constants.GridSize - 1; x++)
        {
            _grid.Board.Place(_singleCellPiece, x, 1, colourId: 0);
        }
        PlaceAt(1, Constants.GridSize - 1, 1); // 2nd consecutive clear, x1.5

        var secondClearEvent = _loggedEvents.FindAll(e => e.name == "line_clear")[1];
        Assert.AreEqual(1.5f, secondClearEvent.parameters["combo_multiplier"]);
        Assert.AreEqual(2, secondClearEvent.parameters["streak_length"]);
    }

    [Test]
    public void NonClearingPlacement_DoesNotLogLineClear()
    {
        CreateWiring();

        PlaceAt(0, 3, 3);

        foreach (var entry in _loggedEvents)
        {
            Assert.AreNotEqual("line_clear", entry.name);
        }
    }

    [Test]
    public void GameOver_LogsGameOverWithScoreAndCounts()
    {
        CreateWiring();

        for (int y = 0; y < Constants.GridSize; y++)
        {
            for (int x = 0; x < Constants.GridSize; x++)
            {
                _grid.Board.Place(_singleCellPiece, x, y, colourId: 0);
            }
        }
        _controller.DealNewHand();

        var evt = FindEvent("game_over");
        Assert.AreEqual(0, evt.parameters["score"]);
        Assert.AreEqual(0, evt.parameters["ceramics_completed"]);
    }

    [Test]
    public void NewBest_LogsPersonalBestWithPreviousAndNewValues()
    {
        CreateWiring();
        for (int x = 0; x < Constants.GridSize - 1; x++)
        {
            _grid.Board.Place(_singleCellPiece, x, 0, colourId: 0);
        }
        PlaceAt(0, Constants.GridSize - 1, 0); // +10 points, a new best over the initial 0

        var evt = FindEvent("personal_best");
        Assert.AreEqual(10, evt.parameters["new_best_score"]);
        Assert.AreEqual(0, evt.parameters["previous_best"]);
    }

    [Test]
    public void CeramicCompleted_LogsCeramicCompleteWithTier()
    {
        var progress = new CeramicProgressData { Tier = 2, TotalCracks = 4, CracksRepaired = 3, ColourVariant = 0 };
        var ceramicManager = new CeramicManager(progress, 0);
        CreateWiring(ceramicManager);

        ceramicManager.RepairCracks(1); // completes it

        var evt = FindEvent("ceramic_complete");
        Assert.AreEqual(2, evt.parameters["ceramic_tier"]);
    }
}
