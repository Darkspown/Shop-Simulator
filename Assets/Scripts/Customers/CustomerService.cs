using System.Collections.Generic;
using ShelfRush.Core;
using ShelfRush.Products;

namespace ShelfRush.Customers
{
    /// <summary>
    /// Сервис клиентов. Генерирует заказы из каталога товаров, ведёт учёт времени,
    /// при выполнении публикует CustomerOrderCompletedEvent (из него Economy берёт награду,
    /// а LevelManager считает прогресс), при тайм-ауте — CustomerLeftEvent.
    /// </summary>
    public sealed class CustomerService : ICustomerService
    {
        private readonly List<CustomerOrder> _orders = new List<CustomerOrder>();
        private IEventBus _events;
        private IProductCatalog _catalog;
        private int _nextOrderId;

        public IReadOnlyList<CustomerOrder> ActiveOrders => _orders;

        public void Initialize(ServiceLocator services)
        {
            _events = services.Get<IEventBus>();
            _catalog = services.Get<IProductCatalog>();
        }

        public void Dispose()
        {
            _orders.Clear();
            _events = null;
            _catalog = null;
        }

        public void Tick(float deltaTime)
        {
            if (_orders.Count == 0) return;

            for (var i = _orders.Count - 1; i >= 0; i--)
            {
                if (_orders[i].Tick(deltaTime))
                {
                    var order = _orders[i];
                    _orders.RemoveAt(i);
                    _events?.Publish(new CustomerLeftEvent(order));
                }
            }
        }

        public CustomerOrder CreateOrder(ProductData product, int count)
        {
            if (product == null || count <= 0) return null;

            // Награда за единицу товара — из ProductData.RewardValue.
            var reward = product.RewardValue * count;
            var order = new CustomerOrder(
                "order_" + (_nextOrderId++),
                product,
                count,
                reward,
                timeLimit: 30f + count * 5f);

            _orders.Add(order);
            _events?.Publish(new CustomerOrderCreatedEvent(order));
            return order;
        }

        public bool TryCompleteOrder(CustomerOrder order, ProductData product)
        {
            if (order == null || !_orders.Contains(order) || order.Product != product || order.IsExpired) return false;

            _orders.Remove(order);
            _events?.Publish(new CustomerOrderCompletedEvent(order, order.Reward));
            _events?.Publish(new ProductDeliveredEvent(product));
            return true;
        }
    }
}