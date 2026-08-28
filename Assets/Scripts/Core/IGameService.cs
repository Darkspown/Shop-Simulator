namespace ShelfRush.Core
{
    /// <summary>
    /// Базовый контракт всех игровых сервисов/систем.
    /// Каждый сервис живёт в <see cref="ServiceLocator"/> и получает зависимости через
    /// <see cref="Initialize"/>. Подписки и состояние освобождаются в <see cref="Dispose"/>.
    /// </summary>
    public interface IGameService
    {
        /// <summary>
        /// Инициализация. Вызывается после регистрации ВСЕХ сервисов,
        /// поэтому внутри можно безопасно получать зависимости: services.Get&lt;T&gt;().
        /// Никакой MonoBehaviour-зависимости от Awake/Start/Update здесь нет.
        /// </summary>
        void Initialize(ServiceLocator services);

        /// <summary>Освобождение ресурсов, отписка от событий при выгрузке сцены/перезапуске.</summary>
        void Dispose();
    }
}