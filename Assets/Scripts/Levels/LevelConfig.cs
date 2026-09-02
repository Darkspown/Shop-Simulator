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

        [Header("Progression")]
        [Tooltip("Максимум товаров, которые игрок может нести одновременно (прогрессия переноски). " +
                 "НЕ должен храниться внутри PlayerCarry — источник этого значения уровень. " +
                 "Пример: L1=1, L2=2, L3=3, L4=5, L5=7.")]
        [SerializeField] private int carryCapacity = 1;

        public int LevelIndex => levelIndex;
        public ProductData[] AvailableProducts => availableProducts ?? System.Array.Empty<ProductData>();
        public ShelfData[] Shelves => shelves ?? System.Array.Empty<ShelfData>();
        public int TargetOrders => Mathf.Max(1, targetOrders);
        public float TimeLimitSeconds => Mathf.Max(10f, timeLimitSeconds);

        /// <summary>Вместимость переноски на этом уровне (никогда не меньше 1).</summary>
        public int CarryCapacity => Mathf.Max(1, carryCapacity);
    }
}