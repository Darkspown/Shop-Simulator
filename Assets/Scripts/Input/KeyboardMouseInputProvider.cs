using UnityEngine;
using UnityEngine.InputSystem;

namespace ShelfRush.Input
{
    /// <summary>
    /// Провайдер ввода с клавиатуры и мыши: WASD/стрелки — движение, E/Enter — взаимодействие.
    /// Работает через New Input System (Keyboard.current), без MonoBehaviours.
    /// </summary>
    public sealed class KeyboardMouseInputProvider : InputProviderBase
    {
        public override void Tick(float deltaTime)
        {
            if (!Enabled) return;
            var kb = Keyboard.current;
            if (kb == null) { _move = Vector2.zero; return; }

            var input = Vector2.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;

            _move = input.sqrMagnitude > 1f ? input.normalized : input;

            if (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
            {
                FireInteract();
            }
        }
    }
}