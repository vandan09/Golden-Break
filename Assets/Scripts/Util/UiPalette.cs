using UnityEngine;

/// <summary>
/// Hex colours from CLAUDE.md §8.1/§8.2/§3.9, parsed once and cached.
/// Placeholder-appropriate flat colours — rounded corners, glow, and
/// colourblind pattern overlays are explicitly Phase 5/8 art work.
/// </summary>
public static class UiPalette
{
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
