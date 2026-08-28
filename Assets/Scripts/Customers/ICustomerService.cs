namespace ShelfRush.Customers
{
    /// <summary>
    /// Управление клиентами: создание заказов, отслеживание времени, завершение.
    /// Держит контракт для Player (доставка товара по заказу) и для UI/Level.
    /// </summary>
    public interface ICustomerService : Core.IGameService, Core.ITickable
    {
        /// <summary>Создать и зарегистрировать новый заказ клиента.</summary>
        CustomerOrder CreateOrder(ShelfRush.Products.ProductData product, int count);

        /// <summary>Попытаться выполнить заказ переданным товаром.</summary>
        bool TryCompleteOrder(CustomerOrder order, ShelfRush.Products.ProductData product);

        /// <summary>Активные (не закрытые) заказы.</summary>
        System.Collections.Generic.IReadOnlyList<CustomerOrder> ActiveOrders { get; }
    }
}