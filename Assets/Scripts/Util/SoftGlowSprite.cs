using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A rounded rectangle that fades outward to nothing — the sprite stand-in
/// for CSS <c>box-shadow: 0 0 Npx rgba(...)</c>, which uGUI has no
/// equivalent for. Sits behind gold-bordered buttons so they carry the same
/// wash of their own colour the Claude Design mockup gives them
/// (<c>0 0 24px rgba(232,192,96,0.22)</c> on the Play button).
///
/// 9-sliced with a border of the full <c>radius + blur</c>, so the corners
/// always carry the complete falloff however wide the button is stretched.
///
/// Deliberately not <see cref="RadialGlowSprite"/>: a circular falloff
/// stretched over a wide pill bunches light at the ends and leaves the long
/// edges dim. This follows the rounded-rect outline, so the glow stays even
/// the whole way around.
/// </summary>
public static class SoftGlowSprite
{
    private const float PixelsPerUnit = 100f;

    private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

    /// <param name="cornerRadiusPixels">Radius of the solid core shape.</param>
    /// <param name="blurRadius">How far the falloff reaches past that shape.</param>
    public static Sprite Get(int cornerRadiusPixels, int blurRadius = 16)
    {
        cornerRadiusPixels = Mathf.Max(0, cornerRadiusPixels);
        blurRadius = Mathf.Max(1, blurRadius);

        int key = (cornerRadiusPixels * 1000) + blurRadius;
        if (Cache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }

        // The texture is sized from the request rather than fixed, so the
        // 9-slice border can never exceed half the texture — the previous
        // fixed 128px version silently produced invalid borders once
        // radius + blur passed 64.
        int border = cornerRadiusPixels + blurRadius;
        int size = (border * 2) + 2;

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var pixels = new Color[size * size];

        // The solid core is the texture inset by blurRadius on every side.
        // Without that inset there is nowhere for the straight edges to fade
        // into, and the sprite comes out opaque to its own border.
        float half = size * 0.5f;
        // The ramp now reaches a blur-width INSIDE the shape, so the solid
        // core must start 2*blur in from the texture edge for the 9-slice
        // centre to stay fully opaque.
        float coreHalf = half - blurRadius;
        float straightHalf = coreHalf - cornerRadiusPixels;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - half) - straightHalf);
                float dy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - half) - straightHalf);
                float distance = Mathf.Sqrt((dx * dx) + (dy * dy)) - cornerRadiusPixels;

                // Centre the ramp ON the shape edge, the way a real gaussian
                // blur of a hard edge behaves: fully opaque a blur-width
                // inside, ~0.5 exactly at the edge, zero a blur-width out.
                //
                // Previously alpha was 1.0 everywhere at or inside the edge,
                // so the ring just outside a button started at full strength
                // and the interior filled as a solid lit box -- a hard bright
                // band hugging the border rather than light blending into the
                // background.
                float t = Mathf.Clamp01((distance + blurRadius) / (2f * blurRadius));

                // Plain smoothstep, NOT squared. Squaring collapses the mid
                // and low values, which shortens the tail and makes the glow
                // stop against the background instead of dissolving into it.
                // Smoothstep is flat at both ends, so the outer edge reaches
                // zero with no detectable seam.
                float falloff = 1f - (t * t * (3f - (2f * t)));
                pixels[(y * size) + x] = new Color(1f, 1f, 1f, falloff);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit,
            0,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));

        Cache[key] = sprite;
        return sprite;
    }
}
