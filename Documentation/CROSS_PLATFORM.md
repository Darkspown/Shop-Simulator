# CROSS_PLATFORM — Shelf Rush: единая система ввода

> Дата: 28.08.2026 | Автор: Cline.
> Раздел описывает единый кросс-платформенный ввод для **PC / Mobile / WebGL / Yandex Games**
> и то, как подключается виртуальный джойстик для мобильных UI.

---

## 1. Принцип

**Gameplay не знает, откуда пришёл input.** Вся игровая логика (PlayerController,
PlayerMovement) работает только с **нормализованным вектором движения [−1..1]** через
абстракцию `IPlayerInput`. Подменой источника ввода (клавиатура/мышь, тач, свайп,
джойстик, геймпад, Яндекс) занимается внутренний, «нижележащий» слой — он **не виден**
геймплею и меняется под платформу без правок игрового кода.

## 2. Архитектура (поток данных)

```
                 ┌───────────────────────────────────────────────────┐
   УСТРОЙСТВА     │  Input Provider (ShelfRush.Input)                  │
                 │  KeyboardMouseInputProvider  → Keyboard.current     │
                 │  TouchInputProvider          → Touchscreen.current   │
                 │  GamepadInputProvider        → Gamepad.current       │
                 │  VirtualJoystick (UGUI prefab, mobile)              │
                 └──────────────────────────────┬──────────────────────┘
                                                │  Move (нормализованный Vector2),
                                                │  событие Interact
                                                ▼
                 ┌───────────────────────────────────────────────────┐
                 │  IPlayerInput  (PlayerInput)                        │
                 │  dead zone → sensitivity → input smoothing          │
                 │  Merge: джойстик активен? -> ось джойстика,          │
                 │         иначе   -> оси провайдеров                   │
                 └──────────────────────────────┬──────────────────────┘
                                                │  Move / MoveWorld (Vector2/Vector3, [−1..1])
                                                ▼
                 ┌───────────────────────────────────────────────────┐
                 │  PlayerController → PlayerMovement                   │
                 │  PlayerMovement НЕ читает input: получает готовый     │
                 │  нормализованный вектор и двигает Transform по XZ     │
                 └───────────────────────────────────────────────────┘
```

Логический слой игры (`PlayerController`, `PlayerMovement`) зависит **только** от
`IPlayerInput`. Классы `PlayerController`/`PlayerMovement` не содержат ни одного
обращения к `Input.GetAxis`, `Keyboard`, `Touchscreen`, `Joystick` или `Gamepad`.

## 3. Слои и их файлы

| Слой | Типы | Файлы |
|---|---|---|
| Устройства | `IInputProvider`, `IInputProviderBase`, конкретные провайдеры | `Input/IInputProvider.cs`, `Input/InputProviderBase.cs`, `Input/InputService.cs`, `Input/KeyboardMouseInputProvider.cs`, `Input/TouchInputProvider.cs`, `Input/GamepadInputProvider.cs` |
| Мобильный джойстик | `IVirtualJoystick`, `VirtualJoystick` | `Input/IVirtualJoystick.cs`, `UI/Joystick/VirtualJoystick.cs` |
| Gameplay-facing input | `IPlayerInput`, `PlayerInput`, `PlayerInputConfig` | `Input/IPlayerInput.cs`, `Input/PlayerInput.cs`, `Input/PlayerInputConfig.cs` |
| Gameplay | `PlayerController`, `PlayerMovement`, `PlayerConfig` | `Player/PlayerController.cs`, `Player/PlayerMovement.cs`, `Player/PlayerConfig.cs` |

## 4. Per-platform источники ввода

