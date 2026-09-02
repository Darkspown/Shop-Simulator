using System;
using System.Collections.Generic;
using DG.Tweening;
using ShelfRush.Core;
using ShelfRush.Levels;
using ShelfRush.Pooling;
using ShelfRush.Products;
using UnityEngine;

namespace ShelfRush.Player.View
{
    /// <summary>
    /// Переноска товаров (MonoBehaviour-вью). Единственная ответственность — инвентарь
    /// игрока: подсчёт, capacity, pickup/drop/remove/clear и визуальная укладка товаров
    /// в руках (visual stack) с DOTween-анимацией перемещения.
    ///
    /// Принципы:
    /// - Прогрессия вместимости НЕ хранится здесь. <see cref="Capacity"/> берётся из
    ///   текущего уровня (<see cref="LevelConfig.CarryCapacity"/> через
    ///   <see cref="ILevelManager"/>) с фолбэком на <see cref="PlayerConfig"/>.
    /// - Визуал товаров спавнится/возвращается через LeanPool (<see cref="IPoolService"/>),
    ///   а не Instantiate/Destroy.
    /// - DOTween используется ТОЛЬКО для визуального перемещения/укладки (не движение/физика).
    /// - Возврат в пул по despawn-контракту (см. <see cref="ReleaseVisual"/>):
    ///   kill tweens → reset state → clear product data → reset transform → unsubscribe.
    /// </summary>
    public sealed class PlayerCarry : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerConfig config;

        [Tooltip("Якорь, к которому стекируются товары (обычно дочерний объект рук/спины).")]
        [SerializeField] private Transform carryAnchor;

        [Header("Stack")]
        [Tooltip("Смещение между товарами в стопке (обычно вверх по Y).")]
        [SerializeField] private Vector3 stackOffset = new Vector3(0f, 0.12f, 0f);

        private readonly List<CarriedItem> _carried = new List<CarriedItem>();
        private readonly List<Tween> _activeTweens = new List<Tween>();
        private readonly List<ProductData> _snapshot = new List<ProductData>();

        private IPoolService _pool;
        private ILevelManager _levels;
        private IEventBus _events;
        private IDisposable _levelStartedSub;

        /// <summary>Количество товаров в руках.</summary>
        public int Count => _carried.Count;

        /// <summary>Якорь стекирования (используется, чтобы исключить из авто-подбора коробки в руках).</summary>
        public Transform CarryAnchor => carryAnchor;

        /// <summary>
        /// Максимум товаров, которые игрок может нести. Источник — текущий уровень
        /// (прогрессия не хранится здесь); фолбэк — PlayerConfig.
        /// </summary>
        public int Capacity
        {
            get
            {
                var level = ResolveLevel()?.Current;
                if (level != null) return level.CarryCapacity;
                return config != null ? config.CarryCapacity : 1;
            }
        }

        public bool IsFull => Count >= Capacity;

        public bool IsEmpty => Count <= 0;

        /// <summary>Текущий переносимый состав (для UI).</summary>
        public IReadOnlyList<ProductData> Items
        {
            get
            {
                _snapshot.Clear();
                for (var i = 0; i < _carried.Count; i++) _snapshot.Add(_carried[i].Data);
                return _snapshot;
            }
        }

        /// <summary>Событие изменения инвентаря: (товар, новое количество).</summary>
        public event Action<ProductData, int> Changed;

        private float PickupDuration => config != null ? config.PickupDuration : 0.3f;

        private float PlacementDuration => config != null ? config.PlacementDuration : 0.3f;

        private void Awake()
        {
            if (config == null) ServiceBridge.TryResolve(out config);
            DOTween.Init();
        }

        private void OnEnable()
        {
            // Ленивый резолв: бутстрап мог ещё не построить ServiceLocator.
            if (_events == null) ServiceBridge.TryResolve(out _events);
            if (_events != null && _levelStartedSub == null)
                _levelStartedSub = _events.Subscribe<LevelStartedEvent>(OnLevelStarted);
        }

        private void OnDisable()
        {
            KillTweens();
            ReleaseAll();
            UnsubscribeEvents();
        }

        /// <summary>Capacity check: можно ли взять/положить (есть место).</summary>
        public bool CanAdd() => !IsFull;

        /// <summary>Capacity check: можно ли снять/доставить (есть товар).</summary>
        public bool CanRemove() => !IsEmpty;

        /// <summary>Pickup → Add To Carry: взять товар в руки, если есть место. Визуал + событие.</summary>
        public bool TryAdd(ProductData product)
        {
            if (product == null) return false;
            // Ленивый резолв конфига: бутстрап мог регистрировать PlayerConfig позже Awake.
            if (config == null) ServiceBridge.TryResolve(out config);
            if (IsFull) return false;

            var slot = _carried.Count;
            var item = SpawnVisual(product, slot);
            _carried.Add(item);
            Changed?.Invoke(product, Count);
            return true;
        }

        /// <summary>Remove From Carry: снять/доставить последний товар из рук. Визуал + событие.</summary>
        public bool TryRemove(out ProductData product)
        {
            product = null;
            if (_carried.Count == 0) return false;

            var index = _carried.Count - 1;
            var item = _carried[index];
            product = item.Data;
            _carried.RemoveAt(index);
            RemoveVisual(item);
            CompactStack();
            Changed?.Invoke(product, Count);
            return true;
        }

        /// <summary>Drop: доставить/выбросить конкретный товар из рук, если он есть.</summary>
        public bool TryDrop(ProductData product)
        {
            if (product == null) return false;
            for (var i = _carried.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_carried[i].Data, product)) continue;

