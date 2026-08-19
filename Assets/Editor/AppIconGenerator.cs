using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time batch-mode generator for the app icon (Claude Design UI rework
/// Screen 6, BUILD_PLAN.md): a kintsugi crack motif on a dark circular
/// badge, rasterized directly from the mockup's exact SVG coordinates —
/// same technique as <see cref="CeramicSilhouetteSprite"/>, just baked to
/// a real PNG asset once and assigned to Player Settings, since an app
/// icon (unlike everything else in this rework) isn't a runtime UI element
/// to build in code every launch.
/// </summary>
public static class AppIconGenerator
{
    private const string OutputPath = "Assets/Icons/AppIcon.png";
    private const int Size = 512;

    public static void GenerateAppIcon()
    {
        Texture2D texture = BuildIconTexture();

        string directory = Path.GetDirectoryName(OutputPath);
        if (!AssetDatabase.IsValidFolder(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(OutputPath, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(OutputPath);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = Size;
        importer.SaveAndReimport();

        Texture2D iconAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(OutputPath);
        var icons = new Texture2D[PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android).Length];
        for (int i = 0; i < icons.Length; i++)
        {
            icons[i] = iconAsset;
        }

        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, icons);
        AssetDatabase.SaveAssets();

        Debug.Log($"AppIconGenerator: wrote {OutputPath} ({Size}x{Size}) and assigned it to every Android icon slot.");
    }

    private static Texture2D BuildIconTexture()
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
        };

        Color badgeA = FromHex("#1f1f38");
        Color badgeB = FromHex("#15142a");
        Color bodyFill = FromHex("#2d2a42");
        Color bodyStroke = FromHex("#3a3660");
        Color crackGold = FromHex("#e8c060");
        Color crackHighlight = FromHex("#f5e6b8");
        Color glowGold = new Color(crackGold.r, crackGold.g, crackGold.b, 1f);

        var pixels = new Color[Size * Size];

        // SVG viewBox is 340x340 centred inside the 512x512 badge (per the
        // mockup: <svg width="340" height="340" ...> centred via flex).
        const float svgSize = 340f;
        float svgOffset = (Size - svgSize) * 0.5f;

        Vector2 bodyCentreSvg = new Vector2(170f, 170f);
        const float bodyRadiusSvg = 150f;

        // Crack paths (SVG coordinates, straight polylines).
        Vector2 crack1A = new Vector2(60, 120), crack1B = new Vector2(140, 170), crack1C = new Vector2(110, 260);
        Vector2 crack2A = new Vector2(140, 170), crack2B = new Vector2(230, 140);
        Vector2 crack3A = new Vector2(170, 60), crack3B = new Vector2(150, 130);

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);

                // Badge background: radial gradient centred at 50%/40% of
                // the full icon, matching the mockup's CSS radial-gradient.
                Vector2 badgeCentre = new Vector2(Size * 0.5f, Size * 0.4f);
                float badgeDist = Vector2.Distance(p, badgeCentre) / (Size * 0.62f);
                Color colour = Color.Lerp(badgeA, badgeB, Mathf.Clamp01(badgeDist));

                // Soft gold ambient glow behind the body (radial-gradient
                // rgba(232,192,96,0.10) -> transparent), only inside the
                // 380px glow circle from the mockup.
                float glowDist = Vector2.Distance(p, new Vector2(Size * 0.5f, Size * 0.5f)) / 190f;
                if (glowDist < 1f)
                {
                    float glowAlpha = (1f - glowDist) * 0.10f;
                    colour = Color.Lerp(colour, crackGold, glowAlpha);
                }

                Vector2 svgP = new Vector2(p.x - svgOffset, svgSize - (p.y - svgOffset)); // SVG y-down -> texture y-up

                float bodyDist = Vector2.Distance(svgP, bodyCentreSvg);
                if (bodyDist <= bodyRadiusSvg)
                {
                    colour = bodyFill;
                    if (bodyDist >= bodyRadiusSvg - 2f)
                    {
                        colour = bodyStroke;
                    }
                }

                float d1 = DistanceToPolyline(svgP, crack1A, crack1B, crack1C);
                float d2 = DistanceToSegment(svgP, crack2A, crack2B);
                float d3 = DistanceToSegment(svgP, crack3A, crack3B);
                float dHighlight = DistanceToPolyline(svgP, crack1A, crack1B, crack1C);

                colour = ApplyGlowStroke(colour, d1, 3.5f, 10f, glowGold);
                colour = ApplyGlowStroke(colour, d2, 3f, 9f, glowGold);
                colour = ApplyGlowStroke(colour, d3, 2.5f, 7f, glowGold);
                colour = ApplyStroke(colour, dHighlight, 0.75f, crackHighlight, 0.8f);

                // Outer rounded-square mask (512x512, corner radius 96px).
                bool inside = IconShapeMath.IsInsideRoundedRect(p, 0, 0, Size, Size, 96f);
                pixels[(y * Size) + x] = inside ? colour : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    // Solid stroke core plus a soft additive glow falloff outside it,
    // approximating the mockup's drop-shadow-based crack glow.
    private static Color ApplyGlowStroke(Color baseColour, float distance, float coreHalfWidth, float glowRadius, Color glowColour)
    {
        if (distance <= coreHalfWidth)
        {
            return glowColour;
        }

        if (distance <= glowRadius)
        {
            float t = 1f - ((distance - coreHalfWidth) / (glowRadius - coreHalfWidth));
            return Color.Lerp(baseColour, glowColour, t * 0.55f);
        }

        return baseColour;
    }

    private static Color ApplyStroke(Color baseColour, float distance, float halfWidth, Color strokeColour, float opacity)
    {
        if (distance <= halfWidth)
        {
            return Color.Lerp(baseColour, strokeColour, opacity);
        }

        return baseColour;
    }

    private static float DistanceToPolyline(Vector2 p, params Vector2[] points)
    {
        float minDist = float.MaxValue;
        for (int i = 0; i < points.Length - 1; i++)
        {
            minDist = Mathf.Min(minDist, DistanceToSegment(p, points[i], points[i + 1]));
        }

        return minDist;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
        Vector2 closest = a + (t * ab);
        return Vector2.Distance(p, closest);
    }

    private static Color FromHex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color colour);
        return colour;
    }

    private static class IconShapeMath
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
}
