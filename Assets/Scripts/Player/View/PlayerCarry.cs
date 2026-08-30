using System;
using System.Collections.Generic;
using DG.Tweening;
using ShelfRush.Products;
using UnityEngine;

namespace ShelfRush.Player.View
{
    /// <summary>
    /// Переноска товаров (MonoBehaviour-вью). Единственная ответственность — инвентарь:
    /// количество, capacity, add/remove/clear и визуальная укладка товаров в руках.
    /// DOTween используется ТОЛЬКО для визуального эффекта появления/убирания товара,
    /// а не как контроллер движения/физики.
    /// </summary>
    public sealed class PlayerCarry : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerConfig config;

        [Tooltip("Prefab «коробки» товара, инстанцируемый в руки (optional). Если не назначен — переноска работает логически без визуала.")]
        [SerializeField] private GameObject carryItemPrefab;

        [Tooltip("Якорь, к которому стекируются товары (обычно дочерний объект рук/спины).")]
        [SerializeField] private Transform carryAnchor;

        private readonly List<ProductData> _items = new List<ProductData>();
        private readonly List<GameObject> _visuals = new List<GameObject>();
        private readonly List<Tween> _activeTweens = new List<Tween>();

        /// <summary>Количество товаров в руках.</summary>
        public int Count => _items.Count;

        /// <summary>Максимальная вместимость из конфига (никогда не меньше 1).</summary>
        public int Capacity => config != null ? config.CarryCapacity : 4;

        public bool IsFull => Count >= Capacity;

        public bool IsEmpty => Count <= 0;

        /// <summary>Текущий переносимый состав (для UI).</summary>
        public IReadOnlyList<ProductData> Items => _items;

        /// <summary>Событие изменения инвентаря: (товар, новое количество).</summary>
        public event Action<ProductData, int> Changed;

        private float PickupDuration => config != null ? config.PickupDuration : 0.3f;

        private float PlacementDuration => config != null ? config.PlacementDuration : 0.3f;

        private void Awake()
        {
            if (config == null) ServiceBridge.TryResolve(out config);
            DOTween.Init();
        }

        private void OnDisable()
        {
            KillTweens();
        }

        public bool CanAdd() => !IsFull;

        public bool CanRemove() => !IsEmpty;

        /// <summary>Добавить товар в руки (если есть место). Визуал + событие.</summary>
        public bool TryAdd(ProductData product)
        {
            // Ленивый резолв конфига: бутстрап мог регистрировать PlayerConfig позже Awake.
            if (config == null) ServiceBridge.TryResolve(out config);
            if (IsFull) return false;
            _items.Add(product);
            SpawnVisual(_items.Count - 1);
            Changed?.Invoke(product, Count);
            return true;
        }

        /// <summary>Убрать последний товар из рук. Визуал + событие.</summary>
        public bool TryRemove(out ProductData product)
        {
            product = null;
            if (_items.Count == 0) return false;

            var index = _items.Count - 1;
            product = _items[index];
            _items.RemoveAt(index);
            RemoveVisual(index);
            Changed?.Invoke(product, Count);
            return true;
        }

        /// <summary>Очистить руки полностью.</summary>
        public void Clear()
        {
            _items.Clear();
            for (var i = 0; i < _visuals.Count; i++)
                if (_visuals[i] != null)
                    Destroy(_visuals[i]);
            _visuals.Clear();
            KillTweens();
            Changed?.Invoke(null, 0);
        }

        /// <summary>Визуальный эффект появления товара в руках (DOTween, только look).</summary>
        private void SpawnVisual(int slot)
        {
            if (carryItemPrefab == null || carryAnchor == null) return;

            var go = Instantiate(carryItemPrefab, carryAnchor);
            var t = go.transform;
            t.localRotation = Quaternion.identity;
            var targetPos = Vector3.up * (slot * 0.12f); // стек вверх

            // DOTween — визуальный эффект «выпрыгивания», НЕ физический контролл.
            t.localPosition = targetPos - Vector3.up * 0.35f;
            t.localScale = Vector3.zero;

            _activeTweens.Add(t.DOLocalJump(targetPos, 0.25f, 1, PickupDuration).SetEase(Ease.OutQuad).SetLink(gameObject));
            _activeTweens.Add(t.DOScale(Vector3.one, PickupDuration).SetEase(Ease.OutBack).SetLink(gameObject));

            _visuals.Add(go);
        }

        /// <summary>Визуальный эффект убирания товара (DOTween) с удалением объекта.</summary>
        private void RemoveVisual(int index)
        {
            if (index < 0 || index >= _visuals.Count) return;

            var go = _visuals[index];
            _visuals.RemoveAt(index);
            if (go == null) return;

            go.transform.DOKill();
            var tween = go.transform.DOScale(Vector3.one * 0.1f, PlacementDuration)
                .SetEase(Ease.InBack)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    _activeTweens.RemoveAll(x => x == null || !x.IsActive());
                    if (go != null) Destroy(go);
                });
            _activeTweens.Add(tween);
        }

        private void KillTweens()
        {
            for (var i = 0; i < _activeTweens.Count; i++)
                _activeTweens[i]?.Kill();
            _activeTweens.Clear();
        }
    }
}