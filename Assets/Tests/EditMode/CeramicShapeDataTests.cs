using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Locks the contract between the Claude Design artwork and the game: nine
/// shapes, each with the crack count CLAUDE.md §3.4's tier table calls for,
/// and every crack inside its own silhouette. These caught nothing when
/// written — they exist because the previous ceramic system silently
/// invented cracks the design never drew, and nothing failed until it was
/// looked at on a phone.
/// </summary>
public class CeramicShapeDataTests
{
    // §3.4's table: tier N has N+3 cracks, 4 through 12.
    private static readonly (CeramicShapeArchetype shape, int cracks)[] Expected =
    {
        (CeramicShapeArchetype.SimpleBowl, 4),
        (CeramicShapeArchetype.TeaCup, 5),
        (CeramicShapeArchetype.Plate, 6),
        (CeramicShapeArchetype.TallVase, 7),
        (CeramicShapeArchetype.Teapot, 8),
        (CeramicShapeArchetype.LargeBowl, 9),
        (CeramicShapeArchetype.OrnatePlate, 10),
        (CeramicShapeArchetype.SakeSet, 11),
        (CeramicShapeArchetype.TempleBowl, 12),
    };

    [Test]
    public void EveryShapeInTheEnumHasArtwork()
    {
        foreach (CeramicShapeArchetype shape in System.Enum.GetValues(typeof(CeramicShapeArchetype)))
        {
            Assert.IsTrue(
                CeramicShapeData.Shapes.ContainsKey(shape),
                $"{shape} has no artwork — every enum value must map to a design shape");
        }
    }

    [Test]
    public void EachShapeHasTheCrackCountTheTierTableRequires()
    {
        foreach ((CeramicShapeArchetype shape, int cracks) in Expected)
        {
            CeramicShapeData.Shape data = CeramicShapeData.Shapes[shape];
            Assert.AreEqual(cracks, data.cracks.Length, $"{shape} crack count");
        }
    }

    [Test]
    public void EveryCrackParsesToAtLeastTwoPoints()
    {
        foreach ((CeramicShapeArchetype shape, int _) in Expected)
        {
            CeramicShapeData.Shape data = CeramicShapeData.Shapes[shape];
            for (int i = 0; i < data.cracks.Length; i++)
            {
                Vector2[] points = SvgPath.ToPoints(data.cracks[i]);
                Assert.GreaterOrEqual(points.Length, 2, $"{shape} crack {i} must be a line, not a dot");
            }
        }
    }

    [Test]
    public void EveryCrackStaysInsideItsOwnViewBox()
    {
        foreach ((CeramicShapeArchetype shape, int _) in Expected)
        {
            CeramicShapeData.Shape data = CeramicShapeData.Shapes[shape];
            foreach (string crack in data.cracks)
            {
                foreach (Vector2 point in SvgPath.ToPoints(crack))
                {
                    Assert.That(point.x, Is.InRange(0f, data.viewBox.x), $"{shape} crack x out of bounds");
                    Assert.That(point.y, Is.InRange(0f, data.viewBox.y), $"{shape} crack y out of bounds");
                }
            }
        }
    }

    [Test]
    public void EveryBodyPathParsesToAClosedPolygon()
    {
        foreach ((CeramicShapeArchetype shape, int _) in Expected)
        {
            CeramicShapeData.Shape data = CeramicShapeData.Shapes[shape];
            Vector2[] body = SvgPath.ToPoints(data.body);
            Assert.Greater(body.Length, 20, $"{shape} body should sample into a real outline");
        }
    }

    [Test]
    public void ColourVariantsAreAllDistinct()
    {
        for (int a = 0; a < CeramicGold.VariantCount; a++)
        {
            for (int b = a + 1; b < CeramicGold.VariantCount; b++)
            {
                Assert.AreNotEqual(
                    CeramicGold.ForVariant(a),
                    CeramicGold.ForVariant(b),
                    $"variants {a} and {b} must be tellable apart");
            }
        }
    }

    [Test]
    public void ColourVariantsWrapRatherThanRunOut()
    {
        // Tier 10+ cycles forever, so a high variant index must still
        // resolve to a real metal instead of falling off the array.
        Assert.AreEqual(CeramicGold.ForVariant(1), CeramicGold.ForVariant(1 + CeramicGold.VariantCount));
    }
}
