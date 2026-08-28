using ShelfRush.Core;
using UnityEngine;

namespace ShelfRush.Save
{
    /// <summary>
    /// Локальное сохранение через PlayerPrefs + JsonUtility.
    /// (WebGL/Яндекс-облачный save подключается отдельной реализацией ISaveService.)
    /// </summary>
    public sealed class PlayerPrefsSaveService : ISaveService
    {
        private const string Key = "shelf_rush_save";

        public bool HasSave => PlayerPrefs.HasKey(Key);

        public void Initialize(ServiceLocator services) { }

        public void Dispose() { }

        public void Save(SaveData data)
        {
            if (data == null) return;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        }

        public bool TryLoad(out SaveData data)
        {
            if (!PlayerPrefs.HasKey(Key))
            {
                data = null;
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key));
                return data != null;
            }
            catch
            {
                data = null;
                return false;
            }
        }

        public void Delete() => PlayerPrefs.DeleteKey(Key);
    }
}