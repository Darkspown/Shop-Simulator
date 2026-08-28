using System;
using System.Collections.Generic;
using ShelfRush.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShelfRush.Input
{
    /// <summary>
    /// Сервис ввода: держит всех провайдеров и активного, выбирает его по платформе
    /// и подключённым устройствам, тикает активного и транслирует его Move/Interact.
    /// Игровой код (Player, UI) зависит только от IInputService.
    /// </summary>
    public sealed class InputService : IInputService, ITickable
    {
        private readonly List<IInputProvider> _providers = new List<IInputProvider>();
        private IInputProvider _active;

        public Vector2 Move => _active != null ? _active.Move : Vector2.zero;

        public event Action Interact;

        public void Initialize(ServiceLocator services)
        {
            _providers.Add(new KeyboardMouseInputProvider());
            _providers.Add(new TouchInputProvider());
            _providers.Add(new GamepadInputProvider());

            RefreshActiveProvider();

            // Меняем активный провайдер при подключении/отключении устройств.
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        public void Dispose()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            if (_active != null)
            {
                _active.Interact -= OnActiveInteract;
                _active.Disable();
            }
            _providers.Clear();
        }

        public void Tick(float deltaTime)
        {
            (_active as ITickable)?.Tick(deltaTime);
        }

        public void Enable()
        {
            _active?.Enable();
        }

        public void Disable()
        {
            _active?.Disable();
        }

        /// <summary>Взаимодействие от активного провайдера пересылается наружу.</summary>
        private void OnActiveInteract() => Interact?.Invoke();

        private void RefreshActiveProvider()
        {
            var next = PickProvider();
            if (ReferenceEquals(next, _active)) return;

            if (_active != null)
            {
                _active.Interact -= OnActiveInteract;
                _active.Disable();
            }

            _active = next;

            if (_active != null)
            {
                _active.Interact += OnActiveInteract;
                _active.Enable();
            }
        }

        private IInputProvider PickProvider()
        {
            // Приоритет: тач на мобильных, геймпад, если подключён, иначе клавиатура/мышь.
            if (Application.isMobilePlatform && Touchscreen.current != null) return Get<TouchInputProvider>();
            if (Gamepad.current != null) return Get<GamepadInputProvider>();
            return Get<KeyboardMouseInputProvider>();
        }

        private IInputProvider Get<T>() where T : IInputProvider
        {
            for (var i = 0; i < _providers.Count; i++)
            {
                if (_providers[i] is T) return _providers[i];
            }
            return null;
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            RefreshActiveProvider();
        }
    }
}