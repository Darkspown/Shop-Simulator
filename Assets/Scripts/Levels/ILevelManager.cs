namespace ShelfRush.Levels
{
    /// <summary>
    /// Управление уровнями: старт/финиш, прогресс по целям (<see cref="LevelProgress"/>),
    /// пауза. Контракт для чужих систем (UI, GameBootstrap, Input).
    /// Уровень определяется данными (<see cref="LevelData"/>) — никакого хардкода в MonoBehaviour.
    /// </summary>
    public interface ILevelManager : Core.IGameService, Core.ITickable
    {
        /// <summary>Текущий уровень (данные уровня; null, если уровень не активен).</summary>
        LevelData Current { get; }

        /// <summary>Runtime-прогресс текущего уровня по целям (0/10 … 10/10).</summary>
        LevelProgress Progress { get; }

        /// <summary>Сколько единиц товара размещено за текущий уровень (сумма по целям).</summary>
        int PlacedProducts { get; }

        /// <summary>Сколько целей текущего уровня полностью выполнено.</summary>
        int CompletedShelves { get; }

        /// <summary>Общий прогресс уровня: PlacedProducts / TotalTarget (0..1).</summary>
        float PlacementProgress { get; }

        /// <summary>Событие изменения прогресса (для UI / LevelProgress).</summary>
        event System.Action PlacementProgressChanged;

        void StartLevel(int index);
        void SetPaused(bool paused);
        void RestartCurrent();
    }
}