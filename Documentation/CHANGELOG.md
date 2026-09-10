# CHANGELOG  Shelf Rush

Все заметные изменения проекта. Формат основан на [Keep a Changelog](https://keepachangelog.com/ru/1.0.0/);
версии следуют [SemVer](https://semver.org/lang/ru/).

## [Unreleased]

### Added  Продукт-плейсмент валидаторЗадача 08: Category Validation

- **`Shelves/IProductPlaceholderValidator.cs`** (новый)  контракт отдельной проверки размещения продукта на полку:
 `CanPlace(product, shelf)` и `CanPlace(product, shelf, currentAmount)`.
- **`Shelves/ProductPlaceholderValidator.cs`** новый  стандартная реализация: порядок `InvalidProduct``InvalidShelf``CategoryMismatch``ShelfFull``Allowed`
  расширяемость через `protected virtual CanBypassCategory`/`EffectiveCapacity`VIP-полки, спец-товары,
  временные полки, квесты, бонусы
- **`Shelves/ProductPlacementResult.cs`** новый  структурированный результат `Allowed` + `Reason` +
  явные флаги`IsInvalidProduct`,`IsInvalidShelf`,`IsCategoryMismatch`,`IsShelfFull`+ `Message`
 
 **`Shelves/ShelfController.cs`** расширен  `ValidatePlacement` теперь делегирует валидатору задача 08,
 добавлен `CanPlaceResult(product)` доступ к structured-результату fallback на локальный экземпляр
 `ProductPlaceholderValidator`если Bootstrap недоступен) Enum `ShelfPlacementFailReason` дополнен
 `None`/`InvalidProduct`/`InvalidShelf`
 **`Core/GameBootstrap.cs`** расширен  регистрирует `IProductPlacementValidator`сервис
- Документация: `SHELVES.md` (9).
### Fixed  Размещённый товар не был виден на полке
- **`Shelves/ShelfController.cs`** (`SpawnPlacedVisual`)  раньше при перелёте товар спавнился с
  `localScale = 0` и ждал, пока DOTween-твин поднимет его до `Vector3.one`. Если твин не отрабатывал,
  товар оставался с масштабом 0  размещался логически, но был невидим. Теперь товар спавнится сразу
  с правильным масштабом из `ProductData.VisualSettings.Scale` и на позиции слота; перелёт  только
  по позиции (от игрока к слоту, `OutCubic`) + лёгкий bounce масштаба. Товар виден во время всего полёта.
  Документация: `SHELVES_SETUP.md` (8 troubleshooting).

### Fixed  Зона не выставляла товары на полку (по близости)
- **`Shelves/ShelfInteractionZone.cs`** (переработан)  раньше полагался на `OnTriggerEnter`, но
  игрок двигается **transform'ом без Rigidbody**, поэтому триггер физикой не вызывался. Теперь зона
  **сама периодически проверяет** (`Scan Interval`, по умолчанию 0.1 с), находится ли позиция игрока
  внутри её `BoxCollider`, и при первом входе с товарами вызывает `PlaceAll`  `TryPlaceFlying`.
  Rigidbody на игроке больше не требуется. Повторная выкладка  после выхода/входа или при наполнении
  рук внутри зоны. Документация: `SHELVES_SETUP.md` (9, troubleshooting).

### Added  Ручная настройка слотов полки
- **`Shelves/ShelfController.cs`** (расширен)  поле `Use Manual Slots` + `BuildManualSlots()`:
  если включено, использует вручную размещённые в сцене/префабе дочерние объекты с компонентом
  `ShelfSlot` (позиции из сцены, порядок  по иерархии SiblingIndex), вместо автогенерации из
  `ShelfData.Slots`/Auto. Добавлены `SlotsRoot`/`UseManualSlots`.
- **`Shelves/ShelfSlot.cs`** (расширен)  сцена-гизмо точки слота (зелёный свободен / красный занят),
  чтобы расставлять слоты вручную в редакторе.
- **`Editor/ShelfControllerEditor.cs`** (новый)  инспектор-кнопки: + Добавить слот (child)
  (создаёт дочерний ShelfSlot) и Синхрон. порядок / имена слотов (`ShelfSlot_0..N` по иерархии).
- Документация: `SHELVES_SETUP.md` (4.1 Ручная настройка слотов).

### Added  Interaction Zone + fly-to-shelf (наступил  товар перелетел)
- **`Shelves/ShelfInteractionZone.cs`** (новый)  зона из объекта **Plane** перед полкой: игрок
  заходит в неё и товары из рук автоматически размещаются. Использует `MeshCollider` плоскости как
  Trigger (IsTrigger), размер настраивается масштабом Plane. **Без подсветки.** Поля:
  `Controller`, `Auto Configure Collider`, `Auto Place On Enter`, `Fly Delay Step`.
- **`Shelves/ShelfController.cs`** (расширен)  `TryPlaceFlying(product, flyFromWorld, flyDelay)`:
  товар рисуется у игрока и DOTween-ом (перелёт + `OutBack`-рост) садится в свободный слот;
  рядом `PlayFlyVisualFeedback` (зелёная подсветка товара без punch полки).
- Игровое решение принимает `ShelfController` по данным (`Category == AllowedCategory`, есть слот);
  зона лишь вызывает его и снимает размещённое с `PlayerCarry.TryDrop` (остаток остаётся у игрока).
- Коллайдеры переносимых коробок (дети `CarryAnchor`) зона игнорирует (без повторного срабатывания).
- Документация: `SHELVES_SETUP.md` (9 Interaction Zone), `CHANGELOG.md`.

### Added  Shelf System (Задача 07)
- **`Shelves/ShelfData.cs`**  расширен для Placement: `AllowedCategory` (type-safe),
  `Slots` (`Vector3[]` локальных позиций), `InteractionRadius`, `SlotCapacity`
  (= `min(Capacity, Slots.Length)`); legacy поля `Product`/`Capacity` (Stock) сохранены.
- **`Shelves/ShelfController.cs`**  ядро: `CanPlace`/`TryPlace`/`TryPlaceRange`,
  `CurrentAmount`, главная проверка `ProductData.Category == AllowedCategory`,
  события `OnProductPlaced`/`OnPlacementFailed`/`OnCategoryMismatch`/`OnShelfFull`/
  `OnShelfCompleted`, feedback (highlight+bounce / негативный тряс),
  создание runtime-слотов из `ShelfData.Slots`, спавн визуала через `ProductSpawner`/LeanPool.
- **`Shelves/ShelfSlot.cs`**  слот размещения (индекс, смещение, `Contents`, `Visual`,
  `IsOccupied`/`IsAvailable`); создаётся автоматически, сцена не изменяется.
- **`Shelves/ShelfInteraction.cs`**  интерактив полки (`IInteractable`): размещает товары
  из рук игрока, снимая только фактически размещённые; остаток (неверный/не поместился) у игрока.
- **Неверный товар**: не удаляется/не уничтожается/не размещается/не приносит reward/не
  увеличивает прогресс; остаётся у игрока.
- **События** (`Core/GameEvents.cs`): `ShelfProductPlacedEvent`, `ShelfPlacementFailedEvent`,
  `ShelfCompletedEvent`; enum `ShelfPlacementFailReason` (NoData/CategoryMismatch/ShelfFull).
- **Reward**: `EconomyService` на `ShelfProductPlacedEvent` начисляет монеты (`ProductData.RewardValue`).
- **LevelProgress**: `ILevelManager` расширен (`PlacedProducts`, `CompletedShelves`,
  `PlacementProgress`, `PlacementProgressChanged`); `LevelConfig`  `TotalShelfCapacity`/`TotalShelfCount`.
- Документация: новый `SHELVES.md` (архитектура) и `SHELVES_SETUP.md` (настройка полок);
  `ARCHITECTURE.md` 3.5 обновлён.

### Added  подбор и переноска (Задача 06)
- **`Player/View/PlayerCarry.cs`**  переработан: запись `(ProductData + runtime Product)`,
  capacity-проверки (`CanAdd`/`CanRemove`/`IsFull`/`IsEmpty`), pickup (`TryAdd`),
  remove (`TryRemove`), drop (`TryDrop`), `Clear()`, несколько товаров + visual stack
  по `stackOffset`; DOTween  только визуальное перемещение/укладка.
- **Авто-подбор**: `PlayerConfig.autoPickup` (default true) + `IInteractable.AutoInteractOnApproach` 
  товар берётся самим при приближении к коробке/полке, **без нажатия кнопок/тапов**
  (`PlayerInteraction.TryAutoInteract`); доставка остаётся по кнопке (`AutoInteractOnApproach = false`).
- **Авто-подбор коробок (`Product`)**: `PlayerInteraction` сканирует runtime-коробки на сцене
  и берёт их при подходе напрямую (`player.Carry.TryAdd(product.Data)` + коробка в пул через
  `ResetState  Despawn`); коробки в руках (дети `carryAnchor`) исключаются.
- **`AutoBoxSpawner`** (`Products/AutoBoxSpawner.cs`)  готовый авто-спавнер коробок
  (MonoBehaviour): на `OnEnable` спавнит коробки через `ProductSpawner`/LeanPool
  (конфиг в инспекторе: `data`, `spawnOffsets`, флаги respawn/despawn), безопасно
  возвращает свои (не подбранные) коробки в пул.
- **`Product.initialData`**  поле для ручных коробок на сцене: в `Awake` компонент сам
  вызывает `Setup(initialData)`, связывая `Data` (без `ProductSpawner`). Это позволяет
  авто-подобрать префаб коробки, размещённый вручную на сцене (Collider + `Product`
  + назначенный `Initial Data`).
- **Визуал переносимых товаров** спавнится/возвращается через LeanPool (`IPoolService`)
  из `ProductData.BoxPrefab`, а не `Instantiate`/`Destroy`.
- **Despawn-контракт** (`ReleaseVisual`): kill tweens  reset state  clear product data 
  reset transform  unsubscribe; `PlayerCarry` подписан на `LevelStartedEvent` (очистка рук
  на новом уровне) с корректной отпиской в `OnDisable`.
- **`LevelConfig`**  добавлено поле `carryCapacity` (прогрессия переноски живёт в данных
  уровня, НЕ внутри `PlayerCarry`). Рекомендация: L1=1, L2=2, L3=3, L4=5, L5=7.
- **`PlayerController`** (plain C# сервис)  `CarryCapacity` читает текущий уровень
  (`ILevelManager.Current.CarryCapacity`) с фолбэком на `PlayerConfig`  единый источник.
- Документация: `PLAYER.md` (6.1 Pickup & Carry, прогрессия), `PRODUCTS.md` (8 переноска
  через пул), `PREFAB_PLAYER.md` (актуальные поля `PlayerCarry`), новый `CARRY_SETUP.md`
  (пошаговая настройка  prefab Player, коробка в руках, интерактивы, прогрессия, чек-лист).

### Added  data-driven Product System (Задача 05)
- **`ProductCategory`** (`Products/ProductCategory.cs`)  type-safe категория товара как
  `ScriptableObject` (ссылка, не строка); `displayName` + `color` для UI.
- **`ProductData`** (`Products/ProductData.cs`)  расширен до полного набора данных:
  `category`, `boxPrefab`, `rewardValue`, `visualSettings` (вложенный `VisualSettings`);
  убран неиспользуемый `basePrice`.
- **`Product`** (`Products/Product.cs`)  runtime-экземпляр: ссылка на `ProductData`,
  `ResetState()` перед возвратом в пул.
- **`ProductVisual`** (`Products/ProductVisual.cs`)  вью визуала: масштаб/тон через
  `MaterialPropertyBlock`; подсветка `SetTint`/`ResetTint`.
- **`ProductSpawner`** (`Products/ProductSpawner.cs`)  спавн/деспавн через `IPoolService`
  (LeanPool) с fallback на `Instantiate`/`Destroy`; `Spawn`, `SpawnBox`, `Despawn`.
- **`IProductCatalog`/`ProductCatalog`**  метод `GetByCategory(ProductCategory)`
  (фильтрация товаров по категории).
- Документация: `Documentation/PRODUCTS.md` (создание категории, префаба, товара, reward).

### Added  единая кросс-платформенная система ввода
- **`IPlayerInput`** (`Input/IPlayerInput.cs`)  абстракция gameplay-facing ввода:
  нормализованный `Move` (Vector2), `MoveWorld` (Vector3 XZ), событие `Interact`,
  `Enable`/`Disable`. Геймплей зависит только от неё и не знает, откуда пришёл input.
- **`PlayerInput`** (`Input/PlayerInput.cs`)  реализация: оборачивает `IInputService`,
  опционально подмешивает ось виртуального джойстика, применяет
  `dead zone  sensitivity  input smoothing` (экспоненциальный, frame-rate independent).
- **`PlayerInputConfig`** (`Input/PlayerInputConfig.cs`)  ScriptableObject с
  настройками `deadZone` / `sensitivity` / `smoothing` (с дефолтами на все платформы).
- **`IVirtualJoystick`** (`Input/IVirtualJoystick.cs`)  контракт мобильного джойстика.
- **`VirtualJoystick`** (`UI/Joystick/VirtualJoystick.cs`)  UGUI prefab-компонент
  (аналоговый, с dead zone). **Не добавляется в сцену**  создаётся как prefab и
  подключается вручную (см. `Documentation/CROSS_PLATFORM.md`, 6).
- **`PlayerMovement`** (`Player/PlayerMovement.cs`)  движение игрока по нормализованному
  вектору (XZ-плоскость). **Не читает никакой input**  получает готовый Vector2/Vector3.

### Changed
- **`PlayerController`**: переведён с `IInputService` на `IPlayerInput`; движение
  делегировано в `PlayerMovement` (`Player/PlayerController.cs`).
- **`GameBootstrap`**: создаёт и регистрирует `IPlayerInput`, опциональный
  `PlayerInputConfig`, опциональный `mobileJoystick` (ручное подключение через поле
  инспектора). Порядок тиков: `InputService  PlayerInput    PlayerController`.

### Проверки
- PC / Editor: WASD + стрелки, E/Enter  работает через единый конвейер.
- Mobile-архитектура: свайп-джойстик `TouchInputProvider` + опциональный виртуальный
  джойстик; общий gameplay-код без `#if UNITY_*`.
- WebGL: общий managed-код без нативных плагинов; совместим с IL2CPP/stripping.
- Yandex: ввод не зависит от YG2 (интеграция платформы  в `IPlatformService`).

### Документация
- Добавлены `Documentation/CROSS_PLATFORM.md`, `Documentation/PLAYER.md`, `CHANGELOG.md`.

---

## Player Controller на prefab (MonoBehaviour вью)

### Added  вью-слой игрока (Assets/Scripts/Player/View/)
- **`PlayerController`** (`View/PlayerController.cs`)  MonoBehaviour-оркестратор на корне
  Player prefab. НЕ содержит всю логику: получает ввод через `IPlayerInput` (`MoveWorld` +
  событие `Interact`), делегирует движение/взаимодействие/анимации в под-компоненты.
- **`PlayerMovement`** (`View/PlayerMovement.cs`)  движение: скорость, ускорение,
  замедление, поворот модели к направлению, остановка. Чистая математика, без DOTween,
  без чтения input.
- **`PlayerInteraction`** (`View/PlayerInteraction.cs`)  поиск ближайшего `IInteractable`
  в радиусе (`Physics.OverlapSphere`), состояние взаимодействия (`InteractionState`),
  обработка `IPlayerInput.Interact`.
- **`PlayerCarry`** (`View/PlayerCarry.cs`)  инвентарь: count/capacity/add/remove/clear +
  DOTween-визуализация стекировки товаров в `carryAnchor`.
- **`PlayerAnimator`** (`View/PlayerAnimator.cs`)  состояния idle/walk/carry/interact
  (bool-параметры Animator + DOTween-эффекты пульса/покачивания).
- **`PlayerCamera`** (`View/PlayerCamera.cs`)  следящая камера: плавно ведёт `Main Camera`
  (или назначенную) за игроком по XZ в `LateUpdate` (offsets/followSmooth/lockY в инспекторе,
  без DOTween).
- **`IInteractable` / `InteractableComponent`** (`View/IInteractable.cs`)  контракт
  интерактивных объектов сцены (полки/клиенты).
- **`ServiceBridge`** (`View/ServiceBridge.cs`)  доступ prefab-вью к сервисам через
  `GameBootstrap.Instance.Services`.

### Added  PlayerConfig расширен
- `PlayerConfig` (`Player/PlayerConfig.cs`) теперь содержит: `moveSpeed`, `acceleration`,
  `deceleration`, `rotationSpeed`, `interactionRadius`, `pickupDuration`,
  `placementDuration`, `carryCapacity` (+ legacy `pickupRadius`). Ничего не hardcoded.

### Changed
- **`GameBootstrap`**  добавлены публичные `Instance` и `Services` для вью-слоя
  (plain C# `PlayerController` сервис не тронут, сцена не изменена).

### DOTween
- Используется ТОЛЬКО для визуальных эффектов (`PlayerAnimator`, `PlayerCarry`),
  а НЕ как контроллер движения/физики (`PlayerMovement`  чистая математика).

### VirtualJoystick  floating/dynamic
- `VirtualJoystick` (`UI/Joystick/VirtualJoystick.cs`) переработан в floating джойстик:
  перехватывает касание по всему экрану, видимый круг с рукояткой появляется в точке
  нажатия и исчезает при отпускании. Корневой `RectTransform` при `Awake` растягивается
  на весь экран как прозрачная зона перехвата; фон/рукоятка создаются динамически
  (или переиспользуется назначенный `Handle`). Достаточно простого prefab  зависит
  только от `EventSystem`.
- Обновлена документация `Documentation/CROSS_PLATFORM.md` (6.2).

### Документация
- Добавлен `Documentation/PREFAB_PLAYER.md`  компоненты prefab, ссылки, параметры
  конфига, проверка PC/Mobile.