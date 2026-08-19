using UnityEngine;

/// <summary>
/// Live colour/sprite lookups for the currently-active
/// <see cref="PaletteDefinition"/> (BUILD_PLAN Phase 5: "Implement
/// palette system as ScriptableObjects (Golden Dark default + 1
/// additional)"). Every property here reads from <see cref="Active"/>,
/// so every existing call site (<c>UiPalette.Background</c>,
/// <c>UiPalette.GetBlockColour(id)</c>, etc.) keeps working completely
/// unchanged while gaining the ability to swap themes underneath them
/// via <see cref="SetActive"/> — deliberately not a wider refactor that
/// would touch every screen file that references this class.
///
/// Both palettes are built as in-memory ScriptableObject instances
/// rather than `.asset` files under Resources — no Editor session is
/// available to author/tweak them visually yet (same "no final art"
/// caveat as every other placeholder system in this project), and this
/// keeps the whole system working identically in EditMode tests and at
/// runtime with no asset-loading dependency.
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

    public static readonly PaletteDefinition GoldenDark = BuildGoldenDark();
    public static readonly PaletteDefinition JadeDusk = BuildJadeDusk();

    private static PaletteDefinition _active = GoldenDark;

    public static PaletteDefinition Active => _active;

    // Falls back to GoldenDark on null rather than throwing — swapping in
    // "no palette" isn't a meaningful state, and a purchase/settings call
    // site passing null by mistake shouldn't leave every screen unable to
    // render instead of just staying on the default theme.
    public static void SetActive(PaletteDefinition palette)
    {
        _active = palette != null ? palette : GoldenDark;
    }

    public static Color EmptyCellFill => _active.emptyCellFill;
    public static Color EmptyCellBorder => _active.emptyCellBorder;
    public static Color BlockObstacle => _active.blockObstacle;
    public static Color TextPrimary => _active.textPrimary;
    public static Color TextSecondary => _active.textSecondary;
    public static Color GoldFill => _active.goldFill;
    public static Color Background => _active.background;
    public static Color Surface => _active.surface;
    public static Color CardBorder => _active.cardBorder;

    public static Color[] BlockColours => new[]
    {
        _active.blockCoral, _active.blockBlue, _active.blockGreen, _active.blockGold, _active.blockPurple
    };

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

        Color[] blockColours = BlockColours;
        return blockColours[colourId % blockColours.Length];
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

    private static PaletteDefinition BuildGoldenDark()
    {
        var palette = ScriptableObject.CreateInstance<PaletteDefinition>();
        palette.paletteName = "Golden Dark";
        palette.background = FromHex("#1a1a2e");
        palette.surface = FromHex("#252545");
        palette.cardBorder = FromHex("#34345a");
        palette.textPrimary = FromHex("#c0c0d8");
        palette.textSecondary = FromHex("#7a7a9a");
        palette.goldFill = FromHex("#e8c060");
        palette.emptyCellFill = FromHex("#1e1e38");
        palette.emptyCellBorder = FromHex("#2a2a4a");
        palette.blockObstacle = FromHex("#4a4a5e");
        palette.blockCoral = FromHex("#e06070");
        palette.blockBlue = FromHex("#60b0e0");
        palette.blockGreen = FromHex("#70d0a0");
        palette.blockGold = FromHex("#e8c060");
        palette.blockPurple = FromHex("#a080d0");
        return palette;
    }

    // The "+1 additional" palette BUILD_PLAN's own task calls for — no
    // second theme is specified anywhere in CLAUDE.md, so this is a
    // documented design choice, not a literal spec reading: a cool jade
    // reskin of the UI chrome only (background/surface/text/accent),
    // deliberately keeping the same 5 gameplay block colours as Golden
    // Dark (piece-colour identity is a legibility concern, not a cosmetic
    // one) rather than inventing a second block palette nothing asked for.
    private static PaletteDefinition BuildJadeDusk()
    {
        var palette = ScriptableObject.CreateInstance<PaletteDefinition>();
        palette.paletteName = "Jade Dusk";
        palette.background = FromHex("#16211e");
        palette.surface = FromHex("#1f2f2a");
        palette.cardBorder = FromHex("#2e453d");
        palette.textPrimary = FromHex("#c4ded2");
        palette.textSecondary = FromHex("#7a9a8a");
        palette.goldFill = FromHex("#6fd9a8");
        palette.emptyCellFill = FromHex("#182620");
        palette.emptyCellBorder = FromHex("#2a3a34");
        palette.blockObstacle = FromHex("#3a4a44");
        palette.blockCoral = FromHex("#e06070");
        palette.blockBlue = FromHex("#60b0e0");
        palette.blockGreen = FromHex("#70d0a0");
        palette.blockGold = FromHex("#e8c060");
        palette.blockPurple = FromHex("#a080d0");
        return palette;
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
