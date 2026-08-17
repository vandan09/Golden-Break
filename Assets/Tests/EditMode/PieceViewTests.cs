using NUnit.Framework;
using UnityEngine;

public class PieceViewTests
{
    private PieceView _pieceView;

    [SetUp]
    public void CreatePieceView()
    {
        var go = new GameObject("PieceViewTestInstance");
        _pieceView = go.AddComponent<PieceView>();
        _pieceView.Initialize();
    }

    [TearDown]
    public void DestroyPieceView()
    {
        if (_pieceView != null)
        {
            Object.DestroyImmediate(_pieceView.gameObject);
        }
    }

    private static PieceDefinition MakePiece(params Vector2Int[] cells)
    {
        var piece = ScriptableObject.CreateInstance<PieceDefinition>();
        piece.pieceId = "test";
        piece.cells = cells;
        piece.spawnWeight = 1;
        return piece;
    }

    [Test]
    public void SetPiece_ThreeCellPiece_CreatesExactlyThreeActiveBlocks()
    {
        PieceDefinition piece = MakePiece(new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1));

        _pieceView.SetPiece(piece, colourId: 0, blockScale: 1f);

        Assert.AreEqual(3, _pieceView.ActiveBlocks.Count);
        Assert.AreSame(piece, _pieceView.CurrentPiece);
    }

    [Test]
    public void SetPiece_SingleCellPiece_IsPositionedAtLocalOrigin()
    {
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));

        _pieceView.SetPiece(single, colourId: 0, blockScale: 1f);

        Vector3 blockPosition = _pieceView.ActiveBlocks[0].transform.localPosition;
        Assert.AreEqual(Vector3.zero, blockPosition);
    }

    [Test]
    public void SetPiece_TwoCellHorizontalPiece_IsCenteredAroundLocalOrigin()
    {
        PieceDefinition horizontalPair = MakePiece(new Vector2Int(0, 0), new Vector2Int(1, 0));

        _pieceView.SetPiece(horizontalPair, colourId: 0, blockScale: 1f);

        float leftX = _pieceView.ActiveBlocks[0].transform.localPosition.x;
        float rightX = _pieceView.ActiveBlocks[1].transform.localPosition.x;

        Assert.AreEqual(0f, leftX + rightX, 0.0001f, "the two blocks should straddle x=0 symmetrically");
        Assert.Less(leftX, rightX);
    }

    [Test]
    public void SetPiece_SmallerBlockScale_ProducesProportionallySmallerSpacing()
    {
        PieceDefinition horizontalPair = MakePiece(new Vector2Int(0, 0), new Vector2Int(1, 0));

        _pieceView.SetPiece(horizontalPair, colourId: 0, blockScale: 1f);
        float fullScaleSpacing = _pieceView.ActiveBlocks[1].transform.localPosition.x - _pieceView.ActiveBlocks[0].transform.localPosition.x;

        _pieceView.SetPiece(horizontalPair, colourId: 0, blockScale: Constants.TrayPieceScale);
        float trayScaleSpacing = _pieceView.ActiveBlocks[1].transform.localPosition.x - _pieceView.ActiveBlocks[0].transform.localPosition.x;

        Assert.AreEqual(fullScaleSpacing * Constants.TrayPieceScale, trayScaleSpacing, 0.0001f);
    }

    [Test]
    public void SetPiece_ReassignedToDifferentPiece_ReturnsOldBlocksAndCreatesNewCount()
    {
        PieceDefinition threeCell = MakePiece(new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1));
        PieceDefinition oneCell = MakePiece(new Vector2Int(0, 0));

        _pieceView.SetPiece(threeCell, colourId: 0, blockScale: 1f);
        _pieceView.SetPiece(oneCell, colourId: 0, blockScale: 1f);

        Assert.AreEqual(1, _pieceView.ActiveBlocks.Count);
        Assert.AreSame(oneCell, _pieceView.CurrentPiece);
    }

    [Test]
    public void SetPiece_NullPiece_ClearsActiveBlocksAndCurrentPiece()
    {
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));
        _pieceView.SetPiece(single, colourId: 0, blockScale: 1f);

        _pieceView.SetPiece(null, colourId: 0, blockScale: 1f);

        Assert.AreEqual(0, _pieceView.ActiveBlocks.Count);
        Assert.IsNull(_pieceView.CurrentPiece);
    }
}
