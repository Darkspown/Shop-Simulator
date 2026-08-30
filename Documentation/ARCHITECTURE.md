# ARCHITECTURE — Shelf Rush

> Базовый каркас игровой архитектуры. Описывает реализованные модули, зависимости,
> интерфейсы, события и порядок инициализации.
> Дата: 28.08.2026 | Автор: Cline.

---

## 1. Обзор

Проект является «чистым стартом». Реализована **базовая архитектура** без сцены и без
игровых сущностей в редакторе: весь игровой код — это **plain C# сервисы** (не MonoBehaviour)
за исключением единственной точки входа `GameBootstrap`. Данные вынесены в `ScriptableObject`,
связь между системами — через `ServiceLocator` (DI без внешних библиотек) и типизированную
шину событий `EventBus`.

Ключевые принципы:

- **SOLID**, отсутствие «God Object».
- **Data-driven**: данные в `ScriptableObject`, логика в сервисах.
- **Интерфейсы вместо конкретных классов** — каждой системе даётся только то, что ей нужно.
- **События вместо прямой связи** — публикация/подписка через `EventBus`.
- **Минимум MonoBehaviour** — только `GameBootstrap` (и будущие view-компоненты на сцене).
- **Один gameplay-API** + разные источники ввода (PC/Mobile/WebGL/Yandex).

---

## 2. Структура кода

Каталог `Assets/Scripts` разбит на модули по зонам ответственности:

```
Assets/Scripts/
├── Core/        # ServiceLocator, EventBus, IGameService/ITickable, GameStateMachine, GameBootstrap
├── Input/       # IInputProvider/IInputService + провайдеры (клавиатура/тач/геймпад)
├── Player/      # PlayerController (движение, инвентарь), PlayerConfig
│   └── View/    # MonoBehaviour-вью на prefab (PlayerController/Movement/Interaction/Carry/Animator)
├── Products/    # ProductData (SO), каталог IProductCatalog/ProductCatalog
├── Shelves/     # ShelfData (SO), учёт запасов IStockService/StockService
├── Customers/   # Модель заказа, ICustomerService/CustomerService
├── Levels/      # LevelConfig (SO), ILevelManager/LevelManager
├── Economy/     # Wallet, CurrencyType, IEconomyService/EconomyService, EconomyConfig
├── Save/        # SaveData, ISaveService/PlayerPrefsSaveService
├── Platform/    # IPlatformService/PlatformService (обёртка платформы)
├── UI/          # Контракт IHUDView + UIService (трансляция событий в UI)
└── Pooling/     # IPoolService/LeanPoolService (обёртка над LeanPool)
```

Все типы в корневой сборке `Assembly-CSharp`. LeanPool подключается как asmdef-плагин
и доступен автоматически.

---

## 3. Модули и их ответственность

### 3.1 Core
- `IGameService` — контракт сервиса: `Initialize(ServiceLocator)` и `Dispose()`.
- `ITickable` — сервис, обновляемый каждый кадр единым тикером (`GameBootstrap.Update`).
- `ServiceLocator` — реестр сервисов (регистрация по интерфейсу, `Get<T>()` / `TryGet<T>()`).
- `EventBus` — типизированная шина событий (`Subscribe<T>`/`Publish<T>`, подписка = `IDisposable`).
- `GameStateMachine` — глобальный lifecycle (`GameState`), публикует `GameStateChangedEvent`.
- `GameEvents` — readonly-структуры-события (payload без логики).
- `GameBootstrap` — **единственный MonoBehaviour**: строит locator/event bus, создаёт,
  регистрирует и инициализирует все сервисы в порядке зависимостей, тикает `ITickable`.

### 3.2 Input
- `IInputProvider` — базовый контракт источника ввода (`Move`, событие `Interact`).
- `IInputService` — выбирает активный провайдер (тач на мобильных, геймпад при подключении,
  иначе клавиатура/мышь) и транслирует его сигналы.
- Провайдеры (`KeyboardMouseInputProvider`, `TouchInputProvider`, `GamepadInputProvider`) —
  чтение New Input System (`Keyboard/Gamepad/Touchscreen.current`) без MonoBehaviour.
- **`IPlayerInput` / `PlayerInput`** — «gameplay-facing» слой ввода (единый для
  PC/Mobile/WebGL/Yandex): `dead zone → sensitivity → input smoothing`, опциональное
  слияние оси виртуального джойстика (`IVirtualJoystick`). Отдаёт нормализованный
  `Move`/`MoveWorld`. Геймплей зависит только от него.
