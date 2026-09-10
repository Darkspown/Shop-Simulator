using System;
using System.Collections.Generic;
using DG.Tweening;
using ShelfRush.Core;
using ShelfRush.Products;
using UnityEngine;

namespace ShelfRush.Shelves
{
    /// <summary>Причина, по которой размещение товара на полку отклонено.</summary>
    public enum ShelfPlacementFailReason
    {
        /// <summary>Нет данных / полка или товар не настроены (не результат игровой проверки).</summary>
        NoData = 1,

        /// <summary>Категория товара не совпадает с AllowedCategory полки.</summary>
        CategoryMismatch = 4,

        /// <summary>На полке нет свободных слотов.</summary>
/// <summary>Нет отклонения  размещение разрешено.</summary>
        None = 0,
 
        /// <summary>Товар не задан / не настроенnull либо без категории. Задача 08: см <see cref="IProductPlacementValidator"/>.</summary>
        InvalidProduct = 2,
 
        /// <summary>Полка не задана / не настроена для размещенияnull либо без AllowedCategory Задача 08: см <see cref="IProductPlacementValidator"/>.</summary>
        InvalidShelf = 3,
        ShelfFull = 5,
    }

    /// <summary>
    /// Контроллер полки (MonoBehaviour). Ядро Shelf System: решает, можно ли разместить
    /// товар, находит свободный слот, размещает, обновляет <see cref="CurrentAmount"/>,
    /// публикует события и выдаёт визуальный feedback.
    ///
    /// <b>Главная проверка:</b> <c>ProductData.Category == ShelfData.AllowedCategory</c>.
    /// Решение принимается ТОЛЬКО по данным и НЕ зависит от UI/цветов: <see cref="TryPlace"/>
    /// возвращает результат и публикует события независимо от того, показывается ли feedback.
    ///
    /// Events:
    /// <list type="bullet">
    ///   <item><see cref="OnProductPlaced"/>  успешно размещён (после этого также публикуется ShelfProductPlacedEvent  reward + level progress).</item>
    ///   <item><see cref="OnPlacementFailed"/>  попытка отклонена (с причина отклонения).</item>
    ///   <item><see cref="OnCategoryMismatch"/>  неверная категория.</item>
    ///   <item><see cref="OnShelfFull"/>  попытка разместить в заполненную полку.</item>
    ///   <item><see cref="OnShelfCompleted"/>  полка стало заполненной.</item>
    /// </list>
    /// </summary>
    [AddComponentMenu("ShelfRush/Shelves/Shelf Controller")]
    public sealed class ShelfController : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("Данные полки (ScriptableObject): AllowedCategory, Capacity, Slots, InteractionRadius.")]
        [SerializeField] private ShelfData data;

        [Tooltip("(Опц.) Родитель слотов. Если пусто  используются/создаются на этой полке (transform).")]
        [SerializeField] private Transform slotsRoot;

        [Tooltip("Ручная настройка слотов: true  используются уже размещённые в сцене/префабе дочерние " +
                 "объекты с компонентом ShelfSlot (позиции задаются вручную в редакторе); " +
                 "false  слоты создаются автоматически из ShelfData.Slots/Auto.")]
        [SerializeField] private bool useManualSlots;

        [Tooltip("(Опц.) Спавнер товаров. Если пусто  подтягивается/создаётся автоматически.")]
        [SerializeField] private ProductSpawner spawner;

        [Header("Fly-in (interaction zone)")]
        [Tooltip("Длительность DOTween-перелёта товара от игрока к свободному слоту (ShelfInteractionZone), сек.")]
        [SerializeField] private float flyDuration = 0.5f;

        private readonly List<ShelfSlot> _slots = new List<ShelfSlot>();
        private readonly List<GameObject> _created = new List<GameObject>();
        private IEventBus _events;
        private IProductPlacementValidator _validator;
        private int _currentAmount;

        // --- События (view/hooks) ---

        /// <summary>Товар успешно размещён на полку.</summary>
        public event Action<ShelfData, ProductData> OnProductPlaced;

        /// <summary>Попытка размещения отклонена (причина  в параметре).</summary>
        public event Action<ShelfData, ProductData, ShelfPlacementFailReason> OnPlacementFailed;

        /// <summary>Неверная категория  размещение заблокировано.</summary>
        public event Action<ShelfData, ProductData> OnCategoryMismatch;

        /// <summary>Попытка размещения в заполненную полку.</summary>
        public event Action<ShelfData> OnShelfFull;

        /// <summary>Полка стала полностью заполненной (все слоты заняты).</summary>
        public event Action<ShelfData> OnShelfCompleted;

        /// <summary>Любое изменение состояния полки (для UI/прогресс-бара).</summary>
        public event Action<ShelfData> OnChanged;

        // --- Публичное состояние ---

        public ShelfData Data => data;
        public ProductCategory AllowedCategory => data != null ? data.AllowedCategory : null;

        /// <summary>Родитель слотов (для ручной настройки/редактора).</summary>
        public Transform SlotsRoot => slotsRoot != null ? slotsRoot : transform;

        /// <summary>Включена ли ручная настройка слотов.</summary>
        public bool UseManualSlots => useManualSlots;
        public int Capacity => data != null ? data.Capacity : 0;

        /// <summary>
        /// Фактическая вместимость размещения  число созданных слотов
        /// (после BuildSlots == data.SlotCapacity). Используется для проверки полноты.
        /// </summary>
        public int UsableCapacity => _slots.Count > 0 ? _slots.Count : (data != null ? data.SlotCapacity : 0);

        public int CurrentAmount => _currentAmount;
        public bool IsFull => UsableCapacity > 0 && _currentAmount >= UsableCapacity;
        public bool IsCompleted => IsFull;
        public float InteractionRadius => data != null ? data.InteractionRadius : 0f;
        public IReadOnlyList<ShelfSlot> Slots => _slots;

        private void Awake()
        {
            ResolveEventBus();
            ResolveValidator();
            if (spawner == null) spawner = GetComponent<ProductSpawner>();
            if (spawner == null) spawner = gameObject.AddComponent<ProductSpawner>();
            BuildSlots();
        }

        // ------------------------------------------------------------------
        //  Проверки и размещение
        // ------------------------------------------------------------------

        /// <summary>Можно ли разместить товар (категория совпадает + есть свободный слот).</summary>
        public bool CanPlace(ProductData product) => ValidatePlacement(product, out _);

        /// <summary>
        /// Главная проверка. Возвращает <c>true</c>, только если товар не null,
        /// полка не полна и <c>product.Category == AllowedCategory</c>.
        /// </summary>
        public bool ValidatePlacement(ProductData product, out ShelfPlacementFailReason reason)
        {
            var result = EvaluatePlacement(product);
            reason = result.Reason;
            return result.Allowed;
        }

        /// <summary>
        /// Structured result of the placement check ( Task 08: IProductPlacementValidator ).
        /// Allows callers to inspect Allowed + reasons ( category mismatch / shelf full / invalid ).
        /// </summary>
        public ProductPlacementResult CanPlaceResult(ProductData product) => EvaluatePlacement(product);

        /// <summary>Single entry point that delegates to the dedicated validator.</summary>
        private ProductPlacementResult EvaluatePlacement(ProductData product)
        {
            ResolveValidator();
            return _validator.CanPlace(product, data, _currentAmount);
        }

        /// <summary>
        /// Попытаться разместить ОДИН товар. При успехе: занимает слот, CurrentAmount++,
        /// вызывает OnProductPlaced + публикацию ShelfProductPlacedEvent (reward + progress),
        /// корректный feedback. При неудаче  вызывает соответствующие события (OnCategoryMismatch /
        /// OnShelfFull), OnPlacementFailed и негативный feedback; товар НЕ удаляется, НЕ
        /// уничтожается и НЕ забирается у игрока (это делает вызывающий код через
        /// <c>PlayerCarry.TryDrop</c> только в случае успеха).
        /// </summary>
        public bool TryPlace(ProductData product, out ShelfPlacementFailReason reason)
        {
            if (!ValidatePlacement(product, out reason))
            {
                RaiseFailure(product, reason);
                return false;
            }

            var slot = GetFirstAvailableSlot();
            if (slot == null)
            {
                reason = ShelfPlacementFailReason.ShelfFull;
                RaiseFailure(product, reason);
                return false;
            }

            PlaceIntoSlot(slot, product);
            return true;
        }

        public bool TryPlace(ProductData product) => TryPlace(product, out _);

        /// <summary>
        /// Разместить несколько товаров, если они подходят категории и есть свободные слоты.
        /// Размещается только доступное количество (пока есть слоты), остаток НЕ размещается
        /// (остаётся у игрока  здесь он не извлекается из рук).
        /// Возвращает число успешно размещённых.
        /// </summary>
        public int TryPlaceRange(IEnumerable<ProductData> products)
        {
            if (products == null) return 0;
            var placed = 0;
            foreach (var p in products)
            {
                if (p == null) continue;
                if (IsFull) break; // доступное количество исчерпано  остаток у игрока
                if (TryPlace(p, out _)) placed++;
            }
            return placed;
        }

        /// <summary>Разместить товар (без внешней причины). Возвращает false при блокировке.</summary>
        public bool Place(ProductData product) => TryPlace(product);

        /// <summary>
        /// Разместить товар с перелётом из мировой позиции <paramref name="flyFromWorld"/>
        /// (обычно позиция игрока при входе в <see cref="ShelfInteractionZone"/>) в свободный слот.
        /// Товар ОТРИСОВЫВАЕТСЯ у <paramref name="flyFromWorld"/> и DOTween-ом (перелёт + рост +
        /// лёгкий bounce) садится в слот. Валидация/награда/события  как в <see cref="TryPlace"/>.
        /// <paramref name="flyDelay"/>  задержка перед стартом перелёта (для вереницы товаров).
        /// </summary>
        public bool TryPlaceFlying(ProductData product, Vector3 flyFromWorld, float flyDelay, out ShelfPlacementFailReason reason)
        {
            if (!ValidatePlacement(product, out reason))
            {
                RaiseFailure(product, reason);
                return false;
            }

            var slot = GetFirstAvailableSlot();
            if (slot == null)
            {
                reason = ShelfPlacementFailReason.ShelfFull;
                RaiseFailure(product, reason);
                return false;
            }

            PlaceIntoSlot(slot, product, flyFromWorld, flyDelay);
            return true;
        }

        // ------------------------------------------------------------------
        //  Внутренняя реализация размещения
        // ------------------------------------------------------------------

        private void PlaceIntoSlot(ShelfSlot slot, ProductData product, Vector3? flyFrom = null, float flyDelay = 0f)
        {
            var visual = SpawnPlacedVisual(product, slot, flyFrom, flyDelay);
            slot.SetPlaced(product, visual);
            _currentAmount = CountOccupied();

            OnProductPlaced?.Invoke(data, product);
            OnChanged?.Invoke(data);

            // Gameplay-интеграция: reward (EconomyService) + level progress (LevelManager).
            _events?.Publish(new ShelfProductPlacedEvent(data, product));

            if (flyFrom.HasValue) PlayFlyVisualFeedback(visual);
            else PlayCorrectFeedback(slot);

            if (IsFull) RaiseCompleted();
        }

        private Product SpawnPlacedVisual(ProductData product, ShelfSlot slot, Vector3? flyFrom = null, float flyDelay = 0f)
        {
            if (spawner == null || slot == null) return null;

            var settings = product.VisualSettings;
            var targetScale = settings != null ? settings.Scale : Vector3.one;
            var targetLocal = settings != null ? settings.ShelfOffset : Vector3.zero;

            // Спавн на слоте (виден сразу), scale сразу из ProductData  НЕ полагаемся на то, что твин "доживёт".
            var world = slot.transform.TransformPoint(targetLocal);
            var visual = spawner.Spawn(product, world, slot.transform);
            if (visual == null) return null;

            var t = visual.transform;
            t.localScale = targetScale;              // видим сразу
            t.localPosition = targetLocal;           // на слоте

            if (flyFrom.HasValue)
            {
                // СТАРТОВАЯ позиция у игрока (для перелёта), но scale обычный  товар виден всё время.
                var startLocal = slot.transform.InverseTransformPoint(flyFrom.Value);
                t.localPosition = startLocal;

                var dur = Mathf.Max(0.05f, flyDuration);
                DOTween.Sequence()
                    .SetLink(gameObject)
                    .AppendInterval(Mathf.Max(0f, flyDelay))
                    .Append(t.DOLocalMove(targetLocal, dur).SetEase(Ease.OutCubic))
                    .Join(t.DOScale(targetScale * 1.15f, dur * 0.3f).SetEase(Ease.OutQuad))
                    .Append(t.DOScale(targetScale, dur * 0.3f).SetEase(Ease.InQuad))
                    .OnComplete(() => { if (t != null) t.localScale = targetScale; });
            }
            else
            {
                // Обычная выкладка: сразу на месте (пunch-фидбек делает PlaceIntoSlot/PlayCorrectFeedback).
            }

            return visual;
        }

        /// <summary>Зелёная подсветка товара при перелёте (без punch самой полки).</summary>
        private void PlayFlyVisualFeedback(Product visual)
        {
            if (visual == null) return;
            var pv = visual.GetComponentInChildren<ProductVisual>(true);
            if (pv == null) return;
            pv.SetTint(new Color(0.35f, 1f, 0.4f, 1f));
            DOTween.Sequence()
                .SetLink(gameObject)
                .AppendInterval(Mathf.Max(0f, flyDuration) + 0.1f)
                .AppendCallback(() => { if (pv != null) pv.ResetTint(); });
        }

        private void RaiseFailure(ProductData product, ShelfPlacementFailReason reason)
        {
            switch (reason)
            {
                case ShelfPlacementFailReason.CategoryMismatch:
                    OnCategoryMismatch?.Invoke(data, product);
                    break;
                case ShelfPlacementFailReason.ShelfFull:
                    OnShelfFull?.Invoke(data);
                    break;
            }

            OnPlacementFailed?.Invoke(data, product, reason);
            _events?.Publish(new ShelfPlacementFailedEvent(data, product, reason));
            PlayWrongFeedback();
        }

        private void RaiseCompleted()
        {
            OnShelfCompleted?.Invoke(data);
            OnChanged?.Invoke(data);
            _events?.Publish(new ShelfCompletedEvent(data));
        }

        // ------------------------------------------------------------------
        //  Слоты
        // ------------------------------------------------------------------

        private void BuildSlots()
        {
            ClearCreatedSlots();
            _slots.Clear();
            _currentAmount = 0;
            if (data == null) return;

            if (useManualSlots)
            {
                // Ручная настройка: используем уже размещённые в сцене/префабе ShelfSlot-объекты.
                BuildManualSlots();
            }
            else
            {
                BuildAutoSlots();
            }

            _currentAmount = CountOccupied();
        }

        /// <summary>
        /// Ручной режим: ищем дочерние объекты с компонентом <see cref="ShelfSlot"/> под
        /// <see cref="slotsRoot"/> (или под полкой) и используем их. Позиции берутся из сцены
        /// (задаются вручную в редакторе), а не из данных полки. Берётся до
        /// <see cref="ShelfData.Capacity"/> слотов, в порядке их иерархии (SiblingIndex).
        /// </summary>
        private void BuildManualSlots()
        {
            var root = slotsRoot != null ? slotsRoot : transform;
            var existing = root.GetComponentsInChildren<ShelfSlot>(true);
            if (existing == null || existing.Length == 0) return;

            // Стабильный порядок: по иерархии (чистый арт-порядок), не по имени/индексу.
            System.Array.Sort(existing, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

            var count = Mathf.Min(existing.Length, data.Capacity);
            for (var i = 0; i < count; i++)
            {
                var slot = existing[i];
                // Init не меняет позицию: передаём собственную localPosition (как расставлено вручную).
                slot.Init(this, i, slot.transform.localPosition);
                _slots.Add(slot);
                // Ручные слоты не трогаем в ClearCreatedSlots  их не добавляем в _created.
            }
        }

        /// <summary>Авто-режим: слоты создаются из ShelfData.Slots или раскидываются автоматически.</summary>
        private void BuildAutoSlots()
        {
            var count = data.SlotCapacity;
            var positions = data.Slots;
            var root = slotsRoot != null ? slotsRoot : transform;

            for (var i = 0; i < count; i++)
            {
                var offset = (positions != null && i < positions.Length)
                    ? positions[i]
                    : SyntheticOffset(i, count);

                var go = new GameObject($"ShelfSlot_{i}");
                go.transform.SetParent(root, false);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localPosition = offset;

                var slot = go.AddComponent<ShelfSlot>();
                slot.Init(this, i, offset);
                _slots.Add(slot);
                _created.Add(go);
            }
        }

        private void ClearCreatedSlots()
        {
            for (var i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null) Destroy(_created[i]);
            }
            _created.Clear();
        }

        private ShelfSlot GetFirstAvailableSlot()
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null && _slots[i].IsAvailable) return _slots[i];
            }
            return null;
        }

        private int CountOccupied()
        {
            var count = 0;
            for (var i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null && _slots[i].IsOccupied) count++;
            }
            return count;
        }

        private static Vector3 SyntheticOffset(int i, int count)
        {
            return new Vector3(i * 0.45f - (count - 1) * 0.225f, 0f, 0f);
        }

        // ------------------------------------------------------------------
        //  Feedback (только визуальный, НЕ геймплей)
        // ------------------------------------------------------------------

        private void PlayCorrectFeedback(ShelfSlot slot)
        {
            if (slot == null) return;
            var t = slot.transform;
            t.DOKill();

            var pv = slot.Visual != null ? slot.Visual.GetComponentInChildren<ProductVisual>(true) : null;
            if (pv != null) pv.SetTint(new Color(0.35f, 1f, 0.4f, 1f));

            t.DOPunchScale(Vector3.one * 0.15f, 0.28f, 4, 0.5f)
                .SetLink(gameObject)
                .OnComplete(() => pv?.ResetTint());
        }

        private void PlayWrongFeedback()
        {
            if (data == null) return;
            var t = transform;
            t.DOKill();
            // Короткий негативный тряс  визуальный сигнал блокировки.
            t.DOPunchPosition(new Vector3(0f, 0.03f, 0.08f), 0.18f, 4, 0.4f).SetLink(gameObject);
        }

        // ------------------------------------------------------------------
        //  Сервисы
        // ------------------------------------------------------------------

        private void ResolveValidator()
        {
            if (_validator != null) return;
            try
            {
                var bootstrap = GameBootstrap.Instance;
                if (bootstrap != null && bootstrap.Services.TryGet(out _validator)) return;
            }
            catch
            {
                _validator = null;
            }
            _validator = new ProductPlacementValidator();
        }

        private void ResolveEventBus()
        {
            if (_events != null) return;
            try
            {
                var bootstrap = GameBootstrap.Instance;
                if (bootstrap != null) bootstrap.Services.TryGet(out _events);
            }
            catch
            {
                _events = null;
            }
        }
    }
}