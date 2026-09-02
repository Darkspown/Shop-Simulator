# PREFAB_PLAYER — Shelf Rush: вью-контроллер игрока на prefab (MonoBehaviour)

> Дата: 28.08.2026 | Автор: Cline.
> Реализация **Player Controller** для Player prefab: `PlayerController`, `PlayerMovement`,
> `PlayerInteraction`, `PlayerCarry`, `PlayerAnimator` + ScriptableObject `PlayerConfig`.
> Это вью-слой (MonoBehaviour), использующий существующую систему ввода (`IPlayerInput`).
> Сцена при этом **не изменяется**.

---

## 1. Связь с общей архитектурой

В проекте есть **два** слоя игрока:

1. **plain C# сервис** (`ShelfRush.Player.PlayerController`) — регистрируется в
   `ServiceLocator` через `GameBootstrap` как `IPlayerController`. Его **не трогаем**.
2. **Вью-слой на prefab** (`ShelfRush.Player.View.*`) — этот документ. Компоненты живут
   на GameObject игрока в сцене и представляют персонажа.

Вью-слой получает зависимости (ввод, конфиг) **не напрямую из ServiceLocator**, а через
`ServiceBridge` → `GameBootstrap.Instance.Services`. Это сохраняет правило
«нет God Object / нет прямого обращения к DI».

**Ввод**: компоненты читают готовый нормализованный `Vector3` (`IPlayerInput.MoveWorld`) и
событие `IPlayerInput.Interact`. Источник ввода (PC/Mobile/WebGL) вью не волнует.

## 2. Архитектура (компоненты Player prefab)

```
Player (корень)
├── PlayerController  (MonoBehaviour, ОРКЕСТРАТОР)
│     ├── PlayerMovement     (движение)
│     ├── PlayerInteraction  (поиск/состояние взаимодействия)
│     ├── PlayerCarry        (инвентарь)
│     └── PlayerAnimator     (визуальные состояния idle/walk/carry/interact)
├── [Model]  (превью-модель, напр. PolyOne Stickman; может иметь Animator)
└── CarryAnchor  (точка стекирования товаров)
```

**Правило:** `PlayerController` не содержит всю логику — только «клей»: получает ввод,
вызывает `Movement.DoMove(...)`, делегирует `Interaction.OnInteractInput()` и подкармливает
`PlayerAnimator`. Вся конкретика — в под-компонентах.

### 2.1 File map

| Файл (Assets/Scripts/Player/View/) | Класс | Ответственность |
|---|---|---|
| `PlayerController.cs` | `PlayerController` (MonoBehaviour) | Оркестрация: ввод → движение → анимации; делегирование |
| `PlayerMovement.cs` | `PlayerMovement` (MonoBehaviour) | Скорость/ускорение/замедление/поворот/остановка |
| `PlayerInteraction.cs` | `PlayerInteraction` + `InteractionState` | Поиск интерактивов в радиусе, состояние, обработка Interact |
| `PlayerCarry.cs` | `PlayerCarry` (MonoBehaviour) | Инвентарь: count/capacity/add/remove/clear + визуал (DOTween) |
| `PlayerAnimator.cs` | `PlayerAnimator` (MonoBehaviour) | Состояния idle/walk/carry/interact; DOTween только для visual |
| `PlayerCamera.cs` | `PlayerCamera` (MonoBehaviour) | Следование камеры за игроком (плавное, LateUpdate) |
| `IInteractable.cs` | `IInteractable`, `InteractableComponent` | Контракт интерактивных объектов сцены |
| `ServiceBridge.cs` | `ServiceBridge` (internal static) | Доступ вью к сервисам через GameBootstrap |

Конфиг: `Assets/Scripts/Player/PlayerConfig.cs` — **ScriptableObject**
(`ShelfRush.Player.PlayerConfig`).

## 3. PlayerConfig (ScriptableObject)

Создать: `Assets > Create > ShelfRush > Player`. Назначить в любое поле «Config» компонентов
(или он подхватится автоматически из `GameBootstrap.playerConfig` через `ServiceBridge`).

