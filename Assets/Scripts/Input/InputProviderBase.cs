using System;
using UnityEngine;

namespace ShelfRush.Input
{
    /// <summary>
    /// База для конкретных провайдеров ввода. Хранит текущий вектор движения,
    /// транслирует событие взаимодействия, обрабатывает Enable/Disable.
    /// </summary>
    public abstract class InputProviderBase : Core.IGameService, IInputProvider, Core.ITickable
    {
        private bool _enabled = true;

        public Vector2 Move => _move;

        public event Action Interact;

        protected Vector2 _move;

        public virtual void Initialize(Core.ServiceLocator services) { }

        public virtual void Dispose() { }

        public virtual void Tick(float deltaTime) { }

        public void Enable()
        {
            _enabled = true;
        }

        public void Disable()
        {
            _enabled = false;
            _move = Vector2.zero;
        }

        /// <summary>Защищённая отсылка события взаимодействия (только если провайдер включён).</summary>
        protected void FireInteract()
        {
            if (_enabled) Interact?.Invoke();
        }

        protected bool Enabled => _enabled;
    }
}