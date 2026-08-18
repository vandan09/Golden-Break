/// <summary>
/// Resolves a save-file ceramic tier number to which base
/// <see cref="CeramicDefinition"/> (1-9) to display and which colour
/// variant to render it with — CLAUDE.md §3.4's "10+: repeat 6-9 with
/// colour variants." Tiers 1-9 are unique; each full cycle through
/// 6,7,8,9 beyond tier 9 increments the colour variant.
/// </summary>
public static class CeramicTierResolver
{
    private const int BaseTierCount = 9;
    private const int CycleStartTier = 10;
    private const int CycleDefinitionStart = 6;
    private const int CycleLength = 4; // definitions 6, 7, 8, 9

    public static int ResolveDefinitionTier(int tier)
    {
        if (tier <= BaseTierCount)
        {
            return tier;
        }

        int offset = (tier - CycleStartTier) % CycleLength;
        return CycleDefinitionStart + offset;
    }

    public static int ResolveColourVariant(int tier)
    {
        if (tier <= BaseTierCount)
        {
            return 0;
        }

        int cycleIndex = (tier - CycleStartTier) / CycleLength;
        return cycleIndex + 1;
    }
}
