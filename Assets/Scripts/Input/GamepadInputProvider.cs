using UnityEngine;
using UnityEngine.InputSystem;

namespace ShelfRush.Input
{
    /// <summary>
    /// Провайдер геймпада (опционально для WebGL/ПК): левый стик — движение,
    /// нижняя кнопка — взаимодействие.
    /// </summary>
    public sealed class GamepadInputProvider : InputProviderBase
    {
        private const float DeadZone = 0.2f;

        public override void Tick(float deltaTime)
        {
            if (!Enabled) return;
            var pad = Gamepad.current;
            if (pad == null) { _move = Vector2.zero; return; }

            var raw = pad.leftStick.ReadValue();
            var mag = raw.magnitude;
            _move = mag < DeadZone ? Vector2.zero : (mag > 1f ? raw : raw / mag) * Mathf.Clamp01(mag);

            if (pad.buttonSouth.wasPressedThisFrame)
            {
                FireInteract();
            }
        }
    }
}