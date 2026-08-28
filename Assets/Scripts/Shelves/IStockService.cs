namespace ShelfRush.Shelves
{
    /// <summary>
    /// Управление запасами полок (Stock). Единая точка учёта товара на всех полках,
    /// изоляция от конкретных ShelfView/GameObject.
    /// </summary>
    public interface IStockService : Core.IGameService
    {
        /// <summary>Зарегистрировать полку и наполнить её до вместимости.</summary>
        void RegisterShelf(ShelfData shelf);

        /// <summary>Текущее количество товара на полке (0, если полка не зарегистрирована).</summary>
        int GetStock(ShelfData shelf);

        /// <summary>Попытаться взять один товар с полки.</summary>
        bool TryTakeProduct(ShelfData shelf, out Products.ProductData product);

        /// <summary>Наполнить полку до вместимости (рестока).</summary>
        void Restock(ShelfData shelf);

        /// <summary>Все зарегистрированные полки текущего уровня.</summary>
        System.Collections.Generic.IReadOnlyList<ShelfData> Shelves { get; }
    }
}