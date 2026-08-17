/// <summary>
/// Raw string storage for save data, abstracted away from
/// <see cref="SaveManager"/> so serialization/corruption-handling logic can
/// be unit-tested with an in-memory fake instead of real PlayerPrefs.
/// </summary>
public interface ISavePersistence
{
    bool HasSavedData();
    string Load();
    void Save(string json);
}
