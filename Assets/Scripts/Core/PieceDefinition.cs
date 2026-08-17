using UnityEngine;

/// <summary>
/// One placeable piece shape (CLAUDE.md §7.4). Cells are relative to the
/// piece's own top-left origin (0,0) — same (0,0)=top-left, x=right,
/// y=down convention as the board (§3.3).
/// </summary>
[CreateAssetMenu(fileName = "Piece", menuName = "Golden Break/PieceDefinition")]
public class PieceDefinition : ScriptableObject
{
    public string pieceId;
    public Vector2Int[] cells;
    public int spawnWeight;
}
