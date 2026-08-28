using UnityEngine;
using UnityEngine.InputSystem;

namespace ShelfRush.Input
{
    /// <summary>
    /// Провайдер сенсорного ввода. Движение — «виртуальный джойстик» из свайпа по левой части
    /// экрана (для базы достаточно смещения тача); короткий тап = взаимодействие.
    /// Реальный джойстик (UGUI) подключается на этапе реализации UI.
    /// </summary>
    public sealed class TouchInputProvider : InputProviderBase
    {
        private const float DeadZone = 0.1f;

        private Vector2 _pressOrigin;
        private bool _pressing;
        private float _pressTime;

        public override void Tick(float deltaTime)
        {
            if (!Enabled) return;
            var ts = Touchscreen.current;
            if (ts == null) { _move = Vector2.zero; return; }

            var touch = ts.primaryTouch;
            var phase = touch.phase.ReadValue();

            switch (phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    _pressing = true;
                    _pressOrigin = touch.position.ReadValue();
                    _pressTime = Time.unscaledTime;
                    _move = Vector2.zero;
                    break;

                case UnityEngine.InputSystem.TouchPhase.Moved:
                    if (!_pressing) break;
                    var delta = (Vector2)touch.position.ReadValue() - _pressOrigin;
                    if (delta.sqrMagnitude > DeadZone * DeadZone)
                    {
                        _move = delta.normalized;
                    }
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    // Короткий тап без смещения — взаимодействие.
                    var moved = ((Vector2)touch.position.ReadValue() - _pressOrigin).sqrMagnitude;
                    bool wasQuick = (Time.unscaledTime - _pressTime) < 0.3f;
                    if (moved < 4f && wasQuick) FireInteract();
                    _pressing = false;
                    _move = Vector2.zero;
                    break;
            }
        }
    }
}