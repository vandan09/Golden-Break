using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural white rounded-rectangle sprite, 9-sliced so any button/card
/// can stretch to its own size without distorting the corners — the shape
/// every card/button in the Claude Design mockup uses (CLAUDE.md's own
/// placeholder rendering was flat rectangles). Tint via
/// <c>Image.color</c>/<c>SpriteRenderer.color</c>, same "white texture,
/// coloured by the multiply-tint" convention as
/// <see cref="PlaceholderSprite"/>/<see cref="PatternSprite"/>.
/// </summary>
public static class RoundedRectSprite
{
    private const int TextureSize = 256;
    private const float PixelsPerUnit = 100f;

    private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

    // cornerRadiusPixels is expressed against the fixed texture size, then
    // 9-sliced — a caller stretching this to e.g. a 320x70 button gets
    // that same visual corner radius regardless of the button's own size,
    // which is what "rounded corners" means for a resizable UI element.
    // TextureSize needs to stay comfortably larger than 2x the biggest
    // corner radius any caller passes — too little stretchable middle
    // strip between the two rounded ends produces visible stretch
    // artifacts (confirmed via an Editor Play Mode screenshot at 64px).
    public static Sprite Get(int cornerRadiusPixels)
    {
        cornerRadiusPixels = Mathf.Clamp(cornerRadiusPixels, 1, TextureSize / 2);

        if (Cache.TryGetValue(cornerRadiusPixels, out Sprite cached))
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

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                pixels[(y * TextureSize) + x] = IsInsideRoundedRect(x + 0.5f, y + 0.5f, r) ? Color.white : Color.clear;
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

        Cache[cornerRadiusPixels] = sprite;
        return sprite;
    }

    private static bool IsInsideRoundedRect(float x, float y, float r)
    {
        float left = r, right = TextureSize - r, bottom = r, top = TextureSize - r;

        float cx = x < left ? left : (x > right ? right : x);
        float cy = y < bottom ? bottom : (y > top ? top : y);

        // Inside the plus-shaped "not a corner" region: always inside.
        if (x >= left && x <= right)
        {
            return true;
        }

        if (y >= bottom && y <= top)
        {
            return true;
        }

        float dx = x - cx;
        float dy = y - cy;
        return ((dx * dx) + (dy * dy)) <= (r * r);
    }
}
