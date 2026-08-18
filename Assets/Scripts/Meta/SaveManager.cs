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
    // version step at a time. No migration steps exist yet — v1 is the only
    // schema this project has ever shipped — but this is the hook
    // BUILD_PLAN's Phase 4 task list calls for, ready to grow a case per
    // future version bump without touching LoadFrom's own control flow.
    private static void Migrate(SaveData data)
    {
        if (data.SaveVersion >= SaveData.CurrentSaveVersion)
        {
            return;
        }

        // Example for the next bump:
        // if (data.SaveVersion < 2) { /* upgrade v1 fields -> v2 shape */ }

        data.SaveVersion = SaveData.CurrentSaveVersion;
    }
}
