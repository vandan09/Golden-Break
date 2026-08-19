using NUnit.Framework;
using UnityEngine;

public class UiPaletteTests
{
    [TearDown]
    public void ResetHighContrast()
    {
        // GetBlockSprite reads a static flag — leaving it flipped after a
        // high-contrast test would leak into whichever test runs next.
        UiPalette.HighContrastEnabled = false;
    }

    [Test]
    public void GetBlockSprite_NegativeColourId_ReturnsFlatPlaceholderSprite()
    {
        Assert.AreEqual(PlaceholderSprite.GetSolid(Color.white), UiPalette.GetBlockSprite(-1));
    }

    [Test]
    public void GetBlockSprite_ObstacleColourId_ReturnsFlatPlaceholderSprite()
    {
        Assert.AreEqual(PlaceholderSprite.GetSolid(Color.white), UiPalette.GetBlockSprite(Constants.ObstacleColourId));
    }

    [Test]
    public void GetBlockSprite_ValidColourId_ReturnsAPatternedSprite()
    {
        Sprite sprite = UiPalette.GetBlockSprite(0);

        Assert.AreEqual(PatternSprite.Get(PatternSprite.Pattern.Dots, false), sprite);
    }

    [Test]
    public void GetBlockSprite_ColourIdWrapsLikeGetBlockColourDoes()
    {
        Sprite atZero = UiPalette.GetBlockSprite(0);
        Sprite atPoolLength = UiPalette.GetBlockSprite(UiPalette.BlockColours.Length);

        Assert.AreEqual(atZero, atPoolLength);
    }

    [Test]
    public void GetBlockSprite_HighContrastEnabled_ReturnsTheHighContrastVariant()
    {
        UiPalette.HighContrastEnabled = true;

        Sprite sprite = UiPalette.GetBlockSprite(0);

        Assert.AreEqual(PatternSprite.Get(PatternSprite.Pattern.Dots, true), sprite);
    }
}
