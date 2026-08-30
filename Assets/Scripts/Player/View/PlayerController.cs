using ShelfRush.Input;
using UnityEngine;

namespace ShelfRush.Player.View
{
    /// <summary>
    /// Контроллер игрока (MonoBehaviour-вью, корень Player prefab). Это ОРКЕСТРАТОР:
    /// он не содержит всю игровую логику. Каждая зона ответственности вынесена в
    /// отдельный компонент и делегируется:
    ///
    ///   PlayerMovement    — скорость/ускорение/замедление/поворот/остановка
    ///   PlayerInteraction — поиск интерактивных объектов в радиусе + состояние
    ///   PlayerCarry       — инвентарь (capacity, add/remove/clear)
    ///   PlayerAnimator    — визуальные состояния idle/walk/carry/interact
    ///
    /// Ввод берётся из существующей системы: <see cref="IPlayerInput"/> (нормализованный
    /// MoveWorld + событие Interact). Источник ввода (PC/Mobile/WebGL) вью не волнует.
    /// DOTween здесь не используется — это «клей», который вызывает компоненты.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerInteraction))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Sub-components (auto-resolved if empty)")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerInteraction interaction;
        [SerializeField] private PlayerCarry carry;
        [SerializeField] private PlayerAnimator playerAnimator;

        private IPlayerInput _input;
        private bool _gameplayEnabled = true;
        private bool _reportedMissingInput;
        private int _inputResolveFrames;

        public PlayerMovement Movement => movement;
        public PlayerInteraction Interaction => interaction;
        public PlayerCarry Carry => carry;
        public PlayerAnimator PlayerAnimator => playerAnimator;
        public IPlayerInput Input => _input;

        /// <summary>Событие, срабатывающее при получении «Interact» из ввода.</summary>
        public event System.Action InteractReceived;

        private void Awake()
        {
            // Собираем под-компоненты (заполняем, если не назначены в инспекторе).
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (interaction == null) interaction = GetComponent<PlayerInteraction>();
            if (carry == null) carry = GetComponentInChildren<PlayerCarry>();
            if (playerAnimator == null) playerAnimator = GetComponentInChildren<PlayerAnimator>();

            // Резолвим сервисы через единственную точку входа (без прямого ServiceLocator).
            ServiceBridge.TryResolve(out _input);
        }

        private void OnEnable()
        {
            // Ленивый резолв: бутстрап мог ещё не построить ServiceLocator.
            if (_input == null) ServiceBridge.TryResolve(out _input);
            if (_input != null) _input.Interact += OnInteract;
            playerAnimator?.SetIdle();
        }

        private void OnDisable()
        {
            if (_input != null) _input.Interact -= OnInteract;
        }

        private void Update()
        {
            // Ленивый резолв ввода: повторяем, пока бутстрап не зарегистрирует IPlayerInput.
            if (_input == null)
            {
                ServiceBridge.TryResolve(out _input);
                if (_input == null)
                {
                    _inputResolveFrames++;
                    if (_inputResolveFrames > 120 && !_reportedMissingInput)
                    {
                        _reportedMissingInput = true;
                        Debug.LogWarning(
                            "[PlayerController] IPlayerInput не найден. Убедитесь, что на сцене есть GameBootstrap " +
                            "(он строит ServiceLocator и регистрирует IPlayerInput). Персонаж не сможет двигаться, " +
                            "пока ввод не будет доступен.", this);
                    }
                }
                else
                {
                    _input.Interact += OnInteract;
                }
            }

            if (!_gameplayEnabled) return;
            Tick(Time.deltaTime);
        }

        private void Tick(float deltaTime)
        {
            if (movement == null) return;

            // Во время взаимодействия игрок останавливается (без «ношения» таргета).
            var interacting = interaction != null && interaction.IsBusy;

            // Детектируем отпускание ввода по СЫРОМУ значению (ДО smoothing): оно
            // обнуляется в тот же кадр, когда тап/клавиша отпущены, тогда как MoveWorld
            // ещё затухает от input smoothing (из-за него и было «скольжение»).
            var rawMove = _input != null ? _input.MoveTargetWorld : Vector3.zero;
            var hasInput = !interacting && rawMove.sqrMagnitude > 0.0001f;

            if (hasInput)
            {
                // Есть ввод — двигаемся плавно (используем сглаженный MoveWorld для разгона).
                movement.DoMove(_input.MoveWorld, deltaTime);
            }
            else if (movement.InstantStop)
            {
                // Ввод отпущен -> мгновенная остановка, без проскальзывания.
                movement.Stop();
            }
            else
            {
                // Плавное замедление через deceleration.
                movement.DoMove(Vector3.zero, deltaTime);
            }

            // Ленивый поиск аниматора: подхватываем, даже если модель была
            // неактивна при Awake и появилась позже.
            if (playerAnimator == null)
            {
                var comps = GetComponentsInChildren<PlayerAnimator>(true);
                playerAnimator = comps != null && comps.Length > 0 ? comps[0] : null;
            }

            if (playerAnimator != null)
            {
                playerAnimator.SetCarrying(carry != null && carry.Count > 0);
                playerAnimator.SetWalking(!interacting && movement.IsMoving);
            }
        }

        private void OnInteract()
        {
            InteractReceived?.Invoke();
            interaction?.OnInteractInput();
        }

        /// <summary>Разрешить/запретить геймплей (пауза, меню и т.п.).</summary>
        public void SetGameplayEnabled(bool enabled)
        {
            _gameplayEnabled = enabled;
            if (!enabled && movement != null) movement.DoMove(Vector3.zero, Time.deltaTime);
        }
    }
}