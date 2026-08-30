using System.Collections.Generic;

namespace ShelfRush.Products
{
    /// <summary>Каталог всех товаров игры (данные, загруженные при старте).</summary>
    public interface IProductCatalog : Core.IGameService
    {
        IReadOnlyList<ProductData> All { get; }

        /// <summary>Товары заданной категории (type-safe, по ссылке).</summary>
        IReadOnlyList<ProductData> GetByCategory(ProductCategory category);

        /// <summary>Получить товар по объекту (быстро, через сравнение ссылок).</summary>
        bool TryFind(ProductData product, out ProductData found);

        /// <summary>Получить товар по его Id (для загрузки сохранений).</summary>
        bool TryFindById(string id, out ProductData found);
    }
}