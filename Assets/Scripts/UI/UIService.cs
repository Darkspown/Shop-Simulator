using System;
using System.Collections.Generic;
using ShelfRush.Core;
using ShelfRush.Customers;
using ShelfRush.Economy;
using ShelfRush.Levels;

namespace ShelfRush.UI
{
    /// <summary>
    /// Сервис UI. Читает игровые события из EventBus и проксирует их в <see cref="IHUDView"/>
    /// (устанавливается из сцены при создании UI). Сам — plain C#, без MonoBehaviour.
    /// </summary>
    public sealed class UIService : IGameService
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private IEventBus _events;

        /// <summary>Активная HUD-вью; подключается из сцены.</summary>
        public IHUDView HUD { get; set; }

        public void Initialize(ServiceLocator services)
        {
            _events = services.Get<IEventBus>();

            _subscriptions.Add(_events.Subscribe<CurrencyChangedEvent>(e => HUD?.SetBalance(e.Currency, e.NewBalance)));
            _subscriptions.Add(_events.Subscribe<CustomerOrderCreatedEvent>(e => HUD?.ShowOrder(e.Order)));
            _subscriptions.Add(_events.Subscribe<CustomerOrderCompletedEvent>(e => HUD?.ShowOrderCompleted(e.Reward)));
            _subscriptions.Add(_events.Subscribe<LevelStartedEvent>(e => HUD?.ShowLevelStart(e.Config)));
            _subscriptions.Add(_events.Subscribe<LevelCompletedEvent>(e => HUD?.ShowLevelComplete(e.CompletedOrders >= e.TargetOrders, e.CompletedOrders, e.TargetOrders)));
            _subscriptions.Add(_events.Subscribe<LevelPauseChangedEvent>(e => HUD?.SetPaused(e.Paused)));
        }

        public void Dispose()
        {
            for (var i = _subscriptions.Count - 1; i >= 0; i--) _subscriptions[i].Dispose();
            _subscriptions.Clear();
            _events = null;
            HUD = null;
        }
    }
}