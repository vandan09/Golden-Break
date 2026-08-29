using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Dev tool: rasterizes the composite ceramic sprites (silhouette + cracks,
/// exactly as <see cref="CeramicSilhouetteSprite.GetComposite"/> builds them
/// for the Home card and Gallery) straight to PNG, so a design-fidelity
/// check against the Claude Design mockup can be done by looking at the real
/// pixels instead of by rebuilding and installing an APK first.
/// </summary>
public static class CeramicRenderDump
{
    public static void DumpAll()
    {
        string outDir = System.Environment.GetEnvironmentVariable("CERAMIC_DUMP_DIR");
        if (string.IsNullOrEmpty(outDir))
        {
            outDir = Path.Combine(Application.dataPath, "../CeramicDump");
        }

        Directory.CreateDirectory(outDir);

        // tier1 bowl (4 cracks), tier3 plate (2), tier4 vase (3), tier9 bowl (6).
        int[] tiers = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        foreach (int tier in tiers)
        {
            var definition = AssetDatabase.LoadAssetAtPath<CeramicDefinition>(
                $"Assets/Resources/CeramicDefinitions/tier{tier}.asset");
            if (definition == null)
            {
                Debug.LogError($"CeramicRenderDump: tier{tier} not found.");
                continue;
            }

            int total = definition.cracks != null ? definition.cracks.Length : 0;

            WriteSprite(outDir, $"tier{tier}_{definition.shape}_v0_all", definition, total, 0);
            if (total > 1)
            {
                WriteSprite(outDir, $"tier{tier}_{definition.shape}_v0_partial", definition, total / 2, 0);
            }

            // Tiers 6-9 are the ones that repeat with colour variants.
            if (tier >= 6)
            {
                for (int v = 1; v < CeramicGold.VariantCount; v++)
                {
                    WriteSprite(outDir, $"tier{tier}_{definition.shape}_v{v}_all", definition, total, v);
                }
            }
        }

        Debug.Log($"CeramicRenderDump: wrote PNGs to {outDir}");
    }

    private static void WriteSprite(string outDir, string name, CeramicDefinition definition, int repaired, int colourVariant)
    {
        Sprite sprite = CeramicSilhouetteSprite.GetComposite(definition.shape, definition.cracks, repaired, colourVariant);
        var texture = sprite.texture;

        // Composite against the design's screen background so the dark
        // silhouette is judged the way it will actually be seen, not against
        // transparency (which reads as a black blob in most viewers).
        var flat = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        Color background = new Color(26f / 255f, 26f / 255f, 46f / 255f, 1f); // #1a1a2e
        Color[] src = texture.GetPixels();
        var dst = new Color[src.Length];
        for (int i = 0; i < src.Length; i++)
        {
            dst[i] = Color.Lerp(background, new Color(src[i].r, src[i].g, src[i].b, 1f), src[i].a);
        }

        flat.SetPixels(dst);
        flat.Apply();

        File.WriteAllBytes(Path.Combine(outDir, name + ".png"), flat.EncodeToPNG());
    }
}
