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