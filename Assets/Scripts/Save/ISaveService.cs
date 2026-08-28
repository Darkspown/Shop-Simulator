namespace ShelfRush.Save
{
    /// <summary>
    /// Абстракция сохранений. Игровая логика (Economy/Level) не знает, куда пишется
    /// save — в PlayerPrefs, Yandex cloud или ещё куда-то.
    /// </summary>
    public interface ISaveService : Core.IGameService
    {
        bool HasSave { get; }

        void Save(SaveData data);

        bool TryLoad(out SaveData data);

        void Delete();
    }
}