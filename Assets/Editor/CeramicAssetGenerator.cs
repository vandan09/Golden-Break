using UnityEditor;
using UnityEngine;

/// <summary>
/// Batch-mode generator for the 9 <see cref="CeramicDefinition"/> assets of
/// CLAUDE.md §3.4's tier table.
///
/// Every silhouette and every crack path is real authored artwork from the
/// Claude Design project's "Ceramic Library - 9 tiers" artboard, held
/// verbatim in <see cref="CeramicShapeData"/>. Each tier has its own shape
/// and its own crack set, so nothing here invents geometry: earlier versions
/// had only 3 shapes for 9 tiers and extrapolated the missing cracks by
/// mirroring and shrinking, which is exactly what made the higher tiers
/// look wrong.
/// </summary>
public static class CeramicAssetGenerator
{
    private const string OutputFolder = "Assets/Resources/CeramicDefinitions";

    private struct CeramicSpec
    {
        public int Tier;
        public string DisplayName;
        public CeramicShapeArchetype Shape;

        public CeramicSpec(int tier, string displayName, CeramicShapeArchetype shape)
        {
            Tier = tier;
            DisplayName = displayName;
            Shape = shape;
        }
    }

    // Tier -> shape is now 1:1. Crack counts are not listed here on purpose:
    // they come from however many paths the design actually draws for that
    // shape, so the assets can never claim a crack the artwork lacks.
    private static readonly CeramicSpec[] Specs =
    {
        new CeramicSpec(1, "Simple bowl", CeramicShapeArchetype.SimpleBowl),
        new CeramicSpec(2, "Tea cup", CeramicShapeArchetype.TeaCup),
        new CeramicSpec(3, "Plate", CeramicShapeArchetype.Plate),
        new CeramicSpec(4, "Tall vase", CeramicShapeArchetype.TallVase),
        new CeramicSpec(5, "Teapot", CeramicShapeArchetype.Teapot),
        new CeramicSpec(6, "Large bowl", CeramicShapeArchetype.LargeBowl),
        new CeramicSpec(7, "Ornate plate", CeramicShapeArchetype.OrnatePlate),
        new CeramicSpec(8, "Sake set", CeramicShapeArchetype.SakeSet),
        new CeramicSpec(9, "Temple bowl", CeramicShapeArchetype.TempleBowl),
    };

    public static void GenerateCeramicAssets()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            throw new System.IO.DirectoryNotFoundException($"CeramicAssetGenerator: {OutputFolder} must exist before running this.");
        }

        int created = 0;
        foreach (CeramicSpec spec in Specs)
        {
            string assetPath = $"{OutputFolder}/tier{spec.Tier}.asset";
            if (AssetDatabase.LoadAssetAtPath<CeramicDefinition>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            var asset = ScriptableObject.CreateInstance<CeramicDefinition>();
            asset.tier = spec.Tier;
            asset.displayName = spec.DisplayName;
            asset.shape = spec.Shape;
            asset.cracks = BuildCracks(spec.Shape);
            asset.totalCracks = asset.cracks.Length;
            asset.silhouette = null;

            AssetDatabase.CreateAsset(asset, assetPath);
            created++;

            Debug.Log($"CeramicAssetGenerator: tier {spec.Tier} \"{spec.DisplayName}\" -> {spec.Shape}, {asset.totalCracks} cracks.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"CeramicAssetGenerator: created {created} ceramic assets in {OutputFolder}.");
    }

    // Crack paths are parsed from the design's own SVG `d` strings and
    // converted from the viewBox's y-down space into the y-up local space
    // (relative to the shape origin) the renderers use.
    private static CrackPath[] BuildCracks(CeramicShapeArchetype shape)
    {
        if (!CeramicShapeData.Shapes.TryGetValue(shape, out CeramicShapeData.Shape data) || data.cracks == null)
        {
            return new CrackPath[0];
        }

        Vector2 origin = CeramicSilhouetteSprite.GetLocalOrigin(shape);
        var cracks = new CrackPath[data.cracks.Length];

        for (int i = 0; i < data.cracks.Length; i++)
        {
            Vector2[] svgPoints = SvgPath.ToPoints(data.cracks[i]);
            var points = new Vector2[svgPoints.Length];
            for (int p = 0; p < svgPoints.Length; p++)
            {
                points[p] = new Vector2(svgPoints[p].x - origin.x, -(svgPoints[p].y - origin.y));
            }

            cracks[i] = new CrackPath { points = points };
        }

        return cracks;
    }
}
