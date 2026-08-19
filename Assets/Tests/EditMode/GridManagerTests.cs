using NUnit.Framework;
using UnityEngine;

public class GridManagerTests
{
    private GridManager _gridManager;

    [SetUp]
    public void CreateGridManager()
    {
        var go = new GameObject("GridManagerTestInstance");
        _gridManager = go.AddComponent<GridManager>();
        _gridManager.BuildGrid();
    }

    [TearDown]
    public void DestroyGridManager()
    {
        if (_gridManager != null)
        {
            Object.DestroyImmediate(_gridManager.gameObject);
        }
    }

    [Test]
    public void Awake_CreatesExactly64ChildCellsWithSpriteRenderers()
    {
        Assert.AreEqual(64, _gridManager.transform.childCount);

        for (int i = 0; i < _gridManager.transform.childCount; i++)
        {
            Assert.IsNotNull(_gridManager.transform.GetChild(i).GetComponent<SpriteRenderer>());
        }
    }

    [Test]
    public void Awake_CreatesAFreshEmptyBoard()
    {
        Assert.IsNotNull(_gridManager.Board);
        Assert.IsFalse(_gridManager.Board.IsFilled(0, 0));
    }

    [Test]
    public void CellToLocalPosition_TopLeftCell_HasNegativeXAndPositiveY()
    {
        Vector3 topLeft = GridManager.CellToLocalPosition(0, 0);

        Assert.Less(topLeft.x, 0f);
        Assert.Greater(topLeft.y, 0f);
    }

    [Test]
    public void CellToLocalPosition_BottomRightCell_HasPositiveXAndNegativeY()
    {
        Vector3 bottomRight = GridManager.CellToLocalPosition(Constants.GridSize - 1, Constants.GridSize - 1);

        Assert.Greater(bottomRight.x, 0f);
        Assert.Less(bottomRight.y, 0f);
    }

    [Test]
    public void CellToLocalPosition_AdjacentCellsInSameRow_AreOneCellWorldSizeApart()
    {
        Vector3 cellZero = GridManager.CellToLocalPosition(0, 0);
        Vector3 cellOne = GridManager.CellToLocalPosition(1, 0);

        Assert.AreEqual(Constants.CellWorldSize, cellOne.x - cellZero.x, 0.0001f);
        Assert.AreEqual(cellZero.y, cellOne.y, 0.0001f);
    }

    [Test]
    public void TryWorldToCell_RoundTripsWithCellToLocalPosition_ForEveryCell()
    {
        for (int y = 0; y < Constants.GridSize; y++)
        {
            for (int x = 0; x < Constants.GridSize; x++)
            {
                Vector3 worldPos = _gridManager.transform.TransformPoint(GridManager.CellToLocalPosition(x, y));

                bool inBounds = _gridManager.TryWorldToCell(worldPos, out int roundTrippedX, out int roundTrippedY);

                Assert.IsTrue(inBounds, $"({x},{y}) should round-trip in bounds");
                Assert.AreEqual(x, roundTrippedX);
                Assert.AreEqual(y, roundTrippedY);
            }
        }
    }

    [Test]
    public void TryWorldToCell_FarOutsideGrid_ReturnsFalse()
    {
        bool inBounds = _gridManager.TryWorldToCell(new Vector3(1000f, 1000f, 0f), out _, out _);

        Assert.IsFalse(inBounds);
    }

    [Test]
    public void RefreshCell_AfterPlacingAPiece_UpdatesRendererColourToBlockColour()
    {
        var piece = ScriptableObject.CreateInstance<PieceDefinition>();
        piece.pieceId = "single";
        piece.cells = new[] { new Vector2Int(0, 0) };

        _gridManager.Board.Place(piece, 3, 3, colourId: 0);
        _gridManager.RefreshCell(3, 3);

        SpriteRenderer cellRenderer = _gridManager.transform.Find("Cell_3_3").GetComponent<SpriteRenderer>();
        Assert.AreEqual(UiPalette.GetBlockColour(0), cellRenderer.color);
    }

    // Regression guard for colourId 0 specifically (the first/coral
    // block colour) — 0 is falsy-looking in other languages and a classic
    // off-by-one/truthiness trap, even though C# has no implicit int-to-
    // bool conversion to actually cause one here. Asserts both the colour
    // AND the sprite explicitly, and that the sprite isn't silently the
    // flat empty-cell placeholder.
    [Test]
    public void RefreshCell_ColourIdZero_RendersItsOwnColourAndPatternedSpriteNotTheEmptyPlaceholder()
    {
        var piece = ScriptableObject.CreateInstance<PieceDefinition>();
        piece.pieceId = "single";
        piece.cells = new[] { new Vector2Int(0, 0) };

        _gridManager.Board.Place(piece, 2, 2, colourId: 0);
        _gridManager.RefreshCell(2, 2);

        SpriteRenderer cellRenderer = _gridManager.transform.Find("Cell_2_2").GetComponent<SpriteRenderer>();
        Assert.AreEqual(UiPalette.GetBlockColour(0), cellRenderer.color);
        Assert.AreEqual(UiPalette.GetBlockSprite(0), cellRenderer.sprite);
        Assert.AreNotEqual(PlaceholderSprite.GetSolid(Color.white), cellRenderer.sprite, "colourId 0 must not render as the flat placeholder used for empty cells");
    }

    [Test]
    public void RefreshCell_EmptyCell_RendersEmptyCellFillColour()
    {
        SpriteRenderer cellRenderer = _gridManager.transform.Find("Cell_5_5").GetComponent<SpriteRenderer>();

        Assert.AreEqual(UiPalette.EmptyCellFill, cellRenderer.color);
    }

    [Test]
    public void RefreshCell_AfterPlacingAPiece_UsesThatColourIdsPatternedSprite()
    {
        var piece = ScriptableObject.CreateInstance<PieceDefinition>();
        piece.pieceId = "single";
        piece.cells = new[] { new Vector2Int(0, 0) };

        _gridManager.Board.Place(piece, 4, 4, colourId: 2);
        _gridManager.RefreshCell(4, 4);

        SpriteRenderer cellRenderer = _gridManager.transform.Find("Cell_4_4").GetComponent<SpriteRenderer>();
        Assert.AreEqual(UiPalette.GetBlockSprite(2), cellRenderer.sprite);
    }

    [Test]
    public void RefreshCell_EmptyCell_UsesTheFlatPlaceholderSprite()
    {
        SpriteRenderer cellRenderer = _gridManager.transform.Find("Cell_6_6").GetComponent<SpriteRenderer>();

        Assert.AreEqual(PlaceholderSprite.GetSolid(Color.white), cellRenderer.sprite);
    }
}
