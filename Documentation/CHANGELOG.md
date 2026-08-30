# CHANGELOG — Shelf Rush

Все заметные изменения проекта. Формат основан на [Keep a Changelog](https://keepachangelog.com/ru/1.0.0/);
версии следуют [SemVer](https://semver.org/lang/ru/).

## [Unreleased]

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