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
