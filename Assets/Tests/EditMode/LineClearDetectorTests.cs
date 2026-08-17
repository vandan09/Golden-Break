using NUnit.Framework;
using UnityEngine;

public class LineClearDetectorTests
{
    private BoardState _board;
    private PieceDefinition _single;

    [SetUp]
    public void CreateBoard()
    {
        _board = new BoardState();
        _single = ScriptableObject.CreateInstance<PieceDefinition>();
        _single.pieceId = "single";
        _single.cells = new[] { new Vector2Int(0, 0) };
    }

    private void FillRow(int y, int skipX = -1)
    {
        for (int x = 0; x < Constants.GridSize; x++)
        {
            if (x != skipX)
            {
                _board.Place(_single, x, y, colourId: 0);
            }
        }
    }

    private void FillColumn(int x, int skipY = -1)
    {
        for (int y = 0; y < Constants.GridSize; y++)
        {
            if (y != skipY)
            {
                _board.Place(_single, x, y, colourId: 0);
            }
        }
    }

    [Test]
    public void DetectAndClear_OneFullRow_ClearsItAndReportsIt()
    {
        FillRow(3);

        LineClearDetector.ClearResult result = LineClearDetector.DetectAndClear(_board);

        Assert.AreEqual(1, result.TotalLinesCleared);
        Assert.Contains(3, (System.Collections.ICollection)result.ClearedRows);
        for (int x = 0; x < Constants.GridSize; x++)
        {
            Assert.IsFalse(_board.IsFilled(x, 3));
        }
    }

    [Test]
    public void DetectAndClear_OneFullColumn_ClearsItAndReportsIt()
    {
        FillColumn(5);

        LineClearDetector.ClearResult result = LineClearDetector.DetectAndClear(_board);

        Assert.AreEqual(1, result.TotalLinesCleared);
        Assert.Contains(5, (System.Collections.ICollection)result.ClearedColumns);
        for (int y = 0; y < Constants.GridSize; y++)
        {
            Assert.IsFalse(_board.IsFilled(5, y));
        }
    }

    [Test]
    public void DetectAndClear_RowMissingOneCell_DoesNotClear()
    {
        FillRow(2, skipX: 4);

        LineClearDetector.ClearResult result = LineClearDetector.DetectAndClear(_board);

        Assert.IsFalse(result.AnyCleared);
        Assert.IsTrue(_board.IsFilled(0, 2), "board should be untouched");
    }

    [Test]
    public void DetectAndClear_RowAndColumnComboSimultaneously_ClearsBoth()
    {
        FillRow(0);
        FillColumn(0, skipY: 0); // (0,0) is the shared intersection cell, already placed by FillRow

        LineClearDetector.ClearResult result = LineClearDetector.DetectAndClear(_board);

        Assert.AreEqual(2, result.TotalLinesCleared);
        Assert.AreEqual(1, result.ClearedRows.Count);
        Assert.AreEqual(1, result.ClearedColumns.Count);
        for (int x = 0; x < Constants.GridSize; x++)
        {
            Assert.IsFalse(_board.IsFilled(x, 0));
        }
        for (int y = 0; y < Constants.GridSize; y++)
        {
            Assert.IsFalse(_board.IsFilled(0, y));
        }
    }

    [Test]
    public void DetectAndClear_IntersectionCellOfComboClear_EndsUpEmptyWithoutError()
    {
        FillRow(4);
        FillColumn(4, skipY: 4);

        Assert.DoesNotThrow(() => LineClearDetector.DetectAndClear(_board));
        Assert.IsFalse(_board.IsFilled(4, 4));
    }

    [Test]
    public void DetectAndClear_CellsOutsideClearedLines_StayInPlace_BlocksDoNotFall()
    {
        FillRow(6);
        _board.Place(_single, 2, 2, colourId: 3);
        _board.Place(_single, 7, 5, colourId: 4);

        LineClearDetector.DetectAndClear(_board);

        Assert.IsTrue(_board.IsFilled(2, 2), "untouched cell should remain exactly where it was");
        Assert.AreEqual(3, _board.GetColourId(2, 2));
        Assert.IsTrue(_board.IsFilled(7, 5));
        Assert.AreEqual(4, _board.GetColourId(7, 5));
    }

    [Test]
    public void DetectAndClear_EmptyBoard_ReportsNoClearsAndDoesNotThrow()
    {
        LineClearDetector.ClearResult result = default;

        Assert.DoesNotThrow(() => result = LineClearDetector.DetectAndClear(_board));
        Assert.IsFalse(result.AnyCleared);
        Assert.AreEqual(0, result.TotalLinesCleared);
    }

    [Test]
    public void DetectAndClear_MultipleFullRowsAtOnce_ClearsAllOfThem()
    {
        FillRow(1);
        FillRow(3);
        FillRow(6);

        LineClearDetector.ClearResult result = LineClearDetector.DetectAndClear(_board);

        Assert.AreEqual(3, result.TotalLinesCleared);
        Assert.AreEqual(3, result.ClearedRows.Count);
    }
}
