using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

public class RefreshTests
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
    public void CanRefresh_FreshHandAllUnplaced_IsTrue()
    {
        Assert.IsTrue(_controller.CanRefresh);
    }

    [Test]
    public void TryRefresh_AllThreeUnplaced_DealsANewFullHandAndReturnsTrue()
    {
        bool refreshed = _controller.TryRefresh();

        Assert.IsTrue(refreshed);
        for (int i = 0; i < Constants.PieceHandSize; i++)
        {
            Assert.IsNotNull(_controller.Hand[i], $"hand[{i}] should be populated after refresh");
            Assert.AreSame(_singleCellPiece, _tray.Slots[i].CurrentPiece);
        }
    }

    [Test]
    public void TryRefresh_HandSizeStaysExactlyThree()
    {
        _controller.TryRefresh();

        Assert.AreEqual(Constants.PieceHandSize, _controller.Hand.Length);
    }

    [Test]
    public void CanRefresh_AfterOnePiecePlaced_IsFalse()
    {
        PlaceAt(0, 3, 3);

        Assert.IsFalse(_controller.CanRefresh, "cannot refresh once part of the hand has been placed");
    }

    [Test]
    public void TryRefresh_AfterOnePiecePlaced_ReturnsFalseAndLeavesHandUntouched()
    {
        PlaceAt(0, 3, 3);
        PieceDefinition slot1Before = _controller.Hand[1];
        PieceDefinition slot2Before = _controller.Hand[2];

        bool refreshed = _controller.TryRefresh();

        Assert.IsFalse(refreshed);
        Assert.IsNull(_controller.Hand[0], "placed slot should remain empty, not refreshed");
        Assert.AreSame(slot1Before, _controller.Hand[1]);
        Assert.AreSame(slot2Before, _controller.Hand[2]);
    }

    [Test]
    public void TryRefresh_Twice_SecondRefreshIsBlocked()
    {
        bool firstRefresh = _controller.TryRefresh();
        bool secondRefresh = _controller.TryRefresh();

        Assert.IsTrue(firstRefresh);
        Assert.IsFalse(secondRefresh, "max 1 refresh per hand");
    }

    [Test]
    public void CanRefresh_AfterOneRefreshAlreadyUsed_IsFalse()
    {
        _controller.TryRefresh();

        Assert.IsFalse(_controller.CanRefresh);
    }

    [Test]
    public void TryRefresh_ResetsOnceANewHandCycleBegins()
    {
        _controller.TryRefresh();
        Assert.IsFalse(_controller.CanRefresh, "used up for this hand");

        // Complete the (refreshed) hand normally so a genuinely new
        // hand-cycle begins via DealNewHand.
        PlaceAt(0, 0, 0);
        PlaceAt(1, 2, 2);
        PlaceAt(2, 4, 4);

        Assert.IsTrue(_controller.CanRefresh, "a fresh hand-cycle should restore refresh availability");
    }

    [Test]
    public void CanRefresh_WhileDragging_IsFalse()
    {
        _controller.BeginDrag(0, _tray.Slots[0].transform.position);

        Assert.IsFalse(_controller.CanRefresh);
    }

    [Test]
    public void TryRefresh_DoesNotResetAlreadyUsedUndoWithinSameHand()
    {
        PlaceAt(0, 3, 3);
        _controller.TryUndo(); // hand is all-unplaced again, undo now used this hand

        bool refreshed = _controller.TryRefresh();

        Assert.IsTrue(refreshed, "refresh should be independently available after an undo reopened the hand");
        Assert.IsFalse(_controller.CanUndo, "refresh must not grant a fresh undo within the same hand-cycle");
    }
}
