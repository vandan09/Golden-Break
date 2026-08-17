using System.Collections.Generic;

/// <summary>
/// Dynamic difficulty adjustment (CLAUDE.md §3.7): biases piece-spawn
/// weights toward smaller pieces when the player is struggling, larger
/// pieces when they're doing well, capped at ±15%.
///
/// CLAUDE.md's own wording ("if the average is in the bottom/top quartile")
/// implies comparing against a population-wide score distribution, which
/// this offline-first solo project has no backend to compute — there is no
/// live analytics service supplying global quartiles. Approximated instead
/// as the player's own last-10 average versus their own lifetime average,
/// the only distribution actually available on-device. Documented as an
/// interpretation in PROGRESS.md, not a literal reading of "quartile."
/// </summary>
public static class DDAManager
{
    public const float MaxAdjustment = 0.15f;

    private const float StrugglingRatio = 0.75f;
    private const float ThrivingRatio = 1.25f;

    public enum PieceSizeTier
    {
        Small,
        Large
    }

    public enum DdaState
    {
        Struggling,
        Normal,
        Thriving
    }

    // CLAUDE.md §3.7 names only a few examples per tier ("smaller pieces
    // (1x2, 2x1, single, 2x2)... larger pieces (3x3, 1x5, 5x1)"). Extended
    // to the full 20-piece set along the same line as the base weight
    // table's own natural grouping (weight 12/10 tier = small, weight
    // 8/6/5/3 tier = large) — an interpretation, not spec'd exhaustively.
    private static readonly HashSet<string> SmallPieceIds = new HashSet<string>
    {
        "single", "1x2", "2x1", "2x2", "L", "J", "S", "Z", "T"
    };

    public static PieceSizeTier GetSizeTier(string pieceId)
    {
        return SmallPieceIds.Contains(pieceId) ? PieceSizeTier.Small : PieceSizeTier.Large;
    }

    public static DdaState Classify(float last10Average, float lifetimeAverage)
    {
        if (lifetimeAverage <= 0f)
        {
            return DdaState.Normal;
        }

        float ratio = last10Average / lifetimeAverage;
        if (ratio < StrugglingRatio)
        {
            return DdaState.Struggling;
        }

        if (ratio > ThrivingRatio)
        {
            return DdaState.Thriving;
        }

        return DdaState.Normal;
    }

    public static float GetWeightMultiplier(PieceSizeTier tier, DdaState state)
    {
        if (state == DdaState.Struggling)
        {
            return tier == PieceSizeTier.Small ? 1f + MaxAdjustment : 1f - MaxAdjustment;
        }

        if (state == DdaState.Thriving)
        {
            return tier == PieceSizeTier.Large ? 1f + MaxAdjustment : 1f - MaxAdjustment;
        }

        return 1f;
    }

    public static float GetWeightMultiplier(PieceDefinition piece, float last10Average, float lifetimeAverage)
    {
        DdaState state = Classify(last10Average, lifetimeAverage);
        PieceSizeTier tier = GetSizeTier(piece.pieceId);
        return GetWeightMultiplier(tier, state);
    }
}
