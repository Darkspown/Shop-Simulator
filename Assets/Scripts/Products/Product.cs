using UnityEngine;

namespace ShelfRush.Products
{
    /// <summary>
    /// Runtime-экземпляр товара. Каждый созданный через <see cref="ProductSpawner"/> товар
    /// содержит этот компонент и ссылается на свой <see cref="ProductData"/> (Data).
    ///
    /// Компонент «тонкий»: не содержит логику, только связку «GameObject ⇄ ProductData» +
    /// перевыставление визуала. Спавнится через LeanPool (IPoolService).
    /// Перед возвратом в пул обязательно вызывается <see cref="ResetState"/>
    /// (см. ProductSpawner.Despawn) — это соответствует требованию «reset state перед Despawn».
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Product : MonoBehaviour
    {
        private ProductVisual _visual;

        /// <summary>Данные товара (ScriptableObject). Может быть null, пока не вызван Setup.</summary>
        public ProductData Data { get; private set; }

        /// <summary>Корректно ли настроен экземпляр.</summary>
        public bool IsValid => Data != null;

        public string Id => Data != null ? Data.Id : string.Empty;
        public string DisplayName => Data != null ? Data.DisplayName : string.Empty;
        public ProductCategory Category => Data != null ? Data.Category : null;
        public int RewardValue => Data != null ? Data.RewardValue : 0;
        public GameObject Prefab => Data != null ? Data.Prefab : null;
        public GameObject BoxPrefab => Data != null ? Data.BoxPrefab : null;

        /// <summary>Связать экземпляр с данными товара и применить визуал.</summary>
        public void Setup(ProductData data)
        {
            Data = data;
            if (Data != null) gameObject.name = $"{Data.name} (Product)";
            ApplyVisual();
        }

        /// <summary>
        /// Сброс состояния перед возвратом в пул (вызывается ProductSpawner.Despawn):
        /// отвязка данных, сброс твинов/цвета, восстановление трансформа.
        /// </summary>
        public void ResetState()
        {
            // Продукт не ведёт твины/события сам — только визуал и данные.
            if (_visual != null) _visual.ResetTint();
            Data = null;
        }

        private void OnEnable()
        {
            // При повторном спавне (Enable) переприменяем визуал текущих данных.
            if (Data != null) ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (_visual == null) _visual = GetComponentInChildren<ProductVisual>(true);
            _visual?.Apply(Data);
        }
    }
}