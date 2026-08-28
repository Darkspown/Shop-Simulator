using System.Collections.Generic;
using ShelfRush.Core;
using ShelfRush.Products;

namespace ShelfRush.Shelves
{
    /// <summary>
    /// Реализация учёта запасов полок. Ведёт курс количества по ShelfData,
    /// извещает остальные системы через EventBus (ShelfStockChangedEvent).
    /// </summary>
    public sealed class StockService : IStockService
    {
        private readonly Dictionary<ShelfData, int> _stock = new Dictionary<ShelfData, int>();
        private IEventBus _events;

        private readonly List<ShelfData> _shelves = new List<ShelfData>();

        public IReadOnlyList<ShelfData> Shelves => _shelves;

        public void Initialize(ServiceLocator services)
        {
            _events = services.Get<IEventBus>();
        }

        public void Dispose()
        {
            _stock.Clear();
            _shelves.Clear();
            _events = null;
        }

        public void RegisterShelf(ShelfData shelf)
        {
            if (shelf == null || _stock.ContainsKey(shelf)) return;
            _shelves.Add(shelf);
            _stock[shelf] = shelf.Capacity;
            _events?.Publish(new ShelfStockChangedEvent(shelf, shelf.Capacity));
        }

        public int GetStock(ShelfData shelf)
        {
            return shelf != null && _stock.TryGetValue(shelf, out var count) ? count : 0;
        }

        public bool TryTakeProduct(ShelfData shelf, out ProductData product)
        {
            product = null;
            if (shelf == null || !_stock.TryGetValue(shelf, out var count) || count <= 0) return false;

            _stock[shelf] = count - 1;
            product = shelf.Product;
            _events?.Publish(new ShelfStockChangedEvent(shelf, count - 1));
            return true;
        }

        public void Restock(ShelfData shelf)
        {
            if (shelf == null) return;
            _stock[shelf] = shelf.Capacity;
            _events?.Publish(new ShelfStockChangedEvent(shelf, shelf.Capacity));
        }
    }
}