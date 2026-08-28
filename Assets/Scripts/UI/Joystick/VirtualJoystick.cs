using System;
using ShelfRush.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ShelfRush.UI
{
    /// <summary>
    /// Виртуальный джойстик (UGUI) для мобильных устройств — аналоговый, с собственным
    /// dead zone. Это view-компонент: он НЕ содержит игровой логики, а лишь реализует
    /// <see cref="IVirtualJoystick"/>, отдавая нормализованный вектор оси.
    ///
    /// НЕ добавляется в сцену автоматически: создаётся как prefab и подключается вручную
    /// (подробности — в Documentation/CROSS_PLATFORM.md). Чтобы ось джойстика попала в
    /// игровую логику, его нужно передать в PlayerInput.AttachJoystick(...) — обычно
    /// перетащив экземпляр на поле GameBootstrap.mobileJoystick.
    ///
    /// Структура prefab (на Canvas):
    ///   VirtualJoystick (этот компонент + Image «фон»)
    ///     └─ Handle (child RectTransform + Image «рукоятка»)
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class VirtualJoystick : MonoBehaviour, IVirtualJoystick,
        IPointerDownHandler, IDragHandler, IPointerUpHandler, IEndDragHandler
    {
        [Header("References")]
        [Tooltip("Рукоятка (child). Перемещается внутри фона.")]
        [SerializeField] private RectTransform handle;

        [Header("Behaviour")]
        [Tooltip("Максимальное смещение рукоятки от центра в пикселях экрана.")]
        [SerializeField] private float dragRadius = 100f;

        [Tooltip("Мёртвая зона джойстика в долях радиуса (0..1).")]
        [SerializeField] [Range(0f, 0.95f)] private float deadZone = 0.15f;

        [Tooltip("Кликовый радиус: отпускание пальца/мыши вне этого расстояния от центра.")]
        [SerializeField] private float clickRadius = 50f;

        private RectTransform _root;
        private Vector2 _center;
        private Vector2 _input;

        public Vector2 Value => _input;

        public bool IsActive => _input.sqrMagnitude > 0.0001f;

        public event Action Interact;

        private void Awake()
        {
            _root = (RectTransform)transform;
            _center = _root.rect.center;
        }

        private void OnEnable()
        {
            Release();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnJoystickDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            OnJoystickDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Очень короткое нажатие без смещения — «взаимодействие» (тап по джойстику).
            if (_input.sqrMagnitude < clickRadius * clickRadius / (dragRadius * dragRadius))
            {
                Interact?.Invoke();
            }
            Release();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Release();
        }

        /// <summary>
        /// Программный вызов «взаимодействия» — например, из кнопки на Canvas
        /// (см. описание ручного подключения кнопки).
        /// </summary>
        public void InvokeInteract()
        {
            Interact?.Invoke();
        }

        private void OnJoystickDrag(PointerEventData eventData)
        {
            if (handle == null) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _root, eventData.position, eventData.pressEventCamera, out var local))
            {
                return;
            }

            var offset = local - _center;
            offset = Vector2.ClampMagnitude(offset, dragRadius);

            handle.anchoredPosition = offset;

            // Нормализация [−1..1] с радиальным dead zone.
            if (offset.sqrMagnitude <= 0.0001f)
            {
                _input = Vector2.zero;
                return;
            }

            _input = offset / dragRadius;
            var mag = _input.magnitude;
            if (mag < deadZone)
            {
                _input = Vector2.zero;
            }
        }

        private void Release()
        {
            _input = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }
    }
}