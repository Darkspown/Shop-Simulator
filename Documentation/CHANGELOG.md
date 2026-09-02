# CHANGELOG — Shelf Rush

Все заметные изменения проекта. Формат основан на [Keep a Changelog](https://keepachangelog.com/ru/1.0.0/);
версии следуют [SemVer](https://semver.org/lang/ru/).

## [Unreleased]

### Added — подбор и переноска (Задача 06)
- **`Player/View/PlayerCarry.cs`** — переработан: запись `(ProductData + runtime Product)`,
  capacity-проверки (`CanAdd`/`CanRemove`/`IsFull`/`IsEmpty`), pickup (`TryAdd`),
  remove (`TryRemove`), drop (`TryDrop`), `Clear()`, несколько товаров + visual stack
  по `stackOffset`; DOTween — только визуальное перемещение/укладка.
- **Авто-подбор**: `PlayerConfig.autoPickup` (default true) + `IInteractable.AutoInteractOnApproach` —
  товар берётся самим при приближении к коробке/полке, **без нажатия кнопок/тапов**
  (`PlayerInteraction.TryAutoInteract`); доставка остаётся по кнопке (`AutoInteractOnApproach = false`).
- **Авто-подбор коробок (`Product`)**: `PlayerInteraction` сканирует runtime-коробки на сцене
  и берёт их при подходе напрямую (`player.Carry.TryAdd(product.Data)` + коробка в пул через
  `ResetState → Despawn`); коробки в руках (дети `carryAnchor`) исключаются.
- **`AutoBoxSpawner`** (`Products/AutoBoxSpawner.cs`) — готовый авто-спавнер коробок
  (MonoBehaviour): на `OnEnable` спавнит коробки через `ProductSpawner`/LeanPool
  (конфиг в инспекторе: `data`, `spawnOffsets`, флаги respawn/despawn), безопасно
  возвращает свои (не подбранные) коробки в пул.
- **`Product.initialData`** — поле для ручных коробок на сцене: в `Awake` компонент сам
  вызывает `Setup(initialData)`, связывая `Data` (без `ProductSpawner`). Это позволяет
  авто-подобрать префаб коробки, размещённый вручную на сцене (Collider + `Product`
  + назначенный `Initial Data`).
- **Визуал переносимых товаров** спавнится/возвращается через LeanPool (`IPoolService`)
  из `ProductData.BoxPrefab`, а не `Instantiate`/`Destroy`.
- **Despawn-контракт** (`ReleaseVisual`): kill tweens → reset state → clear product data →
  reset transform → unsubscribe; `PlayerCarry` подписан на `LevelStartedEvent` (очистка рук
  на новом уровне) с корректной отпиской в `OnDisable`.
- **`LevelConfig`** — добавлено поле `carryCapacity` (прогрессия переноски живёт в данных
  уровня, НЕ внутри `PlayerCarry`). Рекомендация: L1=1, L2=2, L3=3, L4=5, L5=7.
- **`PlayerController`** (plain C# сервис) — `CarryCapacity` читает текущий уровень
  (`ILevelManager.Current.CarryCapacity`) с фолбэком на `PlayerConfig` — единый источник.
- Документация: `PLAYER.md` (§6.1 Pickup & Carry, прогрессия), `PRODUCTS.md` (§8 переноска
  через пул), `PREFAB_PLAYER.md` (актуальные поля `PlayerCarry`), новый `CARRY_SETUP.md`
  (пошаговая настройка — prefab Player, коробка в руках, интерактивы, прогрессия, чек-лист).

### Added — data-driven Product System (Задача 05)
- **`ProductCategory`** (`Products/ProductCategory.cs`) — type-safe категория товара как
  `ScriptableObject` (ссылка, не строка); `displayName` + `color` для UI.
- **`ProductData`** (`Products/ProductData.cs`) — расширен до полного набора данных:
  `category`, `boxPrefab`, `rewardValue`, `visualSettings` (вложенный `VisualSettings`);
  убран неиспользуемый `basePrice`.
- **`Product`** (`Products/Product.cs`) — runtime-экземпляр: ссылка на `ProductData`,
  `ResetState()` перед возвратом в пул.
- **`ProductVisual`** (`Products/ProductVisual.cs`) — вью визуала: масштаб/тон через
  `MaterialPropertyBlock`; подсветка `SetTint`/`ResetTint`.
- **`ProductSpawner`** (`Products/ProductSpawner.cs`) — спавн/деспавн через `IPoolService`
  (LeanPool) с fallback на `Instantiate`/`Destroy`; `Spawn`, `SpawnBox`, `Despawn`.
- **`IProductCatalog`/`ProductCatalog`** — метод `GetByCategory(ProductCategory)`
  (фильтрация товаров по категории).
- Документация: `Documentation/PRODUCTS.md` (создание категории, префаба, товара, reward).

### Added — единая кросс-платформенная система ввода
- **`IPlayerInput`** (`Input/IPlayerInput.cs`) — абстракция «gameplay-facing» ввода:
  нормализованный `Move` (Vector2), `MoveWorld` (Vector3 XZ), событие `Interact`,
  `Enable`/`Disable`. Геймплей зависит только от неё и не знает, откуда пришёл input.
- **`PlayerInput`** (`Input/PlayerInput.cs`) — реализация: оборачивает `IInputService`,
  опционально подмешивает ось виртуального джойстика, применяет
  `dead zone → sensitivity → input smoothing` (экспоненциальный, frame-rate independent).
- **`PlayerInputConfig`** (`Input/PlayerInputConfig.cs`) — ScriptableObject с
  настройками `deadZone` / `sensitivity` / `smoothing` (с дефолтами на все платформы).
- **`IVirtualJoystick`** (`Input/IVirtualJoystick.cs`) — контракт мобильного джойстика.
- **`VirtualJoystick`** (`UI/Joystick/VirtualJoystick.cs`) — UGUI prefab-компонент
  (аналоговый, с dead zone). **Не добавляется в сцену** — создаётся как prefab и
  подключается вручную (см. `Documentation/CROSS_PLATFORM.md`, §6).
- **`PlayerMovement`** (`Player/PlayerMovement.cs`) — движение игрока по нормализованному
  вектору (XZ-плоскость). **Не читает никакой input** — получает готовый Vector2/Vector3.

### Changed
- **`PlayerController`**: переведён с `IInputService` на `IPlayerInput`; движение
  делегировано в `PlayerMovement` (`Player/PlayerController.cs`).
- **`GameBootstrap`**: создаёт и регистрирует `IPlayerInput`, опциональный
  `PlayerInputConfig`, опциональный `mobileJoystick` (ручное подключение через поле
  инспектора). Порядок тиков: `InputService → PlayerInput → … → PlayerController`.

### Проверки
- PC / Editor: WASD + стрелки, E/Enter — работает через единый конвейер.
- Mobile-архитектура: свайп-джойстик `TouchInputProvider` + опциональный виртуальный
  джойстик; общий gameplay-код без `#if UNITY_*`.
- WebGL: общий managed-код без нативных плагинов; совместим с IL2CPP/stripping.
- Yandex: ввод не зависит от YG2 (интеграция платформы — в `IPlatformService`).

### Документация
- Добавлены `Documentation/CROSS_PLATFORM.md`, `Documentation/PLAYER.md`, `CHANGELOG.md`.

---

## Player Controller на prefab (MonoBehaviour вью)

### Added — вью-слой игрока (Assets/Scripts/Player/View/)
- **`PlayerController`** (`View/PlayerController.cs`) — MonoBehaviour-оркестратор на корне
  Player prefab. НЕ содержит всю логику: получает ввод через `IPlayerInput` (`MoveWorld` +
  событие `Interact`), делегирует движение/взаимодействие/анимации в под-компоненты.
- **`PlayerMovement`** (`View/PlayerMovement.cs`) — движение: скорость, ускорение,
  замедление, поворот модели к направлению, остановка. Чистая математика, без DOTween,
  без чтения input.
- **`PlayerInteraction`** (`View/PlayerInteraction.cs`) — поиск ближайшего `IInteractable`
  в радиусе (`Physics.OverlapSphere`), состояние взаимодействия (`InteractionState`),
  обработка `IPlayerInput.Interact`.
- **`PlayerCarry`** (`View/PlayerCarry.cs`) — инвентарь: count/capacity/add/remove/clear +
  DOTween-визуализация стекировки товаров в `carryAnchor`.
- **`PlayerAnimator`** (`View/PlayerAnimator.cs`) — состояния idle/walk/carry/interact
  (bool-параметры Animator + DOTween-эффекты пульса/покачивания).
- **`PlayerCamera`** (`View/PlayerCamera.cs`) — следящая камера: плавно ведёт `Main Camera`
  (или назначенную) за игроком по XZ в `LateUpdate` (offsets/followSmooth/lockY в инспекторе,
  без DOTween).
- **`IInteractable` / `InteractableComponent`** (`View/IInteractable.cs`) — контракт
  интерактивных объектов сцены (полки/клиенты).
- **`ServiceBridge`** (`View/ServiceBridge.cs`) — доступ prefab-вью к сервисам через
  `GameBootstrap.Instance.Services`.

### Added — PlayerConfig расширен
- `PlayerConfig` (`Player/PlayerConfig.cs`) теперь содержит: `moveSpeed`, `acceleration`,
  `deceleration`, `rotationSpeed`, `interactionRadius`, `pickupDuration`,
  `placementDuration`, `carryCapacity` (+ legacy `pickupRadius`). Ничего не hardcoded.

### Changed
- **`GameBootstrap`** — добавлены публичные `Instance` и `Services` для вью-слоя
  (plain C# `PlayerController` сервис не тронут, сцена не изменена).

### DOTween
- Используется ТОЛЬКО для визуальных эффектов (`PlayerAnimator`, `PlayerCarry`),
  а НЕ как контроллер движения/физики (`PlayerMovement` — чистая математика).

### VirtualJoystick → floating/dynamic
- `VirtualJoystick` (`UI/Joystick/VirtualJoystick.cs`) переработан в «floating» джойстик:
  перехватывает касание по всему экрану, видимый круг с рукояткой появляется в точке
  нажатия и исчезает при отпускании. Корневой `RectTransform` при `Awake` растягивается
  на весь экран как прозрачная зона перехвата; фон/рукоятка создаются динамически
  (или переиспользуется назначенный `Handle`). Достаточно простого prefab — зависит
  только от `EventSystem`.
- Обновлена документация `Documentation/CROSS_PLATFORM.md` (§6.2).

### Документация
- Добавлен `Documentation/PREFAB_PLAYER.md` — компоненты prefab, ссылки, параметры
  конфига, проверка PC/Mobile.