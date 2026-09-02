using System.Collections.Generic;
using UnityEngine;

namespace ShelfRush.Products
{
    /// <summary>
    /// Готовый авто-спавнер коробок (MonoBehaviour-пример). Размещается на объекте-точке
    /// выдачи (полка, прилавок, зона спавна — как prefab или в сцене вручную).
    /// На <see cref="OnEnable"/> автоматически спавнит коробки (<see cref="ProductData.BoxPrefab"/>)
    /// через <see cref="ProductSpawner"/> (LeanPool/IPoolService), чтобы игрок мог подобрать
    /// их при подходе. Опционально возвращает их в пул на <see cref="OnDisable"/>.
    ///
    /// Конфигурация — только инспектор, код править не нужно:
    /// - <see cref="data"/> — товар, коробку которого спавним;
    /// - <see cref="spawnOffsets"/> — локальные смещения от этой точки (мировые =
    ///   transform.TransformPoint). Если пусто — одна коробка в позиции объекта;
    /// - <see cref="autoDespawnOnDisable"/> — возвращать свои коробки в пул при OnDisable.
    ///
    /// Сцена не модифицируется: компонент добавляется на объект/prefab вручную.
    /// </summary>
    [AddComponentMenu("ShelfRush/Products/Auto Box Spawner")]
    public sealed class AutoBoxSpawner : MonoBehaviour
    {
        [Header("Product")]
        [Tooltip("Товар, коробку которого спавним (ProductData.boxPrefab).")]
        [SerializeField] private ProductData data;

        [Header("Spawn")]
        [Tooltip("Локальные offset'ы от этого объекта. Мировая позиция = transform.TransformPoint(offset). Если пусто — одна коробка на объекте.")]
        [SerializeField] private Vector3[] spawnOffsets;

        [Tooltip("При пере-включении (OnEnable) снова спавнить коробки, если data задана.")]
        [SerializeField] private bool respawnOnEnable = true;

        [Tooltip("При выключении (OnDisable) возвращать свои коробки в пул (избегает дублей).")]
        [SerializeField] private bool autoDespawnOnDisable = true;

        private ProductSpawner _spawner;
        private readonly List<Product> _spawned = new List<Product>();

        private void Awake()
        {
            _spawner = GetComponent<ProductSpawner>();
            if (_spawner == null) _spawner = gameObject.AddComponent<ProductSpawner>();
        }

        private void OnEnable()
        {
            if (respawnOnEnable && _spawner != null) SpawnAll();
        }

        private void OnDisable()
        {
            if (autoDespawnOnDisable) DespawnAll();
        }

        /// <summary>Спавнит коробки (или первая команда спавна) — очищает предыдущий список.</summary>
        public void SpawnAll()
        {
            if (data == null || _spawner == null) return;

            _spawned.Clear();
            if (spawnOffsets == null || spawnOffsets.Length == 0)
            {
                var p = SpawnAt(Vector3.zero);
                if (p != null) _spawned.Add(p);
                return;
            }

            for (var i = 0; i < spawnOffsets.Length; i++)
            {
                var p = SpawnAt(spawnOffsets[i]);
                if (p != null) _spawned.Add(p);
            }
        }

        /// <summary>Вернуть все ещё живые коробки в пул (активные уже подобраны — не трогаем).</summary>
        public void DespawnAll()
        {
            if (_spawner == null) return;
            for (var i = 0; i < _spawned.Count; i++)
            {
                var p = _spawned[i];
                // Коробка могла быть подобрана игроком (уже в пуле/неактивна) — пропускаем.
                if (p == null || !p.gameObject.activeSelf) continue;
                _spawner.Despawn(p);
            }
            _spawned.Clear();
        }

        private Product SpawnAt(Vector3 localOffset)
        {
            var world = transform.TransformPoint(localOffset);
            return _spawner.SpawnBox(data, world);
        }
    }
}