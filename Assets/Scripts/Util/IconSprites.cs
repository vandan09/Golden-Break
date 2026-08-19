using UnityEngine;

/// <summary>
/// Small procedural icon glyphs for the Claude Design UI rework (Home
/// screen's play/gallery/settings icons) — same "generate a white shape
/// once, cache it, tint via Image.color" convention as
/// <see cref="PlaceholderSprite"/>/<see cref="RoundedRectSprite"/>, since
/// no icon art assets exist (Phase 5/8 scope, same gap as every other
/// placeholder in this build).
/// </summary>
public static class TriangleSprite
{
    private const int Size = 32;
    private static Sprite _cached;

    public static Sprite Get()
    {
        if (_cached != null)
        {
            return _cached;
        }

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var pixels = new Color[Size * Size];

        // Right-pointing triangle: base on the left edge, apex at the
        // right-middle — matches the mockup's CSS border-trick play icon.
        Vector2 top = new Vector2(0, Size);
        Vector2 bottom = new Vector2(0, 0);
        Vector2 apex = new Vector2(Size, Size * 0.5f);

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                pixels[(y * Size) + x] = IsInsideTriangle(p, top, bottom, apex) ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        _cached = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        return _cached;
    }

    private static bool IsInsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Cross(p - a, b - a);
        float d2 = Cross(p - b, c - b);
        float d3 = Cross(p - c, a - c);
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }

    private static float Cross(Vector2 a, Vector2 b) => (a.x * b.y) - (a.y * b.x);
}

/// <summary>
/// Two overlapping rounded-square outlines (a simplified stand-in for the
/// mockup's exact icon, which relies on one square's fill matching a
/// specific parent background colour to fake a "cutout" — that trick
/// doesn't generalize across this icon's different real parents, e.g. the
/// Surface-coloured Gallery button vs a Gallery-screen header, so both
/// squares are outline-only here instead).
/// </summary>
public static class GalleryIconSprite
{
    private const int Size = 32;
    private const float StrokeWidth = 2.4f;
    private static Sprite _cached;

    public static Sprite Get()
    {
        if (_cached != null)
        {
            return _cached;
        }

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var pixels = new Color[Size * Size];

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                bool onBack = IsOnRoundedRectStroke(p, 2f, 6f, 18f, 18f, 4f, StrokeWidth);
                bool onFront = IsOnRoundedRectStroke(p, 10f, 10f, 18f, 18f, 4f, StrokeWidth);
                pixels[(y * Size) + x] = (onBack || onFront) ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        _cached = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        return _cached;
    }

    private static bool IsOnRoundedRectStroke(Vector2 p, float left, float bottom, float width, float height, float radius, float stroke)
    {
        bool outer = IconShapeMath.IsInsideRoundedRect(p, left, bottom, width, height, radius);
        bool inner = IconShapeMath.IsInsideRoundedRect(p, left + stroke, bottom + stroke, width - (2 * stroke), height - (2 * stroke), Mathf.Max(0f, radius - stroke));
        return outer && !inner;
    }
}

/// <summary>Sun/settings glyph — a ring plus 8 short radiating spokes.</summary>
public static class GearIconSprite
{
    private const int Size = 32;
    private static Sprite _cached;

    public static Sprite Get()
    {
        if (_cached != null)
        {
            return _cached;
        }

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var pixels = new Color[Size * Size];
        Vector2 centre = new Vector2(Size * 0.5f, Size * 0.5f);
        const float ringRadius = 7f;
        const float strokeWidth = 2.4f;
        const float spokeInner = 10f;
        const float spokeOuter = 15f;

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float dist = Vector2.Distance(p, centre);
                bool onRing = Mathf.Abs(dist - ringRadius) <= strokeWidth * 0.5f;
                bool onSpoke = IsOnAnySpoke(p, centre, spokeInner, spokeOuter, strokeWidth);
                pixels[(y * Size) + x] = (onRing || onSpoke) ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        _cached = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        return _cached;
    }

    private static bool IsOnAnySpoke(Vector2 p, Vector2 centre, float innerR, float outerR, float stroke)
    {
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 a = centre + (dir * innerR);
            Vector2 b = centre + (dir * outerR);
            if (DistanceToSegment(p, a, b) <= stroke * 0.5f)
            {
                return true;
            }
        }

        return false;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
        Vector2 closest = a + (t * ab);
        return Vector2.Distance(p, closest);
    }
}

/// <summary>
/// Small flame glyph — Unity's legacy uGUI Text has no colour-emoji glyph
/// support, so the mockup's "🔥 {streak}" badge renders as just the
/// number with a real emoji character; this draws a simple flame shape
/// instead so the badge still reads as "streak," not a bare integer.
/// </summary>
public static class FlameIconSprite
{
    private const int Size = 24;
    private static Sprite _cached;

    public static Sprite Get()
    {
        if (_cached != null)
        {
            return _cached;
        }

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var pixels = new Color[Size * Size];

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / Size, (y + 0.5f) / Size);
                pixels[(y * Size) + x] = IsInsideFlame(p) ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        _cached = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        return _cached;
    }

    // Two stacked, offset teardrop-ish blobs (outer flame body + a
    // slightly inset, higher inner core) approximated with circles —
    // simple enough to rasterize exactly, reads as a flame silhouette at
    // this icon's small display size.
    private static bool IsInsideFlame(Vector2 uv)
    {
        Vector2 outerCentre = new Vector2(0.5f, 0.42f);
        bool inOuter = Vector2.Distance(uv, outerCentre) <= 0.40f && uv.y <= 0.85f;
        bool inTip = uv.y > 0.72f && Mathf.Abs(uv.x - 0.5f) <= (0.95f - uv.y) * 0.7f;

        return inOuter || inTip;
    }
}

internal static class IconShapeMath
{
    public static bool IsInsideRoundedRect(Vector2 p, float left, float bottom, float width, float height, float radius)
    {
        float right = left + width;
        float top = bottom + height;

        if (p.x < left || p.x > right || p.y < bottom || p.y > top)
        {
            return false;
        }

        float innerLeft = left + radius, innerRight = right - radius;
        float innerBottom = bottom + radius, innerTop = top - radius;

        if (p.x >= innerLeft && p.x <= innerRight)
        {
            return true;
        }

        if (p.y >= innerBottom && p.y <= innerTop)
        {
            return true;
        }

        float cx = p.x < innerLeft ? innerLeft : innerRight;
        float cy = p.y < innerBottom ? innerBottom : innerTop;
        float dx = p.x - cx, dy = p.y - cy;
        return ((dx * dx) + (dy * dy)) <= (radius * radius);
    }
}
