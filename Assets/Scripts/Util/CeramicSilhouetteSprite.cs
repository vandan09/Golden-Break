using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedurally rasterizes the 3 ceramic silhouette shapes (bowl, vase,
/// plate) from the Claude Design mockup's SVG data at high resolution
/// with edge anti-aliasing for crisp rendering on modern phone screens.
/// Same "generate once, cache, reuse" pattern as <see cref="PatternSprite"/>.
/// </summary>
public static class CeramicSilhouetteSprite
{
    // 4× the SVG viewBox dimensions — gives smooth edges on 1080p+ screens
    // while keeping generation fast (each shape runs once and is cached).
    private const int RasterScale = 4;

    private static readonly float PixelsPerUnit = RasterScale / Constants.CeramicWorldScale;

    private static readonly Color RimColour = FromHex("#33304a");
    private static readonly Color BodyColour = FromHex("#2d2a42");
    private static readonly Color OpeningColour = FromHex("#221f36");

    private const int BezierSamplesPerSegment = 40;

    private static readonly Dictionary<CeramicShapeArchetype, Sprite> Cache =
        new Dictionary<CeramicShapeArchetype, Sprite>();

    public static Sprite Get(CeramicShapeArchetype shape)
    {
        if (Cache.TryGetValue(shape, out Sprite cached))
        {
            return cached;
        }

        // The bare silhouette is the composite with nothing repaired yet.
        Sprite sprite = GetComposite(shape, null, 0);
        Cache[shape] = sprite;
        return sprite;
    }

    // The shape origin is its viewBox centre — crack coordinates are
    // stored relative to it, so it has to track the artwork rather than be
    // restated per shape.
    public static Vector2 GetLocalOrigin(CeramicShapeArchetype shape)
    {
        return GetViewBoxSize(shape) * 0.5f;
    }

    public static Vector2 GetViewBoxSize(CeramicShapeArchetype shape)
    {
        return CeramicShapeData.Shapes.TryGetValue(shape, out CeramicShapeData.Shape data)
            ? data.viewBox
            : new Vector2(200f, 150f);
    }

    // Design crack strokes, verbatim: gold #e8c060 at strokeWidth 3 (2.5 on
    // the plate) with drop-shadow(0 0 3px rgba(240,216,144,0.8)); unrepaired
    // #4a4768 at strokeWidth 2 with no glow.
    private static readonly Color UnrepairedCrackColour = FromHex("#4a4768");
    private static readonly Color CrackGlowColour = FromHex("#f0d890");
    private const float RepairedStrokeWidth = 3f;
    private const float PlateRepairedStrokeWidth = 2.5f;
    private const float UnrepairedStrokeWidth = 2f;
    private const float GlowRadius = 3f;
    private const float GlowStrength = 0.8f;

    private static readonly Dictionary<(CeramicShapeArchetype shape, int cracks, int repaired, int variant), Sprite> CompositeCache =
        new Dictionary<(CeramicShapeArchetype, int, int, int), Sprite>();

    /// <summary>
    /// The complete ceramic — silhouette plus every crack — rasterized in
    /// one pass, exactly the way the design's SVG paints it.
    ///
    /// Cracks are stroked from a distance field (a pixel is covered when it
    /// is within half the stroke width of the polyline) rather than as a
    /// quad per segment. That is what reproduces SVG's strokeLinecap and
    /// strokeLinejoin of "round" for free: the distance to a polyline is
    /// naturally round at the ends and at every bend, whereas per-segment
    /// quads leave square ends and a notch on the outside of each bend.
    /// Coverage comes from the distance analytically instead of by
    /// supersampling — smoother than a 4x4 grid at a fraction of the cost,
    /// which matters because the crack pass runs over the whole texture.
    /// </summary>
    public static Sprite GetComposite(CeramicShapeArchetype shape, CrackPath[] cracks, int repairedCount, int colourVariant = 0)
    {
        int crackCount = cracks?.Length ?? 0;
        repairedCount = Mathf.Clamp(repairedCount, 0, crackCount);
        var key = (shape, crackCount, repairedCount, colourVariant);

        if (CompositeCache.TryGetValue(key, out Sprite cached) && cached != null)
        {
            return cached;
        }

        Vector2 viewBox = GetViewBoxSize(shape);
        int w = (int)viewBox.x * RasterScale, h = (int)viewBox.y * RasterScale;
        Texture2D tex = NewTransparentTexture(w, h);

        PaintBody(tex, shape);

        if (crackCount > 0)
        {
            Vector2 origin = GetLocalOrigin(shape);
            float repairedWidth = shape == CeramicShapeArchetype.Plate
                ? PlateRepairedStrokeWidth
                : RepairedStrokeWidth;

            // Glow underneath every gold crack first, then the strokes on
            // top — the SVG applies the drop-shadow per path, but since the
            // strokes themselves are opaque the result is identical and
            // this avoids re-walking the texture once per crack.
            Color gold = CeramicGold.ForVariant(colourVariant);
            Color glow = colourVariant <= 0 ? CrackGlowColour : CeramicGold.GlowForVariant(colourVariant);

            for (int i = 0; i < repairedCount; i++)
            {
                StrokePolyline(tex, ToSvgSpace(cracks[i], origin), repairedWidth + GlowRadius, glow, GlowStrength, softEdge: true);
            }

            for (int i = 0; i < crackCount; i++)
            {
                bool repaired = i < repairedCount;
                StrokePolyline(
                    tex,
                    ToSvgSpace(cracks[i], origin),
                    repaired ? repairedWidth : UnrepairedStrokeWidth,
                    repaired ? gold : UnrepairedCrackColour,
                    1f,
                    softEdge: false);
            }
        }

        tex.Apply();
        Sprite sprite = CreateSprite(tex, w, h, GetLocalOrigin(shape));
        CompositeCache[key] = sprite;
        return sprite;
    }

    // CrackPath points are local space (y-up, relative to the shape origin);
    // rasterizing happens in the SVG's own y-down viewBox space.
    private static Vector2[] ToSvgSpace(CrackPath path, Vector2 origin)
    {
        if (path.points == null)
        {
            return System.Array.Empty<Vector2>();
        }

        var svg = new Vector2[path.points.Length];
        for (int i = 0; i < path.points.Length; i++)
        {
            svg[i] = new Vector2(path.points[i].x + origin.x, origin.y - path.points[i].y);
        }

        return svg;
    }

    // Rim ellipse, then the body wall, then the opening — the exact paint
    // order the design SVG uses, driven off its own coordinates.
    private static void PaintBody(Texture2D tex, CeramicShapeArchetype shape)
    {
        if (!CeramicShapeData.Shapes.TryGetValue(shape, out CeramicShapeData.Shape data))
        {
            return;
        }

        FillEllipseAA(tex, data.rim.cx, data.rim.cy, data.rim.rx, data.rim.ry, RimColour);

        Vector2[] body = SvgPath.ToPoints(data.body);
        if (body.Length > 2)
        {
            FillClosedPathAA(tex, new List<Vector2>(body), BodyColour);
        }

        FillEllipseAA(tex, data.opening.cx, data.opening.cy, data.opening.rx, data.opening.ry, OpeningColour);
    }

    // Blends a round-capped, round-joined stroke along the polyline.
    // softEdge fades linearly across the whole radius (the drop-shadow);
    // otherwise coverage is a one-pixel analytic edge (the stroke itself).
    private static void StrokePolyline(Texture2D tex, Vector2[] svgPoints, float strokeWidth, Color colour, float strength, bool softEdge)
    {
        if (svgPoints.Length < 2)
        {
            return;
        }

        float half = strokeWidth * 0.5f;
        int w = tex.width, h = tex.height;

        // Only touch the pixels this stroke can reach.
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        foreach (Vector2 p in svgPoints)
        {
            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
        }

        int x0 = Mathf.Max(0, (int)((minX - half - 1f) * RasterScale));
        int x1 = Mathf.Min(w - 1, (int)((maxX + half + 1f) * RasterScale));

        // The viewBox is y-down but the texture is y-up, so the SVG y range
        // has to be flipped before it can index rows: the largest SVG y is
        // the SMALLEST texture row. Using the SVG range directly clipped
        // every stroke to the sliver where the two ranges happened to
        // overlap — short cracks vanished to a nub, long ones lost an end.
        int y0 = Mathf.Max(0, (int)(h - 1 - ((maxY + half + 1f) * RasterScale)));
        int y1 = Mathf.Min(h - 1, (int)(h - 1 - ((minY - half - 1f) * RasterScale)));

        for (int py = y0; py <= y1; py++)
        {
            for (int px = x0; px <= x1; px++)
            {
                // Texture is y-up, the viewBox is y-down.
                float sx = (px + 0.5f) / RasterScale;
                float sy = ((h - 1 - py) + 0.5f) / RasterScale;
                var sample = new Vector2(sx, sy);

                float distance = float.MaxValue;
                for (int s = 1; s < svgPoints.Length; s++)
                {
                    distance = Mathf.Min(distance, DistanceToSegment(sample, svgPoints[s - 1], svgPoints[s]));
                    if (distance <= 0f)
                    {
                        break;
                    }
                }

                float coverage = softEdge
                    ? Mathf.Clamp01(1f - (distance / half))
                    : Mathf.Clamp01((half - distance) * RasterScale + 0.5f);

                if (coverage <= 0f)
                {
                    continue;
                }

                BlendPixel(tex, px, py, colour, coverage * strength);
            }
        }
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSquared = ab.sqrMagnitude;
        if (lengthSquared < 1e-6f)
        {
            return Vector2.Distance(p, a);
        }

        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSquared);
        return Vector2.Distance(p, a + (ab * t));
    }

    private static void BlendPixel(Texture2D tex, int x, int y, Color colour, float alpha)
    {
        Color existing = tex.GetPixel(x, y);
        float outAlpha = alpha + (existing.a * (1f - alpha));
        if (outAlpha <= 0f)
        {
            tex.SetPixel(x, y, Color.clear);
            return;
        }

        Color blended = ((colour * alpha) + (existing * existing.a * (1f - alpha))) / outAlpha;
        blended.a = outAlpha;
        tex.SetPixel(x, y, blended);
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

    // Anti-aliased ellipse: uses signed distance from the ellipse boundary
    // to smoothly blend alpha at the edges instead of a hard in/out test.
    private static void FillEllipseAA(Texture2D tex, float cx, float cy, float rx, float ry, Color colour)
    {
        int w = tex.width, h = tex.height;
        float edgeWidth = 1.2f;

        int minTx = Mathf.Max(0, Mathf.FloorToInt((cx - rx - 1f) * RasterScale));
        int maxTx = Mathf.Min(w - 1, Mathf.CeilToInt((cx + rx + 1f) * RasterScale));
        int minTy = Mathf.Max(0, Mathf.FloorToInt(((float)h / RasterScale - cy - ry - 1f) * RasterScale));
        int maxTy = Mathf.Min(h - 1, Mathf.CeilToInt(((float)h / RasterScale - cy + ry + 1f) * RasterScale));

        for (int ty = minTy; ty <= maxTy; ty++)
        {
            float svgY = (h - (ty + 0.5f)) / RasterScale;
            for (int tx = minTx; tx <= maxTx; tx++)
            {
                float svgX = (tx + 0.5f) / RasterScale;
                float nx = (svgX - cx) / rx;
                float ny = (svgY - cy) / ry;
                float dist = (nx * nx) + (ny * ny);

                if (dist <= 1f)
                {
                    tex.SetPixel(tx, ty, colour);
                }
                else
                {
                    float edgeDist = (Mathf.Sqrt(dist) - 1f) * Mathf.Min(rx, ry) * RasterScale;
                    if (edgeDist < edgeWidth)
                    {
                        float alpha = 1f - (edgeDist / edgeWidth);
                        Color existing = tex.GetPixel(tx, ty);
                        Color blended = Color.Lerp(existing, colour, alpha);
                        blended.a = Mathf.Max(existing.a, alpha);
                        tex.SetPixel(tx, ty, blended);
                    }
                }
            }
        }
    }

    // Anti-aliased polygon fill: uses 2×2 sub-pixel sampling at the edges.
    private static void FillClosedPathAA(Texture2D tex, List<Vector2> polygon, Color colour)
    {
        int w = tex.width, h = tex.height;

        Rect bounds = ComputeBounds(polygon);
        int minTx = Mathf.Max(0, Mathf.FloorToInt((bounds.xMin - 1f) * RasterScale));
        int maxTx = Mathf.Min(w - 1, Mathf.CeilToInt((bounds.xMax + 1f) * RasterScale));
        int minTy = Mathf.Max(0, Mathf.FloorToInt(((float)h / RasterScale - bounds.yMax - 1f) * RasterScale));
        int maxTy = Mathf.Min(h - 1, Mathf.CeilToInt(((float)h / RasterScale - bounds.yMin + 1f) * RasterScale));

        for (int ty = minTy; ty <= maxTy; ty++)
        {
            float svgYCenter = (h - (ty + 0.5f)) / RasterScale;
            for (int tx = minTx; tx <= maxTx; tx++)
            {
                float svgXCenter = (tx + 0.5f) / RasterScale;

                if (IsInsidePolygon(polygon, svgXCenter, svgYCenter))
                {
                    tex.SetPixel(tx, ty, colour);
                }
                else
                {
                    // 2×2 sub-pixel AA at edges
                    float step = 0.25f / RasterScale;
                    int hits = 0;
                    if (IsInsidePolygon(polygon, svgXCenter - step, svgYCenter - step)) hits++;
                    if (IsInsidePolygon(polygon, svgXCenter + step, svgYCenter - step)) hits++;
                    if (IsInsidePolygon(polygon, svgXCenter - step, svgYCenter + step)) hits++;
                    if (IsInsidePolygon(polygon, svgXCenter + step, svgYCenter + step)) hits++;

                    if (hits > 0)
                    {
                        float alpha = hits * 0.25f;
                        Color existing = tex.GetPixel(tx, ty);
                        Color blended = Color.Lerp(existing, colour, alpha);
                        blended.a = Mathf.Max(existing.a, alpha);
                        tex.SetPixel(tx, ty, blended);
                    }
                }
            }
        }
    }

    private static Rect ComputeBounds(List<Vector2> polygon)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (Vector2 p in polygon)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }
        return new Rect(minX, minY, maxX - minX, maxY - minY);
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
        Vector2 pivot = new Vector2(svgOrigin.x * RasterScale / width, 1f - (svgOrigin.y * RasterScale / height));
        return Sprite.Create(tex, new Rect(0, 0, width, height), pivot, PixelsPerUnit);
    }

    private static Color FromHex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color colour);
        return colour;
    }
}
