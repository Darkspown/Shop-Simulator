using UnityEngine;

namespace ShelfRush.Player.View
{
    /// <summary>
    /// Движение игрока (MonoBehaviour-вью). Единственная ответственность — превратить
    /// нормализованный Vector3 (XZ) в смещение с плавным набором/гашением скорости,
    /// поворотом модели к направлению движения и корректной остановкой.
    ///
    /// ВАЖНО: этот компонент получает готовый нормализованный ввод от PlayerController
    /// (тот читает IPlayerInput). Здесь НЕТ вызовов Input.GetAxis/Keyboard/Touchscreen.
    /// DOTween здесь не используется — это математика движения (не визуальный эффект).
    /// </summary>
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerConfig config;

        [Tooltip("(Опционально) кинематичный Rigidbody для MovePosition вместо прямого движения Transform.")]
        [SerializeField] private Rigidbody body;

        private Vector3 _velocity;
        private Vector3 _faceDirection = Vector3.forward;

        public PlayerConfig Config
        {
            get => config;
            set => config = value;
        }

        /// <summary>Мгновенная ли остановка при отпускании ввода (из конфига).</summary>
        public bool InstantStop => config != null && config.InstantStop;

        /// <summary>Двигается ли игрок в данный момент (скорость выше порога).</summary>
        public bool IsMoving { get; private set; }

        /// <summary>Текущий вектор скорости (в плоскости XZ).</summary>
        public Vector3 Velocity => _velocity;

        /// <summary>Текущее направление «лица» (normalized, XZ).</summary>
        public Vector3 FaceDirection => _faceDirection;

        private void Awake()
        {
            if (config == null) ServiceBridge.TryResolve(out config);
            if (body == null) body = GetComponentInChildren<Rigidbody>();
        }

        private void OnDisable()
        {
            _velocity = Vector3.zero;
            IsMoving = false;
        }

        /// <summary>Мгновенно остановить игрока (обнулить скорость) — без инерции.</summary>
        public void Stop()
        {
            _velocity = Vector3.zero;
            IsMoving = false;
        }

        /// <summary>
        /// Один шаг движения. <paramref name="worldMove"/> — нормализованное направление
        /// в мировых XZ (игнорируется y). Нулевой вектор = тормозим до полной остановки.
        /// </summary>
        public void DoMove(Vector3 worldMove, float deltaTime)
        {
            // Ленивый резолв конфига: бутстрап мог регистрировать PlayerConfig позже Awake.
            if (config == null) ServiceBridge.TryResolve(out config);
            if (config == null) return;

            var input = new Vector3(worldMove.x, 0f, worldMove.z);
            var inputMag = input.magnitude;

            if (inputMag > 0.0001f)
            {
                // ---- Ускорение ----
                var dir = input / inputMag;
                _faceDirection = dir;
                _velocity += dir * (config.Acceleration * deltaTime);

                // Ограничиваем максимальной скоростью.
                var sqrSpeed = config.MoveSpeed * config.MoveSpeed;
                if (_velocity.sqrMagnitude > sqrSpeed)
                    _velocity = _velocity.normalized * config.MoveSpeed;
            }
            else
            {
                // ---- Остановка при отпускании ввода (тап/кнопка) ----
                if (config.InstantStop)
                {
                    // Мгновенная пауза: без инерции, без проскальзывания.
                    _velocity = Vector3.zero;
                }
                else if (_velocity.sqrMagnitude > 0f)
                {
                    // ---- Плавное замедление (если instantStop выключен) ----
                    var speed = _velocity.magnitude;
                    var next = Mathf.Max(0f, speed - config.Deceleration * deltaTime);
                    _velocity = next <= 0f ? Vector3.zero : _velocity * (next / speed);
                }
            }

            IsMoving = _velocity.sqrMagnitude > 0.0001f;

            // ---- Перемещение ----
            var step = _velocity * deltaTime;
            if (body != null)
            {
                body.MovePosition(body.position + step);
            }
            else
            {
                transform.position += step;
            }

            // ---- Поворот модели к направлению движения ----
            if (IsMoving && _velocity.sqrMagnitude > 0.0001f)
            {
                var target = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
                var t = Mathf.Min(1f, config.RotationSpeed * deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, t);
            }
        }
    }
}