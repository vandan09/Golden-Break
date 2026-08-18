using System;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Singleton owner of the local save file. Serialization and corruption
/// handling (<see cref="LoadFrom"/>) is a static, scene-independent method
/// so it can be unit-tested with an in-memory <see cref="ISavePersistence"/>
/// fake — the MonoBehaviour wrapper only wires that logic to Unity's
/// lifecycle (load on Awake, save on pause/quit).
/// </summary>
public sealed class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public static event Action<SaveData> OnSaveLoaded;
    public static event Action OnSaveCorrupted;

    [SerializeField]
    private bool _persistAcrossScenes = true;

    private ISavePersistence _persistence;
    private SaveData _current;

    public SaveData Current => _current;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (_persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        _persistence = new PlayerPrefsSavePersistence();
        Load();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Save();
        }
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    public void Load()
    {
        _current = LoadFrom(_persistence);
        OnSaveLoaded?.Invoke(_current);
    }

    public void Save()
    {
        try
        {
            string json = JsonConvert.SerializeObject(_current);
            _persistence.Save(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveManager: failed to save — {e.Message}");
        }
    }

    internal static SaveData LoadFrom(ISavePersistence persistence)
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        if (!persistence.HasSavedData())
        {
            return SaveData.CreateFresh(today);
        }

        string json = persistence.Load();
        try
        {
            SaveData data = JsonConvert.DeserializeObject<SaveData>(json);
            if (data == null)
            {
                throw new JsonException("Deserialized to null.");
            }

            Migrate(data);
            return data;
        }
        catch (JsonException e)
        {
            Debug.LogError($"SaveManager: save data corrupted — {e.Message}");
            OnSaveCorrupted?.Invoke();
            return SaveData.CreateFresh(today);
        }
    }

    // Upgrades an older on-disk save to the current schema in place, one
    // version step at a time. No version-bump migration steps exist yet —
    // v1 is the only schema this project has ever shipped — but this is
    // the hook BUILD_PLAN's Phase 4 task list calls for, ready to grow a
    // case per future version bump without touching LoadFrom's own
    // control flow.
    private static void Migrate(SaveData data)
    {
        // Backfills any collection/object field that predates this
        // on-disk save — safe to run unconditionally, not gated by the
        // version check below. Newtonsoft leaves a JSON-missing field at
        // C#'s default (null, for every reference-type field in this
        // schema) rather than erroring, and fields get added to the v1
        // schema over time without a version bump (same precedent as
        // CeramicCumulativeScore in Phase 3) — a save written before
        // gallery_frames_owned existed, for example, would otherwise NPE
        // the first time anything calls .Add on it.
        if (data.Gallery == null) data.Gallery = new System.Collections.Generic.List<GalleryEntryData>();
        if (data.MilestonesClaimed == null) data.MilestonesClaimed = new System.Collections.Generic.List<int>();
        if (data.DailyCompleted == null) data.DailyCompleted = new System.Collections.Generic.List<string>();
        if (data.DailyBestScores == null) data.DailyBestScores = new System.Collections.Generic.Dictionary<string, int>();
        if (data.IapThemesOwned == null) data.IapThemesOwned = new System.Collections.Generic.List<string>();
        if (data.GalleryFramesOwned == null) data.GalleryFramesOwned = new System.Collections.Generic.List<string>();
        if (data.DdaLast10Scores == null) data.DdaLast10Scores = new System.Collections.Generic.List<int>();
        if (data.CurrentCeramic == null) data.CurrentCeramic = CeramicProgressData.CreateFresh();
        if (data.Settings == null) data.Settings = SaveSettingsData.CreateDefault();

        if (data.SaveVersion >= SaveData.CurrentSaveVersion)
        {
            return;
        }

        // Example for the next version bump:
        // if (data.SaveVersion < 2) { /* upgrade v1 fields -> v2 shape */ }

        data.SaveVersion = SaveData.CurrentSaveVersion;
    }
}
