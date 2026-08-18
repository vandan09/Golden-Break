using NUnit.Framework;
using UnityEngine;

public class BoardStateTests
{
    private static PieceDefinition MakePiece(params Vector2Int[] cells)
    {
        var piece = ScriptableObject.CreateInstance<PieceDefinition>();
        piece.pieceId = "test";
        piece.cells = cells;
        piece.spawnWeight = 1;
        return piece;
    }

    [Test]
    public void CanPlace_EmptyBoardWithinBounds_ReturnsTrue()
    {
        var board = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));

        Assert.IsTrue(board.CanPlace(single, 3, 3));
    }

    [Test]
    public void CanPlace_PieceExtendingPastRightEdge_ReturnsFalse()
    {
        var board = new BoardState();
        PieceDefinition horizontalPair = MakePiece(new Vector2Int(0, 0), new Vector2Int(1, 0));

        Assert.IsFalse(board.CanPlace(horizontalPair, 7, 0));
    }

    [Test]
    public void CanPlace_PieceExtendingPastBottomEdge_ReturnsFalse()
    {
        var board = new BoardState();
        PieceDefinition verticalPair = MakePiece(new Vector2Int(0, 0), new Vector2Int(0, 1));

        Assert.IsFalse(board.CanPlace(verticalPair, 0, 7));
    }

    [Test]
    public void CanPlace_NegativeOrigin_ReturnsFalse()
    {
        var board = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));

        Assert.IsFalse(board.CanPlace(single, -1, 0));
    }

    [Test]
    public void CanPlace_OverlappingAlreadyFilledCell_ReturnsFalse()
    {
        var board = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));
        board.Place(single, 4, 4, colourId: 0);

        Assert.IsFalse(board.CanPlace(single, 4, 4));
    }

    [Test]
    public void Place_ValidPosition_FillsExactlyThePieceCells()
    {
        var board = new BoardState();
        PieceDefinition lShape = MakePiece(new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1));

        board.Place(lShape, 2, 2, colourId: 3);

        Assert.IsTrue(board.IsFilled(2, 2));
        Assert.IsTrue(board.IsFilled(3, 2));
        Assert.IsTrue(board.IsFilled(2, 3));
        Assert.IsFalse(board.IsFilled(3, 3));
        Assert.AreEqual(3, board.GetColourId(2, 2));
    }

    [Test]
    public void Place_InvalidPosition_ThrowsAndDoesNotMutateBoard()
    {
        var board = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));
        board.Place(single, 0, 0, colourId: 1);

        Assert.Throws<System.InvalidOperationException>(() => board.Place(single, 0, 0, colourId: 2));
        Assert.AreEqual(1, board.GetColourId(0, 0));
    }

    [Test]
    public void IsFilled_FreshBoard_AllCellsEmpty()
    {
        var board = new BoardState();

        for (int y = 0; y < Constants.GridSize; y++)
        {
            for (int x = 0; x < Constants.GridSize; x++)
            {
                Assert.IsFalse(board.IsFilled(x, y), $"({x},{y}) should start empty");
            }
        }
    }

    [Test]
    public void Clear_AfterPlacements_ResetsAllCellsToEmpty()
    {
        var board = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));
        board.Place(single, 0, 0, colourId: 1);
        board.Place(single, 5, 5, colourId: 2);

        board.Clear();

        Assert.IsFalse(board.IsFilled(0, 0));
        Assert.IsFalse(board.IsFilled(5, 5));
    }

    [Test]
    public void ClearCell_FilledCell_MakesItEmptyWithoutAffectingNeighbours()
    {
        var board = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));
        board.Place(single, 3, 3, colourId: 2);
        board.Place(single, 4, 3, colourId: 2);

        board.ClearCell(3, 3);

        Assert.IsFalse(board.IsFilled(3, 3));
        Assert.IsTrue(board.IsFilled(4, 3), "neighbouring cell should be untouched");
    }

    [Test]
    public void CanPlace_NullPiece_ThrowsArgumentNullException()
    {
        var board = new BoardState();

        Assert.Throws<System.ArgumentNullException>(() => board.CanPlace(null, 0, 0));
    }

    [Test]
    public void RemovePiece_PreviouslyPlacedPiece_ClearsExactlyItsCells()
    {
        var board = new BoardState();
        PieceDefinition lShape = MakePiece(new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1));
        board.Place(lShape, 2, 2, colourId: 3);

        board.RemovePiece(lShape, 2, 2);

        Assert.IsFalse(board.IsFilled(2, 2));
        Assert.IsFalse(board.IsFilled(3, 2));
        Assert.IsFalse(board.IsFilled(2, 3));
    }

    [Test]
    public void RemovePiece_LeavesUnrelatedCellsUntouched()
    {
        var board = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));
        board.Place(single, 3, 3, colourId: 1);
        board.Place(single, 5, 5, colourId: 2);

        board.RemovePiece(single, 3, 3);

        Assert.IsFalse(board.IsFilled(3, 3));
        Assert.IsTrue(board.IsFilled(5, 5), "unrelated cell should be untouched");
        Assert.AreEqual(2, board.GetColourId(5, 5));
    }

    [Test]
    public void RemovePiece_NullPiece_ThrowsArgumentNullException()
    {
        var board = new BoardState();

        Assert.Throws<System.ArgumentNullException>(() => board.RemovePiece(null, 0, 0));
    }

    [Test]
    public void SnapshotColourIds_ThenRestoreOnADifferentBoard_ReproducesExactState()
    {
        var original = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));
        original.Place(single, 3, 3, colourId: 2);
        original.Place(single, 5, 1, colourId: 4);

        int[] snapshot = original.SnapshotColourIds();

        var restoredInto = new BoardState();
        restoredInto.Place(single, 0, 0, colourId: 9); // pre-existing content should be fully overwritten
        restoredInto.RestoreColourIds(snapshot);

        Assert.IsTrue(restoredInto.IsFilled(3, 3));
        Assert.AreEqual(2, restoredInto.GetColourId(3, 3));
        Assert.IsTrue(restoredInto.IsFilled(5, 1));
        Assert.AreEqual(4, restoredInto.GetColourId(5, 1));
        Assert.IsFalse(restoredInto.IsFilled(0, 0), "restore should fully overwrite, not merge");
    }

    [Test]
    public void SnapshotColourIds_MutatingOriginalAfterward_DoesNotAffectTheSnapshot()
    {
        var board = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));
        board.Place(single, 2, 2, colourId: 1);

        int[] snapshot = board.SnapshotColourIds();
        board.Place(single, 6, 6, colourId: 5);

        var restoredInto = new BoardState();
        restoredInto.RestoreColourIds(snapshot);

        Assert.IsFalse(restoredInto.IsFilled(6, 6), "snapshot must be a copy, not a live view");
    }

    [Test]
    public void RestoreColourIds_WrongLength_ThrowsArgumentException()
    {
        var board = new BoardState();

        Assert.Throws<System.ArgumentException>(() => board.RestoreColourIds(new int[10]));
    }

    [Test]
    public void RestoreColourIds_Null_ThrowsArgumentException()
    {
        var board = new BoardState();

        Assert.Throws<System.ArgumentException>(() => board.RestoreColourIds(null));
    }
}
