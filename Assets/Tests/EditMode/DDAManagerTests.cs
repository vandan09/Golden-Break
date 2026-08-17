using NUnit.Framework;
using UnityEngine;

public class DDAManagerTests
{
    [TestCase("single", DDAManager.PieceSizeTier.Small)]
    [TestCase("1x2", DDAManager.PieceSizeTier.Small)]
    [TestCase("2x1", DDAManager.PieceSizeTier.Small)]
    [TestCase("2x2", DDAManager.PieceSizeTier.Small)]
    [TestCase("L", DDAManager.PieceSizeTier.Small)]
    [TestCase("J", DDAManager.PieceSizeTier.Small)]
    [TestCase("S", DDAManager.PieceSizeTier.Small)]
    [TestCase("Z", DDAManager.PieceSizeTier.Small)]
    [TestCase("T", DDAManager.PieceSizeTier.Small)]
    [TestCase("3x3", DDAManager.PieceSizeTier.Large)]
    [TestCase("1x5", DDAManager.PieceSizeTier.Large)]
    [TestCase("5x1", DDAManager.PieceSizeTier.Large)]
    [TestCase("L3", DDAManager.PieceSizeTier.Large)]
    [TestCase("J3", DDAManager.PieceSizeTier.Large)]
    public void GetSizeTier_KnownPieceIds_MatchesExpectedTier(string pieceId, DDAManager.PieceSizeTier expected)
    {
        Assert.AreEqual(expected, DDAManager.GetSizeTier(pieceId));
    }

    [Test]
    public void Classify_LifetimeAverageZero_ReturnsNormal()
    {
        Assert.AreEqual(DDAManager.DdaState.Normal, DDAManager.Classify(500f, 0f));
    }

    [Test]
    public void Classify_Last10WellBelowLifetime_ReturnsStruggling()
    {
        Assert.AreEqual(DDAManager.DdaState.Struggling, DDAManager.Classify(500f, 1000f));
    }

    [Test]
    public void Classify_Last10WellAboveLifetime_ReturnsThriving()
    {
        Assert.AreEqual(DDAManager.DdaState.Thriving, DDAManager.Classify(1500f, 1000f));
    }

    [Test]
    public void Classify_Last10CloseToLifetime_ReturnsNormal()
    {
        Assert.AreEqual(DDAManager.DdaState.Normal, DDAManager.Classify(1050f, 1000f));
    }

    [Test]
    public void GetWeightMultiplier_Struggling_BoostsSmallAndSuppressesLarge()
    {
        Assert.AreEqual(1.15f, DDAManager.GetWeightMultiplier(DDAManager.PieceSizeTier.Small, DDAManager.DdaState.Struggling), 0.0001f);
        Assert.AreEqual(0.85f, DDAManager.GetWeightMultiplier(DDAManager.PieceSizeTier.Large, DDAManager.DdaState.Struggling), 0.0001f);
    }

    [Test]
    public void GetWeightMultiplier_Thriving_BoostsLargeAndSuppressesSmall()
    {
        Assert.AreEqual(1.15f, DDAManager.GetWeightMultiplier(DDAManager.PieceSizeTier.Large, DDAManager.DdaState.Thriving), 0.0001f);
        Assert.AreEqual(0.85f, DDAManager.GetWeightMultiplier(DDAManager.PieceSizeTier.Small, DDAManager.DdaState.Thriving), 0.0001f);
    }

    [Test]
    public void GetWeightMultiplier_Normal_IsAlwaysOne()
    {
        Assert.AreEqual(1f, DDAManager.GetWeightMultiplier(DDAManager.PieceSizeTier.Small, DDAManager.DdaState.Normal));
        Assert.AreEqual(1f, DDAManager.GetWeightMultiplier(DDAManager.PieceSizeTier.Large, DDAManager.DdaState.Normal));
    }

    [Test]
    public void GetWeightMultiplier_MaxAdjustmentIsFifteenPercent()
    {
        Assert.AreEqual(0.15f, DDAManager.MaxAdjustment);
    }

    [Test]
    public void GetWeightMultiplier_PieceOverload_MatchesTierAndStateComputedSeparately()
    {
        var piece = ScriptableObject.CreateInstance<PieceDefinition>();
        piece.pieceId = "single";

        float viaPiece = DDAManager.GetWeightMultiplier(piece, last10Average: 500f, lifetimeAverage: 1000f);
        float viaTierState = DDAManager.GetWeightMultiplier(DDAManager.PieceSizeTier.Small, DDAManager.DdaState.Struggling);

        Assert.AreEqual(viaTierState, viaPiece);
    }
}
