using UnityEngine;

/// <summary>
/// Hex colours from CLAUDE.md §8.1/§8.2/§3.9, parsed once and cached.
/// Placeholder-appropriate flat colours — rounded corners and glow are
/// still Phase 5/8 art work, but the colourblind pattern overlays
/// (§3.9) are simple enough flat geometry to generate directly (see
/// <see cref="PatternSprite"/>) rather than wait on sourced art.
/// </summary>
public static class UiPalette
{
    // Set once from the saved setting at startup (GameplayController) and
    // live-updated when the player toggles it in Settings — read by
    // GetBlockSprite so every call site that already asks "what sprite
    // does this block use" picks up the current contrast level for free,
    // without threading an extra parameter through GridManager/PieceView/
    // PieceController's many SetPiece/RefreshCell call sites.
    public static bool HighContrastEnabled { get; set; }

    // CLAUDE.md §8.2's own colour order: coral=dots, blue=diagonal lines,
    // green=crosshatch, gold=horizontal lines, purple=circles — indices
    // line up 1:1 with BlockColours below.
    private static readonly PatternSprite.Pattern[] BlockPatterns =
    {
        PatternSprite.Pattern.Dots,
        PatternSprite.Pattern.DiagonalLines,
        PatternSprite.Pattern.Crosshatch,
        PatternSprite.Pattern.HorizontalLines,
        PatternSprite.Pattern.Circles
    };


    public static readonly Color EmptyCellFill = FromHex("#1e1e38");
    public static readonly Color EmptyCellBorder = FromHex("#2a2a4a");

    public static readonly Color BlockCoral = FromHex("#e06070");
    public static readonly Color BlockBlue = FromHex("#60b0e0");
    public static readonly Color BlockGreen = FromHex("#70d0a0");
    public static readonly Color BlockGold = FromHex("#e8c060");
    public static readonly Color BlockPurple = FromHex("#a080d0");

    public static readonly Color[] BlockColours =
    {
        BlockCoral, BlockBlue, BlockGreen, BlockGold, BlockPurple
    };

    // Daily Challenge's pre-filled obstacle cells (Constants.ObstacleColourId)
    // render as this distinct neutral colour, never one of BlockColours'
    // indices, so an obstacle reads visually different from a piece the
    // player placed.
    public static readonly Color BlockObstacle = FromHex("#4a4a5e");

    public static readonly Color TextPrimary = FromHex("#c0c0d8");
    public static readonly Color TextSecondary = FromHex("#7a7a9a");
    public static readonly Color GoldFill = FromHex("#e8c060");
    public static readonly Color Background = FromHex("#1a1a2e");
    public static readonly Color Surface = FromHex("#252545");

    public static Color GetBlockColour(int colourId)
    {
        if (colourId == Constants.ObstacleColourId)
        {
            return BlockObstacle;
        }

        if (colourId < 0)
        {
            return EmptyCellFill;
        }

        return BlockColours[colourId % BlockColours.Length];
    }

    // Obstacle/empty cells stay flat (no pattern) — the pattern is
    // specifically how a *placed piece* reads as more than just a colour;
    // an obstacle is already visually distinct via BlockObstacle's own
    // colour, and adding a pattern to it too would blur that distinction.
    public static Sprite GetBlockSprite(int colourId)
    {
        if (colourId < 0)
        {
            return PlaceholderSprite.GetSolid(Color.white);
        }

        PatternSprite.Pattern pattern = BlockPatterns[colourId % BlockPatterns.Length];
        return PatternSprite.Get(pattern, HighContrastEnabled);
    }

    private static Color FromHex(string hex)
    {
        if (!ColorUtility.TryParseHtmlString(hex, out Color colour))
        {
            Debug.LogError($"UiPalette: failed to parse hex colour '{hex}', falling back to magenta.");
            return Color.magenta;
        }

        return colour;
    }
}