| Платформа | Механики | Провайдер / компонент |
|---|---|---|
| **PC (Standalone)** | WASD + стрелки (движение), E/Enter (взаимодействие), мышь | `KeyboardMouseInputProvider` |
| **Mobile (Android/iOS)** | Свайп-джойстик по экрану, лёгкий тап = взаимодействие; **или** виртуальный джойстик (UGUI prefab) | `TouchInputProvider` + опционально `VirtualJoystick` |
| **WebGL** | Клавиатура + мышь на десктоп-браузере; тач на мобильном браузере; при желании геймпад | `KeyboardMouseInputProvider`, `TouchInputProvider`, `GamepadInputProvider` |
| **Yandex Games** | WebGL-билд: клавиатура/мышь/тач (зависит от устройства игрока). Никакого отдельного кода ввода не нужно | те же провайдеры; интеграция YG2 только в `IPlatformService` |

**Выбор активного провайдера** (`InputService.PickProvider`):
1. Мобильная платформа и есть тачскрин → `TouchInputProvider`;
2. Есть геймпад → `GamepadInputProvider`;
3. Иначе → `KeyboardMouseInputProvider`.

Активный провайдер пересчитывается на лету при подключении/отключении устройств
(`InputSystem.onDeviceChange`).
## 5. Единая обработка ввода (`PlayerInput`)

Независимо от устройства, `PlayerInput` приводит ввод к одному виду каждый кадр
(`Tick`):

```
raw (от провайдера/джойстика)
   → dead zone: |v| <= deadZone → 0; иначе remap [deadZone..1] → [0..1]
   → sensitivity: умножить, зажать в [0..1]
   → input smoothing: экспоненциальный фильтр, frame-rate independent:
        alpha = 1 - exp(-smoothing * dt)
        smoothed = Lerp(smoothed, target, alpha)
   → Move (Vector2), MoveWorld (Vector3 XZ)
```

Настройки в `PlayerInputConfig` (ScriptableObject):

| Параметр | По умолчанию | Описание |
|---|---|---|
| `deadZone` | `0.15` | Мёртвая зона (доля от максимального отклонения) |
| `sensitivity` | `1.0` | Чувствительность (множитель) |
| `smoothing` | `12` | Частота сглаживания, с⁻¹ (`0` = без сглаживания) |

Если на сцене нет назначенного `PlayerInputConfig` — используются дефолтные значения
(зашиты в коде), так что система работает «из коробки».

## 6. Mobile: виртуальный джойстик (prefab, ручное подключение)

**Сцена не изменяется.** Джойстик — это prefab, который вы создаёте один раз и
подключаете вручную. Без него мобильное управление работает через свайп
(`TouchInputProvider`).

### 6.1 Автоматический `PlayerInputConfig` (создать ассет)
`Assets > Create > ShelfRush > Input > PlayerInputConfig` и при желании настроить
`dead zone / sensitivity / smoothing`.

### 6.2 Создание prefab джойстика
1. Создайте `Canvas` (при необходимости) и удостоверьтесь, что на сцене есть
   `EventSystem` (объект с компонентами `EventSystem` + `StandaloneInputModule`).
2. Под Canvas создайте пустой GameObject **`VirtualJoystick`** с компонентом
   `RectTransform`.
3. Добавьте на него компонент **`ShelfRush.UI > VirtualJoystick`**.
4. Добавьте **`Image`** (фон) на этот же объект — настройте спрайт/размер (например, 200×200).
5. Создайте child-объект **`Handle`** с `RectTransform` + `Image` (рукоятка, например 100×100).
6. Перетащите `Handle` в поле **`Handle`** компонента `VirtualJoystick`.
7. Настройте `dragRadius` (радиус хода рукоятки) и `deadZone`.
8. Сохраните как prefab: перетащите объект из Hierarchy в `Assets/Prefabs/UI/`.

> Джойстик должен быть верхним, последним захватываемым элементом в зоне своего
> размера (Raycast Target у него включён), иначе перехват драга будет спорить с другими
> UI-элементами. Располагайте его в углу экрана (нижний левый/правый).

### 6.3 Подключение джойстика к игре
Есть несколько равноценных способов; рекомендуемый — через **GameBootstrap**.

