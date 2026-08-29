using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates procedural sprites for grid cells and piece blocks matching
/// the Claude Design mockup's exact visual spec:
/// - Empty cells: rounded rect with fill + border, exact colours baked in
/// - Filled cells: rounded rect with pattern + 3D bevel, white-tintable
/// - Plain rounded blocks: for tray/drag pieces, white-tintable
/// </summary>
public static class BlockCellSprite
{
    private const int CellTextureSize = 128;
    private const float CellPixelsPerUnit = 128f;
    private const int BlockTextureSize = 64;
    private const float BlockPixelsPerUnit = 64f;

    private const float CornerRadiusFraction = 0.22f; // 9px/41px from design
    private const float BorderFraction = 0.024f;       // 1px/41px
    private const float BevelHighlightFraction = 0.05f; // ~2px/41px top highlight
    private const float BevelShadowFraction = 0.073f;   // ~3px/41px bottom shadow start
    private const float BevelShadowBlurFraction = 0.146f; // ~6px/41px shadow blur extent
    private const float BevelHighlightStrength = 0.25f;
    private const float BevelShadowStrength = 0.25f;

    private const int PatternRepeat = 16;

    private static Sprite _emptyCell;
    private static Sprite _roundedBlock;
    private static readonly Dictionary<(PatternSprite.Pattern, bool), Sprite> FilledCache =
        new Dictionary<(PatternSprite.Pattern, bool), Sprite>();

    public static Sprite GetEmptyCell()
    {
        if (_emptyCell != null) return _emptyCell;

        int size = CellTextureSize;
        float radius = size * CornerRadiusFraction;
        float border = Mathf.Max(2f, size * BorderFraction);

        ColorUtility.TryParseHtmlString("#1e1e38", out Color fill);
        ColorUtility.TryParseHtmlString("#3a3a5a", out Color borderColor);

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float outerDist = RoundedRectSDF(px, py, size, size, radius);
                float innerDist = RoundedRectSDF(px, py, size - border * 2, size - border * 2, Mathf.Max(0, radius - border), border);

                if (outerDist > 0.5f)
                {
                    pixels[y * size + x] = Color.clear;
                }
                else if (innerDist > 0.5f)
                {
                    float outerAlpha = Mathf.Clamp01(0.5f - outerDist + 1f);
                    Color c = borderColor;
                    c.a = outerAlpha;
                    pixels[y * size + x] = c;
                }
                else
                {
                    pixels[y * size + x] = fill;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        _emptyCell = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), CellPixelsPerUnit);
        return _emptyCell;
    }

    public static Sprite GetFilledCell(PatternSprite.Pattern pattern, bool highContrast)
    {
        var key = (pattern, highContrast);
        if (FilledCache.TryGetValue(key, out Sprite cached)) return cached;

        int size = CellTextureSize;
        float radius = size * CornerRadiusFraction;
        float highlightH = size * BevelHighlightFraction;
        float shadowStart = size * BevelShadowFraction;
        float shadowBlur = size * BevelShadowBlurFraction;
        float contrast = highContrast ? 0.40f : 0.15f;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float dist = RoundedRectSDF(px, py, size, size, radius);

                if (dist > 0.5f)
                {
                    pixels[y * size + x] = Color.clear;
                    continue;
                }

                float alpha = Mathf.Clamp01(0.5f - dist + 1f);
                bool isFg = IsPatternForeground(pattern, x, y);
                float baseBrightness = isFg ? 1f : (1f - contrast);

                // Bevel: top highlight
                float topDist = size - py;
                if (topDist < highlightH)
                {
                    float t = 1f - (topDist / highlightH);
                    baseBrightness = Mathf.Min(1f, baseBrightness + BevelHighlightStrength * t);
                }

                // Bevel: bottom shadow (with blur/gradient)
                if (py < shadowStart + shadowBlur)
                {
                    float shadowDist = (shadowStart + shadowBlur) - py;
                    float t = Mathf.Clamp01(shadowDist / (shadowStart + shadowBlur));
                    baseBrightness *= (1f - BevelShadowStrength * t);
                }

                pixels[y * size + x] = new Color(baseBrightness, baseBrightness, baseBrightness, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), CellPixelsPerUnit);
        FilledCache[key] = sprite;
        return sprite;
    }

    public static Sprite GetRoundedBlock()
    {
        if (_roundedBlock != null) return _roundedBlock;

        int size = BlockTextureSize;
        float radius = size * 0.235f; // 4px/17px from design

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = RoundedRectSDF(x + 0.5f, y + 0.5f, size, size, radius);
                if (dist > 0.5f)
                    pixels[y * size + x] = Color.clear;
                else
                {
                    float a = Mathf.Clamp01(0.5f - dist + 1f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        _roundedBlock = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), BlockPixelsPerUnit);
        return _roundedBlock;
    }

    // Signed distance to a rounded rectangle centered at (w/2, h/2).
    // Negative = inside, positive = outside.
    private static float RoundedRectSDF(float px, float py, float w, float h, float r, float offset = 0f)
    {
        float cx = w * 0.5f + offset;
        float cy = h * 0.5f + offset;
        float hw = w * 0.5f;
        float hh = h * 0.5f;

        float dx = Mathf.Abs(px - cx) - (hw - r);
        float dy = Mathf.Abs(py - cy) - (hh - r);

        float outsideDist = Mathf.Sqrt(Mathf.Max(dx, 0) * Mathf.Max(dx, 0) + Mathf.Max(dy, 0) * Mathf.Max(dy, 0));
        float insideDist = Mathf.Min(Mathf.Max(dx, dy), 0);

        return outsideDist + insideDist - r;
    }

    private static bool IsPatternForeground(PatternSprite.Pattern pattern, int x, int y)
    {
        int px = x % PatternRepeat;
        int py = y % PatternRepeat;

        switch (pattern)
        {
            case PatternSprite.Pattern.Dots:
                float dcx = px - PatternRepeat * 0.5f;
                float dcy = py - PatternRepeat * 0.5f;
                return (dcx * dcx + dcy * dcy) <= 9f;

            case PatternSprite.Pattern.DiagonalLines:
                return ((px + py) % PatternRepeat) < 4;

            case PatternSprite.Pattern.Crosshatch:
                return ((px + py) % PatternRepeat) < 4 || ((px - py + PatternRepeat) % PatternRepeat) < 4;

            case PatternSprite.Pattern.HorizontalLines:
                return py < 4;

            case PatternSprite.Pattern.Circles:
                float ccx = px - PatternRepeat * 0.5f;
                float ccy = py - PatternRepeat * 0.5f;
                float cdist = Mathf.Sqrt(ccx * ccx + ccy * ccy);
                return cdist >= 5f && cdist <= 7f;

            default:
                return false;
        }
    }
}
