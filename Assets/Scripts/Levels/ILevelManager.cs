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

        /// <summary>Сколько товаров размещено на полках уровня (ShelfProductPlacedEvent).</summary>
        int PlacedProducts { get; }

        /// <summary>Сколько полок уровня полностью заполнено (ShelfCompletedEvent).</summary>
        int CompletedShelves { get; }

        /// <summary>Прогресс наполнения полок уровня: PlacedProducts / суммарная вместимость (0..1).</summary>
        float PlacementProgress { get; }

        /// <summary>Событие изменения прогресса наполнения полок (для UI/LevelProgress).</summary>
        event System.Action PlacementProgressChanged;

        void StartLevel(int index);
        void SetPaused(bool paused);
        void RestartCurrent();
    }
}