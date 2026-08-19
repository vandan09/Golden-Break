using NUnit.Framework;
using UnityEngine;

public class PatternSpriteTests
{
    [Test]
    public void Get_SamePatternAndContrast_ReturnsTheSameCachedSprite()
    {
        Sprite first = PatternSprite.Get(PatternSprite.Pattern.Dots, highContrast: false);
        Sprite second = PatternSprite.Get(PatternSprite.Pattern.Dots, highContrast: false);

        Assert.AreSame(first, second);
    }

    [Test]
    public void Get_DifferentPatterns_ReturnDifferentSprites()
    {
        Sprite dots = PatternSprite.Get(PatternSprite.Pattern.Dots, highContrast: false);
        Sprite lines = PatternSprite.Get(PatternSprite.Pattern.DiagonalLines, highContrast: false);

        Assert.AreNotSame(dots, lines);
    }

    [Test]
    public void Get_NormalVsHighContrast_ReturnDifferentSprites()
    {
        Sprite normal = PatternSprite.Get(PatternSprite.Pattern.Crosshatch, highContrast: false);
        Sprite high = PatternSprite.Get(PatternSprite.Pattern.Crosshatch, highContrast: true);

        Assert.AreNotSame(normal, high);
    }

    [Test]
    public void Get_EveryPattern_ContainsBothForegroundAndBackgroundPixels()
    {
        foreach (PatternSprite.Pattern pattern in System.Enum.GetValues(typeof(PatternSprite.Pattern)))
        {
            Sprite sprite = PatternSprite.Get(pattern, highContrast: false);
            Texture2D texture = sprite.texture;
            Color[] pixels = texture.GetPixels();

            bool hasForeground = false;
            bool hasBackground = false;
            foreach (Color pixel in pixels)
            {
                if (pixel == Color.white)
                {
                    hasForeground = true;
                }
                else
                {
                    hasBackground = true;
                }
            }

            Assert.IsTrue(hasForeground, $"{pattern} should have at least one foreground pixel");
            Assert.IsTrue(hasBackground, $"{pattern} should have at least one background pixel");
        }
    }

    [Test]
    public void Get_HighContrast_BackgroundIsDarkerThanNormalContrast()
    {
        Sprite normal = PatternSprite.Get(PatternSprite.Pattern.HorizontalLines, highContrast: false);
        Sprite high = PatternSprite.Get(PatternSprite.Pattern.HorizontalLines, highContrast: true);

        // Sample a pixel known to be background for HorizontalLines (y % 8 >= 2): row 4.
        Color normalBackground = normal.texture.GetPixel(0, 4);
        Color highBackground = high.texture.GetPixel(0, 4);

        Assert.Less(highBackground.r, normalBackground.r, "high-contrast background should be darker (bigger gap from the white foreground)");
    }
}
