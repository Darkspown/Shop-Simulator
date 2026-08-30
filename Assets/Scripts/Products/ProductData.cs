using System;
using UnityEngine;

namespace ShelfRush.Products
{
    /// <summary>
    /// Статические данные товара (ScriptableObject — Data).
    /// Идентификация объекта — по ссылке (reference); <see cref="Id"/> — стабильный код
    /// только для сохранений/ключей. Строковая идентификация категорий в логике не используется:
    /// категория — это type-safe ссылка на <see cref="ProductCategory"/>.SO.
    /// Каждый runtime-товар (<see cref="Product"/>) ссылается на один такой ассет.
    /// </summary>
    [CreateAssetMenu(fileName = "ProductData", menuName = "ShelfRush/Products/Product")]
    public sealed class ProductData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Стабильный идентификатор для сохранений (генерируется автоматически).")]
        [SerializeField] private string id;

        [Tooltip("Отображаемое название товара (для UI/подсказок).")]
        [SerializeField] private string displayName;

        [Tooltip("Категория товара (type-safe ссылка на ProductCategory, не строка).")]
        [SerializeField] private ProductCategory category;

        [Tooltip("Иконка для UI (магазин, HUD, ценники).")]
        [SerializeField] private Sprite icon;

        [Header("Prefabs")]
        [Tooltip("Runtime-префаб товара (то, что лежит на полке / в руках). Должен содержать компонент Product и, опционально, ProductVisual.")]
        [SerializeField] private GameObject prefab;

        [Tooltip("Префаб «коробки» товара (для переноски с полки до кассы/клиента). Если не задан — используется prefab.")]
        [SerializeField] private GameObject boxPrefab;

        [Header("Reward")]
        [Tooltip("Награда (монеты) при успешной доставке товара.")]
        [SerializeField] private int rewardValue = 10;

        [Header("Visual Settings")]
        [Tooltip("Визуальные настройки, применяемые ProductVisual при спавне: масштаб, тон, смещения.")]
        [SerializeField] private VisualSettings visualSettings = new VisualSettings
        {
            Scale = Vector3.one,
            Tint = Color.white,
            StackOffset = new Vector3(0f, 0.12f, 0f),
            ShelfOffset = Vector3.zero,
        };

        // --- Пропубликованные данные (readonly для потребителей) ---

        public string Id => id;
        public string DisplayName => displayName;
        public ProductCategory Category => category;
        public Sprite Icon => icon;
        public GameObject Prefab => prefab;
        public GameObject BoxPrefab => boxPrefab != null ? boxPrefab : prefab;
        public int RewardValue => rewardValue;
        public VisualSettings VisualSettings => visualSettings;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = System.Guid.NewGuid().ToString("N");
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
            if (string.IsNullOrEmpty(displayName)) displayName = name;
            if (prefab == null && boxPrefab != null) prefab = boxPrefab;
            if (boxPrefab == null) boxPrefab = prefab;
        }
    }

    /// <summary>
    /// Визуальные настройки товара (часть <see cref="ProductData"/>).
    /// Применяются <see cref="ProductVisual"/> при спавне/подсветке; подхватываются
    /// <see cref="ProductSpawner"/> для позиционирования стека товаров.
    /// </summary>
    [Serializable]
    public sealed class VisualSettings
    {
        [Tooltip("Масштаб runtime-префаба на полке/в руках.")]
        public Vector3 Scale = Vector3.one;

        [Tooltip("Базовый тон (tint) товара. Применяется через MaterialPropertyBlock, без инстанцирования материалов.")]
        public Color Tint = Color.white;

        [Tooltip("Смещение между экземплярами при стекировании (например, вверх по Y).")]
        public Vector3 StackOffset = new Vector3(0f, 0.12f, 0f);

        [Tooltip("Базовое смещение товара от точки спавна (обычно над поверхностью полки).")]
        public Vector3 ShelfOffset = Vector3.zero;
    }
}