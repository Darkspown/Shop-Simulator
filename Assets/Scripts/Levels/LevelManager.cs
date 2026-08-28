using System.Collections.Generic;
using ShelfRush.Core;
using ShelfRush.Customers;
using ShelfRush.Shelves;

namespace ShelfRush.Levels
{
    /// <summary>
    /// Менеджер уровней. При старте регистрирует полки в StockService, публикует
    /// LevelStartedEvent, тикает таймер, считает выполненные заказы (подписка на
    /// CustomerOrderCompletedEvent) и завершает уровень по цели или тайм-ауту.
    /// </summary>
    public sealed class LevelManager : ILevelManager
    {
        private readonly List<LevelConfig> _levels = new List<LevelConfig>();

        private IEventBus _events;
        private IStockService _stock;
        private ICustomerService _customers;
        private System.IDisposable _orderCompletedSub;
        private System.IDisposable _orderLeftSub;
        private System.IDisposable _pauseRequestSub;

        private float _remainingTime;
        private bool _paused;

        public LevelConfig Current { get; private set; }
        public int CompletedOrders { get; private set; }

        public LevelManager(IEnumerable<LevelConfig> levels)
        {
            if (levels != null)
            {
                foreach (var l in levels)
                {
                    if (l != null) _levels.Add(l);
                }
            }
        }

        public void Initialize(ServiceLocator services)
        {
            _events = services.Get<IEventBus>();
            _stock = services.Get<IStockService>();
            _customers = services.Get<ICustomerService>();

            _orderCompletedSub = _events.Subscribe<CustomerOrderCompletedEvent>(OnOrderCompleted);
            _orderLeftSub = _events.Subscribe<CustomerLeftEvent>(OnCustomerLeft);
            _pauseRequestSub = _events.Subscribe<GamePauseRequestedEvent>(OnPauseRequested);
        }

        public void Dispose()
        {
            _orderCompletedSub?.Dispose();
            _orderLeftSub?.Dispose();
            _pauseRequestSub?.Dispose();
            _events = null;
            _stock = null;
            _customers = null;
        }

        public void Tick(float deltaTime)
        {
            if (Current == null || _paused) return;

            _remainingTime -= deltaTime;
            if (_remainingTime <= 0f)
            {
                FinishLevel();
            }
        }

        public void StartLevel(int index)
        {
            if (index < 0 || index >= _levels.Count) return;

            Current = _levels[index];
            CompletedOrders = 0;
            _remainingTime = Current.TimeLimitSeconds;
            _paused = false;

            // Регистрируем полки уровня в учёте запасов.
            foreach (var shelf in Current.Shelves)
            {
                _stock.RegisterShelf(shelf);
            }

            _events?.Publish(new LevelStartedEvent(Current));
        }

        public void SetPaused(bool paused)
        {
            if (_paused == paused) return;
            _paused = paused;
            _events?.Publish(new LevelPauseChangedEvent(paused));
        }

        public void RestartCurrent()
        {
            if (Current != null) StartLevel(Current.LevelIndex);
        }

        private void OnOrderCompleted(CustomerOrderCompletedEvent evt)
        {
            if (Current == null) return;
            CompletedOrders++;
            if (CompletedOrders >= Current.TargetOrders)
            {
                FinishLevel();
            }
        }

        private void OnPauseRequested(GamePauseRequestedEvent evt)
        {
            SetPaused(evt.Paused);
        }

        private void OnCustomerLeft(CustomerLeftEvent evt)
        {
            // Базовая архитектура: уход клиента не штрафует прогресс. Логика штрафов — позже.
        }

        private void FinishLevel()
        {
            var config = Current;
            Current = null;
            _events?.Publish(new LevelCompletedEvent(config, CompletedOrders, config.TargetOrders));
        }
    }
}