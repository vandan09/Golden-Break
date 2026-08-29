using UnityEngine;

/// <summary>
/// The metal a ceramic's repaired cracks are drawn in.
///
/// CLAUDE.md §3.4: "10+: repeat 6-9 with colour variants". No variant
/// colours are specified anywhere, so these are chosen here: the four
/// metals real kintsugi actually uses — gold, then rose gold, silver and
/// copper. An earlier version rotated the base gold's hue by a fixed step
/// per variant, which sent variants 1-3 through green and cyan; nothing
/// about a green seam reads as a precious-metal repair, and it clashed with
/// the design's warm gold palette everywhere else in the UI.
///
/// Shared because the same colour has to come out of two renderers that
/// must agree pixel for pixel: <see cref="CeramicSilhouetteSprite"/>, which
/// bakes the resting ceramic into a sprite, and CeramicView's LineRenderers,
/// which draw a crack while its metal is flowing in. If these disagree, a
/// crack visibly changes colour the instant its fill animation ends.
/// </summary>
public static class CeramicGold
{
    // Variant 0 is the design's own gold (#e8c060) and is read from the
    // active palette so a theme swap still drives it.
    private static readonly Color[] Variants =
    {
        default,                            // variant 0 - palette gold
        new Color(0.910f, 0.643f, 0.549f),  // #e8a48c rose gold
        new Color(0.804f, 0.824f, 0.878f),  // #cdd2e0 silver
        new Color(0.831f, 0.533f, 0.306f),  // #d4884e copper
    };

    public static int VariantCount => Variants.Length;

    public static Color ForVariant(int colourVariant)
    {
        if (colourVariant <= 0)
        {
            return UiPalette.GoldFill;
        }

        // Tier 10+ cycles forever, so wrap rather than clamp — a player deep
        // into the loop keeps seeing the metals rotate instead of every
        // ceramic beyond variant 3 looking identical.
        int index = colourVariant % Variants.Length;
        return index == 0 ? UiPalette.GoldFill : Variants[index];
    }

    /// <summary>
    /// The halo colour for a repaired crack: the metal lifted toward white
    /// so the glow reads as light coming off it, matching the design's
    /// drop-shadow(0 0 3px rgba(240,216,144,0.8)) on the base gold.
    /// </summary>
    public static Color GlowForVariant(int colourVariant)
    {
        Color metal = ForVariant(colourVariant);
        return new Color(
            Mathf.Lerp(metal.r, 1f, 0.35f),
            Mathf.Lerp(metal.g, 1f, 0.35f),
            Mathf.Lerp(metal.b, 1f, 0.35f));
    }
}
