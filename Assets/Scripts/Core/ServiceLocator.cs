using System;
using System.Collections.Generic;

namespace ShelfRush.Core
{
    /// <summary>
    /// Лёгкий реестр сервисов (Service Locator). Замена DI-библиотеке.
    /// Сервисы регистрируются по интерфейсу в <see cref="GameBootstrap"/>, затем
    /// инициализируются в порядке зависимостей и получают ссылки друг на друга через Get&lt;T&gt;().
    /// Использование FindObjectOfType / God Object / статического состояния не допускается.
    /// </summary>
    public sealed class ServiceLocator
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>Регистрирует экземпляр по типу T (обычно интерфейсу).</summary>
        public void Register<T>(object instance) => _services[typeof(T)] = instance ?? throw new ArgumentNullException(nameof(instance));

        /// <summary>Регистрирует экземпляр по его конкретному типу.</summary>
        public void Register(object instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            _services[instance.GetType()] = instance;
        }

        /// <summary>Получает сервис. Кидает исключение, если сервис не зарегистрирован.</summary>
        public T Get<T>()
        {
            if (_services.TryGetValue(typeof(T), out var service)) return (T)service;
            throw new InvalidOperationException($"Service '{typeof(T)}' is not registered in the ServiceLocator.");
        }

        /// <summary>Пробует получить сервис без исключения.</summary>
        public bool TryGet<T>(out T service)
        {
            if (_services.TryGetValue(typeof(T), out var value))
            {
                service = (T)value;
                return true;
            }
            service = default;
            return false;
        }

        /// <summary>Полная очистка реестра (при выгрузке сцены).</summary>
        public void Clear() => _services.Clear();
    }
}