using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

public class UndoTests
{
    private GridManager _grid;
    private PieceTrayController _tray;
    private PieceController _controller;
    private PieceDefinition _singleCellPiece;

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

        var spawner = new PieceSpawner(new[] { _singleCellPiece }, new Random(1));

        _controller = new GameObject("Controller").AddComponent<PieceController>();
        _controller.Configure(_grid, _tray, spawner, new ScoreManager(initialBestScore: 0));
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

    [Test]
    public void CanUndo_BeforeAnyPlacement_IsFalse()
    {
        Assert.IsFalse(_controller.CanUndo);
    }

    [Test]
    public void TryUndo_NoPlacementYet_ReturnsFalse()
    {
        bool undone = _controller.TryUndo();

        Assert.IsFalse(undone);
    }

    [Test]
    public void TryUndo_AfterNonClearingPlacement_RestoresCellAndReturnsPieceToTray()
    {
        PlaceAt(0, 3, 3);
        Assert.IsTrue(_grid.Board.IsFilled(3, 3), "sanity: placement should have filled the cell");
        Assert.IsNull(_controller.Hand[0], "sanity: placement should have cleared the hand slot");

        bool undone = _controller.TryUndo();

        Assert.IsTrue(undone);
        Assert.IsFalse(_grid.Board.IsFilled(3, 3));
        Assert.AreSame(_singleCellPiece, _controller.Hand[0]);
        Assert.AreSame(_singleCellPiece, _tray.Slots[0].CurrentPiece);
    }

    [Test]
    public void CanUndo_AfterLineClearingPlacement_IsFalse()
    {
        // Fill row 0 except the last column, then place the single piece
        // there to complete the row and trigger a clear.
        for (int x = 0; x < Constants.GridSize - 1; x++)
        {
            _grid.Board.Place(_singleCellPiece, x, 0, colourId: 0);
        }

        PlaceAt(0, Constants.GridSize - 1, 0);

        Assert.IsFalse(_grid.Board.IsFilled(0, 0), "sanity: row should have cleared");
        Assert.IsFalse(_controller.CanUndo, "undo must be blocked once the placement caused a line clear");
    }

    [Test]
    public void TryUndo_AfterLineClearingPlacement_ReturnsFalseAndDoesNotRestoreBoard()
    {
        for (int x = 0; x < Constants.GridSize - 1; x++)
        {
            _grid.Board.Place(_singleCellPiece, x, 0, colourId: 0);
        }

        PlaceAt(0, Constants.GridSize - 1, 0);

        bool undone = _controller.TryUndo();

        Assert.IsFalse(undone, "the clear has already happened — that move is permanent per CLAUDE.md §4.5");
    }

    [Test]
    public void CanUndo_AfterAllThreePlacedAndNewHandDealt_IsFalse()
    {
        PlaceAt(0, 0, 0);
        PlaceAt(1, 2, 2);
        PlaceAt(2, 4, 4);

        Assert.IsFalse(_controller.AllPiecesPlaced(), "sanity: a new hand should already have been dealt");
        Assert.IsFalse(_controller.CanUndo);
    }

    [Test]
    public void TryUndo_AfterAllThreePlacedAndNewHandDealt_ReturnsFalseAndLeavesPlacementPermanent()
    {
        PlaceAt(0, 0, 0);
        PlaceAt(1, 2, 2);
        PlaceAt(2, 4, 4);

        bool undone = _controller.TryUndo();

        Assert.IsFalse(undone);
        Assert.IsTrue(_grid.Board.IsFilled(4, 4), "the completing placement should remain permanent");
    }

    [Test]
    public void TryUndo_TwiceInSameHand_SecondUndoIsBlocked()
    {
        PlaceAt(0, 3, 3);
        bool firstUndo = _controller.TryUndo();

        // Place a different piece into the now-reopened slot 0 so there's
        // something new to attempt a second undo against.
        PlaceAt(0, 5, 5);
        bool secondUndo = _controller.TryUndo();

        Assert.IsTrue(firstUndo);
        Assert.IsFalse(secondUndo, "max 1 undo per hand — this hand hasn't been fully replaced yet");
        Assert.IsTrue(_grid.Board.IsFilled(5, 5), "second placement should remain since its undo was blocked");
    }

    [Test]
    public void CanUndo_WhileDragging_IsFalse()
    {
        PlaceAt(0, 3, 3);
        _controller.BeginDrag(1, _tray.Slots[1].transform.position);

        Assert.IsFalse(_controller.CanUndo);
    }

    [Test]
    public void TryUndo_RestoresUndoAvailabilityFalseAfterUse_ButHandStaysConsistent()
    {
        PlaceAt(0, 3, 3);
        _controller.TryUndo();

        Assert.IsFalse(_controller.CanUndo, "undo already used this hand");
        Assert.IsNotNull(_controller.Hand[0], "the undone piece must be back in the hand");
    }
}
