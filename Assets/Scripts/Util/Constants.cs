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
    public const float TrayPieceScale = 0.5f;
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
    // World units per SVG unit for the gameplay ceramic. Raised from 0.012
    // so the vessel and its cracks read clearly on a phone: at the old
    // scale the thinner unrepaired seams were hard to tell apart, which
    // matters because tracking "how many cracks left" is the whole loop.
    //
    // Free to raise because the gameplay camera is width-bound on a
    // portrait phone (the grid.s width sets orthographicSize, ~11.3 versus
    // ~8.9 for height), so the extra vertical extent costs nothing and the
    // grid does not shrink. Crack stroke widths are multiples of this, so
    // the whole piece scales together.
    //
    // The UI previews on Home and in the Gallery are unaffected: those fit
    // the shape to their own container rather than using this scale.
    public const float CeramicWorldScale = 0.017f;

    // Coin economy (CLAUDE.md §4.5)
    public const int CoinsForGameOver = 5;
    public const int CoinsForNewBestBonus = 10;
    public const int CoinsForCeramicCompleted = 25;
    public const int CoinsForDailyChallengeCompletion = 30;
    public const int UndoCostCoins = 50;
    public const int RefreshCostCoins = 75;

    // Undo (50) and refresh (75) are coin-gated per CLAUDE.md §5.1, but a
    // save used to start at 0 coins and the rewarded-ad fallback that is
    // meant to cover a broke player is still a stub (AdManager.ShowRewarded
    // never fires onReward — see its TODO), so a fresh player had two
    // permanently dead buttons. This grant covers roughly three undos or
    // two refreshes up front; after that the §4.5 earn rates (game-over,
    // streak, daily) take over. Revisit once AppLovin is actually wired.
    public const int StartingCoins = 150;

    // DDA save history (CLAUDE.md §3.7)
    public const int DdaLast10ScoresCapacity = 10;

    // Daily Challenge hard mode (CLAUDE.md §4.2, confirmed with the
    // player: a genuinely harder challenge, structurally separate from
    // regular play rather than a reshuffled variant of it). Obstacle
    // cells are marked with this reserved colour id rather than one of
    // UiPalette.BlockColours' indices, so the player can tell "pre-filled
    // obstacle" apart from "a piece I placed" at a glance.
    public const int DailyChallengeObstacleCellCount = 8;
    public const int ObstacleColourId = -2;
    public const int CoinsForDailyPerfectRun = 20;
}
