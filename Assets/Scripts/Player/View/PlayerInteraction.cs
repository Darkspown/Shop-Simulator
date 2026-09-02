using System.Collections;
using System.Collections.Generic;
using ShelfRush.Pooling;
using ShelfRush.Products;
using UnityEngine;

namespace ShelfRush.Player.View
{
    /// <summary>Состояние взаимодействия игрока.</summary>
    public enum InteractionState
    {
        /// <summary>Простой поиск цели каждые searchInterval сек.</summary>
        Idle,

        /// <summary>Взаимодействие выполняется (лок по pickupDuration).</summary>
        Interacting,

        /// <summary>Зарезервировано для future (например, короткий кулдаун).</summary>
        Cooldown
    }

    /// <summary>
    /// Взаимодействие игрока (MonoBehaviour-вью). Единственная ответственность — поиск
    /// ближайшего интерактивного объекта в радиусе (<see cref="PlayerConfig.InteractionRadius"/>),
    /// удержание текущей цели и обработка события «Interact» от ввода.
    /// Сам ничего «игрового» не делает — вызывает <see cref="IInteractable.Interact"/>.
    /// </summary>
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerConfig config;

        [Tooltip("LayerMask объектов, которые могут быть интерактивными (полки/клиенты/прилавки).")]
        [SerializeField] private LayerMask interactableMask = ~0;

        [Tooltip("Как часто пересканировать радиус в пределах сцены (сек).")]
        [SerializeField] private float searchInterval = 0.1f;

        private PlayerController _controller;
        private IInteractable _current;
        private InteractionState _state = InteractionState.Idle;
        private Coroutine _searchRoutine;
        private Coroutine _interactionRoutine;
        private readonly List<Collider> _buffer = new List<Collider>();
        private IPoolService _pool;

        /// <summary>Текущий (ближайший доступный) интерактив, либо null.</summary>
        public IInteractable Current => _current;

        /// <summary>Есть ли цель для взаимодействия.</summary>
        public bool HasTarget => _current != null;

        /// <summary>Текущее состояние взаимодействия.</summary>
        public InteractionState State => _state;

        /// <summary>Занят ли игрок интерактивом (нельзя снова взаимодействовать).</summary>
        public bool IsBusy => _state != InteractionState.Idle;

        /// <summary>Радиус поиска из конфига.</summary>
        public float InteractionRadius => config != null ? config.InteractionRadius : 2f;

        /// <summary>Включён ли авто-подбор при приближении (без нажатия кнопки).</summary>
        public bool AutoPickupEnabled => config != null && config.AutoPickup;

        private void Awake()
        {
            if (config == null) ServiceBridge.TryResolve(out config);
            _controller = GetComponent<PlayerController>();
        }

        private void OnEnable()
        {
            _searchRoutine = StartCoroutine(SearchRoutine());
        }

        private void OnDisable()
        {
            if (_searchRoutine != null) StopCoroutine(_searchRoutine);
            if (_interactionRoutine != null) StopCoroutine(_interactionRoutine);
            _searchRoutine = null;
            _interactionRoutine = null;
        }

        private IEnumerator SearchRoutine()
        {
            while (true)
            {
                if (_state == InteractionState.Idle)
                {
                    SearchForTarget();
                    TryAutoInteract();
                    TryAutoCollectProduct();
                }
                yield return new WaitForSeconds(searchInterval);
            }
        }

        /// <summary>Найти ближайший доступный IInteractable в радиусе (OverlapSphere).</summary>
        private void SearchForTarget()
        {
            // Ленивый резолв конфига: бутстрап мог регистрировать PlayerConfig позже Awake.
            if (config == null) ServiceBridge.TryResolve(out config);

            IInteractable best = null;
            if (_controller != null && config != null)
            {
                var origin = transform.position + Vector3.up * 0.5f;
                _buffer.Clear();
                _buffer.AddRange(Physics.OverlapSphere(origin, config.InteractionRadius, interactableMask, QueryTriggerInteraction.Collide));

                var bestSqr = float.MaxValue;
                for (var i = 0; i < _buffer.Count; i++)
                {
                    var col = _buffer[i];
                    var interactable = col.GetComponent<IInteractable>();
                    if (interactable == null || !interactable.CanInteract(_controller)) continue;

                    var sqr = (col.transform.position - transform.position).sqrMagnitude;
                    if (sqr < bestSqr)
                    {
                        bestSqr = sqr;
                        best = interactable;
                    }
                }
            }

            _current = best;
        }

