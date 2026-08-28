using UnityEngine;

namespace ShelfRush.Input
{
    /// <summary>
    /// Абстракция источника ввода. Игровой код зависит только от этого интерфейса,
    /// а конкретные провайдеры (клавиатура/мышь, тач, геймпад) подменяются через InputService.
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>Нормализованный вектор движения ([−1..1]).</summary>
        Vector2 Move { get; }

        /// <summary>Событие «нажата кнопка взаимодействия» (поднять/положить товар).</summary>
        event System.Action Interact;

        void Enable();
        void Disable();
    }

    /// <summary>
    /// Сервис ввода — обёртка над активным провайдером. Им пользуются Player и UI.
    /// Сам выбирает подходящий провайдер под платформу/подключённые устройства.
    /// </summary>
    public interface IInputService : IInputProvider, Core.IGameService
    {
    }
}