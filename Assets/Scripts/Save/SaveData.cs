using System;

namespace ShelfRush.Save
{
    /// <summary>
    /// Снимок данных для сохранения (сериализуется JsonUtility). Отдельные поля вместо
    /// Dictionary, чтобы JsonUtility мог напрямую сериализовать.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public int coins;
        public int gems;
        public int lastCompletedLevel;
        public string language;
    }
}