| Параметр | Тип | Default | Описание |
|---|---|---|---|
| `moveSpeed` | float | 4 | Макс. скорость движения, units/сек |
| `acceleration` | float | 40 | Ускорение (Units/s²) при наборе скорости |
| `deceleration` | float | 60 | Замедление (Units/s²) при отпускании ввода (если `instantStop` выключен) |
| `instantStop` | bool | true | Мгновенная остановка при отпускании тапа/кнопки — без инерции/проскальзывания |
| `rotationSpeed` | float | 540 | Скорость поворота модели, град/сек |
| `interactionRadius` | float | 2 | Радиус поиска интерактивных объектов |
| `autoPickup` | bool | true | Авто-подбор при приближении (без нажатия кнопки/тапа) |
| `pickupDuration` | float | 0.35 | Длительность анимации «взять в руки» + лок |
| `placementDuration` | float | 0.35 | Длительность анимации «положить» |
| `carryCapacity` | int | 4 | Максимальное число товаров в руках |
| `pickupRadius` (legacy) | float | 1.2 | Унаследованно для plain C# сервиса (вью не использует) |

> Ничего не хранится hardcoded в компонентах: все параетры читаются только из конфига.

## 4. Какие компоненты должны быть на Player prefab

Соберите иерархию и добавьте компоненты:

### 4.1 Корень `Player`
- `Transform` — двигается и поворачивается компонентом `PlayerMovement`.
- **`PlayerController`** (MonoBehaviour). Требуемые: `PlayerMovement`, `PlayerInteraction`
  (добавляются автоматически через `[RequireComponent]`). Вложенные компоненты можно не
  назначать вручную — находятся `GetComponent`/`GetComponentInChildren` в `Awake`.
- **`PlayerMovement`** (MonoBehaviour):
  - `Config` → `PlayerConfig` (опц., авто-резолв).
  - `Body` → опц. кинематичный `Rigidbody` (иначе двигает `Transform`).
- **`PlayerInteraction`** (MonoBehaviour):
  - `Config` → `PlayerConfig` (опц.).
  - `interactableMask` → LayerMask для полок/клиентов (default `Everything`).
  - `searchInterval` → 0.1 c.
- **`PlayerCarry`** (MonoBehaviour):
  - `Config` → `PlayerConfig` (опц.).
  - `carryAnchor` → child-объект (руки/спина) для стекировки.
  - `stackOffset` → смещение между товарами в стопке.
  - Визуал «коробки» берётся из `ProductData.BoxPrefab` и спавнится через LeanPool (`IPoolService`).
  - Пошаговая настройка: `Documentation/CARRY_SETUP.md`.
- **`PlayerAnimator`** (MonoBehaviour):
  - `animator` → опц. Unity `Animator` (bools `Idle/Walk/Carry/Interact`).
  - `interactPulse`, `bobTarget`, `bobAmplitude`, `bobDuration` → DOTween-эффекты.
- **`PlayerCamera`** (MonoBehaviour, опционально) — следящая камера:
  - повесьте на корень Player (или на саму камеру), `target` → (опц.) цель,
    `cameraToMove` → (опц.) камера, `offset`, `followSmooth`, `lockY`.

### 4.2 Child `Visual` (модель)
- Превью-модель (`PolyOne Stick Man` или простая capsule/cube) с mesh renderer.
- Опц. `Animator` с bool-параметрами `Idle/Walk/Carry/Interact`.

> Если `Animator` нет — работают DOTween-эффекты (пульс при взаимодействии, покачивание
> на `bobTarget`). Этого достаточно для проверки без сцены.

### 4.3 Child `CarryAnchor`
- Трансформ на уровне груди, к которому `PlayerCarry` прикрепляет инстансируемые «коробки»
  товаров. Опц. — если пуст, переноска работает логически (без визуала).

## 5. Ссылки, которые нужно назначить (проверочный чек-лист)

| Где | Поле | Что |
|---|---|---|
| `PlayerMovement` | `Config` | `PlayerConfig` asset |
| `PlayerMovement` | `Body` | (опц.) кинематичный `Rigidbody` |
| `PlayerInteraction` | `Config` | `PlayerConfig` |
| `PlayerInteraction` | `interactableMask` | LayerMask полок/клиентов |
| `PlayerCarry` | `Config` | `PlayerConfig` |
| `PlayerCarry` | `carryAnchor` | (опц.) child для стекировки |
| `PlayerCarry` | `stackOffset` | смещение между товарами в стопке |
| `PlayerAnimator` | `animator` | (опц.) `Animator` модели |
| `PlayerAnimator` | `bobTarget` | (опц.) child для покачивания |
| `PlayerController` | все | (опц.) можно оставить пустыми — авто-резолв в `Awake` |
| `GameBootstrap` | `Player Config` | тот же `PlayerConfig`, чтобы вью-резолв работал |