        /// <summary>
        /// Авто-подбор: товар берётся сам при приближении к цели с
        /// <see cref="IInteractable.AutoInteractOnApproach"/> — без нажатий кнопок/тапов.
        /// Проверяет capacity (CanInteract), близость и уважает лок взаимодействия.
        /// </summary>
        private void TryAutoInteract()
        {
            if (!AutoPickupEnabled) return;
            if (_current == null || _state != InteractionState.Idle) return;
            if (!_current.CanInteract(_controller)) return;
            if (!_current.AutoInteractOnApproach) return;
            if (!IsCloseEnough(_current)) return;

            _interactionRoutine = StartCoroutine(DoInteraction(_current));
        }

        /// <summary>Достаточно ли близко игрок к цели (уже в радиусе взаимодействия).</summary>
        private bool IsCloseEnough(IInteractable target)
        {
            if (target == null || _controller == null) return true;
            var radius = InteractionRadius;
            return (target.Position - transform.position).sqrMagnitude <= radius * radius;
        }

        /// <summary>
        /// Авто-подбор коробок (runtime <see cref="Product"/>) при подходе: товар берётся
        /// в руки без нажатий, коробка возвращается в пул (ResetState → Despawn).
        /// Коробки, уже лежащие в руках (дети carryAnchor), исключаются.
        /// </summary>
        private void TryAutoCollectProduct()
        {
            if (!AutoPickupEnabled) return;
            if (_controller == null || _state != InteractionState.Idle) return;

            var carry = _controller.Carry;
            if (carry == null || !carry.CanAdd()) return;

            var product = FindCollectibleProduct(carry);
            if (product == null) return;

            if (carry.TryAdd(product.Data))
            {
                DespawnProduct(product);
            }
        }

        /// <summary>Ближайшая коробка-товар (Product с данными) в радиусе, не в руках.</summary>
        private Product FindCollectibleProduct(PlayerCarry carry)
        {
            // Ленивый резолв конфига: бутстрап мог регистрировать PlayerConfig позже Awake.
            if (config == null) ServiceBridge.TryResolve(out config);
            if (config == null) return null;

            var origin = transform.position + Vector3.up * 0.5f;
            _buffer.Clear();
            _buffer.AddRange(Physics.OverlapSphere(origin, config.InteractionRadius, interactableMask, QueryTriggerInteraction.Collide));

            Product best = null;
            var bestSqr = float.MaxValue;
            var anchor = carry != null ? carry.CarryAnchor : null;

            for (var i = 0; i < _buffer.Count; i++)
            {
                var col = _buffer[i];

                // Коробки в руках (дети якоря) не подбираем повторно.
                if (anchor != null && col.transform.IsChildOf(anchor)) continue;

                var product = col.GetComponent<Product>();
                if (product == null || product.Data == null) continue;

                var sqr = (col.transform.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = product;
                }
            }
            return best;
        }

        /// <summary>Вернуть коробку в пул по despawn-контракту (ResetState → Despawn).</summary>
        private void DespawnProduct(Product product)
        {
            if (product == null) return;
            var go = product.gameObject;
            product.ResetState(); // отвязка данных + сброс визуала
            var pool = ResolvePool();
            if (pool != null) pool.Despawn(go);
            else Destroy(go);
        }

        private IPoolService ResolvePool()
        {
            if (_pool != null) return _pool;
            ServiceBridge.TryResolve(out _pool);
            return _pool;
        }

        /// <summary>
        /// Обработчик нажатия «Interact» от ввода. Игнорируется, если уже занят или цели нет.
        /// </summary>
        public void OnInteractInput()
        {
            if (_state != InteractionState.Idle) return;
            if (_current == null) return;
            if (!_current.CanInteract(_controller)) { SearchForTarget(); return; }

            _interactionRoutine = StartCoroutine(DoInteraction(_current));
        }

        private IEnumerator DoInteraction(IInteractable target)
        {
            _state = InteractionState.Interacting;
            if (_controller != null) _controller.PlayerAnimator?.SetInteraction(true);

            target.Interact(_controller); // внутри дергает PlayerCarry (взять/положить)

            var lockSec = config != null ? config.PickupDuration : 0.3f;
            yield return new WaitForSeconds(lockSec);

            _state = InteractionState.Idle;
            if (_controller != null) _controller.PlayerAnimator?.SetInteraction(false);
            SearchForTarget();
        }
    }
}