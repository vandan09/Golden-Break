using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time batch-mode generator for the 20 <see cref="PieceDefinition"/>
/// assets. Cell sets and weights per CLAUDE.md §3.1 — see PROGRESS.md
/// deviations for why J/T/2x3/J3 differ from the spec's literal ASCII
/// diagram (measured, then corrected for four likely diagram typos).
/// </summary>
public static class PieceAssetGenerator
{
    private const string OutputFolder = "Assets/Resources/PieceDefinitions";

    private struct PieceSpec
    {
        public string Id;
        public Vector2Int[] Cells;
        public int Weight;

        public PieceSpec(string id, int weight, params Vector2Int[] cells)
        {
            Id = id;
            Weight = weight;
            Cells = cells;
        }
    }

    public static void GeneratePieceAssets()
    {
        PieceSpec[] specs =
        {
            new PieceSpec("single", 12, new Vector2Int(0, 0)),
            new PieceSpec("1x2", 12, new Vector2Int(0, 0), new Vector2Int(1, 0)),
            new PieceSpec("2x1", 12, new Vector2Int(0, 0), new Vector2Int(0, 1)),

            new PieceSpec("2x2", 10,
                new Vector2Int(0, 0), new Vector2Int(1, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1)),
            new PieceSpec("L", 10,
                new Vector2Int(0, 0), new Vector2Int(1, 0),
                new Vector2Int(0, 1)),
            new PieceSpec("J", 10,
                new Vector2Int(1, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1)),
            new PieceSpec("S", 10,
                new Vector2Int(0, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1)),
            new PieceSpec("Z", 10,
                new Vector2Int(0, 0), new Vector2Int(1, 0),
                new Vector2Int(1, 1)),
            new PieceSpec("T", 10,
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                new Vector2Int(1, 1)),

            new PieceSpec("1x3", 8, new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0)),
            new PieceSpec("3x1", 8, new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2)),
            new PieceSpec("2x3", 8,
                new Vector2Int(0, 0), new Vector2Int(1, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1),
                new Vector2Int(0, 2), new Vector2Int(1, 2)),
            new PieceSpec("3x2", 8,
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1)),

            new PieceSpec("L3", 6,
                new Vector2Int(0, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, 2), new Vector2Int(1, 2)),
            new PieceSpec("J3", 6,
                new Vector2Int(1, 0),
                new Vector2Int(1, 1),
                new Vector2Int(0, 2), new Vector2Int(1, 2)),

            new PieceSpec("1x4", 5, new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0)),
            new PieceSpec("4x1", 5, new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3)),

            new PieceSpec("1x5", 3, new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0)),
            new PieceSpec("5x1", 3, new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(0, 4)),
            new PieceSpec("3x3", 3,
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
                new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)),
        };

        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            throw new System.IO.DirectoryNotFoundException($"PieceAssetGenerator: {OutputFolder} must exist before running this.");
        }

        int created = 0;
        foreach (PieceSpec spec in specs)
        {
            string assetPath = $"{OutputFolder}/{spec.Id}.asset";
            if (AssetDatabase.LoadAssetAtPath<PieceDefinition>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            var asset = ScriptableObject.CreateInstance<PieceDefinition>();
            asset.pieceId = spec.Id;
            asset.cells = spec.Cells;
            asset.spawnWeight = spec.Weight;

            AssetDatabase.CreateAsset(asset, assetPath);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"PieceAssetGenerator: created {created} piece assets in {OutputFolder}.");
    }
}
