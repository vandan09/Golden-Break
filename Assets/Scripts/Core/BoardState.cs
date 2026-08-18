using System;
using UnityEngine;

/// <summary>
/// The 8×8 grid's cell state and placement rules (CLAUDE.md §3.1). Plain
/// C# — no MonoBehaviour, no scene dependency — so it's directly
/// unit-testable and reusable by both live gameplay and
/// <see cref="GameOverDetector"/>'s what-if checks.
/// </summary>
public sealed class BoardState
{
    public const int EmptyColourId = -1;

    private readonly int[] _cellColourId;

    public BoardState()
    {
        _cellColourId = new int[Constants.GridSize * Constants.GridSize];
        Clear();
    }

    public void Clear()
    {
        for (int i = 0; i < _cellColourId.Length; i++)
        {
            _cellColourId[i] = EmptyColourId;
        }
    }

    public bool IsFilled(int x, int y)
    {
        return GetColourId(x, y) != EmptyColourId;
    }

    public int GetColourId(int x, int y)
    {
        AssertInBounds(x, y);
        return _cellColourId[Index(x, y)];
    }

    public bool CanPlace(PieceDefinition piece, int originX, int originY)
    {
        if (piece == null)
        {
            throw new ArgumentNullException(nameof(piece));
        }

        foreach (Vector2Int cell in piece.cells)
        {
            int x = originX + cell.x;
            int y = originY + cell.y;

            if (x < 0 || x >= Constants.GridSize || y < 0 || y >= Constants.GridSize)
            {
                return false;
            }

            if (IsFilled(x, y))
            {
                return false;
            }
        }

        return true;
    }

    public void Place(PieceDefinition piece, int originX, int originY, int colourId)
    {
        if (!CanPlace(piece, originX, originY))
        {
            throw new InvalidOperationException($"BoardState: cannot place piece '{piece.pieceId}' at ({originX},{originY}).");
        }

        foreach (Vector2Int cell in piece.cells)
        {
            _cellColourId[Index(originX + cell.x, originY + cell.y)] = colourId;
        }
    }

    public void ClearCell(int x, int y)
    {
        AssertInBounds(x, y);
        _cellColourId[Index(x, y)] = EmptyColourId;
    }

    // Undo support (CLAUDE.md §4.5): empties exactly the cells a piece
    // occupies at originX/originY, the exact complement of Place(). Only
    // valid to call for a placement that hasn't triggered a line clear
    // since — the caller (PieceController) enforces that restriction, not
    // this method, since BoardState has no notion of "this game's most
    // recent placement."
    public void RemovePiece(PieceDefinition piece, int originX, int originY)
    {
        if (piece == null)
        {
            throw new ArgumentNullException(nameof(piece));
        }

        foreach (Vector2Int cell in piece.cells)
        {
            ClearCell(originX + cell.x, originY + cell.y);
        }
    }

    // Regular play and a Daily Challenge session (CLAUDE.md §4.2) need to
    // be genuinely independent — switching to one must not silently wipe
    // the other's board. Captures/restores the raw cell contents so
    // PieceController can snapshot a session before switching away and
    // restore it exactly when switching back.
    public int[] SnapshotColourIds()
    {
        var copy = new int[_cellColourId.Length];
        Array.Copy(_cellColourId, copy, _cellColourId.Length);
        return copy;
    }

    public void RestoreColourIds(int[] colourIds)
    {
        if (colourIds == null || colourIds.Length != _cellColourId.Length)
        {
            throw new ArgumentException($"BoardState: snapshot must contain exactly {_cellColourId.Length} cells.", nameof(colourIds));
        }

        Array.Copy(colourIds, _cellColourId, _cellColourId.Length);
    }

    private static void AssertInBounds(int x, int y)
    {
        Debug.Assert(x >= 0 && x < Constants.GridSize && y >= 0 && y < Constants.GridSize,
            $"BoardState: ({x},{y}) is out of the {Constants.GridSize}x{Constants.GridSize} grid.");
    }

    private static int Index(int x, int y)
    {
        return (y * Constants.GridSize) + x;
    }
}
