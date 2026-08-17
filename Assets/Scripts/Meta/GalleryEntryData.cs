using Newtonsoft.Json;

/// <summary>
/// One completed ceramic in the player's gallery (CLAUDE.md §3.5). Score is
/// the cumulative total earned across every game played while working on
/// this ceramic, not a single best game.
/// </summary>
[System.Serializable]
public sealed class GalleryEntryData
{
    [JsonProperty("tier")]
    public int Tier;

    [JsonProperty("date")]
    public string Date;

    [JsonProperty("score")]
    public int Score;
}
