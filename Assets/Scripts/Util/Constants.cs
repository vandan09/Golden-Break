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

    // Scoring (CLAUDE.md §3.2)
    public const int PointsPerSingleLine = 10;
    public const int PointsPerDoubleLine = 30;
    public const int PointsPerTripleLine = 60;
    public const int PointsPerQuadLine = 100;
    public const int PointsBaseForFivePlusLines = 150;
    public const int PointsPerAdditionalLineBeyondFive = 50;
    public const float StreakMultiplierStep = 0.5f;
    public const float StreakMultiplierMax = 3f;

    // Clear/combo animation timing (CLAUDE.md §3.8)
    public const float ClearFlashDurationSeconds = 0.1f;
    public const float ClearFadeDurationSeconds = 0.25f;
    public const float ComboScreenShakeDurationSeconds = 0.15f;
    public const float ComboScreenShakeStrength = 0.03f;

    // Kintsugi meta (CLAUDE.md §3.4)
    public const float GoldFlowAnimationSeconds = 0.5f;
    public const float CeramicCompletionPauseSeconds = 1f;
    public const float CeramicCelebrationDurationSeconds = 1.4f;
    public const float CeramicWorldScale = 0.012f;
}
