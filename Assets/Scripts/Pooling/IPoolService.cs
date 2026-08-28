using UnityEngine;

namespace ShelfRush.Pooling
{
    /// <summary>
    /// Абстракция объектного пула. Игровые системы (продукты, клиенты, VFX) зависят
    /// только от этого интерфейса; реализация использует LeanPool.
    /// </summary>
    public interface IPoolService : Core.IGameService
    {
        /// <summary>Взять клон префаба из пула (или создать, если пул пуст).</summary>
        GameObject Spawn(GameObject prefab);

        /// <summary>Взять клон-компонент из пула.</summary>
        T Spawn<T>(T prefab) where T : Component;

        /// <summary>Вернуть объект в пул.</summary>
        void Despawn(GameObject instance);

        /// <summary>Вернуть все объекты всех пулов.</summary>
        void DespawnAll();
    }
}