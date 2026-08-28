using System;
using UnityEngine;

namespace ShelfRush.Input
{
    /// <summary>
    /// Контракт виртуального джойстика (mobile UI). Игровая логика не знает про него,
    /// но <see cref="PlayerInput"/> опционально сливает его ось с осью провайдеров:
    /// пока джойстик активен — движение из джойстика, иначе из <see cref="IInputService"/>.
    ///
    /// Реализации — обычные view-компоненты на Canvas (см. ShelfRush.UI.VirtualJoystick),
    /// которые создаются как prefab и подключаются вручную (сцена не меняется).
    /// </summary>
    public interface IVirtualJoystick
    {
        /// <summary>Нормализованный вектор оси [−1..1] (после собственного dead zone джойстика).</summary>
        Vector2 Value { get; }

        /// <summary>True, пока джойстик зажат пользователем и выдаёт значимый ввод.</summary>
        bool IsActive { get; }

        /// <summary>Событие «нажата кнопка взаимодействия» на джойстике (по желанию).</summary>
        event Action Interact;
    }
}