using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates and caches flat-colour square sprites for placeholder
/// rendering — no final block/cell art exists yet (Phase 5/8). Cached by
/// colour so repeated calls for the same colour never allocate a new
/// texture.
/// </summary>
public static class PlaceholderSprite
{
    private const int TextureSize = 32;
    private const float PixelsPerUnit = 32f;

    private static readonly Dictionary<Color, Sprite> Cache = new Dictionary<Color, Sprite>();

    public static Sprite GetSolid(Color colour)
    {
        if (Cache.TryGetValue(colour, out Sprite cached))
        {
            return cached;
        }

        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color[TextureSize * TextureSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = colour;
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit);

        Cache[colour] = sprite;
        return sprite;
    }
}
