using Newtonsoft.Json;

/// <summary>
/// Player-configurable settings (CLAUDE.md §7.3 "settings" block).
/// </summary>
[System.Serializable]
public sealed class SaveSettingsData
{
    [JsonProperty("sound")]
    public bool Sound;

    [JsonProperty("music")]
    public bool Music;

    [JsonProperty("haptics")]
    public bool Haptics;

    [JsonProperty("high_contrast")]
    public bool HighContrast;

    public static SaveSettingsData CreateDefault()
    {
        return new SaveSettingsData
        {
            Sound = true,
            Music = true,
            Haptics = true,
            HighContrast = false
        };
    }
}
