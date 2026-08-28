# ARCHITECTURE — Shelf Rush

> Текущее архитектурное описание проекта. Состояние на момент аудита — проект на
> начальном этапе (greenfield): игрового кода нет. Этот документ фиксирует **целевую**
> архитектуру и текущий базис, на который она должна ложиться.

---

## 1. Целевые принципы

- **SOLID**, разделение ответственности, отсутствие «God Object».
- **Data-driven design**: данные/настройки — в `ScriptableObject`, логика — в компонентах.
- **Интерфейсы + события** для связи между системами.
- **Dependency Injection / Service Locator** там, где оправдано (разд. 5).
- **Object pooling** через LeanPool для часто создаваемых объектов.
- **Единый gameplay-API** + разные источники ввода (PC/Mobile/WebGL/Yandex).
- Отсутствие `FindObjectOfType`/`GetComponent` в `Update`, строковой идентификации категорий,
  hardcoded логики товаров/полок.

---

## 2. Текущий базис (что уже есть в проекте)

| Слой | Готовый компонент | Использование |
|---|---|---|
| Графика/анимации | Built-in RP, PolyOne Free Stickman (+ Animator Controller) | Игрок |
| UI | uGUI + **TextMeshPro** (`TMPro`) в составе `com.unity.ugui 2.0.0` | Весь текст/UI |
| Анимации | **DOTween** (`Assets/Plugins/Demigiant`) | UI/движение/фидбек/награды |
| Пулинг | **LeanPool** (`Assets/Plugins/CW/LeanPool`, asmdef) | товары/боксы/клиенты/VFX |
| Платформа | **PluginYourGames (YG2)** + WebGL-шаблон Yandex | сейвы/реклама/платформа |
| Ввод | **New Input System** (`com.unity.inputsystem`) | единая схема ввода |

---

## 3. Предлагаемая структура каталогов `Assets/Scripts`

```
Assets/Scripts/
├── Core/                 # bootstrap, ServiceLocator/DI, EventBus, PlayerLoop
├── Input/                # единый IInputProvider + адаптеры (Keyboard/Mouse, Touch, Gamepad)
├── Player/               # PlayerController, PlayerMovement, PlayerAnimation, Interaction
├── Products/             # ProductData (SO), Product, ProductSpawner, ProductPool
├── Shelves/              # ShelfData (SO), Shelf, ShelfView, StockService
├── Customers/            # Customer, CustomerSpawner, CustomerFlow
├── Levels/               # LevelConfig (SO), LevelManager, LevelBootstrap
├── Economy/              # EconomyService, Wallet, RewardService
├── Save/                 # ISaveService, PlayerPrefsSave, YandexSave
├── UI/                   # UIManager, панели, HUD, VirtualJoystick
├── Yandex/               # YandexService-обёртка над YG2 (ads/saves/ready/lang)
└── Interfaces/           # общие интерфейсы и события
```

Каталог `Assets/Scripts` снабжается собственным **`asmdef`** для изоляции игрового кода.

---

## 4. Слои и зависимости

```
UI ───────────────► Core (locator/events)
Gameplay systems ─► Core
Gameplay systems ─► Interfaces
Player ───────────► Input (IInputProvider), Core
Products/Shelves ─► Products/Shelves (data), Core
Save ─────────────► Yandex (YG2) / PlayerPrefs
Economy ──────────► Core (events), Save
Yandex ───────────► PluginYourGames (YG2) — единственная точка работы с SDK
```

Зависимость «сверху вниз»: **UI/Сцены → игровые системы → Core**.
Плагины (DOTween/LeanPool/YG2/TMPro) потребляются через тонкие обёртки или напрямую
внутри своих слоёв без распространения на всю архитектуру.

---

## 5. Dependency Injection / Service Locator

- Ввести лёгкий **ServiceLocator** (без сторонних DI-библиотек) как контейнер сервисов
  (EconomyService, SaveService, YandexService и т.д.).
- Инициализация — через **Scene Bootstrap** (объект-ориджинатор), который регистрирует
  сервисы по порядку и только затем разрешает остальным системам выполняться.
- Сервисы не должны создаваться через `FindObjectOfType`; компоненты получают зависимости
  через конструктор/`Init(...)` от Bootstrap или через осознанный lookup в Locator.

---

## 6. Ввод (Input)

Единый API:

```
interface IInputProvider
{
    Vector2 Move { get; }        // нормализованный вектор
    event Action OnInteract;     // кнопка взаимодействия (E/Enter/первый тач/joystick)
}
```

- `KeyboardMouseInputProvider` — WASD / стрелки, E — взаимодействие, Mouse при необходимости.
- `TouchInputProvider` — виртуальный джойстик / тач, кнопка взаимодействия.
- `GamepadInputProvider` — опционально (WebGL).
- **Выбор** провайдера — на основе платформы/устройств, один gameplay-API для всех платформ.

---

## 7. Игровые данные (ScriptableObjects)

- `ProductData` — спрайт/модель, цена, имя, id (перечисление/enum или SO-ссылка — **не строки**).
- `ShelfData` — товары, вместимость, позиции размещения.
- `LevelConfig` — состав полок/клиентов/порядок уровней.
- `EconomyConfig` — стартовый баланс, цены, награды.
- `PoolConfig`/ссылки на prefab'ы пулов (LeanPool).

---

## 8. Взаимодействие с платформой (Yandex)

Единственная обёртка `YandexService` (поверх `YG2`):
- сейвы cloud/local (`YG2` save API),
- реклама inter/rewarded (`nowInterAdv`, `nowRewardAdv`, события),
- `GameReadyAPI`, `GameplayStart/Stop`,
- язык (`infoYG.Basic`), пауза при рекламе.
Игровая логика зависит только от `ISaveService`/абстракций и не знает про Yandex напрямую.

---

## 9. Анти-паттерны (запрещено)

- `FindObjectOfType` в `Update` / постоянный `GetComponent` в `Update`.
- Строки-идентификаторы категорий/товаров.
- Hardcoded логика товаров/полок/уровней.
- God Object / огромные MonoBehaviour.
- Отдельные gameplay-системы под каждую платформу (единый API + разные источники ввода).
- Статическое глобальное состояние без необходимости.

---

## 10. Платформенная компиляция

- `UNITY_*` символы — только там, где это действительно необходимо (например,
  `#if UNITY_WEBGL && !UNITY_EDITOR` внутри Yandex-обёртки).
- Gameplay-код — кроссплатформенный, без директив.
