using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedurally rasterizes the 3 real ceramic silhouette shapes (bowl,
/// vase, plate) supplied by the player's Claude Design mockup — replacing
/// <see cref="CeramicView"/>'s previous flat-rectangle placeholder. Same
/// "generate once, cache, reuse" pattern as <see cref="PatternSprite"/>/
/// <see cref="PlaceholderSprite"/>: no sourced art exists, but unlike
/// those two, exact SVG path/ellipse coordinates for these 3 shapes DO
/// exist (from the design), so they're rasterized faithfully rather than
/// invented.
///
/// Coordinates below are copied directly from the design's SVG (viewBox
/// units, y-down). Each shape is layered: a lighter back-rim ellipse,
/// the body (a closed cubic-bezier path), then a darker "opening" ellipse
/// on top — exactly the design's 3-layer construction. Pixels outside all
/// three layers stay fully transparent, giving a real cutout silhouette
/// instead of an opaque rectangle.
/// </summary>
public static class CeramicSilhouetteSprite
{
    // Matches CeramicView's crack-path scale exactly, so a silhouette and
    // its cracks (control points authored in the same SVG-unit space, see
    // CeramicAssetGenerator) line up when rendered as siblings at the same
    // transform.
    private static readonly float PixelsPerUnit = 1f / Constants.CeramicWorldScale;

    private static readonly Color RimColour = FromHex("#33304a");
    private static readonly Color BodyColour = FromHex("#2d2a42");
    private static readonly Color OpeningColour = FromHex("#221f36");

    private const int BezierSamplesPerSegment = 22;

    private static readonly Dictionary<CeramicShapeArchetype, Sprite> Cache = new Dictionary<CeramicShapeArchetype, Sprite>();

    public static Sprite Get(CeramicShapeArchetype shape)
    {
        if (Cache.TryGetValue(shape, out Sprite cached))
        {
            return cached;
        }

        Sprite sprite = shape switch
        {
            CeramicShapeArchetype.Bowl => BuildBowl(),
            CeramicShapeArchetype.Vase => BuildVase(),
            CeramicShapeArchetype.Plate => BuildPlate(),
            _ => BuildBowl(),
        };

        Cache[shape] = sprite;
        return sprite;
    }

    // The SVG-unit point every crack's control points must be authored
    // relative to (CeramicAssetGenerator subtracts this before storing),
    // so a shape's cracks and its silhouette share one local origin — the
    // shape's own bounding-box centre, since the design's own paths don't
    // otherwise call out a single canonical "centre" point.
    public static Vector2 GetLocalOrigin(CeramicShapeArchetype shape)
    {
        return shape switch
        {
            CeramicShapeArchetype.Bowl => new Vector2(100f, 75f),
            CeramicShapeArchetype.Vase => new Vector2(70f, 100f),
            CeramicShapeArchetype.Plate => new Vector2(100f, 50f),
            _ => Vector2.zero,
        };
    }

    // The SVG viewBox size each shape was rasterized at — lets UI-space
    // callers (see UiCeramicPreview) work out how many pixels one local
    // "ceramic unit" covers once the silhouette sprite is stretched to
    // fit some on-screen rect.
    public static Vector2 GetViewBoxSize(CeramicShapeArchetype shape)
    {
        return shape switch
        {
            CeramicShapeArchetype.Bowl => new Vector2(200f, 150f),
            CeramicShapeArchetype.Vase => new Vector2(140f, 200f),
            CeramicShapeArchetype.Plate => new Vector2(200f, 100f),
            _ => new Vector2(200f, 150f),
        };
    }

    private static Sprite BuildBowl()
    {
        const int w = 200, h = 150;
        Texture2D tex = NewTransparentTexture(w, h);

        FillEllipse(tex, 100, 35, 82, 16, RimColour);
        FillClosedPath(tex, BowlBodyPolygon(), BodyColour);
        FillEllipse(tex, 100, 33, 72, 12, OpeningColour);

        tex.Apply();
        return CreateSprite(tex, w, h, GetLocalOrigin(CeramicShapeArchetype.Bowl));
    }

    private static Sprite BuildVase()
    {
        const int w = 140, h = 200;
        Texture2D tex = NewTransparentTexture(w, h);

        FillEllipse(tex, 70, 24, 40, 10, RimColour);
        FillClosedPath(tex, VaseBodyPolygon(), BodyColour);
        FillEllipse(tex, 70, 22, 32, 7, OpeningColour);

        tex.Apply();
        return CreateSprite(tex, w, h, GetLocalOrigin(CeramicShapeArchetype.Vase));
    }

    private static Sprite BuildPlate()
    {
        const int w = 200, h = 100;
        Texture2D tex = NewTransparentTexture(w, h);

        FillEllipse(tex, 100, 30, 90, 12, RimColour);
        FillClosedPath(tex, PlateBodyPolygon(), BodyColour);
        FillEllipse(tex, 100, 28, 78, 9, OpeningColour);

        tex.Apply();
        return CreateSprite(tex, w, h, GetLocalOrigin(CeramicShapeArchetype.Plate));
    }

    // "M18,35 C18,35 22,120 100,132 C178,120 182,35 182,35 L172,38
    //  C168,95 140,118 100,120 C60,118 32,95 28,38 Z"
    private static List<Vector2> BowlBodyPolygon()
    {
        var pts = new List<Vector2>();
        Vector2 start = new Vector2(18, 35);
        pts.Add(start);
        AppendCubic(pts, start, new Vector2(18, 35), new Vector2(22, 120), new Vector2(100, 132));
        AppendCubic(pts, new Vector2(100, 132), new Vector2(178, 120), new Vector2(182, 35), new Vector2(182, 35));
        pts.Add(new Vector2(172, 38));
        AppendCubic(pts, new Vector2(172, 38), new Vector2(168, 95), new Vector2(140, 118), new Vector2(100, 120));
        AppendCubic(pts, new Vector2(100, 120), new Vector2(60, 118), new Vector2(32, 95), new Vector2(28, 38));
        return pts;
    }

    // "M40,24 C30,90 30,150 70,180 C110,150 110,90 100,24 L92,26
    //  C96,80 92,140 70,164 C48,140 44,80 48,26 Z"
    private static List<Vector2> VaseBodyPolygon()
    {
        var pts = new List<Vector2>();
        Vector2 start = new Vector2(40, 24);
        pts.Add(start);
        AppendCubic(pts, start, new Vector2(30, 90), new Vector2(30, 150), new Vector2(70, 180));
        AppendCubic(pts, new Vector2(70, 180), new Vector2(110, 150), new Vector2(110, 90), new Vector2(100, 24));
        pts.Add(new Vector2(92, 26));
        AppendCubic(pts, new Vector2(92, 26), new Vector2(96, 80), new Vector2(92, 140), new Vector2(70, 164));
        AppendCubic(pts, new Vector2(70, 164), new Vector2(48, 140), new Vector2(44, 80), new Vector2(48, 26));
        return pts;
    }

    // "M14,30 C14,30 20,70 100,78 C180,70 186,30 186,30 L176,33
    //  C170,58 140,64 100,66 C60,64 30,58 24,33 Z"
    private static List<Vector2> PlateBodyPolygon()
    {
        var pts = new List<Vector2>();
        Vector2 start = new Vector2(14, 30);
        pts.Add(start);
        AppendCubic(pts, start, new Vector2(14, 30), new Vector2(20, 70), new Vector2(100, 78));
        AppendCubic(pts, new Vector2(100, 78), new Vector2(180, 70), new Vector2(186, 30), new Vector2(186, 30));
        pts.Add(new Vector2(176, 33));
        AppendCubic(pts, new Vector2(176, 33), new Vector2(170, 58), new Vector2(140, 64), new Vector2(100, 66));
        AppendCubic(pts, new Vector2(100, 66), new Vector2(60, 64), new Vector2(30, 58), new Vector2(24, 33));
        return pts;
    }

    private static void AppendCubic(List<Vector2> pts, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        for (int i = 1; i <= BezierSamplesPerSegment; i++)
        {
            float t = i / (float)BezierSamplesPerSegment;
            float mt = 1f - t;
            Vector2 point = (mt * mt * mt * p0) + (3f * mt * mt * t * p1) + (3f * mt * t * t * p2) + (t * t * t * p3);
            pts.Add(point);
        }
    }

    private static Texture2D NewTransparentTexture(int width, int height)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var clear = new Color[width * height];
        tex.SetPixels(clear);
        return tex;
    }

    private static void FillEllipse(Texture2D tex, float cx, float cy, float rx, float ry, Color colour)
    {
        int w = tex.width, h = tex.height;
        for (int ty = 0; ty < h; ty++)
        {
            float svgY = h - (ty + 0.5f);
            for (int tx = 0; tx < w; tx++)
            {
                float svgX = tx + 0.5f;
                float nx = (svgX - cx) / rx;
                float ny = (svgY - cy) / ry;
                if ((nx * nx) + (ny * ny) <= 1f)
                {
                    tex.SetPixel(tx, ty, colour);
                }
            }
        }
    }

    // Standard even-odd ray-casting point-in-polygon test, evaluated per
    // pixel against the bezier-sampled boundary — fast enough here since
    // every shape is generated exactly once and cached (same tradeoff
    // PatternSprite already makes for its own per-pixel generation).
    private static void FillClosedPath(Texture2D tex, List<Vector2> polygon, Color colour)
    {
        int w = tex.width, h = tex.height;
        for (int ty = 0; ty < h; ty++)
        {
            float svgY = h - (ty + 0.5f);
            for (int tx = 0; tx < w; tx++)
            {
                float svgX = tx + 0.5f;
                if (IsInsidePolygon(polygon, svgX, svgY))
                {
                    tex.SetPixel(tx, ty, colour);
                }
            }
        }
    }

    private static bool IsInsidePolygon(List<Vector2> polygon, float x, float y)
    {
        bool inside = false;
        int count = polygon.Count;
        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];
            bool crosses = ((a.y > y) != (b.y > y)) &&
                           (x < ((b.x - a.x) * (y - a.y) / (b.y - a.y)) + a.x);
            if (crosses)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static Sprite CreateSprite(Texture2D tex, int width, int height, Vector2 svgOrigin)
    {
        Vector2 pivot = new Vector2(svgOrigin.x / width, 1f - (svgOrigin.y / height));
        return Sprite.Create(tex, new Rect(0, 0, width, height), pivot, PixelsPerUnit);
    }

    private static Color FromHex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color colour);
        return colour;
    }
}
