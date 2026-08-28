using System.Collections.Generic;
using ShelfRush.Core;

namespace ShelfRush.Products
{
    /// <summary>
    /// Хранит все товары игры. Экземпляр строится из массива ScriptableObject,
    /// переданного в GameBootstrap.
    /// </summary>
    public sealed class ProductCatalog : IProductCatalog
    {
        private readonly List<ProductData> _all = new List<ProductData>();

        public ProductCatalog(IEnumerable<ProductData> products)
        {
            if (products != null)
            {
                foreach (var p in products)
                {
                    if (p != null && !_all.Contains(p)) _all.Add(p);
                }
            }
        }

        public IReadOnlyList<ProductData> All => _all;

        public void Initialize(ServiceLocator services) { }

        public void Dispose() => _all.Clear();

        public bool TryFind(ProductData product, out ProductData found)
        {
            for (var i = 0; i < _all.Count; i++)
            {
                if (ReferenceEquals(_all[i], product))
                {
                    found = _all[i];
                    return true;
                }
            }
            found = null;
            return false;
        }

        public bool TryFindById(string id, out ProductData found)
        {
            if (!string.IsNullOrEmpty(id))
            {
                for (var i = 0; i < _all.Count; i++)
                {
                    if (_all[i].Id == id)
                    {
                        found = _all[i];
                        return true;
                    }
                }
            }
            found = null;
            return false;
        }
    }
}