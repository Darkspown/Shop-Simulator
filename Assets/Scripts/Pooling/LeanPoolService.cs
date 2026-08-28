using Lean.Pool;
using ShelfRush.Core;
using UnityEngine;

namespace ShelfRush.Pooling
{
    /// <summary>
    /// Реализация пула через сторонний ассет LeanPool (namespace Lean.Pool).
    /// Тонкая обёртка — вся логика пулинга спрятана за IPoolService.
    /// </summary>
    public sealed class LeanPoolService : IPoolService
    {
        public void Initialize(ServiceLocator services) { }

        public void Dispose() => LeanPool.DespawnAll();

        public GameObject Spawn(GameObject prefab) => LeanPool.Spawn(prefab);

        public T Spawn<T>(T prefab) where T : Component => LeanPool.Spawn(prefab);

        public void Despawn(GameObject instance) => LeanPool.Despawn(instance);

        public void DespawnAll() => LeanPool.DespawnAll();
    }
}