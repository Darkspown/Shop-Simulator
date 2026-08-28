using System;
using System.Collections.Generic;

namespace ShelfRush.Core
{
    /// <summary>
    /// Тайпизированная шина событий. Связывает системы без прямых ссылок:
    /// издатель вызывает <see cref="Publish{T}"/>, подписчики — <see cref="Subscribe{T}"/>.
    /// Подписка возвращает IDisposable для корректной отписки (перед Dispose система отписывается).
    /// </summary>
    public interface IEventBus
    {
        IDisposable Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
        void Publish<T>(T evt);
        void Clear();
    }

    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            GetList<T>().Add(handler);
            return new Subscription<T>(this, handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;
            list.Remove(handler);
        }

        public void Publish<T>(T evt)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list) || list.Count == 0) return;
            // Копия, чтобы безопасно отписываться во время публикации.
            var snapshot = new List<Delegate>(list);
            for (var i = 0; i < snapshot.Count; i++)
            {
                ((Action<T>)snapshot[i]).Invoke(evt);
            }
        }

        public void Clear() => _handlers.Clear();

        private List<Delegate> GetList<T>()
        {
            if (_handlers.TryGetValue(typeof(T), out var list)) return list;
            list = new List<Delegate>();
            _handlers[typeof(T)] = list;
            return list;
        }

        private sealed class Subscription<T> : IDisposable
        {
            private readonly EventBus _bus;
            private readonly Action<T> _handler;
            private bool _disposed;

            public Subscription(EventBus bus, Action<T> handler)
            {
                _bus = bus;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _bus.Unsubscribe(_handler);
            }
        }
    }
}