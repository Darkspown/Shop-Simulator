namespace ShelfRush.Core
{
    /// <summary>Управление глобальным состоянием игры.</summary>
    public interface IGameStateMachine : IGameService
    {
        GameState Current { get; }
        void Set(GameState next);
    }

    /// <summary>
    /// Хранит текущее состояние и публикует <see cref="GameStateChangedEvent"/> в EventBus.
    /// Никто не может менять состояние напрямую, кроме этого сервиса, что делает lifecycle предсказуемым.
    /// </summary>
    public sealed class GameStateMachine : IGameStateMachine
    {
        private IEventBus _events;

        public GameState Current { get; private set; } = GameState.Boot;

        public void Initialize(ServiceLocator services)
        {
            _events = services.Get<IEventBus>();
        }

        public void Set(GameState next)
        {
            if (Current == next) return;
            var previous = Current;
            Current = next;
            _events?.Publish(new GameStateChangedEvent(previous, next));
        }

        public void Dispose()
        {
            _events = null;
        }
    }
}