- `PlayerInputConfig` — настройки единого ввода (SO).

### 3.3 Player
- `PlayerController` — «оркестратор»: через `IPlayerInput` получает нормализованный ввод,
  делегирует движение в `PlayerMovement`; ведёт инвентарь (`Carried`, `CarryCapacity`),
  берёт товар с полки (`IStockService`) и выполняет заказ клиента
  (`ICustomerService.TryCompleteOrder`).
- `PlayerMovement` — движение по готовому нормализованному вектору (XZ); **не читает
  никакой input** (нет `Input.GetAxis`/`Keyboard`/`Touch`/`Joystick`).
- `PlayerConfig` — настройки движения/вместимости.

> **Вью-слой на prefab** (MonoBehaviour) см. в `Documentation/PREFAB_PLAYER.md`:
> `ShelfRush.Player.View.PlayerController` (оркестратор) + `PlayerMovement`,
> `PlayerInteraction`, `PlayerCarry`, `PlayerAnimator`, `IInteractable`. Они получают
> зависимости через `ServiceBridge` → `GameBootstrap.Instance.Services` и используют
> только `IPlayerInput` (без чтения используемых устройств).

### 3.4 Products
- `ProductData` — ScriptableObject товара (id, имя, спрайт, prefab, цена).
- `IProductCatalog` / `ProductCatalog` — каталог всех товаров (строится на бустрапе).

### 3.5 Shelves
- `ShelfData` — ScriptableObject полки (товар, вместимость, точки размещения).
- `IStockService` / `StockService` — учёт остатков: `RegisterShelf`, `GetStock`,
  `TryTakeProduct`, `Restock`; публикует `ShelfStockChangedEvent`.

### 3.6 Customers
- `CustomerOrder` — модель заказа (товар, количество, награда, лимит времени).
- `CustomerService` — создаёт заказы из каталога, ведёт таймеры; при выполнении публикует
  `CustomerOrderCompletedEvent`, при тайм-ауте — `CustomerLeftEvent`.

### 3.7 Economy
- `CurrencyType`, `Wallet` — балансы валют (data-модель с событием `Changed`).
- `IEconomyService` / `EconomyService` — начисление/списание, публикация `CurrencyChangedEvent`,
  авто-награда за выполненный заказ.

### 3.8 Levels
- `LevelConfig` — ScriptableObject уровня (полки, товары, цель заказов, лимит времени).
- `ILevelManager` / `LevelManager` — старт/пауза/перезапуск, таймер, прогресс (по
  `CustomerOrderCompletedEvent`), публикует `LevelStartedEvent`/`LevelCompletedEvent`/
  `LevelPauseChangedEvent`; применяет паузу по `GamePauseRequestedEvent`.

### 3.9 Save
- `SaveData` — сериализуемый снимок (монеты, кристаллы, прогресс, язык).
- `ISaveService` / `PlayerPrefsSaveService` — локальное сохранение (PlayerPrefs + JsonUtility).
  (Облачный save Yandex — отдельная реализация интерфейса.)

### 3.10 Platform
- `IPlatformService` / `PlatformService` — единая обёртка платформы: пауза при потере фокуса
  (`PauseToggled`, публикация `GamePauseRequestedEvent`), язык, сигнал ready, реклама
  (interstitial/rewarded), запрос save. Yandex-реализация (YG2) подключается отдельным классом.

### 3.11 UI
- `IHUDView` — контракт HUD (реализуется компонентом на Canvas).
- `UIService` — подписывается на события и транслирует их в `IHUDView` (без MonoBehaviour).

### 3.12 Pooling
- `IPoolService` — абстракция пула (`Spawn`/`Despawn`/`DespawnAll`).
- `LeanPoolService` — обёртка над `Lean.Pool.LeanPool`.



---

## 4. Зависимости систем

Диаграмма «кто кого вызывает». Стрелка `A → B` означает «A зависит от B».

```
PlayerController → IPlayerInput, IStockService, ICustomerService, IEventBus, PlayerConfig
PlayerInput ─────────► IInputService, PlayerInputConfig
LevelManager ────► IStockService, ICustomerService, IEventBus
CustomerService ─► IProductCatalog, IEventBus
StockService ────► IEventBus
EconomyService ──► Wallet, IEventBus, EconomyConfig
UIService ───────► IEventBus (+ IHUDView)
PlatformService ─► IEventBus
LeanPoolService ─► Lean.Pool.LeanPool (плагин)
PlayerPrefsSave  ─► PlayerPrefs/JsonUtility (Unity)
InputService ────► провайдеры ввода (New Input System)

GameBootstrap → создаёт и инициализирует ВСЕ сервисы (ServiceLocator + EventBus)
```

