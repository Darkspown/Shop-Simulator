namespace ShelfRush.Levels
{
    /// <summary>
    /// Управление уровнями: старт/финиш, прогресс заказов, таймер, пауза.
    /// Контракт для чужих систем (UI, GameBootstrap, Input).
    /// </summary>
    public interface ILevelManager : Core.IGameService, Core.ITickable
    {
        LevelConfig Current { get; }
        int CompletedOrders { get; }

        void StartLevel(int index);
        void SetPaused(bool paused);
        void RestartCurrent();
    }
}