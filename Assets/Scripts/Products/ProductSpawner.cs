using ShelfRush.Core;
using ShelfRush.Pooling;
using UnityEngine;

namespace ShelfRush.Products
{
    /// <summary>
    /// Спавнер товаров на runtime (MonoBehaviour). Единственная точка создания/уничтожения
    /// товаров в игре: создаёт экземпляры <see cref="ProductData.Prefab"/> / коробок через
    /// <see cref="IPoolService"/> (обёртка над LeanPool) там, где это соответствует архитектуре.
    ///
    /// Правила:
    /// - Спавн идёт через LeanPool (<see cref="IPoolService.Spawn{T}"/>), если пул доступен;
    ///   при его отсутствии (например, prefab открыт без GameBootstrap) — аккуратный
    ///   fallback на Instantiate/Destroy.
    /// - Каждый спавн возвращает компонент <see cref="Product"/>, связанный с ProductData.
    /// - Перед <c>Despawn</c> вызывается <see cref="Product.ResetState"/> (сброс состояния),
    ///   как требует правило despawn-контракта.
    ///
    /// Размещается на объекте-«спавнере» (полка, точка выдачи) — новых объектов в сцену
    /// не добавляет, может создаваться как prefab.
    /// </summary>
    public sealed class ProductSpawner : MonoBehaviour
    {
        private IPoolService _pool;

        /// <summary>Создать товар из ProductData.prefab в заданной позиции.</summary>
        public Product Spawn(ProductData data, Vector3 position, Transform parent = null)
        {
            if (data == null || data.Prefab == null) return null;
            var go = SpawnObject(data.Prefab, position, parent);
            return Setup(go, data);
        }

        /// <summary>Создать «коробку» товара (ProductData.BoxPrefab); если её нет — товар.</summary>
        public Product SpawnBox(ProductData data, Vector3 position, Transform parent = null)
        {
            if (data == null) return null;
            var prefab = data.BoxPrefab;
            if (prefab == null) return Spawn(data, position, parent);
            var go = SpawnObject(prefab, position, parent);
            return Setup(go, data);
        }

        /// <summary>Создать товар без данных (для тестов/заглушек) по сырому префабу.</summary>
        public GameObject SpawnRaw(GameObject prefab, Vector3 position, Transform parent = null)
        {
            if (prefab == null) return null;
            return SpawnObject(prefab, position, parent);
        }

        /// <summary>Вернуть товар в пул после сброса состояния.</summary>
        public void Despawn(Product product)
        {
            if (product == null) return;
            product.ResetState();
            DespawnObject(product.gameObject);
        }

        /// <summary>Вернуть сырой GameObject в пул.</summary>
        public void DespawnRaw(GameObject go)
        {
            if (go == null) return;
            foreach (var p in go.GetComponentsInChildren<Product>(true))
                p.ResetState();
            DespawnObject(go);
        }

        private GameObject SpawnObject(GameObject prefab, Vector3 position, Transform parent)
        {
            var pool = ResolvePool();
            GameObject go;
            if (pool != null)
                go = pool.Spawn(prefab);
            else
                go = parent != null ? Instantiate(prefab, parent) : Instantiate(prefab);

            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = position;
            return go;
        }

        private void DespawnObject(GameObject go)
        {
            if (go == null) return;
            var pool = ResolvePool();
            if (pool != null)
                pool.Despawn(go);
            else
                Destroy(go);
        }

        private Product Setup(GameObject go, ProductData data)
        {
            if (go == null) return null;
            var product = go.GetComponent<Product>();
            if (product == null) product = go.AddComponent<Product>();
            product.Setup(data);
            return product;
        }

        /// <summary>
        /// Ленивый резолв пула через единственную точку входа (GameBootstrap.Instance.Services).
        /// Не ходим в ServiceLocator напрямую в компонентах префаба напрямую — только через bootstrapper.
        /// </summary>
        private IPoolService ResolvePool()
        {
            if (_pool != null) return _pool;
            try
            {
                var bootstrap = GameBootstrap.Instance;
                if (bootstrap != null)
                    bootstrap.Services.TryGet(out _pool);
            }
            catch
            {
                _pool = null;
            }
            return _pool;
        }
    }
}