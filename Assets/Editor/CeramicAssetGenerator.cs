using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time batch-mode generator for the 9 base <see cref="CeramicDefinition"/>
/// assets (CLAUDE.md §3.4's tier table). Crack paths are procedurally
/// generated — cracks fanning downward from the rim with a slight organic
/// bend, echoing the Claude Design mockup's own crack style (cracks
/// radiate down from a point near the rim into the body, not a full
/// 360° starburst) — since the mockup only supplies exact coordinates
/// for one example (a 6-crack bowl), not a scheme for every tier's
/// different crack count (4-12, CLAUDE.md §3.4).
///
/// Silhouette shape is now real (<see cref="CeramicSilhouetteSprite"/>,
/// rasterized from the mockup's exact SVG data) rather than a flat
/// placeholder rectangle — each tier is assigned the closest of the
/// mockup's 3 supplied shapes (bowl/vase/plate) by its own display name,
/// since nothing supplies 9 distinct shapes.
/// </summary>
public static class CeramicAssetGenerator
{
    private const string OutputFolder = "Assets/Resources/CeramicDefinitions";
    private const float BendAmount = 6f;

    // Cracks fan downward into the body — centred on straight down (270°
    // in this generator's standard-math angle convention, since crack
    // coordinates are authored directly in the same Y-up space the
    // silhouette ends up rendering in, see CeramicSilhouetteSprite's own
    // doc comment) with a wide-but-not-full spread, matching the mockup's
    // own downward-fanning cracks rather than a full circle radiating in
    // every direction including up through the rim, which the mockup's
    // cracks never do.
    private const float FanCenterDegrees = 270f;
    private const float FanSpreadDegrees = 200f;

    // Roughly each shape's own vertical half-extent (see the exact SVG
    // coordinates in CeramicSilhouetteSprite) so cracks stay inside their
    // silhouette instead of one fixed radius poking outside the flattest
    // shape (Plate) or falling far short of the tallest (Vase).
    private static readonly System.Collections.Generic.Dictionary<CeramicShapeArchetype, (float inner, float outer)> RadiusByShape =
        new System.Collections.Generic.Dictionary<CeramicShapeArchetype, (float, float)>
        {
            { CeramicShapeArchetype.Bowl, (10f, 50f) },
            { CeramicShapeArchetype.Vase, (12f, 65f) },
            { CeramicShapeArchetype.Plate, (6f, 25f) },
        };

    private struct CeramicSpec
    {
        public int Tier;
        public string DisplayName;
        public int TotalCracks;

        public CeramicSpec(int tier, string displayName, int totalCracks)
        {
            Tier = tier;
            DisplayName = displayName;
            TotalCracks = totalCracks;
        }
    }

    public static void GenerateCeramicAssets()
    {
        CeramicSpec[] specs =
        {
            new CeramicSpec(1, "Simple bowl", 4),
            new CeramicSpec(2, "Tea cup", 5),
            new CeramicSpec(3, "Plate", 6),
            new CeramicSpec(4, "Tall vase", 7),
            new CeramicSpec(5, "Teapot", 8),
            new CeramicSpec(6, "Large bowl", 9),
            new CeramicSpec(7, "Ornate plate", 10),
            new CeramicSpec(8, "Sake set", 11),
            new CeramicSpec(9, "Temple bowl", 12),
        };

        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            throw new System.IO.DirectoryNotFoundException($"CeramicAssetGenerator: {OutputFolder} must exist before running this.");
        }

        int created = 0;
        foreach (CeramicSpec spec in specs)
        {
            string assetPath = $"{OutputFolder}/tier{spec.Tier}.asset";
            if (AssetDatabase.LoadAssetAtPath<CeramicDefinition>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            var asset = ScriptableObject.CreateInstance<CeramicDefinition>();
            asset.tier = spec.Tier;
            asset.displayName = spec.DisplayName;
            asset.totalCracks = spec.TotalCracks;
            asset.shape = ArchetypeForDisplayName(spec.DisplayName);
            asset.cracks = GenerateCracks(spec.TotalCracks, asset.shape);
            asset.silhouette = null;

            AssetDatabase.CreateAsset(asset, assetPath);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"CeramicAssetGenerator: created {created} ceramic assets in {OutputFolder}.");
    }

    // The design's Gallery screen only supplies 3 concrete shapes (bowl,
    // vase, plate) for CLAUDE.md's 9 named tiers — matched here by the
    // closest keyword in each tier's own display name rather than
    // inventing 6 more shapes nothing designed.
    private static CeramicShapeArchetype ArchetypeForDisplayName(string displayName)
    {
        string lower = displayName.ToLowerInvariant();
        if (lower.Contains("plate"))
        {
            return CeramicShapeArchetype.Plate;
        }

        if (lower.Contains("vase"))
        {
            return CeramicShapeArchetype.Vase;
        }

        // Bowl, cup, teapot, sake set, temple bowl — all round vessels,
        // closest to the mockup's bowl silhouette.
        return CeramicShapeArchetype.Bowl;
    }

    private static CrackPath[] GenerateCracks(int count, CeramicShapeArchetype shape)
    {
        (float innerRadius, float outerRadius) = RadiusByShape[shape];

        // A small upward bias so the fan's origin sits nearer the rim
        // (where the mockup's own cracks start) rather than the
        // silhouette's exact geometric centre.
        Vector2 fanOrigin = new Vector2(0f, outerRadius * 0.2f);

        float halfSpread = FanSpreadDegrees * 0.5f;
        var cracks = new CrackPath[count];

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float angleDegrees = FanCenterDegrees - halfSpread + (t * FanSpreadDegrees);
            float angle = angleDegrees * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);

            Vector2 p0 = fanOrigin + (direction * innerRadius);
            Vector2 p3 = fanOrigin + (direction * outerRadius);
            Vector2 p1 = Vector2.Lerp(p0, p3, 0.33f) + (perpendicular * BendAmount);
            Vector2 p2 = Vector2.Lerp(p0, p3, 0.66f) - (perpendicular * BendAmount);

            cracks[i] = new CrackPath { controlPoints = new[] { p0, p1, p2, p3 } };
        }

        return cracks;
    }
}
