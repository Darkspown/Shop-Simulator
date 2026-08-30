using ShelfRush.Core;

namespace ShelfRush.Player.View
{
    /// <summary>
    /// Тонкий мост между prefab-вью (MonoBehaviour) и plain C# сервисами.
    /// Вью-компоненты не должны сами строить зависимости и не должны ходить в
    /// ServiceLocator напрямую — они резолвят нужные сервисы только через этот хелпер,
    /// обращающийся к единственной точке входа <see cref="GameBootstrap"/>.
    /// Если bootstrap не запущен (например, prefab открыт в тестовой сцене без него) —
    /// возвращает false, и компонент аккуратно работает с дефолтами.
    /// </summary>
    internal static class ServiceBridge
    {
        /// <summary>Попытаться получить сервис T из ServiceLocator (без исключений).</summary>
        public static bool TryResolve<T>(out T value) where T : class
        {
            value = null;
            var bootstrap = GameBootstrap.Instance;
            var services = bootstrap != null ? bootstrap.Services : null;
            if (services == null) return false;

            try
            {
                return services.TryGet(out value);
            }
            catch
            {
                value = null;
                return false;
            }
        }
    }
}