Все параметры (`moveSpeed`, `acceleration`, и т.д.) выставляются в самом `PlayerConfig`.

## 6. Как работают компоненты (кратко)

- **Движение**: `PlayerController.Tick` → `movement.DoMove(input.MoveWorld, dt)`.
  Ввод нормализован; разгон — `acceleration`, поворот к направлению — `rotationSpeed`.
  **Отпускание** (тап/клавиши) детектируется по **сырому** вводу `MoveTargetWorld` (ДО
  smoothing — обнуляется в тот же кадр), а не по сглаженному `MoveWorld` (который затухает
  от input smoothing и давал «проскальзывание»). Если `instantStop = true` — вызывается
  `movement.Stop()` (мгновенная остановка без инерции); если false — плавное замедление
  через `deceleration`. Во время взаимодействия игрок стоит.
- **Взаимодействие**: `PlayerInteraction` каждые `searchInterval` ищет ближайший
  `IInteractable` в радиусе `interactionRadius` (`Physics.OverlapSphere`). По
  `IPlayerInput.Interact` вызывает `IInteractable.Interact(player)`, ставит состояние
  `Interacting` на `pickupDuration`, включает `PlayerAnimator.SetInteraction(true/false)`.
- **Переноска**: `PlayerCarry.TryAdd(ProductData)` / `TryRemove(out)` / `Clear()` —
  capacity из конфига, стекировка в `carryAnchor` с DOTween-эффектом появления
  (pickupDuration) и удаления (placementDuration). Событие `Changed` — для HUD.
- **Анимации**: `PlayerAnimator.SetIdle / SetWalking / SetCarrying / SetInteraction`
  выставляют bool-параметры конвенционального `Animator`, а также запускают DOTween-эффекты
  (пульс при взаимодействии, покачивание при ходьбе на `bobTarget`).

## 7. DOTween: где уместно, где нет

**Да (только visual):**
- `PlayerAnimator` — пульс scale при взаимодействии, покачивание при ходьбе.
- `PlayerCarry` — появление/убирание «коробки» товара в руках.

**Нет (никогда):**
- `PlayerMovement` не использует DOTween вообще (движение — чистая математика).
- DOTween не является физическим контроллером и не движет игрока.

## 7.1 Камера: следование за игроком

Компонент `PlayerCamera` (MonoBehaviour) плавно ведёт камеру за игроком по горизонтали (XZ).

**Размещение (2 способа):**
1. **На корне Player** — добавьте `PlayerCamera` на объект игрока; он сам найдёт главную камеру
   (по тегу `MainCamera`) и будет двигать её. `target` можно не назначать (по умолчанию — корень).
2. **На самой камере** — добавьте `PlayerCamera` на `Main Camera` и перетащите объект игрока в `target`.

**Параметры (в инспекторе `PlayerCamera`):**
| Поле | По умолчанию | Описание |
|---|---|---|
| `target` | — | За кем следить; если пусто — корень компонента (игрок) |
| `cameraToMove` | — | Какая камера двигается; если пусто — `Camera.main` |
| `offset` | `(0, 12, -8)` | Смещение камеры относительно цели (вид сверху-сбоку) |
| `followSmooth` | `12` | Скорость сглаживания, 1/с; больше = жёстче, меньше = инерция |
| `lockY` | `true` | Не менять высоту камеры (не прыгать по Y за целью) |

> Не используется DOTween — обычный `Lerp` в `LateUpdate`. При `Start` камера сразу
> встаёт в правильную точку, чтобы не было «долёта» в первом кадре.


## 8. IInteractable: как сделать полку/клиента интерактивными

Сейчас сцена пустая. Чтобы игрок мог брать товар, создайте компонент на объекте полки/клиента,
реализующий `IInteractable` (удобно наследоваться от `InteractableComponent`):

```csharp
public sealed class DebugShelfInteractable : InteractableComponent
{
    public override bool CanInteract(PlayerController player) => player.Carry.CanAdd();

    // Авто-подбор: товар берётся сам при приближении, без кнопки/тапа.
    public override bool AutoInteractOnApproach => true;

    public override void Interact(PlayerController player)
    {
        // Пример: взять ProductData и положить в руки:
        // if (player.Carry.TryAdd(someProduct)) { /* эффект */ }
    }
}
```

