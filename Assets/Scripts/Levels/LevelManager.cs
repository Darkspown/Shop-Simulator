using System.Collections.Generic;
using ShelfRush.Core;
using ShelfRush.Economy;
using ShelfRush.Shelves;

namespace ShelfRush.Levels
{
    /// <summary>
    /// Менеджер уровней. Data-driven: конкретный уровень описан в <see cref="LevelData"/> (SO),
    /// здесь НЕТ хардкода товара/полки/цели. Один LevelManager работает с любым LevelData.
    /// Flow (L1): Place (ShelfController → ShelfProductPlacedEvent) → Reward за товар
    /// (EconomyService) → Progress (0/10..10/10) → 10/10 → Shelf Complete (reward за цель)
    /// → Level Complete (reward за уровень) → LevelCompletedEvent.
    /// </summary>
    public sealed class LevelManager : ILevelManager
    {
        private readonly List<LevelData> _levels = new List<LevelData>();

        private IEventBus _events;
        private IStockService _stock;
        private IEconomyService _economy;

        private System.IDisposable _shelfPlacedSub;
        private System.IDisposable _shelfCompletedSub;
        private System.IDisposable _pauseRequestSub;

        private bool _paused;
        private bool _finished;

        public LevelData Current { get; private set; }

        public LevelProgress Progress { get; private set; }

        public int PlacedProducts => Progress != null ? Progress.TotalPlaced : 0;

        public int CompletedShelves { get; private set; }

        public float PlacementProgress
        {
            get
            {
                if (Progress == null) return 0f;
                var total = Progress.TotalTarget;
                return total <= 0 ? 0f : System.Math.Clamp(PlacedProducts / (float)total, 0f, 1f);
            }
        }

        public event System.Action PlacementProgressChanged;

        public LevelManager(IEnumerable<LevelData> levels)
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
            _economy = services.Get<IEconomyService>();

            _shelfPlacedSub = _events.Subscribe<ShelfProductPlacedEvent>(OnShelfProductPlaced);
            _shelfCompletedSub = _events.Subscribe<ShelfCompletedEvent>(OnShelfCompleted);
            _pauseRequestSub = _events.Subscribe<GamePauseRequestedEvent>(OnPauseRequested);
        }

        public void Dispose()
        {
            _shelfPlacedSub?.Dispose();
            _shelfCompletedSub?.Dispose();
            _pauseRequestSub?.Dispose();
            _events = null;
            _stock = null;
            _economy = null;
        }

        public void Tick(float deltaTime)
        {
            // Level 1 не имеет таймера: цель достигается размещением товаров.
        }

        public void StartLevel(int index)
        {
            if (index < 0 || index >= _levels.Count) return;

            Current = _levels[index];
            Progress = new LevelProgress(Current);
            CompletedShelves = 0;
            _paused = false;
            _finished = false;
            PlacementProgressChanged?.Invoke();

            // Регистрируем полки всех целей уровня в учёте запасов.
            var objectives = Current.Objectives;
            for (var i = 0; i < objectives.Length; i++)
            {
                if (objectives[i] != null && objectives[i].TargetShelf != null)
                {
                    _stock.RegisterShelf(objectives[i].TargetShelf);
                }
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

        private void OnPauseRequested(GamePauseRequestedEvent evt) => SetPaused(evt.Paused);

        /// <summary>Товар размещён → прогресс цели + награды (цель и уровень).</summary>
        private void OnShelfProductPlaced(ShelfProductPlacedEvent evt)
        {
            if (Current == null || Progress == null || _finished) return;

            var index = FindObjective(evt);
            if (index < 0) return;

            if (Progress.Advance(index))
            {
                CompletedShelves++;
                var obj = Current.Objectives[index];
                if (obj != null && obj.ShelfCompleteReward > 0)
                {
                    _economy?.AddCurrency(CurrencyType.Coins, obj.ShelfCompleteReward);
                }
            }
            PlacementProgressChanged?.Invoke();

            if (Progress.IsComplete) FinishLevel();
        }

        /// <summary>Страховка: полка физически заполнилась и все цели достигнуты → завершаем.</summary>
        private void OnShelfCompleted(ShelfCompletedEvent evt)
        {
            if (Current == null || Progress == null || _finished) return;
            if (Progress.IsComplete) FinishLevel();
        }

        /// <summary>Первая невыполненная цель, которой соответствует размещение.</summary>
        private int FindObjective(ShelfProductPlacedEvent evt)
        {
            var objectives = Current.Objectives;
            for (var i = 0; i < objectives.Length; i++)
            {
                var o = objectives[i];
                if (o == null || o.Type != LevelObjectiveType.FillShelf) continue;
                if (o.TargetShelf != evt.Shelf) continue;
                if (o.TargetProduct != null && o.TargetProduct != evt.Product) continue;
                if (Progress.IsCompleted(i)) continue;
                return i;
            }
            return -1;
        }

        private void FinishLevel()
        {
            if (_finished) return;
            _finished = true;

            var config = Current;
            var placed = PlacedProducts;
            var total = config != null ? config.TotalTarget : 0;
            var success = config != null && Progress != null && Progress.IsComplete;

            if (config != null && config.CompletionReward > 0)
            {
                _economy?.AddCurrency(CurrencyType.Coins, config.CompletionReward);
            }

            Current = null;
            _events?.Publish(new LevelCompletedEvent(config, success, CompletedShelves, placed, total));
        }
    }
}
