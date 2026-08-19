using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedurally generates the per-block-colour pattern textures CLAUDE.md
/// §8.1/§8.2 call for ("filled: coloured with inner texture pattern") and
/// §3.9's colourblind-accessibility requirement ("pattern opacity
/// 15% -> 40%" in high-contrast mode). No source pattern art exists
/// (Phase 5/8 Figma scope, same as ceramic silhouettes/audio), but unlike
/// those, these five patterns (dots, diagonal lines, crosshatch,
/// horizontal lines, circles) are simple enough flat geometry to generate
/// directly rather than wait on sourced art.
///
/// Every pattern is baked as pure white (foreground mark) against a
/// dimmed white (background) — <see cref="SpriteRenderer.color"/>
/// multiplies a sprite's texture, so tinting with any block colour
/// reproduces that exact colour at full brightness on the pattern marks
/// and a darker shade of the same colour everywhere else, without baking
/// a specific colour into the texture. One texture per pattern+contrast
/// combination is cached and reused by every block that uses it,
/// regardless of colour.
/// </summary>
public static class PatternSprite
{
    public enum Pattern
    {
        Dots,
        DiagonalLines,
        Crosshatch,
        HorizontalLines,
        Circles
    }

    private const int TextureSize = 32;
    private const float PixelsPerUnit = 32f;

    // CLAUDE.md §3.9: "colourblind pattern opacity 15% -> 40%" — the gap
    // between foreground and background brightness at normal vs
    // high-contrast settings.
    private const float NormalContrast = 0.15f;
    private const float HighContrastAmount = 0.40f;

    private static readonly Dictionary<(Pattern, bool), Sprite> Cache = new Dictionary<(Pattern, bool), Sprite>();

    public static Sprite Get(Pattern pattern, bool highContrast)
    {
        var key = (pattern, highContrast);
        if (Cache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }

        float contrast = highContrast ? HighContrastAmount : NormalContrast;
        var background = new Color(1f - contrast, 1f - contrast, 1f - contrast, 1f);

        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Repeat
        };

        var pixels = new Color[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                pixels[(y * TextureSize) + x] = IsForegroundPixel(pattern, x, y) ? Color.white : background;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit);

        Cache[key] = sprite;
        return sprite;
    }

    private static bool IsForegroundPixel(Pattern pattern, int x, int y)
    {
        switch (pattern)
        {
            case Pattern.Dots:
                return IsDot(x, y);
            case Pattern.DiagonalLines:
                return ((x + y) % 8) < 2;
            case Pattern.Crosshatch:
                return (((x + y) % 8) < 2) || (((x - y + TextureSize) % 8) < 2);
            case Pattern.HorizontalLines:
                return (y % 8) < 2;
            case Pattern.Circles:
                return IsCircleRing(x, y);
            default:
                return false;
        }
    }

    // A small filled dot centred in each 8x8 cell of the texture.
    private static bool IsDot(int x, int y)
    {
        const int cellSize = 8;
        float cx = ((x / cellSize) * cellSize) + (cellSize / 2f);
        float cy = ((y / cellSize) * cellSize) + (cellSize / 2f);
        float dx = x - cx;
        float dy = y - cy;
        return ((dx * dx) + (dy * dy)) <= 4f;
    }

    // One centred ring, distinct from Dots' multiple small filled circles.
    private static bool IsCircleRing(int x, int y)
    {
        float dx = x - (TextureSize / 2f) + 0.5f;
        float dy = y - (TextureSize / 2f) + 0.5f;
        float dist = Mathf.Sqrt((dx * dx) + (dy * dy));
        return dist >= 9f && dist <= 12f;
    }
}
