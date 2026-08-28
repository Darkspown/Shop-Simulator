namespace ShelfRush.Core
{
    /// <summary>
    /// Помечает сервис, который нужно обновлять каждый кадр.
    /// Вызывается единым тикером (GameBootstrap.Update) — без MonoBehaviour на каждом сервисе.
    /// </summary>
    public interface ITickable
    {
        void Tick(float deltaTime);
    }
}