                var item = _carried[i];
                _carried.RemoveAt(i);
                RemoveVisual(item);
                CompactStack();
                Changed?.Invoke(product, Count);
                return true;
            }
            return false;
        }

        /// <summary>Очистить руки полностью (визуалы возвращаются в пул).</summary>
        public void Clear()
        {
            KillTweens();
            ReleaseAll();
            Changed?.Invoke(null, 0);
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            // Не переносим товары между уровнями; capacity перечитывается из нового уровня.
            Clear();
        }

        // ------------------------------------------------------------------
        //  Visual stack
        // ------------------------------------------------------------------

        private CarriedItem SpawnVisual(ProductData data, int slot)
        {
            var item = new CarriedItem { Data = data };
            if (carryAnchor == null) return item;

            var prefab = data != null ? data.BoxPrefab : null;
            if (prefab == null) return item;

            var go = SpawnFromPool(prefab);
            if (go == null) return item;

            var product = go.GetComponent<Product>();
            if (product == null) product = go.AddComponent<Product>();
            product.Setup(data);
            item.Visual = product;

            var t = go.transform;
            t.SetParent(carryAnchor, false);
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;

            var target = stackOffset * slot;
            t.localPosition = target - Vector3.up * 0.4f; // старт ниже → DOTween поднимает в стопку

            t.DOKill();
            var move = t.DOLocalMove(target, PickupDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
            var settle = t.DOScale(Vector3.one * 1.08f, PickupDuration * 0.5f)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    t.DOScale(Vector3.one, PickupDuration * 0.5f).SetEase(Ease.InQuad).SetLink(gameObject);
                });

            _activeTweens.Add(move);
            _activeTweens.Add(settle);
            return item;
        }

        /// <summary>Визуальное убирание товара (DOTween shrink) с возвратом в пул.</summary>
        private void RemoveVisual(CarriedItem item)
        {
            if (item == null) return;
            if (item.Visual != null)
            {
                var go = item.Visual.gameObject;
                var t = go.transform;
                t.DOKill();
                var shrink = t.DOScale(Vector3.one * 0.05f, PlacementDuration)
                    .SetEase(Ease.InBack)
                    .SetLink(gameObject)
                    .OnComplete(() => ReleaseVisual(item));
                _activeTweens.Add(shrink);
            }
            else
            {
                item.Data = null;
                item.Released = true;
            }
        }

        /// <summary>
        /// Возврат визуала товара в пул по despawn-контракту:
        /// kill tweens → reset state → clear product data → reset transform → (unsubscribe).
        /// </summary>
        private void ReleaseVisual(CarriedItem item)
        {
            if (item == null || item.Released) return;
            item.Released = true;
            item.Data = null; // clear product data

            var visual = item.Visual;
            item.Visual = null;
            if (visual == null) return;

            var go = visual.gameObject;

            // 1) kill tweens (все DOTween на этом объекте).
            go.transform.DOKill();

            // 2) reset state + clear product data (Product.ResetState отвязывает Data и сбрасывает визуал).
            visual.ResetState();

            // 3) reset transform: нейтральное состояние для переиспользования пулом.
            var t = go.transform;
            t.localRotation = Quaternion.identity;
            t.localPosition = Vector3.zero;
            t.localScale = Vector3.one;

            // 4) unsubscribe: у Product нет внешних подписок; сам PlayerCarry отписывается
            //    в UnsubscribeEvents (см. OnDisable).
            DespawnFromPool(go);
        }

        private void ReleaseAll()
        {
            for (var i = 0; i < _carried.Count; i++) ReleaseVisual(_carried[i]);
            _carried.Clear();
        }

        /// <summary>После удаления товара поднимаем оставшиеся ниже до корректных слотов.</summary>
        private void CompactStack()
        {
            for (var i = 0; i < _carried.Count; i++)
            {
                var item = _carried[i];
                if (item?.Visual == null) continue;

                var t = item.Visual.transform;
                var target = stackOffset * i;
                if ((t.localPosition - target).sqrMagnitude < 0.0001f) continue;

                t.DOKill();
                var move = t.DOLocalMove(target, PlacementDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
                _activeTweens.Add(move);
            }
        }

        private void KillTweens()
        {
            for (var i = 0; i < _activeTweens.Count; i++) _activeTweens[i]?.Kill();
            _activeTweens.Clear();
        }

        private void UnsubscribeEvents()
        {
            if (_levelStartedSub != null)
            {
                _levelStartedSub.Dispose();
                _levelStartedSub = null;
            }
            _events = null;
        }

        // ------------------------------------------------------------------
        //  Pooling (LeanPool через IPoolService; fallback на Instantiate/Destroy)
        // ------------------------------------------------------------------

        private GameObject SpawnFromPool(GameObject prefab)
        {
            var pool = ResolvePool();
            if (pool != null) return pool.Spawn(prefab);
            return prefab != null ? Instantiate(prefab) : null; // fallback без bootstrap
        }

        private void DespawnFromPool(GameObject go)
        {
            var pool = ResolvePool();
            if (pool != null) pool.Despawn(go);
            else Destroy(go);
        }

        private IPoolService ResolvePool()
        {
            if (_pool != null) return _pool;
            ServiceBridge.TryResolve(out _pool);
            return _pool;
        }

        private ILevelManager ResolveLevel()
        {
            if (_levels == null) ServiceBridge.TryResolve(out _levels);
            return _levels;
        }

        /// <summary>Runtime-запись одного переносимого товара: данные + его визуал.</summary>
        private sealed class CarriedItem
        {
            public ProductData Data;
            public Product Visual;
            public bool Released;
        }
    }
}