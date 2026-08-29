using UnityEngine;

/// <summary>
/// Soft radial falloff, transparent at the edges — the Claude Design
/// mockup's ambient glow behind the ceramic
/// (<c>radial-gradient(circle, rgba(232,192,96,0.09), transparent 70%)</c>).
///
/// The previous attempt at this glow used a flat <see cref="PlaceholderSprite"/>
/// rectangle tinted to 9% alpha. A solid rect has hard edges no matter how
/// low the alpha, so it read as a faint grey box sitting behind the bowl
/// rather than as light — which is exactly why it got removed. The fix is
/// the falloff, not the removal: alpha has to reach zero before the sprite
/// bounds do.
/// </summary>
public static class RadialGlowSprite
{
    private const int Size = 128;

    // Matches the mockup's "transparent 70%" stop: alpha hits zero at 70%
    // of the radius, so the outer 30% of the quad is fully clear.
    private const float FalloffEnd = 0.7f;

    private static Sprite _cached;

    public static Sprite Get()
    {
        if (_cached != null)
        {
            return _cached;
        }

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var pixels = new Color[Size * Size];
        float centre = Size * 0.5f;

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float dx = (x + 0.5f - centre) / centre;
                float dy = (y + 0.5f - centre) / centre;
                float distance = Mathf.Sqrt((dx * dx) + (dy * dy));

                float t = Mathf.Clamp01(distance / FalloffEnd);

                // Smoothstep rather than linear: a linear ramp leaves a
                // visible ring where the gradient meets full transparency.
                float alpha = 1f - (t * t * (3f - (2f * t)));

                pixels[(y * Size) + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        _cached = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        return _cached;
    }
}
