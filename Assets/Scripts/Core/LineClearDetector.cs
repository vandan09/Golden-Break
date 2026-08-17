using System.Collections.Generic;

/// <summary>
/// Detects and clears full rows/columns after a placement (CLAUDE.md
/// §3.1). Blocks never fall — clearing only empties the exact cells in a
/// completed line, everything else stays exactly where it was. Detection
/// happens entirely against the board's state before any clearing begins,
/// so clearing one line can never affect whether another line was counted
/// as full (clearing only empties cells, so it can only ever make a line
/// *less* full, never more).
/// </summary>
public static class LineClearDetector
{
    public readonly struct ClearResult
    {
        public readonly IReadOnlyList<int> ClearedRows;
        public readonly IReadOnlyList<int> ClearedColumns;

        public ClearResult(IReadOnlyList<int> clearedRows, IReadOnlyList<int> clearedColumns)
        {
            ClearedRows = clearedRows;
            ClearedColumns = clearedColumns;
        }

        public int TotalLinesCleared => ClearedRows.Count + ClearedColumns.Count;
        public bool AnyCleared => TotalLinesCleared > 0;
    }

    public static ClearResult DetectAndClear(BoardState board)
    {
        var clearedRows = new List<int>();
        var clearedColumns = new List<int>();

        for (int y = 0; y < Constants.GridSize; y++)
        {
            if (IsRowFull(board, y))
            {
                clearedRows.Add(y);
            }
        }

        for (int x = 0; x < Constants.GridSize; x++)
        {
            if (IsColumnFull(board, x))
            {
                clearedColumns.Add(x);
            }
        }

        foreach (int y in clearedRows)
        {
            for (int x = 0; x < Constants.GridSize; x++)
            {
                board.ClearCell(x, y);
            }
        }

        foreach (int x in clearedColumns)
        {
            for (int y = 0; y < Constants.GridSize; y++)
            {
                board.ClearCell(x, y);
            }
        }

        return new ClearResult(clearedRows, clearedColumns);
    }

    private static bool IsRowFull(BoardState board, int y)
    {
        for (int x = 0; x < Constants.GridSize; x++)
        {
            if (!board.IsFilled(x, y))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsColumnFull(BoardState board, int x)
    {
        for (int y = 0; y < Constants.GridSize; y++)
        {
            if (!board.IsFilled(x, y))
            {
                return false;
            }
        }

        return true;
    }
}
