using UnityEngine;

namespace ShelfRush.Player.View
{
    /// <summary>
    /// Контракт интерактивного объекта (полка, прилавок, клиент, кнопка и т.п.).
    /// Игрок ищет ближайший объект с этим интерфейсом внутри
    /// <see cref="PlayerConfig.InteractionRadius"/> и вызывает <see cref="Interact"/>.
    /// Вью-слой: реализуется на GameObject'ах сцены (будущие ShelfView/CustomerView),
    /// не содержит игровой логики.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Мировая позиция объекта (используется для проверки радиуса).</summary>
        Vector3 Position { get; }

        /// <summary>Подсказка для UI (например, «Взять товар»).</summary>
        string Prompt { get; }

        /// <summary>
        /// Можно ли сейчас взаимодействовать с этим объектом у конкретного игрока.
        /// Например, полка недоступна, если руки полны (player.Carry.IsFull).
        /// </summary>
        bool CanInteract(PlayerController player);

        /// <summary>
        /// Авто-взаимодействие при приближении (подбор без нажатия кнопки/тапа).
        /// Истина для «коробок»/полок с товаром; доставка обычно false (по кнопке).
        /// </summary>
        bool AutoInteractOnApproach { get; }

        /// <summary>Выполнить взаимодействие (дергает player.Carry / player-эффекты).</summary>
        void Interact(PlayerController player);
    }

    /// <summary>
    /// Необязательная база для MonoBehaviour-интерактивов. Обёртывает Position из Transform
    /// и подсказку из поля. Достаточно унаследоваться и реализовать два метода.
    /// </summary>
    public abstract class InteractableComponent : MonoBehaviour, IInteractable
    {
        [Tooltip("Подсказка взаимодействия (показывается в UI/HUD).")]
        [SerializeField] private string prompt = "Взаимодействовать";

        public Vector3 Position => transform.position;

        public string Prompt => prompt;

        public abstract bool CanInteract(PlayerController player);

        /// <summary>Авто-подбор при приближении (по умолчанию выключен).</summary>
        public virtual bool AutoInteractOnApproach => false;

        public abstract void Interact(PlayerController player);
    }
}