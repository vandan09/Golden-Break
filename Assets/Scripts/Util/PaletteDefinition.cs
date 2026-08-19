using UnityEngine;

/// <summary>
/// One reskin of Golden Break's UI chrome (CLAUDE.md §8.2's "Golden dark"
/// plus BUILD_PLAN Phase 5's "+1 additional"). A ScriptableObject rather
/// than another static class, per that task's own wording — this is the
/// thing a purchasable <see cref="IapItem.ThemePack"/> would actually
/// swap in via <see cref="UiPalette.SetActive"/>.
///
/// Deliberately does not vary the 5 gameplay block colours between
/// palettes — those are a legibility/gameplay concern (piece colour
/// identity), not cosmetic chrome, so every theme keeps the same block
/// palette CLAUDE.md §8.2 specifies and only reskins background/surface/
/// text/accent.
/// </summary>
[CreateAssetMenu(fileName = "Palette", menuName = "Golden Break/PaletteDefinition")]
public sealed class PaletteDefinition : ScriptableObject
{
    public string paletteName;

    public Color background;
    public Color surface;
    public Color cardBorder;
    public Color textPrimary;
    public Color textSecondary;
    public Color goldFill;
    public Color emptyCellFill;
    public Color emptyCellBorder;
    public Color blockObstacle;

    public Color blockCoral;
    public Color blockBlue;
    public Color blockGreen;
    public Color blockGold;
    public Color blockPurple;
}
