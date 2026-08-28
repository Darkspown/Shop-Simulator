using ShelfRush.Customers;
using ShelfRush.Economy;
using ShelfRush.Levels;
using ShelfRush.Products;
using ShelfRush.Shelves;

namespace ShelfRush.Core
{
    // ---------------------------------------------------------------------------
    //  Игровые события (payload'ы), публикуемые через IEventBus.
    //  Императив: события передают только данные (readonly-структуры), без логики.
    // ---------------------------------------------------------------------------

    /// <summary>Смена глобального состояния игры.</summary>
    public readonly struct GameStateChangedEvent
    {
        public readonly GameState Previous;
        public readonly GameState Current;

        public GameStateChangedEvent(GameState previous, GameState current)
        {
            Previous = previous;
            Current = current;
        }
    }

    /// <summary>Изменение баланса валюты (публикует EconomyService).</summary>
    public readonly struct CurrencyChangedEvent
    {
        public readonly CurrencyType Currency;
        public readonly int NewBalance;
        public readonly int Delta;

        public CurrencyChangedEvent(CurrencyType currency, int newBalance, int delta)
        {
            Currency = currency;
            NewBalance = newBalance;
            Delta = delta;
        }
    }

    /// <summary>Игрок взял товар с полки (публикует PlayerController/StockService).</summary>
    public readonly struct ProductPickedEvent
    {
        public readonly ProductData Product;

        public ProductPickedEvent(ProductData product) => Product = product;
    }

    /// <summary>Игрок доставил товар клиенту (публикует CustomerService).</summary>
    public readonly struct ProductDeliveredEvent
    {
        public readonly ProductData Product;

        public ProductDeliveredEvent(ProductData product) => Product = product;
    }

    /// <summary>Изменение количества товара на конкретной полке (публикует StockService).</summary>
    public readonly struct ShelfStockChangedEvent
    {
        public readonly ShelfData Shelf;
        public readonly int Remaining;

        public ShelfStockChangedEvent(ShelfData shelf, int remaining)
        {
            Shelf = shelf;
            Remaining = remaining;
        }
    }

    /// <summary>Создан новый заказ клиента (публикует CustomerService).</summary>
    public readonly struct CustomerOrderCreatedEvent
    {
        public readonly CustomerOrder Order;

        public CustomerOrderCreatedEvent(CustomerOrder order) => Order = order;
    }

    /// <summary>Заказ клиента выполнен (публикует CustomerService; слушает EconomyService и LevelManager).</summary>
    public readonly struct CustomerOrderCompletedEvent
    {
        public readonly CustomerOrder Order;
        public readonly int Reward;

        public CustomerOrderCompletedEvent(CustomerOrder order, int reward)
        {
            Order = order;
            Reward = reward;
        }
    }

    /// <summary>Клиент ушёл, не дождавшись заказа (тайм-аут).</summary>
    public readonly struct CustomerLeftEvent
    {
        public readonly CustomerOrder Order;

        public CustomerLeftEvent(CustomerOrder order) => Order = order;
    }

    /// <summary>Начат уровень (публикует LevelManager).</summary>
    public readonly struct LevelStartedEvent
    {
        public readonly LevelConfig Config;

        public LevelStartedEvent(LevelConfig config) => Config = config;
    }

    /// <summary>Уровень завершён (публикует LevelManager).</summary>
    public readonly struct LevelCompletedEvent
    {
        public readonly LevelConfig Config;
        public readonly int CompletedOrders;
        public readonly int TargetOrders;

        public LevelCompletedEvent(LevelConfig config, int completedOrders, int targetOrders)
        {
            Config = config;
            CompletedOrders = completedOrders;
            TargetOrders = targetOrders;
        }
    }

    /// <summary>Пауза/возобновление уровня (публикует LevelManager по запросу PlatformService).</summary>
    public readonly struct LevelPauseChangedEvent
    {
        public readonly bool Paused;

        public LevelPauseChangedEvent(bool paused) => Paused = paused;
    }

    /// <summary>
    /// Запрос паузы от платформы (потеря фокуса/реклама). Публикует PlatformService;
    /// слушает LevelManager и применяет паузу.
    /// </summary>
    public readonly struct GamePauseRequestedEvent
    {
        public readonly bool Paused;

        public GamePauseRequestedEvent(bool paused) => Paused = paused;
    }
}