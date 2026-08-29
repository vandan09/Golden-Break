using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dashed stroke-only rounded rectangle — the Claude Design Gallery's
/// "next piece" placeholder tile (2px dashed #34345a, 18px radius).
///
/// Deliberately NOT 9-sliced, unlike <see cref="RoundedRectBorderSprite"/>:
/// slicing repeats the middle region, which restarts the dash phase at
/// every slice boundary and reads as an irregular broken line. A single
/// stretched FullRect sprite keeps the dash rhythm continuous; the gallery
/// tile it is used on is close to square (~168x174), so the horizontal and
/// vertical dashes stretch by different amounts by under 5% — not visible.
///
/// Corners are drawn solid. Walking true arc length around a rounded rect
/// to phase dashes through the curve is a lot of machinery for something
/// that reads worse: dashes breaking mid-corner look like rendering errors,
/// whereas solid corners read as a deliberate drawn box.
/// </summary>
public static class DashedRoundedRectSprite
{
    private const int TextureSize = 256;
    private const float PixelsPerUnit = 100f;

    private static readonly Dictionary<(int radius, int stroke, int dash, int gap), Sprite> Cache =
        new Dictionary<(int, int, int, int), Sprite>();

    public static Sprite Get(int cornerRadiusPixels, int strokeWidthPixels, int dashPixels, int gapPixels)
    {
        cornerRadiusPixels = Mathf.Clamp(cornerRadiusPixels, 1, TextureSize / 2);
        strokeWidthPixels = Mathf.Clamp(strokeWidthPixels, 1, cornerRadiusPixels);
        dashPixels = Mathf.Max(1, dashPixels);
        gapPixels = Mathf.Max(1, gapPixels);

        var key = (cornerRadiusPixels, strokeWidthPixels, dashPixels, gapPixels);
        if (Cache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }

        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var pixels = new Color[TextureSize * TextureSize];
        float r = cornerRadiusPixels;
        float period = dashPixels + gapPixels;

        // Same 4x4 supersampled coverage as RoundedRectBorderSprite — a
        // stroke this thin aliases into a broken line once the CanvasScaler
        // rescales it, which would be indistinguishable from the dashes.
        const int supersample = 4;
        float innerLeft = strokeWidthPixels, innerBottom = strokeWidthPixels;
        float innerW = TextureSize - (2 * strokeWidthPixels), innerH = TextureSize - (2 * strokeWidthPixels);
        float innerR = Mathf.Max(0f, r - strokeWidthPixels);

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                int covered = 0;
                for (int sy = 0; sy < supersample; sy++)
                {
                    for (int sx = 0; sx < supersample; sx++)
                    {
                        float px = x + ((sx + 0.5f) / supersample);
                        float py = y + ((sy + 0.5f) / supersample);

                        bool outer = IconShapeMath.IsInsideRoundedRect(
                            new Vector2(px, py), 0f, 0f, TextureSize, TextureSize, r);
                        bool inner = IconShapeMath.IsInsideRoundedRect(
                            new Vector2(px, py), innerLeft, innerBottom, innerW, innerH, innerR);

                        if (!outer || inner)
                        {
                            continue;
                        }

                        if (IsDashOn(px, py, r, period, dashPixels))
                        {
                            covered++;
                        }
                    }
                }

                float coverage = covered / (float)(supersample * supersample);
                pixels[(y * TextureSize) + x] = new Color(1f, 1f, 1f, coverage);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit,
            0,
            SpriteMeshType.FullRect);

        Cache[key] = sprite;
        return sprite;
    }

    // Phase runs along whichever axis the pixel's edge is parallel to.
    // Inside a corner's radius box the pixel belongs to the arc, which is
    // always drawn solid (see the class docstring).
    private static bool IsDashOn(float px, float py, float radius, float period, float dashPixels)
    {
        bool inCornerBand = (px < radius || px > TextureSize - radius)
                            && (py < radius || py > TextureSize - radius);
        if (inCornerBand)
        {
            return true;
        }

        bool horizontalEdge = py < radius || py > TextureSize - radius;
        float distanceAlong = horizontalEdge ? px : py;

        return Mathf.Repeat(distanceAlong, period) < dashPixels;
    }
}