**Способ A (рекомендуется): поля в GameBootstrap.**
- Перетащите экземпляр prefab джойстика на сцену (в ваш Canvas).
- В инспекторе `GameBootstrap` перетащите этот экземпляр в поле
  **`Mobile Joystick`**. Бутстрап вызовет `playerInput.AttachJoystick(...)`.

**Способ B (программно, из runtime).**
- Получите сервис и подключите джойстик:
  ```csharp
  var playerInput = serviceLocator.Get<IPlayerInput>() as PlayerInput;
  playerInput?.AttachJoystick(myJoystick); // myJoystick : IVirtualJoystick
  ```
  Удобно в `Awake` самого prefab-компонента (если есть доступ к locator).

**Способ C (кнопка взаимодействия).**
- На Canvas добавьте кнопку; в её `OnClick()` вызывайте `joystick.InvokeInteract()` —
  событие пройдёт в `IPlayerInput.Interact`.

> Пока пользователь зажал джойстик (`IsActive == true`), `PlayerInput` берёт ось из
> джойстика; как только отпустил — снова переключается на свайп/клавиатуру. Это
> позволяет миксовать методы без конфликтов.

## 7. Input Actions / Input System (как подключаются)

Проект использует **только New Input System** (`ProjectSettings.asset` →
`activeInputHandler: 1`; legacy `Input.*` отключён). Поэтому **второй input-фреймворк не
создаётся**.

- В корне `Assets` лежит стандартный ассет **`InputSystem_Actions.inputactions`**
  (мапы `Player`/`UI`, группы `Keyboard&Mouse`, `Gamepad`, `Touch`, `Joystick`).
- Реализованные провайдеры читают устройства **без** сгенерированного C#-класса —
  напрямую через `Keyboard.current`, `Touchscreen.current`, `Gamepad.current`
  (это режим «по-устройству» New Input System, совместим со всеми платформами).
- **Если** понадобится работать через экшены из `.inputactions`:
  1. Выберите `InputSystem_Actions.inputactions` в Project.
  2. В инспекторе включите **`Generate C# Class`** (класс `InputSystem_Actions`) и
     нажмите **Apply**.
  3. Используйте экшены: `new InputSystem_Actions().Player.Move.ReadValue<Vector2>()`,
     подписываясь на `performed`/`canceled`.
  4. Провайдеры остаются тонкой прослойкой, возвращающей нормализованный `Move` в
     `IInputService`. Геймплей по-прежнему не меняется.

## 8. Проверки по платформам

### PC / Editor
- WASD и стрелки двигают игрока; E/Enter — взаимодействие.
- Аппаратная задержка отсутствует; smoothing настраивается в `PlayerInputConfig`.
- `GameBootstrap` чисто компилируется в `Assembly-CSharp`.

### Mobile (Android/iOS) — архитектура
- Ввод через `TouchInputProvider` (свайп) без каких-либо MonoBehaviour в базе.
- Опциональный `VirtualJoystick` — view-компонент prefab; **не добавляется в сцену
  автоматически**, подключается вручную (см. §6).
- Весь gameplay-код общий: нет `#if UNITY_ANDROID`/`UNITY_IOS` веток для движения.

### WebGL
- Ввод (клавиатура/мышь/тач) — тот же общий код, никаких нативных плагинов.
- Совместимо с IL2CPP и агрессивной managed stripping (используется только managed API).
- Для Yandex: сервис ввода не зависит от YG2; интеграция платформы живёт в
  `IPlatformService` (пауза/реклама/сейвы), а не в вводе.

## 9. Анти-паттерны (запрещено)

- Вызывать `Input.GetAxis`/`Input.GetKey` в gameplay (legacy отключён).
- Читать `Keyboard.current`/`Touchscreen.current`/`Joystick` в `PlayerMovement`.
- Добавлять `using UnityEngine.InputSystem` в `PlayerController`/`PlayerMovement`.
- Создавать второй input-фреймворк параллельно с New Input System.
- Прятать разброс логики движения под `#if UNITY_*`.