using System;
using System.Collections.Generic;
using ShelfRush.Customers;
using ShelfRush.Economy;
using ShelfRush.Input;
using ShelfRush.Levels;
using ShelfRush.Platform;
using ShelfRush.Player;
using ShelfRush.Pooling;
using ShelfRush.Products;
using ShelfRush.Save;
using ShelfRush.Shelves;
using ShelfRush.UI;
using UnityEngine;

namespace ShelfRush.Core
{
    /// <summary>
    /// Единственный MonoBehaviour-точки входа в игру (Bootstrap).
    /// Строит ServiceLocator, EventBus, создаёт и регистрирует все сервисы,
    /// инициализирует их в порядке зависимостей и тикает ITickable каждый кадр.
    ///
    /// ВАЖНО: здесь фабрикуются объекты систем (plain C#). Сцена не используется,
    /// а сам компонент лишь «разгоняет» архитектуру.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        /// <summary>Публичный доступ к единственному Bootstrap для вью-компонентов (резолв сервисов).</summary>
        public static GameBootstrap Instance { get; private set; }

        /// <summary>Доступ к реестру сервисов для prefab-вью (см. Player/View/ServiceBridge).</summary>
        public ServiceLocator Services => _services;

        [Header("DATA (ScriptableObjects)")]
        [SerializeField] private EconomyConfig economyConfig;
        [SerializeField] private PlayerConfig playerConfig;
        [SerializeField] private PlayerInputConfig playerInputConfig;
        [SerializeField] private ProductData[] products;
        [SerializeField] private LevelConfig[] levels;

        [Header("MOBILE (optional)")]
        [Tooltip("Экземпляр виртуального джойстика на Canvas (prefab). Если не назначен — мобильный ввод работает свайпом.")]
        [SerializeField] private VirtualJoystick mobileJoystick;

        private ServiceLocator _services;
        private IEventBus _eventBus;
        private readonly List<IGameService> _initialized = new List<IGameService>();
        private readonly List<ITickable> _tickables = new List<ITickable>();

        private void Awake()
        {
            Instance = this;
            Build();
        }

        private void Update()
        {
            // Единый тикер: сервисы не имеют собственных Update/MonoBehaviour.
            var dt = Time.deltaTime;
            for (var i = 0; i < _tickables.Count; i++) _tickables[i].Tick(dt);
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Instance, this)) Instance = null;
            for (var i = _initialized.Count - 1; i >= 0; i--) _initialized[i].Dispose();
            _services?.Clear();
        }

        public void Restart() { OnDestroy(); Build(); }

        private void Build()
        {
            Instance = this;
            _services = new ServiceLocator();
            _eventBus = new EventBus();
            _initialized.Clear();
            _tickables.Clear();

            // ---- Инфраструктура ----
            _services.Register<IEventBus>(_eventBus);

            // ---- Создание сервисов (без зависимостей на данном этапе) ----
            var gameState = new GameStateMachine();
            var platform = new PlatformService();
            var save = new PlayerPrefsSaveService();
            var pool = new LeanPoolService();
            var catalog = new ProductCatalog(products ?? Array.Empty<ProductData>());
            var input = new InputService();
            var playerInput = new PlayerInput();
            var wallet = new Wallet();
            var economy = new EconomyService(wallet);
            var stock = new StockService();
            var customers = new CustomerService();
            var player = new PlayerController();
            var ui = new UIService();
            var levelManager = new LevelManager(levels ?? Array.Empty<LevelConfig>());

            // Register by interface (контракты для остальных систем).
            _services.Register<IGameStateMachine>(gameState);
            _services.Register<IPlatformService>(platform);
            _services.Register<ISaveService>(save);
            _services.Register<IPoolService>(pool);
            _services.Register<IProductCatalog>(catalog);
            _services.Register<IInputService>(input);
            _services.Register<IPlayerInput>(playerInput);
            _services.Register<IEconomyService>(economy);
            _services.Register<IStockService>(stock);
            _services.Register<ICustomerService>(customers);
            _services.Register<IPlayerController>(player);
            _services.Register<ILevelManager>(levelManager);

            // Данные-конфиги доступны для Lookup (не сервисы).
            if (economyConfig != null) _services.Register(economyConfig);
            if (playerConfig != null) _services.Register(playerConfig);
            if (playerInputConfig != null) _services.Register(playerInputConfig);

            // Опциональный виртуальный джойстик (mobile UI) встраивается в ввод вручную.
            if (mobileJoystick != null) playerInput.AttachJoystick(mobileJoystick);

            // Порядок инициализации = порядок зависимостей (родитель раньше детей).
            Initialize(gameState);
            Initialize(platform);
            Initialize(save);
            Initialize(pool);
            Initialize(catalog);
            Initialize(input);
            Initialize(playerInput);
            Initialize(economy);
            Initialize(stock);
            Initialize(customers);
            Initialize(player);
            Initialize(ui);
            Initialize(levelManager);

            // Старт жизненного цикла.
            gameState.Set(GameState.MainMenu);
        }

        private void Initialize(IGameService service)
        {
            service.Initialize(_services);
            _initialized.Add(service);
            if (service is ITickable tickable) _tickables.Add(tickable);
        }
    }
}