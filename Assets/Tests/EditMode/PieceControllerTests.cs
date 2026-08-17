using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

public class PieceControllerTests
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
        _controller.Configure(_grid, _tray, spawner);
    }

    [TearDown]
    public void DestroyAll()
    {
        Object.DestroyImmediate(_controller.gameObject);
        Object.DestroyImmediate(_tray.gameObject);
        Object.DestroyImmediate(_grid.gameObject);
    }

    [Test]
    public void Configure_DealsAFullHandIntoTheTray()
    {
        Assert.AreEqual(Constants.PieceHandSize, _controller.Hand.Length);
        for (int i = 0; i < _controller.Hand.Length; i++)
        {
            Assert.AreSame(_singleCellPiece, _controller.Hand[i]);
            Assert.AreSame(_singleCellPiece, _tray.Slots[i].CurrentPiece);
        }
    }

    [Test]
    public void BeginDrag_ValidSlot_SetsIsDraggingAndClearsTraySlot()
    {
        _controller.BeginDrag(0, _tray.Slots[0].transform.position);

        Assert.IsTrue(_controller.IsDragging);
        Assert.IsNull(_tray.Slots[0].CurrentPiece);
    }

    [Test]
    public void BeginDrag_SlotWithNoPiece_DoesNothing()
    {
        Vector3 target = _grid.transform.TransformPoint(GridManager.CellToLocalPosition(0, 0));
        _controller.BeginDrag(0, _tray.Slots[0].transform.position);
        _controller.UpdateDrag(target);
        _controller.EndDrag();
        Assert.IsNull(_controller.Hand[0], "sanity check: slot 0 should now be empty");

        _controller.BeginDrag(0, Vector3.zero);

        Assert.IsFalse(_controller.IsDragging);
    }

    [Test]
    public void BeginDrag_WhileAlreadyDragging_IsIgnored()
    {
        _controller.BeginDrag(0, _tray.Slots[0].transform.position);
        _controller.BeginDrag(1, _tray.Slots[1].transform.position);

        Assert.IsNotNull(_tray.Slots[1].CurrentPiece, "slot 1 should be untouched since a drag was already in progress");
    }

    [Test]
    public void EndDrag_OverEmptyValidCell_PlacesPieceOnBoardAndClearsHandSlot()
    {
        Vector3 targetWorld = _grid.transform.TransformPoint(GridManager.CellToLocalPosition(3, 3));

        _controller.BeginDrag(0, _tray.Slots[0].transform.position);
        _controller.UpdateDrag(targetWorld);
        _controller.EndDrag();

        Assert.IsFalse(_controller.IsDragging);
        Assert.IsTrue(_grid.Board.IsFilled(3, 3));
        Assert.IsNull(_controller.Hand[0]);
    }

    [Test]
    public void EndDrag_OverAlreadyFilledCell_BouncesBackToTrayWithoutMutatingBoard()
    {
        _grid.Board.Place(_singleCellPiece, 3, 3, colourId: 0);
        Vector3 occupiedWorld = _grid.transform.TransformPoint(GridManager.CellToLocalPosition(3, 3));

        _controller.BeginDrag(1, _tray.Slots[1].transform.position);
        _controller.UpdateDrag(occupiedWorld);
        _controller.EndDrag();

        Assert.IsFalse(_controller.IsDragging);
        Assert.AreSame(_singleCellPiece, _controller.Hand[1], "piece should still be in the hand, not consumed");
        Assert.AreSame(_singleCellPiece, _tray.Slots[1].CurrentPiece, "piece should be back in its tray slot");
    }

    [Test]
    public void EndDrag_OffGridEntirely_BouncesBackAsInvalid()
    {
        Vector3 farAway = new Vector3(1000f, 1000f, 0f);

        _controller.BeginDrag(2, _tray.Slots[2].transform.position);
        _controller.UpdateDrag(farAway);
        _controller.EndDrag();

        Assert.AreSame(_singleCellPiece, _controller.Hand[2]);
        Assert.AreSame(_singleCellPiece, _tray.Slots[2].CurrentPiece);
    }

    [Test]
    public void EndDrag_AllThreeHandSlotsPlaced_DealsANewFullHand()
    {
        int[,] targets = { { 0, 0 }, { 2, 2 }, { 4, 4 } };

        for (int i = 0; i < 3; i++)
        {
            Vector3 target = _grid.transform.TransformPoint(GridManager.CellToLocalPosition(targets[i, 0], targets[i, 1]));
            _controller.BeginDrag(i, _tray.Slots[i].transform.position);
            _controller.UpdateDrag(target);
            _controller.EndDrag();
        }

        Assert.IsTrue(_grid.Board.IsFilled(0, 0));
        Assert.IsTrue(_grid.Board.IsFilled(2, 2));
        Assert.IsTrue(_grid.Board.IsFilled(4, 4));

        for (int i = 0; i < Constants.PieceHandSize; i++)
        {
            Assert.IsNotNull(_controller.Hand[i], $"hand[{i}] should have been refilled after all 3 were placed");
        }
    }

    [Test]
    public void TryFindSlotAt_PositionOnASlotWithAPiece_ReturnsThatSlot()
    {
        bool found = _controller.TryFindSlotAt(_tray.Slots[1].transform.position, out int slotIndex);

        Assert.IsTrue(found);
        Assert.AreEqual(1, slotIndex);
    }

    [Test]
    public void TryFindSlotAt_PositionFarFromAnySlot_ReturnsFalse()
    {
        bool found = _controller.TryFindSlotAt(new Vector3(1000f, 1000f, 0f), out _);

        Assert.IsFalse(found);
    }
}