Правило: игровая логика знает только интерфейсы + `IEventBus`, но не конкретные системы
и сторонние SDK (YG2, DOTween и т.п.). Сторонние плагины употребляются через тонкие обёртки.

---

## 5. Основные интерфейсы

| Интерфейс | Роль | Где потребляется |
|---|---|---|
| `IGameService` | базовый контракт сервиса (init/dispose) | все системы |
| `ITickable` | сервис, обновляемый каждый кадр | кор-тикер `GameBootstrap` |
| `IEventBus` | шина событий | все системы |
| `IInputService` / `IInputProvider` | устройства ввода (`Move`, `Interact`) | `PlayerInput`, UI |
| `IPlayerInput` / `PlayerInput` | единый gameplay-ввод (`Move`, `MoveWorld`, `Interact`) | Player |
| `IStockService` | учёт запасов полок (`TryTakeProduct`, `Restock`) | Player, Level |
| `ICustomerService` | заказы клиентов (`CreateOrder`, `TryCompleteOrder`) | Player, Level |
| `IEconomyService` | экономика (`Wallet`, `AddCurrency`, `TrySpend`) | Level, UI |
| `ILevelManager` | уровни (`StartLevel`, `SetPaused`) | GameBootstrap, UI |
| `IPlayerController` | контроллер игрока | сцена/UI (будущее) |
| `IProductCatalog` | каталог товаров | CustomerService |
| `ISaveService` | сохранение (PlayerPrefs/облако) | Economy/Level (будущее) |
| `IPlatformService` | платформа (пауза, реклама, язык) | Level, Bootstrap |
| `IPoolService` | пулинг объектов | Products/Customers (будущее) |
| `IHUDView` | вью HUD | UIService |

---

## 6. Игровые события (EventBus)

Payload-структуры объявлены в `Core/GameEvents.cs`:

| Событие | Публикует | Слушают |
|---|---|---|
| `GameStateChangedEvent` | `GameStateMachine` | UI |
| `CurrencyChangedEvent` | `EconomyService` | UI |
| `ProductPickedEvent` | `PlayerController` | UI |
| `ProductDeliveredEvent` | `CustomerService` | UI |
| `ShelfStockChangedEvent` | `StockService` | UI |
| `CustomerOrderCreatedEvent` | `CustomerService` | UI |
| `CustomerOrderCompletedEvent` | `CustomerService` | **Economy** (награда), **LevelManager** (прогресс), UI |
| `CustomerLeftEvent` | `CustomerService` | LevelManager |
| `LevelStartedEvent` | `LevelManager` | UI |
| `LevelCompletedEvent` | `LevelManager` | UI |
| `LevelPauseChangedEvent` | `LevelManager` | UI |
| `GamePauseRequestedEvent` | `PlatformService` | **LevelManager** (применяет паузу) |

---

## 7. Lifecycle и порядок инициализации

### Lifecycle состояний (`GameState`)
```
Boot → MainMenu → LevelPlaying ⇄ LevelPaused → LevelCompleted → (GameOver)
```
`GameStateMachine.Set()` публикует `GameStateChangedEvent`. Никто, кроме неё, не меняет
состояние напрямую.

### Порядок инициализации в `GameBootstrap.Build()`
1. Создаются `EventBus` и `ServiceLocator` (инфраструктура).
2. Регистрируются ВСЕ сервисы по интерфейсам (locator заполняется целиком).
3. Конфиги (`EconomyConfig`, `PlayerConfig`) тоже регистрируются для `Get<T>()`.
4. `Initialize(...)` в порядке зависимостей (родитель раньше детей):
   1. `GameStateMachine`
   2. `PlatformService`
   3. `PlayerPrefsSaveService`
   4. `LeanPoolService`
   5. `ProductCatalog`
   6. `InputService`
   7. `EconomyService`
   8. `StockService`
   9. `CustomerService`
   10. `PlayerController`
   11. `UIService`
   12. `LevelManager`
5. `gameState.Set(GameState.MainMenu)` — старт жизненного цикла.

