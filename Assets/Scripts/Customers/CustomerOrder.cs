using ShelfRush.Products;

namespace ShelfRush.Customers
{
    /// <summary>
    /// Модель заказа клиента (runtime-данные). Создаётся CustomerService на основе
    /// ProductCatalog и конфигурации уровня. Не является MonoBehaviour.
    /// </summary>
    public sealed class CustomerOrder
    {
        public string Id { get; }
        public ProductData Product { get; }
        public int Count { get; }
        public int Reward { get; }
        public float TimeLimit { get; }
        public float Elapsed { get; private set; }

        public bool IsExpired => Elapsed >= TimeLimit;

        public CustomerOrder(string id, ProductData product, int count, int reward, float timeLimit)
        {
            Id = id;
            Product = product;
            Count = count;
            Reward = reward;
            TimeLimit = timeLimit;
        }

        /// <summary>Обновляет время жизни заказа. Возвращает true, если срок истёк сейчас.</summary>
        public bool Tick(float dt)
        {
            Elapsed += dt;
            return IsExpired;
        }
    }
}