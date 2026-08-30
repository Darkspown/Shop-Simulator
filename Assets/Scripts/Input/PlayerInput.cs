using System;
using ShelfRush.Core;
using UnityEngine;

namespace ShelfRush.Input
{
    /// <summary>
    /// «Gameplay-facing» слой ввода. Оборачивает <see cref="IInputService"/> (провайдеры
    /// под капотом), опционально подмешивает ось виртуального джойстика для мобильных
    /// и приводит всё к единому нормализованному вектору: dead zone → sensitivity →
    /// input smoothing.
    ///
    /// Потребители (PlayerController, PlayerMovement) зависят только от
    /// <see cref="IPlayerInput"/> и никогда не читают Keyboard/Touch/Joystick/Input.GetAxis.
    /// </summary>
    public sealed class PlayerInput : IPlayerInput
    {
        private IInputService _source;
        private PlayerInputConfig _config;

        private IVirtualJoystick _joystick;
        private bool _enabled = true;

        private Vector2 _target;
        private Vector2 _smoothed;

        public event Action Interact;

        public Vector2 Move => _smoothed;

        public Vector3 MoveWorld => new Vector3(_smoothed.x, 0f, _smoothed.y);

        /// <summary>Сырое (ДО smoothing) значение: обнуляется сразу при отпускании.</summary>
        public Vector2 MoveTarget => _target;

        /// <summary>Сырое значение, разложенное на XZ (y = 0).</summary>
        public Vector3 MoveTargetWorld => new Vector3(_target.x, 0f, _target.y);

        /// <summary>Подключить виртуальный джойстик (mobile UI). Необязательно.</summary>
        public void AttachJoystick(IVirtualJoystick joystick)
        {
            if (_joystick != null) _joystick.Interact -= OnJoystickInteract;
            _joystick = joystick;
            if (_joystick != null) _joystick.Interact += OnJoystickInteract;
        }

        public void Initialize(ServiceLocator services)
        {
            _source = services.Get<IInputService>();
            services.TryGet<PlayerInputConfig>(out _config);
            if (_source != null) _source.Interact += OnSourceInteract;
        }

        public void Dispose()
        {
            if (_source != null) _source.Interact -= OnSourceInteract;
            if (_joystick != null) _joystick.Interact -= OnJoystickInteract;
            _source = null;
            _joystick = null;
            _config = null;
            _target = Vector2.zero;
            _smoothed = Vector2.zero;
        }

        public void Tick(float deltaTime)
        {
            if (!_enabled) return;

            // Источник: если джойстик активен — его ось, иначе — оси провайдеров
            // (клавиатура/тач/геймпад уже нормализованы на своём слое).
            var raw = (_joystick != null && _joystick.IsActive)
                ? _joystick.Value
                : (_source != null ? _source.Move : Vector2.zero);

            _target = ApplyDeadZoneAndSensitivity(raw);

            // Frame-rate independent exponential smoothing.
            var smoothing = _config != null ? _config.Smoothing : 12f;
            if (smoothing <= 0f)
            {
                _smoothed = _target;
            }
            else
            {
                var alpha = 1f - Mathf.Exp(-smoothing * deltaTime);
                _smoothed = Vector2.Lerp(_smoothed, _target, alpha);
            }
        }

        public void Enable()
        {
            _enabled = true;
        }

        public void Disable()
        {
            _enabled = false;
            _target = Vector2.zero;
            _smoothed = Vector2.zero;
        }

        private Vector2 ApplyDeadZoneAndSensitivity(Vector2 raw)
        {
            var deadZone = _config != null ? _config.DeadZone : 0.15f;
            var sensitivity = _config != null ? _config.Sensitivity : 1f;

            var mag = raw.magnitude;
            if (mag <= deadZone) return Vector2.zero;

            // Перемапим [deadZone..1] -> [0..1], чтобы старт движения был плавным.
            var scaled = (mag - deadZone) / Mathf.Max(0.0001f, 1f - deadZone);
            scaled *= sensitivity;
            scaled = Mathf.Clamp01(scaled);

            return raw.normalized * scaled;
        }

        private void OnSourceInteract() => Interact?.Invoke();

        private void OnJoystickInteract() => Interact?.Invoke();
    }
}