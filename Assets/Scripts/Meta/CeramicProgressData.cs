using Newtonsoft.Json;

/// <summary>
/// Crack-repair progress for the ceramic currently in play. Persists across
/// game-overs by design (CLAUDE.md §3.4) — never reset except on completion.
/// </summary>
[System.Serializable]
public sealed class CeramicProgressData
{
    [JsonProperty("tier")]
    public int Tier;

    [JsonProperty("total_cracks")]
    public int TotalCracks;

    [JsonProperty("cracks_repaired")]
    public int CracksRepaired;

    // Not in CLAUDE.md §7.3's example JSON, but required by §3.4's tier
    // 10+ "repeat 6-9 with colour variants" rule — without it there is no
    // way to know which colour variant of a repeated tier is in progress.
    [JsonProperty("colour_variant")]
    public int ColourVariant;

    public static CeramicProgressData CreateFresh()
    {
        return new CeramicProgressData
        {
            Tier = 1,
            TotalCracks = 4,
            CracksRepaired = 0,
            ColourVariant = 0
        };
    }
}
