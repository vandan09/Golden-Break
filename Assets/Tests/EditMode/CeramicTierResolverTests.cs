using NUnit.Framework;

public class CeramicTierResolverTests
{
    [TestCase(1, 1)]
    [TestCase(5, 5)]
    [TestCase(9, 9)]
    public void ResolveDefinitionTier_BaseTiers_ReturnsSameTier(int tier, int expectedDefinitionTier)
    {
        Assert.AreEqual(expectedDefinitionTier, CeramicTierResolver.ResolveDefinitionTier(tier));
    }

    [TestCase(1, 0)]
    [TestCase(9, 0)]
    public void ResolveColourVariant_BaseTiers_ReturnsZero(int tier, int expectedVariant)
    {
        Assert.AreEqual(expectedVariant, CeramicTierResolver.ResolveColourVariant(tier));
    }

    [TestCase(10, 6)]
    [TestCase(11, 7)]
    [TestCase(12, 8)]
    [TestCase(13, 9)]
    public void ResolveDefinitionTier_FirstCycle_CyclesThroughSixToNine(int tier, int expectedDefinitionTier)
    {
        Assert.AreEqual(expectedDefinitionTier, CeramicTierResolver.ResolveDefinitionTier(tier));
    }

    [TestCase(10, 1)]
    [TestCase(11, 1)]
    [TestCase(12, 1)]
    [TestCase(13, 1)]
    public void ResolveColourVariant_FirstCycle_IsVariantOne(int tier, int expectedVariant)
    {
        Assert.AreEqual(expectedVariant, CeramicTierResolver.ResolveColourVariant(tier));
    }

    [TestCase(14, 6)]
    [TestCase(15, 7)]
    [TestCase(16, 8)]
    [TestCase(17, 9)]
    public void ResolveDefinitionTier_SecondCycle_CyclesThroughSixToNineAgain(int tier, int expectedDefinitionTier)
    {
        Assert.AreEqual(expectedDefinitionTier, CeramicTierResolver.ResolveDefinitionTier(tier));
    }

    [TestCase(14, 2)]
    [TestCase(17, 2)]
    public void ResolveColourVariant_SecondCycle_IsVariantTwo(int tier, int expectedVariant)
    {
        Assert.AreEqual(expectedVariant, CeramicTierResolver.ResolveColourVariant(tier));
    }

    [Test]
    public void ResolveColourVariant_ThirdCycleStart_IsVariantThree()
    {
        Assert.AreEqual(3, CeramicTierResolver.ResolveColourVariant(18));
        Assert.AreEqual(6, CeramicTierResolver.ResolveDefinitionTier(18));
    }
}
