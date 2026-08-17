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
}
