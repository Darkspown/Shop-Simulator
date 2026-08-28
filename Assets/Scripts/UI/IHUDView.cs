using ShelfRush.Customers;
using ShelfRush.Economy;
using ShelfRush.Levels;

namespace ShelfRush.UI
{
    /// <summary>
    /// Контракт «вью» HUD (реализует MonoBehaviour на Canvas в сцене).
    /// UIService транслирует игровые события в эти методы, не зная про конкретный UI.
    /// </summary>
    public interface IHUDView
    {
        void SetBalance(CurrencyType currency, int amount);
        void ShowOrder(CustomerOrder order);
        void ShowOrderCompleted(int reward);
        void ShowLevelStart(LevelConfig config);
        void ShowLevelComplete(bool success, int completed, int target);
        void SetPaused(bool paused);
    }
}