using ShelfRush.Products;
using UnityEngine;

namespace ShelfRush.Shelves
{
    /// <summary>
    /// Данные полки (ScriptableObject — Data). Полка может использоваться в двух режимах:
    ///
    /// <b>Placement (Shelf System)</b> — игрок <u>наполняет</u> полку товарами:
    ///   — <see cref="AllowedCategory"/> — какая категория может быть размещена
    ///     (главная проверка: <c>ProductData.Category == AllowedCategory</c>);
    ///   — <see cref="Slots"/> — локальные позиции слотов (скольки точек);
    ///   — <see cref="Capacity"/> — сколько слотов задействуется;
    ///   — <see cref="InteractionRadius"/> — радиус досягаемости полки.
    ///
    /// <b>Stock (legacy)</b> — полка, из которой игрок <u>забирает</u> товар (IStockService):
    ///   — <see cref="Product"/> — товар, который выдаётся на этой полке;
    ///   — <see cref="Capacity"/> — стартовый запас.
    ///
    /// Всё игровое решение (можно/нельзя разместить) опирается только на данные ниже,
    /// НЕ на UI/цвета. Текущее количество на полке — runtime-состояние и хранится в
    /// <see cref="ShelfController.CurrentAmount"/> (не в этом ассете).
    /// </summary>
    [CreateAssetMenu(fileName = "ShelfData", menuName = "ShelfRush/Shelf")]
    public sealed class ShelfData : ScriptableObject
    {
        [Header("Placement (Shelf System)")]
        [Tooltip("Категория, которую можно разместить на этой полке (type-safe ссылка, не строка). " +
                 "Главная проверка: ProductData.Category == AllowedCategory.")]
        [SerializeField] private ProductCategory allowedCategory;

        [Tooltip("Локальные позиции слотов размещения (локальные от полки). " +
                 "Используются первые Capacity точек.")]
        [SerializeField] private Vector3[] placements = System.Array.Empty<Vector3>();

        [Tooltip("Радиус, с которого игрок может взаимодействовать с этой полкой (ShelfInteraction).")]
        [SerializeField] private float interactionRadius = 2f;

        [Header("Common")]
        [Tooltip("Вместимость полки: сколько слотов задействуется для размещения. " +
                 "В режиме Stock — стартовое количество товара.")]
        [SerializeField] private int capacity = 8;

        [Header("Stock (legacy supply) — IStockService")]
        [Tooltip("Товар, выдаваемый этой полкой в режиме Stock (забор с полки). " +
                 "Для Placement-режима не используется — используйте AllowedCategory.")]
        [SerializeField] private ProductData product;

        public ProductCategory AllowedCategory => allowedCategory;

        public int Capacity => Mathf.Max(1, capacity);

        /// <summary>Локальные позиции слотов размещения (Vector3[]).</summary>
        public Vector3[] Slots => placements;

        /// <summary>Алиас <see cref="Slots"/> (legacy).</summary>
        public Vector3[] Placements => placements;

        public float InteractionRadius => Mathf.Max(0f, interactionRadius);

        /// <summary>
        /// Сколько слотов реально задействуется: не больше Capacity и не больше
        /// числа заданных позиций. Если позиции не заданы — берётся Capacity.
        /// </summary>
        public int SlotCapacity =>
            placements != null && placements.Length > 0
                ? Mathf.Max(1, Mathf.Min(capacity, placements.Length))
                : Mathf.Max(1, capacity);

        public ProductData Product => product;
    }
}