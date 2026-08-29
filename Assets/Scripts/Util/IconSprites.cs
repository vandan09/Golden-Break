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

/// <summary>Left-pointing chevron (back-navigation glyph, e.g. Gallery's header).</summary>
public static class ChevronLeftSprite
{
    private const int Size = 32;
    private const float StrokeWidth = 3f;
    private static Sprite _cached;

    public static Sprite Get()
    {
        if (_cached != null)
        {
            return _cached;
        }

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var pixels = new Color[Size * Size];

        Vector2 top = new Vector2(20, 8);
        Vector2 mid = new Vector2(11, 16);
        Vector2 bottom = new Vector2(20, 24);

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                bool onLine = DistanceToSegment(p, top, mid) <= StrokeWidth * 0.5f || DistanceToSegment(p, mid, bottom) <= StrokeWidth * 0.5f;
                pixels[(y * Size) + x] = onLine ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        _cached = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        return _cached;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
        Vector2 closest = a + (t * ab);
        return Vector2.Distance(p, closest);
    }
}

/// <summary>Simple bent undo-arrow glyph (↩ shape) for the gameplay HUD undo button.</summary>
public static class UndoIconSprite
{
    private const int Size = 48;
    private const float StrokeWidth = 4.0f;
    private static Sprite _cached;

    public static Sprite Get()
    {
        if (_cached != null) return _cached;

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var pixels = new Color[Size * Size];

        Vector2 arrowTip = new Vector2(8, 24);
        Vector2 armUp = new Vector2(18, 34);
        Vector2 armDown = new Vector2(18, 14);
        Vector2 shaftRight = new Vector2(34, 24);
        Vector2 tailTop = new Vector2(34, 40);

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float half = StrokeWidth * 0.5f;
                bool hit = DistToSeg(p, arrowTip, armUp) <= half
                        || DistToSeg(p, arrowTip, armDown) <= half
                        || DistToSeg(p, arrowTip, shaftRight) <= half
                        || DistToSeg(p, shaftRight, tailTop) <= half;
                pixels[y * Size + x] = hit ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        _cached = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        return _cached;
    }

    private static float DistToSeg(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
        return Vector2.Distance(p, a + t * ab);
    }
}

/// <summary>
/// Reroll/cycle glyph (↻) for the gameplay HUD refresh button. Sized to
/// match <see cref="UndoIconSprite"/>'s 48px/4.0-stroke weight — the two
/// sit side by side, and the old 32px/2.6 version read noticeably thinner
/// than its neighbour.
///
/// Replaces the lightbulb this button used to carry: a bulb reads as
/// "hint", but the button discards the current hand and deals three new
/// pieces, and there is no hint feature in the game at all.
/// </summary>
public static class RefreshIconSprite
{
    private const int Size = 48;
    private const float StrokeWidth = 4.0f;
    private const float ArcRadius = 13.5f;
    private const float ArrowWingLength = 7f;

    // The arc is drawn counterclockwise over this sweep, leaving an 80°
    // gap on the right that the two arrowheads sit in.
    private const float ArcStartDegrees = 30f;
    private const float ArcEndDegrees = 310f;

    private static Sprite _cached;

    public static Sprite Get()
    {
        if (_cached != null) return _cached;

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var pixels = new Color[Size * Size];
        Vector2 centre = new Vector2(Size * 0.5f, Size * 0.5f);
        float half = StrokeWidth * 0.5f;

        // Arrowheads are aligned to the arc's tangent at each end so they
        // read as direction of travel rather than as stray ticks.
        Vector2 endPoint = PointOnArc(centre, ArcEndDegrees);
        Vector2 endDirection = CounterClockwiseTangent(ArcEndDegrees);
        Vector2 startPoint = PointOnArc(centre, ArcStartDegrees);
        Vector2 startDirection = -CounterClockwiseTangent(ArcStartDegrees);

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);

                float angle = Mathf.Atan2(p.y - centre.y, p.x - centre.x) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;

                bool onArc = Mathf.Abs(Vector2.Distance(p, centre) - ArcRadius) <= half
                             && angle >= ArcStartDegrees
                             && angle <= ArcEndDegrees;

                bool onArrow = IsOnArrowHead(p, endPoint, endDirection, half)
                               || IsOnArrowHead(p, startPoint, startDirection, half);

                pixels[y * Size + x] = (onArc || onArrow) ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        _cached = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        return _cached;
    }

    private static Vector2 PointOnArc(Vector2 centre, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return centre + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * ArcRadius;
    }

    private static Vector2 CounterClockwiseTangent(float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
    }

    // Two wings swept back 30° either side of the incoming direction.
    private static bool IsOnArrowHead(Vector2 p, Vector2 tip, Vector2 direction, float halfStroke)
    {
        Vector2 back = -direction.normalized;
        Vector2 wingA = tip + (Rotate(back, 30f) * ArrowWingLength);
        Vector2 wingB = tip + (Rotate(back, -30f) * ArrowWingLength);
        return DistToSeg(p, tip, wingA) <= halfStroke || DistToSeg(p, tip, wingB) <= halfStroke;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        return new Vector2((v.x * cos) - (v.y * sin), (v.x * sin) + (v.y * cos));
    }

    private static float DistToSeg(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
        return Vector2.Distance(p, a + t * ab);
    }
}

/// <summary>Lightbulb glyph for the gameplay HUD hint/refresh button — matches Claude Design Screen 2.</summary>
public static class LightbulbIconSprite
{
    private const int Size = 48;
    private const float StrokeWidth = 3.5f;
    private static Sprite _cached;

    public static Sprite Get()
    {
        if (_cached != null) return _cached;

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var pixels = new Color[Size * Size];
        Vector2 centre = new Vector2(24, 27);
        const float bulbRadius = 11f;

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float dist = Vector2.Distance(p, centre);

                bool onBulb = Mathf.Abs(dist - bulbRadius) <= StrokeWidth * 0.5f && p.y >= 18f;

                bool onBaseLine1 = Mathf.Abs(p.y - 13.5f) <= StrokeWidth * 0.4f && p.x >= 18f && p.x <= 30f;
                bool onBaseLine2 = Mathf.Abs(p.y - 8.5f) <= StrokeWidth * 0.4f && p.x >= 19.5f && p.x <= 28.5f;

                pixels[y * Size + x] = (onBulb || onBaseLine1 || onBaseLine2) ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        _cached = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        return _cached;
    }
}

/// <summary>Three-dot horizontal menu glyph (···) for compact menu buttons.</summary>
public static class ThreeDotsIconSprite
{
    private const int Size = 32;
    private static Sprite _cached;

    public static Sprite Get()
    {
        if (_cached != null) return _cached;

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var pixels = new Color[Size * Size];
        const float dotRadius = 2.4f;
        Vector2[] dots = { new Vector2(8, 16), new Vector2(16, 16), new Vector2(24, 16) };

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                bool onDot = false;
                foreach (Vector2 d in dots)
                {
                    if (Vector2.Distance(p, d) <= dotRadius)
                    {
                        onDot = true;
                        break;
                    }
                }
                pixels[y * Size + x] = onDot ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        _cached = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        return _cached;
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
