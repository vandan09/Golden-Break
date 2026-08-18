using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time batch-mode generator for the 9 base <see cref="CeramicDefinition"/>
/// assets (CLAUDE.md §3.4's tier table). Crack paths are procedurally
/// generated — cracks radiating from the ceramic's centre with a slight
/// organic bend — since no real Figma art exists yet (Phase 5/8 scope).
/// Silhouette sprites are left null; <c>CeramicView</c> falls back to a
/// runtime placeholder shape rather than baking a placeholder sprite into
/// the asset as a sub-asset.
/// </summary>
public static class CeramicAssetGenerator
{
    private const string OutputFolder = "Assets/Resources/CeramicDefinitions";
    private const float InnerRadius = 15f;
    private const float OuterRadius = 90f;
    private const float BendAmount = 8f;

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
            asset.cracks = GenerateCracks(spec.TotalCracks);
            asset.silhouette = null;

            AssetDatabase.CreateAsset(asset, assetPath);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"CeramicAssetGenerator: created {created} ceramic assets in {OutputFolder}.");
    }

    private static CrackPath[] GenerateCracks(int count)
    {
        var cracks = new CrackPath[count];

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);

            Vector2 p0 = direction * InnerRadius;
            Vector2 p3 = direction * OuterRadius;
            Vector2 p1 = Vector2.Lerp(p0, p3, 0.33f) + (perpendicular * BendAmount);
            Vector2 p2 = Vector2.Lerp(p0, p3, 0.66f) - (perpendicular * BendAmount);

            cracks[i] = new CrackPath { controlPoints = new[] { p0, p1, p2, p3 } };
        }

        return cracks;
    }
}
