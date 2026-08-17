using UnityEngine;

/// <summary>
/// Pure math: converts a piece's dragged bounding-box-centre position
/// (continuous grid cell units, same convention as
/// <see cref="GridManager.WorldToContinuousCell"/>) into the nearest
/// integer grid origin for that piece — CLAUDE.md §3.3's "snaps to the
/// nearest valid grid position when the centre of the piece is within the
/// grid bounds." Validity itself is a separate check via
/// <see cref="BoardState.CanPlace"/>; this only does the continuous→
/// discrete rounding.
/// </summary>
public static class PlacementSnapper
{
    public static Vector2Int ComputeNearestOrigin(PieceDefinition piece, Vector2 centerGridPosition)
    {
        Vector2 boundsCenter = PieceView.ComputeCellBoundsCenter(piece.cells);
        int originX = Mathf.RoundToInt(centerGridPosition.x - boundsCenter.x);
        int originY = Mathf.RoundToInt(centerGridPosition.y - boundsCenter.y);
        return new Vector2Int(originX, originY);
    }
}
