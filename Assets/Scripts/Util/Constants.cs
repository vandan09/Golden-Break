/// <summary>
/// Every non-zero/non-one literal used in gameplay or meta logic lives here,
/// grouped by the spec section it comes from. Extended phase by phase as
/// each system is implemented — do not hardcode a value in logic that
/// belongs here instead.
/// </summary>
public static class Constants
{
    // Grid (CLAUDE.md §3.1)
    public const int GridSize = 8;

    // Piece hand (CLAUDE.md §3.1)
    public const int PieceHandSize = 3;

    // Grid rendering (CLAUDE.md §7.5, §8.1). No final art yet (Phase 5) —
    // cell size/gap are placeholder-appropriate world-unit values, not
    // pixel-exact to the design mockup.
    public const float CellWorldSize = 1f;
    public const float CellGap = 0.08f;
    public const float TrayPieceScale = 0.7f;
    public const float DragPieceScale = 1f;
}