Каждый кадр `GameBootstrap.Update()` вызывает `Tick(deltaTime)` у всех `ITickable`.
При выгрузке `OnDestroy()` сервисы `Dispose()` в обратном порядке.



---

## 8. Data flow (ключевые последовательности)

### Взятие товара с полки игроком
```
IInputService.Interact → PlayerController.TryPickUp(shelf)
   → IStockService.TryTakeProduct(shelf) : остаток −1
   → EventBus.Publish(ProductPickedEvent)
   → EventBus.Publish(ShelfStockChangedEvent)
   → PlayerController.Carried += product
```

### Выполнение заказа клиента (клиент → экономика → прогресс → UI)
```
PlayerController.TryDeliver(order)
   → ICustomerService.TryCompleteOrder(order, product)
      → EventBus.Publish(CustomerOrderCompletedEvent)
      → EventBus.Publish(ProductDeliveredEvent)
   → EconomyService.OnOrderCompleted: AddCurrency(Coins, reward)
      → EventBus.Publish(CurrencyChangedEvent)
   → LevelManager.OnOrderCompleted: CompletedOrders += 1
      → при цели → EventBus.Publish(LevelCompletedEvent)
   → UIService транслирует события в IHUDView (баланс/заказ/прогресс)
```

### Смена уровня
```
ILevelManager.StartLevel(index)
   → IStockService.RegisterShelf(shelf) для каждой полки
   → EventBus.Publish(LevelStartedEvent)
   → таймер тикает; попытка заказа → ICustomerService.CreateOrder(...)
```

### Пауза со стороны платформы
```
PlatformService (потеря фокуса) → EventBus.Publish(GamePauseRequestedEvent(true))
   → LevelManager.SetPaused(true) → EventBus.Publish(LevelPauseChangedEvent(true)) → UI
```

---

## 9. Где будут MonoBehaviour (не созданы осознанно)

В базе только один MonoBehaviour — `GameBootstrap`. При реализации сцены появятся
view-компоненты (без изменения архитектуры):

- **View-компоненты** на объектах сцены: `PlayerView` (движение/анимации), `ShelfView`,
  `ProductView`, `CustomerView` — реализуют интерфейсы/контракты, а не содержат логику.
- **UI**: компонент-реализация `IHUDView` (Canvas, TMPro), виртуальный джойстик для тача.
- Будущие реализации уже объявленных интерфейсов (Yandex и т.п.).

> **Уже сделано:** вью-слой игрока как MonoBehaviour на Player prefab
> (`ShelfRush.Player.View.*` — `PlayerController`, `PlayerMovement`, `PlayerInteraction`,
> `PlayerCarry`, `PlayerAnimator`). Детали: `Documentation/PREFAB_PLAYER.md`. Это
> именно view-компоненты: они не содержат игровую сервисную логику, а используют
> существующие сервисы (`IPlayerInput`, `PlayerConfig`) через `ServiceBridge`.

---

## 10. Анти-паттерны (запрещено)

- `FindObjectOfType` / `GetComponent` в `Update` и «God Object».
- Строковая идентификация категорий/товаров в игровой логике (ид — по ссылке на объект;
  строковый `Id` — только для сохранений).
- Hardcoded логика товаров/полок/уровней (всё — в `ScriptableObject`).
- Прямые обращения игровой логики к сторонним SDK (YG2, DOTween и т.п.) — через обёртки.
- Статическое глобальное состояние без необходимости.

---

## 11. Статус и следующие шаги

Реализовано (компилируется, 0 ошибок):
- `Core` (locator, event bus, сервисы, state machine, bootstrap),
- `Input` (единый ввод + провайдеры),
- `Player`, `Products`, `Shelves`, `Customers`, `Economy`, `Levels`,
- `Save` (PlayerPrefs), `Platform` (база), `UI` (трансляция), `Pooling` (LeanPool).
- **Player prefab вью** (`Player/View/*`): MonoBehaviour `PlayerController`, `PlayerMovement`,
  `PlayerInteraction`, `PlayerCarry`, `PlayerAnimator`, `IInteractable` (см.
  `Documentation/PREFAB_PLAYER.md`); расширен `PlayerConfig`.

Запланировано (без изменения архитектуры):
- asmdef-изоляция игрового кода (по рекомендациям аудита);
- Yandex-реализации `IPlatformService`/`ISaveService` через YG2;
- компоненты-вью и UI (реализации `IHUDView`), конфиги-ассеты, сцена;
- DOTween-анимации и настройка `DOTweenSettings`.