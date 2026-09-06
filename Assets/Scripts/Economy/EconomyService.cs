using ShelfRush.Core;

namespace ShelfRush.Economy
{
    /// <summary>
    /// Сервис экономики. Обёртка над Wallet: начисляет/списывает валюту,
    /// публикует CurrencyChangedEvent в EventBus и автоматически начисляет награду
    /// за выполненные заказы клиентов (подписка на CustomerOrderCompletedEvent).
    /// </summary>
    public sealed class EconomyService : IEconomyService
    {
        private readonly Wallet _wallet;
        private IEventBus _events;
        private System.IDisposable _orderCompletedSubscription;
        private System.IDisposable _shelfPlacedSubscription;

        public EconomyService(Wallet wallet) => _wallet = wallet ?? new Wallet();

        public Wallet Wallet => _wallet;

        public void Initialize(ServiceLocator services)
        {
            _events = services.Get<IEventBus>();

            if (services.TryGet<EconomyConfig>(out var config) && config != null)
            {
                LoadInitial(config);
            }

            // Data flow: Customer (заказ выполнен) -> Economy (награда) -> UI.
            _orderCompletedSubscription = _events.Subscribe<CustomerOrderCompletedEvent>(OnOrderCompleted);

            // Shelf System: размещение товара на полку -> reward за размещение.
            // Награда выдаётся ТОЛЬКО при успешном ShelfProductPlacedEvent (контроллер
            // публикует его только для правильного товара и свободного слота).
            _shelfPlacedSubscription = _events.Subscribe<ShelfProductPlacedEvent>(OnShelfProductPlaced);
        }

        public void Dispose()
        {
            _orderCompletedSubscription?.Dispose();
            _shelfPlacedSubscription?.Dispose();
            _events = null;
        }

        public void LoadInitial(EconomyConfig config)
        {
            if (config == null) return;
            _wallet.SetBalance(CurrencyType.Coins, config.StartingCoins);
            _wallet.SetBalance(CurrencyType.Gems, config.StartingGems);
        }

        public bool TrySpend(CurrencyType currency, int amount)
        {
            var ok = _wallet.TrySpend(currency, amount);
            if (ok) _events?.Publish(new CurrencyChangedEvent(currency, _wallet.GetBalance(currency), -amount));
            return ok;
        }

        public void AddCurrency(CurrencyType currency, int amount)
        {
            if (amount < 0) return;
            _wallet.Add(currency, amount);
            _events?.Publish(new CurrencyChangedEvent(currency, _wallet.GetBalance(currency), amount));
        }

        public void SetCurrency(CurrencyType currency, int amount)
        {
            var previous = _wallet.GetBalance(currency);
            _wallet.SetBalance(currency, amount);
            _events?.Publish(new CurrencyChangedEvent(currency, amount, amount - previous));
        }

        private void OnOrderCompleted(CustomerOrderCompletedEvent evt)
        {
            if (evt.Reward > 0)
            {
                AddCurrency(CurrencyType.Coins, evt.Reward);
            }
        }

        /// <summary>
        /// Награда за размещение товара на полке. Публикуется контроллером полки ТОЛЬКО
        /// при успешном размещении (правильная категория + свободный слот), поэтому
        /// неверный товар никогда не приносит reward.
        /// </summary>
        private void OnShelfProductPlaced(ShelfProductPlacedEvent evt)
        {
            if (evt.Product == null) return;
            var reward = evt.Product.RewardValue;
            if (reward > 0)
            {
                AddCurrency(CurrencyType.Coins, reward);
            }
        }
    }
}