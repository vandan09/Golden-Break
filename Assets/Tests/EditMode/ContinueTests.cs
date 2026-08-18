using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

public class ContinueTests
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

        // Deliberately distinct pools for the DDA-weighted spawner vs the
        // continue-only standard spawner, so tests can prove TryContinue
        // deals from the standard one, not the (here: domino-only) live
        // spawner — mirrors CLAUDE.md §5.1's explicit "standard pool, not
        // the DDA-adjusted pool" requirement.
        var ddaSpawner = new PieceSpawner(new[] { _dominoPiece }, new Random(1));
        var standardSpawner = new PieceSpawner(new[] { _singleCellPiece }, new Random(1));

        _controller = new GameObject("Controller").AddComponent<PieceController>();
        _controller.Configure(_grid, _tray, ddaSpawner, new ScoreManager(initialBestScore: 0), standardSpawner);
    }

    [TearDown]
    public void DestroyAll()
    {
        Object.DestroyImmediate(_controller.gameObject);
        Object.DestroyImmediate(_tray.gameObject);
        Object.DestroyImmediate(_grid.gameObject);
    }

    private void ForceGameOverByFillingBoard()
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
    public void CanContinue_BeforeGameOver_IsFalse()
    {
        Assert.IsFalse(_controller.CanContinue);
    }

    [Test]
    public void TryContinue_BeforeGameOver_ReturnsFalseAndDoesNothing()
    {
        bool result = _controller.TryContinue();

        Assert.IsFalse(result);
    }

    [Test]
    public void CanContinue_AfterGameOver_IsTrue()
    {
        ForceGameOverByFillingBoard();

        Assert.IsTrue(_controller.CanContinue);
    }

    [Test]
    public void TryContinue_ClearsBottomTwoRowsOnly()
    {
        ForceGameOverByFillingBoard();
        Assert.IsTrue(_grid.Board.IsFilled(0, 5), "sanity: board is completely full before continuing");

        _controller.TryContinue();

        for (int x = 0; x < Constants.GridSize; x++)
        {
            Assert.IsFalse(_grid.Board.IsFilled(x, 6), $"({x},6) should be cleared");
            Assert.IsFalse(_grid.Board.IsFilled(x, 7), $"({x},7) should be cleared");
        }
        for (int y = 0; y < Constants.GridSize - 2; y++)
        {
            for (int x = 0; x < Constants.GridSize; x++)
            {
                Assert.IsTrue(_grid.Board.IsFilled(x, y), $"({x},{y}) in rows 0-5 must remain untouched");
            }
        }
    }

    [Test]
    public void TryContinue_ResolvesGameOverAndDealsFromStandardPoolNotDdaPool()
    {
        ForceGameOverByFillingBoard();

        bool result = _controller.TryContinue();

        Assert.IsTrue(result);
        Assert.IsFalse(_controller.IsGameOver, "clearing 16 cells should reopen valid moves for the single-cell standard pool");
        foreach (PieceDefinition piece in _controller.Hand)
        {
            Assert.AreEqual("single", piece.pieceId, "continue must deal from the standard pool, not the DDA/live spawner (domino pool)");
        }
    }

    [Test]
    public void TryContinue_DoesNotChangeScore()
    {
        ForceGameOverByFillingBoard();
        int scoreBefore = _controller.Score.CurrentScore;

        _controller.TryContinue();

        Assert.AreEqual(scoreBefore, _controller.Score.CurrentScore, "CLAUDE.md §5.1: no score penalty from continuing");
    }

    [Test]
    public void CanContinue_AfterOneUse_IsFalse()
    {
        ForceGameOverByFillingBoard();
        _controller.TryContinue();

        Assert.IsFalse(_controller.CanContinue, "only one continue per game");
    }

    [Test]
    public void TryContinue_SecondAttemptSameGame_ReturnsFalse()
    {
        ForceGameOverByFillingBoard();
        _controller.TryContinue();

        // Force game-over again to make sure a *second* continue is still
        // blocked even though IsGameOver is true again.
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
        Assert.IsTrue(_controller.IsGameOver, "sanity: game should be over again after refilling");

        bool secondContinue = _controller.TryContinue();

        Assert.IsFalse(secondContinue, "the continue button must not reappear on the second game-over");
    }

    [Test]
    public void RestartGame_ResetsContinueAvailabilityForTheNewGame()
    {
        ForceGameOverByFillingBoard();
        _controller.TryContinue();
        Assert.IsFalse(_controller.CanContinue);

        _controller.RestartGame();
        ForceGameOverByFillingBoard();

        Assert.IsTrue(_controller.CanContinue, "a fresh game should have its own continue available");
    }

    [Test]
    public void Configure_WithoutExplicitStandardSpawner_FallsBackToTheMainSpawner()
    {
        var pool = new[] { _singleCellPiece };
        var spawner = new PieceSpawner(pool, new Random(1));
        var grid = new GameObject("Grid2").AddComponent<GridManager>();
        grid.BuildGrid();
        var tray = new GameObject("Tray2").AddComponent<PieceTrayController>();
        tray.BuildSlots();
        var controller = new GameObject("Controller2").AddComponent<PieceController>();

        Assert.DoesNotThrow(() => controller.Configure(grid, tray, spawner, new ScoreManager(0)));

        Object.DestroyImmediate(controller.gameObject);
        Object.DestroyImmediate(tray.gameObject);
        Object.DestroyImmediate(grid.gameObject);
    }
}
