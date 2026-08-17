using NUnit.Framework;
using UnityEngine;

public class PlacementSnapperTests
{
    private static PieceDefinition MakePiece(params Vector2Int[] cells)
    {
        var piece = ScriptableObject.CreateInstance<PieceDefinition>();
        piece.pieceId = "test";
        piece.cells = cells;
        return piece;
    }

    [Test]
    public void ComputeNearestOrigin_SingleCellPiece_CenterPositionEqualsOrigin()
    {
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));

        Vector2Int origin = PlacementSnapper.ComputeNearestOrigin(single, new Vector2(3f, 4f));

        Assert.AreEqual(new Vector2Int(3, 4), origin);
    }

    [Test]
    public void ComputeNearestOrigin_TwoCellHorizontalPiece_AccountsForBoundsCenterOffset()
    {
        // Bounds centre for cells (0,0),(1,0) is (0.5, 0) — dragging its
        // centre to grid position (3.5, 4) should land the origin at (3,4).
        PieceDefinition pair = MakePiece(new Vector2Int(0, 0), new Vector2Int(1, 0));

        Vector2Int origin = PlacementSnapper.ComputeNearestOrigin(pair, new Vector2(3.5f, 4f));

        Assert.AreEqual(new Vector2Int(3, 4), origin);
    }

    [Test]
    public void ComputeNearestOrigin_PositionBetweenCells_RoundsToNearest()
    {
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));

        Vector2Int origin = PlacementSnapper.ComputeNearestOrigin(single, new Vector2(3.6f, 4.2f));

        Assert.AreEqual(new Vector2Int(4, 4), origin);
    }

    [Test]
    public void ComputeNearestOrigin_3x3Piece_BoundsCenterIsMiddleCell()
    {
        PieceDefinition threeByThree = MakePiece(
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
            new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
            new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2));

        Vector2Int origin = PlacementSnapper.ComputeNearestOrigin(threeByThree, new Vector2(5f, 5f));

        Assert.AreEqual(new Vector2Int(4, 4), origin);
    }
}
