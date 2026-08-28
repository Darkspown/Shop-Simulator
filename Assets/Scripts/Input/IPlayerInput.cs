using System;
using UnityEngine;

namespace ShelfRush.Input
{
    /// <summary>
    /// Абстракция ввода для геймплея. Единственный контакт PlayerController/PlayerMovement
    /// с «внешним миром» ввода.
    ///
    /// Геймплей НЕ знает, откуда пришёл input (клавиатура/мышь, тач, геймпад, джойстик):
    /// этот контракт отдаёт уже нормализованный вектор движения в диапазоне [−1..1]
    /// (после dead zone, sensitivity и input smoothing) и событие взаимодействия.
    ///
    /// Конкретные источники (провайдеры) живут на слое <see cref="IInputProvider"/> ниже,
    /// а этот интерфейс — на слое «gameplay-facing input».
    /// </summary>
    public interface IPlayerInput : Core.IGameService, Core.ITickable
    {
        /// <summary>
        /// Нормализованный 2D-вектор движения [−1..1] после обработки:
        /// dead zone → sensitivity → input smoothing. Ноль, если ввода нет/контроллер выключен.
        /// </summary>
        Vector2 Move { get; }

        /// <summary>
        /// Тот же нормализованный ввод, но разложенный на горизонтальную плоскость XZ
        /// (y = 0). Удобно для перемещения Transform в мировом пространстве.
        /// </summary>
        Vector3 MoveWorld { get; }

        /// <summary>
        /// Событие «взаимодействие» (взять/положить товар). Транслируется от активного
        /// источника: клавиатура (E/Enter), тап на таче, кнопка виртуального джойстика.
        /// </summary>
        event Action Interact;

        /// <summary>Включить чтение ввода и обновление сглаженного вектора.</summary>
        void Enable();

        /// <summary>Выключить ввод: вектор обнуляется, smoothing замирает.</summary>
        void Disable();
    }
}