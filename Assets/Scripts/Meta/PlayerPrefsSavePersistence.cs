using UnityEngine;

/// <summary>
/// The only <see cref="ISavePersistence"/> implementation that touches
/// UnityEngine — everything else in the save pipeline is plain,
/// scene-independent C#.
/// </summary>
public sealed class PlayerPrefsSavePersistence : ISavePersistence
{
    private const string SaveKey = "GoldenBreak.SaveData";

    public bool HasSavedData()
    {
        return PlayerPrefs.HasKey(SaveKey);
    }

    public string Load()
    {
        return PlayerPrefs.GetString(SaveKey, null);
    }

    public void Save(string json)
    {
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }
}
