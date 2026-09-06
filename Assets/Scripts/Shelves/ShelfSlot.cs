using ShelfRush.Products;
using UnityEngine;

namespace ShelfRush.Shelves
{
    /// <summary>
    /// Слот размещения на полке (MonoBehaviour-вью). Представляет одну физическую
    /// точку полки, в которую можно положить товар. Создаётся автоматически
    /// <see cref="ShelfController"/> в Awake из <see cref="ShelfData.Slots"/> —
    /// руками на сцене создавать/расставлять слоты не нужно (сцена не изменяется).
    ///
    /// Слот «тонкий»: хранит привязку к владельцу <see cref="ShelfController"/>,
    /// индекс/позицию и текущее содержимое. <b>Не содержит игровое решение</b> —
    /// всё решает <see cref="ShelfController.TryPlace"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShelfSlot : MonoBehaviour
    {
        [Tooltip("Индекс слота (0..Capacity-1). Заполняется владельцем при инициализации.")]
        [SerializeField] private int index;

        [Tooltip("Локальное смещение слота от полки (источник — ShelfData.Slots).")]
        [SerializeField] private Vector3 localOffset;

        private ShelfController _owner;
        private ProductData _contents;
        private Product _visual;

        /// <summary>Владелец-полка.</summary>
        public ShelfController Owner => _owner;

        /// <summary>Индекс слота.</summary>
        public int Index => index;

        /// <summary>Локальное смещение от полки.</summary>
        public Vector3 LocalOffset => localOffset;

        /// <summary>Данные размещённого товара (null, если слот пуст).</summary>
        public ProductData Contents => _contents;

        /// <summary>Визуал размещённого товара (если создан).</summary>
        public Product Visual => _visual;

        /// <summary>Занят ли слот товаром.</summary>
        public bool IsOccupied => _contents != null;

        /// <summary>Свободен ли слот (можно размещать).</summary>
        public bool IsAvailable => !IsOccupied;

        /// <summary>Инициализация владельцем при создании/пересборке.</summary>
        public void Init(ShelfController owner, int slotIndex, Vector3 offset)
        {
            _owner = owner;
            index = slotIndex;
            localOffset = offset;
            transform.localPosition = offset;
        }

        /// <summary>
        /// Разместить товар в слоте (вызывается <see cref="ShelfController"/> после проверки).
        /// Визуал создаётся спавнером полки, здесь — только фиксируем содержимое.
        /// </summary>
        public void SetPlaced(ProductData data, Product visual)
        {
            _contents = data;
            _visual = visual;
            if (gameObject != null) gameObject.name = $"ShelfSlot_{index} [{data?.name ?? "?"}]";
        }

        /// <summary>Очистить содержимое слота (для пересборки/тестов).</summary>
        public void Clear()
        {
            _contents = null;
            _visual = null;
        }

        /// <summary>
        /// Отладочный gizmo в окне Scene: показывает точку слота (помогает при ручной расстановке).
        /// Зелёный — слот свободен; красный — занят товаром.
        /// </summary>
        private void OnDrawGizmos()
        {
            var c = SlotGizmoColor();
            Gizmos.color = new Color(c.r, c.g, c.b, 0.7f);
            Gizmos.DrawSphere(transform.position, 0.06f);
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }

        private Color SlotGizmoColor()
        {
            return IsOccupied
                ? new Color(1f, 0.3f, 0.3f, 1f)
                : new Color(0.3f, 1f, 0.45f, 1f);
        }
    }
}