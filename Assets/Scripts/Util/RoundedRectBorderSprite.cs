using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stroke-only rounded-rectangle sprite (a hollow ring, not a filled
/// shape) — used as a child overlay Image for card/button borders instead
/// of <see cref="UnityEngine.UI.Outline"/>, whose 4-direction shadow trick
/// reads as a dashed/segmented line at the sizes this UI renders borders
/// at (visible on Home screen's gold Play-button border). 9-sliced the
/// same way as <see cref="RoundedRectSprite"/>.
/// </summary>
public static class RoundedRectBorderSprite
{
    private const int TextureSize = 256;
    private const float PixelsPerUnit = 100f;

    private static readonly Dictionary<(int radius, int stroke), Sprite> Cache = new Dictionary<(int, int), Sprite>();

    public static Sprite Get(int cornerRadiusPixels, int strokeWidthPixels)
    {
        cornerRadiusPixels = Mathf.Clamp(cornerRadiusPixels, 1, TextureSize / 2);
        strokeWidthPixels = Mathf.Clamp(strokeWidthPixels, 1, cornerRadiusPixels);
        var key = (cornerRadiusPixels, strokeWidthPixels);

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

        // 4x4 supersampled coverage per pixel, not a hard in/out test —
        // a 1-bit-per-pixel stroke this thin (a few texture px) aliases
        // badly once the sprite is scaled up/down by the CanvasScaler's
        // reference-resolution ratio, which read as a dashed/broken line
        // rather than a clean border (confirmed via an Editor Play Mode
        // screenshot before this was added).
        const int supersample = 4;
        float outerLeft = 0f, outerBottom = 0f, outerW = TextureSize, outerH = TextureSize;
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
                        bool outer = IconShapeMath.IsInsideRoundedRect(new Vector2(px, py), outerLeft, outerBottom, outerW, outerH, r);
                        bool inner = IconShapeMath.IsInsideRoundedRect(new Vector2(px, py), innerLeft, innerBottom, innerW, innerH, innerR);
                        if (outer && !inner)
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

        var border = new Vector4(r, r, r, r);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit,
            0,
            SpriteMeshType.FullRect,
            border);

        Cache[key] = sprite;
        return sprite;
    }
}