На объекте должен быть **Collider** (Trigger ок), слой из `interactableMask` — тогда
`PlayerInteraction` найдёт его и вызовет взаимодействие по нажатию E (PC) / тапу (Mobile).

## 9. Как проверить PC / Editor

1. Откройте сцену с `GameBootstrap` (или тестовую), где на сцене стоит Player prefab.
2. Убедитесь, что в `GameBootstrap.playerConfig` назначен `PlayerConfig`, а инспектор
   `GameBootstrap` имеет готовый `PlayerInputConfig` — ввод работает.
3. **WASD / стрелки** — игрок двигается с плавным разгоном и торможением, модель
   поворачивается к направлению движения.
4. **Проверка анимации**: при движении переключается `Walk`; при `carry.Count > 0` — `Carry`;
   при взаимодействии — `Interact` (и DOTween-пульс).
5. **Проверка взаимодействия** (без сцены): поставьте рядом тестовый объект с
   `InteractableComponent` (`AutoInteractOnApproach = true`, и Collider); просто
   **подойдите в радиус** `interactionRadius` — товар берётся автоматически (без нажатий)
   и добавляется в `PlayerCarry` (Count растёт; при заполненном `ProductData.BoxPrefab`
   появится «коробка» с DOTween-эффектом из пула).
6. **Вместимость**: заполните руки `carryCapacity` раз — следующие «взять» игнорируются
   (`IsFull`, авто-подбор останавливается); `TryRemove` (положить) убавляет по одному,
   `Clear()` — мгновенно.
7. Измените `acceleration` / `deceleration` / `moveSpeed` в `PlayerConfig` — отклик движения
   меняется без правок логики.

### 9.1 Устранение: «персонаж не двигается»

Движение зависит от `IPlayerInput` и `PlayerConfig`, которые компоненты берут из
`ServiceLocator` через `GameBootstrap`. Если игрок стоит:

1. **Проверьте Console.** Если компонент `PlayerController` (prefab) пишет
   `[PlayerController] IPlayerInput не найден...` — значит на сцене нет активного
   `GameBootstrap` ИЛИ он не успел зарегистрировать ввод. На сцене должен быть `GameBootstrap`.
2. **Добавьте `GameBootstrap` в сцену** (GameObject → Add Component → `ShelfRush.Core.GameBootstrap`),
   назначьте в нём `PlayerConfig`, `PlayerInputConfig` (и опц. `mobileJoystick`).
3. **Порядок больше не важен**: компоненты prefab выполняют «ленивый» резолв каждый кадр,
   пока `IPlayerInput` не станет доступен (бутстрап строит `ServiceLocator` тоже в `Awake`).
4. Если ввод есть, но движения нет — проверьте, что `PlayerMovement.config` не пуст
   (в инспекторе prefab) и что `moveSpeed`/`acceleration` > 0 в `PlayerConfig`.
5. Убедитесь, что на объекте не вызван `SetGameplayEnabled(false)` (пауза).

## 10. Как проверить Mobile

Логика движения/взаимодействия **общая** — нет веток `#if UNITY_*`. Проверьте:

1. **TouchInputProvider** (свайп) — движение от свайпа по экрану (нормализованный вектор).
2. **VirtualJoystick** — добавьте префаб джойстика на Canvas, назначьте в
   `GameBootstrap.Mobile Joystick`; пока палец зажат — ось берётся из джойстика.
3. **Кнопка взаимодействия** — на Canvas добавьте кнопку; в `OnClick` вызовите
   `joystick.InvokeInteract()` (событие от `IPlayerInput.Interact`). Обработается так же,
   как клавиша E на PC.
4. Соберите под Android/iOS (или WebGL mobile-режим). Движение/взять/положить работают
   идентично PC (dead zone / smoothing — в `PlayerInputConfig`).

## 11. Анти-паттерны (запрещено)

- Содержать всю игровую логику в `PlayerController` (он только «клей»).
- Двигать `Transform` в `PlayerController` (это делает `PlayerMovement`).
- Читать ввод (`Keyboard`/`Touchscreen`/`GetAxis`) в любом из этих компонентов — только
  `IPlayerInput`.
- `FindObjectOfType` / `GetComponent` в `Update` (bootstrap-резолв — разово в `Awake`).
- DOTween как контроллер движения/физики (только visual-эффекты).
- Хранить параметры геймплея hardcoded — все в `PlayerConfig`.