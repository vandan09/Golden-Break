using NUnit.Framework;
using UnityEngine;

public class UiPaletteTests
{
    [TearDown]
    public void ResetStaticState()
    {
        // GetBlockSprite/GetBlockColour read static state — leaving either
        // flipped after a test would leak into whichever test runs next.
        UiPalette.HighContrastEnabled = false;
        UiPalette.SetActive(UiPalette.GoldenDark);
    }

    [Test]
    // Negative ids (obstacles) get the design rounded block, not a flat
    // square — the Claude Design rework replaced the placeholder sprite.
    public void GetBlockSprite_NegativeColourId_ReturnsTheRoundedBlock()
    {
        Assert.AreEqual(BlockCellSprite.GetRoundedBlock(), UiPalette.GetBlockSprite(-1));
    }

    [Test]
    public void GetBlockSprite_ObstacleColourId_ReturnsTheRoundedBlock()
    {
        Assert.AreEqual(BlockCellSprite.GetRoundedBlock(), UiPalette.GetBlockSprite(Constants.ObstacleColourId));
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

    [Test]
    public void Active_DefaultsToGoldenDark()
    {
        Assert.AreSame(UiPalette.GoldenDark, UiPalette.Active);
    }

    [Test]
    public void SetActive_SwapsTheActivePalette()
    {
        UiPalette.SetActive(UiPalette.JadeDusk);

        Assert.AreSame(UiPalette.JadeDusk, UiPalette.Active);
    }

    [Test]
    public void SetActive_Null_FallsBackToGoldenDarkRatherThanLeavingNoPalette()
    {
        UiPalette.SetActive(UiPalette.JadeDusk);

        UiPalette.SetActive(null);

        Assert.AreSame(UiPalette.GoldenDark, UiPalette.Active);
    }

    [Test]
    public void Background_ReadsFromWhicheverPaletteIsActive()
    {
        Assert.AreEqual(UiPalette.GoldenDark.background, UiPalette.Background);

        UiPalette.SetActive(UiPalette.JadeDusk);

        Assert.AreEqual(UiPalette.JadeDusk.background, UiPalette.Background);
    }

    [Test]
    public void GetBlockColour_BothPalettes_KeepTheSameGameplayBlockColours()
    {
        Color goldenDarkCoral = UiPalette.GetBlockColour(0);

        UiPalette.SetActive(UiPalette.JadeDusk);
        Color jadeDuskCoral = UiPalette.GetBlockColour(0);

        Assert.AreEqual(goldenDarkCoral, jadeDuskCoral, "gameplay block colours must stay consistent across themes, only the chrome reskins");
    }

    [Test]
    public void TwoPalettes_HaveDifferentNamesAndAreNotTheSameInstance()
    {
        Assert.AreNotEqual(UiPalette.GoldenDark.paletteName, UiPalette.JadeDusk.paletteName);
        Assert.AreNotSame(UiPalette.GoldenDark, UiPalette.JadeDusk);
    }
}
