using ShelfRush.Products;
using ShelfRush.Shelves;
using UnityEngine;

namespace ShelfRush.Levels
{
    /// <summary>
    /// Конфигурация уровня (ScriptableObject — Data): набор полок, доступные товары,
    /// целевое число заказов и лимит времени.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "ShelfRush/Level")]
    public sealed class LevelConfig : ScriptableObject
    {
        [SerializeField] private int levelIndex;
        [SerializeField] private ProductData[] availableProducts;
        [SerializeField] private ShelfData[] shelves;
        [SerializeField] private int targetOrders = 5;
        [SerializeField] private float timeLimitSeconds = 90f;

        public int LevelIndex => levelIndex;
        public ProductData[] AvailableProducts => availableProducts ?? System.Array.Empty<ProductData>();
        public ShelfData[] Shelves => shelves ?? System.Array.Empty<ShelfData>();
        public int TargetOrders => Mathf.Max(1, targetOrders);
        public float TimeLimitSeconds => Mathf.Max(10f, timeLimitSeconds);
    }
}