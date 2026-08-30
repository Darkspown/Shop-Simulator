using UnityEngine;

namespace ShelfRush.Player.View
{
    /// <summary>
    /// Следящая камера (MonoBehaviour-вью). Единственная ответственность — плавно
    /// следовать за игроком по горизонтали (XZ), сохраняя смещение по высоте и дистанции.
    ///
    /// Способ размещения (любой):
    ///  - повесьте на объект игрока (корень Player) — тогда компонент сам находит
    ///    главную камеру (тег MainCamera) и двигает её;
    ///  - либо повесьте на саму камеру и назначьте Target вручную.
    ///
    /// DOTween здесь НЕ используется — обычное Lerp-сглаживание в LateUpdate (не движение
    /// игрока и не физика). Качество следования настраивается полями инспектора.
    /// </summary>
    public sealed class PlayerCamera : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("За кем следить. Если пусто — берём корень этого компонента (обычно игрок).")]
        [SerializeField] private Transform target;

        [Tooltip("Камера, которую двигаем. Если пусто — ищем по тегу MainCamera.")]
        [SerializeField] private Camera cameraToMove;

        [Header("Follow")]
        [Tooltip("Смещение камеры относительно цели (в мировых координатах). По умолчанию ~сверху-сбоку.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -8f);

        [Tooltip("Скорость сглаживания (с^-1). Больше = быстрее/жёстче след, меньше = плавнее/инерция.")]
        [Range(1f, 30f)]
        [SerializeField] private float followSmooth = 12f;

        [Tooltip("Фиксировать Y во время следования (не двигать камеру по вертикали относительно цели).")]
        [SerializeField] private bool lockY = true;

        [Header("Rotation")]
        [Tooltip("Строго фиксировать поворот камеры (не вращается при повороте персонажа). Поворот хранится в момент старта.")]
        [SerializeField] private bool lockRotation = true;

        [Tooltip("Если включено — камера всегда смотрит на цель (LookAt). Если выключено и lockRotation включено — держит поворот из момента старта. Работает только при lockRotation.")]
        [SerializeField] private bool lookAtTarget = false;

        private Transform _followTarget;
        private Quaternion _fixedRotation = Quaternion.identity;

        private void Awake()
        {
            if (target != null)
            {
                _followTarget = target;
            }
            else
            {
                // Если компонент висит на игроке — следить за ним.
                _followTarget = transform;
            }

            if (cameraToMove == null)
            {
                var cam = Camera.main;
                if (cam == null) cam = GetComponent<Camera>();
                cameraToMove = cam;
            }
        }

        private void Start()
        {
            if (cameraToMove != null)
            {
                // Запоминаем «мёртвый» поворот камеры при старте, чтобы потом его держать
                // независимо от поворота персонажа/родителя.
                _fixedRotation = cameraToMove.transform.rotation;
            }

            // Мгновенно поставить камеру в правильную позицию при старте,
            // чтобы не было «долёта» камеры к игроку в первом кадре.
            if (cameraToMove != null && _followTarget != null)
            {
                ApplyPosition(1f);
            }
        }

        private void LateUpdate()
        {
            if (cameraToMove == null || _followTarget == null) return;

            // Frame-rate independent smooth: t = 1 - exp(-k * dt).
            var t = followSmooth <= 0f ? 1f : 1f - Mathf.Exp(-followSmooth * Time.deltaTime);
            ApplyPosition(t);
        }

        private void ApplyPosition(float t)
        {
            var camTransform = cameraToMove.transform;
            var desired = _followTarget.position + offset;
            if (lockY)
            {
                // Держим исходную высоту камеры (не прыгаем по Y вслед за целью).
                desired.y = camTransform.position.y;
            }

            camTransform.position = Vector3.Lerp(camTransform.position, desired, t);

            // ---- Жёсткая фиксация поворота ----
            // Даже если камера — дочерний объект персонажа, каждый кадр возвращаем
            // постоянную ориентацию, чтобы она не вращалась при повороте игрока.
            if (lockRotation)
            {
                if (lookAtTarget)
                {
                    var look = _followTarget.position + Vector3.up * 0.5f;
                    var dir = look - camTransform.position;
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        camTransform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                    }
                }
                else
                {
                    camTransform.rotation = _fixedRotation;
                }
            }
        }
    }
}