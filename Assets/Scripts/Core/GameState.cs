namespace ShelfRush.Core
{
    /// <summary>Глобальные состояния игры (lifecycle):</summary>
    public enum GameState
    {
        /// <summary>Старт, ещё не началась (инициализация сервисов).</summary>
        Boot,
        /// <summary>Главное меню.</summary>
        MainMenu,
        /// <summary>Активный игровой уровень.</summary>
        LevelPlaying,
        /// <summary>Уровень на паузе (реклама, потеря фокуса, встроенный вызов).</summary>
        LevelPaused,
        /// <summary>Уровень завершён (победа/тайм-аут).</summary>
        LevelCompleted,
        /// <summary>Игра окончена (например, конец кампании).</summary>
        GameOver
    }
}