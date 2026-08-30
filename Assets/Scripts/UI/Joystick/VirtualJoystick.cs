using System;
using ShelfRush.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShelfRush.UI
{
    /// <summary>
    /// Виртуальный джойстик (UGUI) — «floating / dynamic» для мобильных.
    /// В отличие от статичного джойстика:
    ///   • работает из ЛЮБОЙ точки экрана (палец/мышь можно нажать где угодно);
    ///   • визуальный круг с рукояткой ПОЯВЛЯЕТСЯ в точке нажатия и исчезает при отпускании.
    ///
    /// Это view-компонент: реализует <see cref="IVirtualJoystick"/>, отдаёт нормализованный
    /// вектор оси; игровой логики не содержит. Подключается вручную (GameBootstrap.mobileJoystick).
    ///
    /// Автонастройка иерархии при Awake:
    ///   корневой RectTransform растягивается на весь экран и становится прозрачной зоной
    ///   перехвата касаний (если есть Image — цвет прозрачный, RaycastTarget=true);
    ///   видимый «фон»-круг и «рукоятка» создаются динамически под пальцем.
    ///   Если в инспекторе назначен существующий Handle — он переиспользуется.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class VirtualJoystick : MonoBehaviour, IVirtualJoystick,
        IPointerDownHandler, IDragHandler, IPointerUpHandler, IEndDragHandler
    {
        [Header("References (optional)")]
        [Tooltip("Существующая рукоятка (child). Если пусто — создаётся автоматически.")]
        [SerializeField] private RectTransform handle;

        [Tooltip("Спрайт рукоятки (если создаётся автоматически). Опционально.")]
        [SerializeField] private Sprite handleSprite;

        [Tooltip("Спрайт фона-круга (если создаётся автоматически). Опционально.")]
        [SerializeField] private Sprite backgroundSprite;

        [Header("Behaviour")]
        [Tooltip("Максимальное смещение рукоятки от точки нажатия, в пикселях экрана.")]
        [SerializeField] private float dragRadius = 120f;

        [Tooltip("Мёртвая зона джойстика в долях радиуса (0..1).")]
        [SerializeField] [Range(0f, 0.95f)] private float deadZone = 0.15f;

        [Tooltip("Скрывать круг джойстика, когда палец отпущен (появляется при нажатии).")]
        [SerializeField] private bool showOnlyWhileHeld = true;

        [Tooltip("Размер видимого круга (фона) в пикселях.")]
        [SerializeField] private float baseSize = 240f;

        [Tooltip("Размер рукоятки в пикселях.")]
        [SerializeField] private float handleSize = 110f;

        private RectTransform _root;
        private RectTransform _background;
        private Graphic _rootGraphic;
        private bool _held;
        private Vector2 _baseLocal;      // точка нажатия в лок. координатах root
        private Vector2 _input;

        public Vector2 Value => _input;

        public bool IsActive => _held && _input.sqrMagnitude > 0.0001f;

        public event Action Interact;

        private void Awake()
        {
            _root = (RectTransform)transform;

            // 1) Root = полноэкранная прозрачная зона перехвата касаний.
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;
            _root.pivot = new Vector2(0.5f, 0.5f);

            // Делаем зону прозрачной, но raycastable.
            _rootGraphic = GetComponent<Graphic>();
            if (_rootGraphic != null)
            {
                var c = _rootGraphic.color;
                c.a = 0f;
                _rootGraphic.color = c;
                _rootGraphic.raycastTarget = true;
            }

            // 2) Создаём (или переиспользуем) видимый круг-фон и рукоятку.
            _background = CreateCircleIfMissing("Background", backgroundSprite, baseSize, _root);
            if (handle == null)
            {
                handle = Create(handleSprite, handleSize, "Handle", _background);
            }
            else if (handle.parent != _background)
            {
                handle.SetParent(_background, false);
                SetRectSize(handle, handleSize);
            }

            HideJoystick();
        }

        private void OnEnable()
        {
            Release();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!ScreenToRootLocal(eventData, out var local)) return;

            _held = true;
            _baseLocal = local; // джойстик появляется в точке касания

            // Показываем круг в точке нажатия.
            _background.gameObject.SetActive(true);
            _background.anchoredPosition = local;

            if (handle != null) handle.anchoredPosition = Vector2.zero;
            _input = Vector2.zero;

            OnJoystickDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            OnJoystickDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Очень короткое нажатие без смещения — «взаимодействие» (тап по джойстику).
            if (_input.sqrMagnitude < (deadZone * deadZone))
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

        private bool ScreenToRootLocal(PointerEventData eventData, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _root, eventData.position, eventData.pressEventCamera, out local);
        }

        private void OnJoystickDrag(PointerEventData eventData)
        {
            if (handle == null || !_held) return;
            if (!ScreenToRootLocal(eventData, out var local)) return;

            // Смещение относительно точки нажатия (базы), ограниченное радиусом.
            var offset = local - _baseLocal;
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
            _held = false;
            _input = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            if (showOnlyWhileHeld) HideJoystick();
        }

        private void HideJoystick()
        {
            if (_background != null) _background.gameObject.SetActive(false);
        }

        // ---- Helpers: создание UI ----

        private RectTransform CreateCircleIfMissing(string name, Sprite sprite, float size, RectTransform parent)
        {
            var existing = FindChild(name);
            if (existing != null)
            {
                if (existing.parent == _root)
                {
                    SetRectSize(existing, size);
                    return existing;
                }
            }

            return Create(sprite, size, name, parent);
        }

        private RectTransform FindChild(string name)
        {
            for (var i = 0; i < _root.childCount; i++)
            {
                var child = _root.GetChild(i);
                if (child.name == name) return child as RectTransform;
            }
            return null;
        }

        private RectTransform Create(Sprite sprite, float size, string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            SetRectSize(rt, size);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false; // только root ловит касания
            if (sprite != null) img.sprite = sprite;

            return rt;
        }

        private void SetRectSize(RectTransform rt, float size)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
        }
    }
}