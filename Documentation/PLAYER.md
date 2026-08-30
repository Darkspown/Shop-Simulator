# PLAYER — Shelf Rush: игрок, ввод и движение

> Дата: 28.08.2026 | Автор: Cline.
> Описывает модуль Player: `IPlayerController`, `PlayerController`, `PlayerMovement`,
> `PlayerConfig` — и как они связаны с единым вводом.
>
> **См. также:** `Documentation/PREFAB_PLAYER.md` — MonoBehaviour-вью на Player prefab
> (`PlayerController`, `PlayerMovement`, `PlayerInteraction`, `PlayerCarry`, `PlayerAnimator`).

---

## 1. Назначение модуля Player

Модуль отвечает за:
- **движение** игрового объекта по плоскости XZ (top-down вид),
- **инвентарь** переносимых товаров (`Carried` / `CarryCapacity`),
- **взаимодействие** с полками (взять товар) и клиентами (доставить заказ).

Движение изолировано в `PlayerMovement`; сам `PlayerController` — это «оркестратор»:
через `IPlayerInput` получает нормализованный ввод и передаёт его в `PlayerMovement`,
а также управляет инвентарём и заказами.

## 2. Файлы модуля

| Файл | Классы | Ответственность |
|---|---|---|
| `Player/IPlayerController.cs` | `IPlayerController` | Публичный контракт (View, Carried, TryPickUp, TryDeliver) |
| `Player/PlayerController.cs` | `PlayerController` | Оркестрация: ввод → движение → инвентарь → заказы |
| `Player/PlayerMovement.cs` | `PlayerMovement` | Чистая математика движения по нормализованному вектору |
| `Player/PlayerConfig.cs` | `PlayerConfig` | ScriptableObject-настройки (скорость, вместимость, радиус) |

## 3. Важно: PlayerMovement не знает про input

`PlayerMovement` **запрещено** читать:
- `Input.GetAxis` / `Input.GetKey`,
- `Keyboard` / `Touchscreen` / `Joystick` / `Gamepad`,
- любые классы из `UnityEngine.InputSystem`.

Он получает **уже нормализованный** `Vector2`/`Vector3` и считает только смещение:

```csharp
var step = new Vector3(move.x, 0f, move.z) * (MoveSpeed * deltaTime);
view.position += step;
```

## 4. Поток данных (ввод → движение)

```
IInputService (провайдеры: клавиатура/тач/геймпад/джойстик)
      ↓  Move (Vector2)
IPlayerInput (PlayerInput): dead zone → sensitivity → smoothing
      ↓  Move / MoveWorld
PlayerController.Tick(deltaTime)
      ↓  MoveWorld (Vector3 XZ)
PlayerMovement.Move(view, move, dt)
      ↓
Transform.position += (move.x, 0, move.z) * MoveSpeed * dt
```

Ни `PlayerController`, ни `PlayerMovement` не импортируют `UnityEngine.InputSystem`.

## 5. Регистрация и порядок тиков (GameBootstrap)

В `GameBootstrap.Build()`:
- создаётся `new PlayerInput()` — регистрируется как `IPlayerInput`;
- `PlayerInputConfig` (если назначен на ассете) регистрируется как данные;
- при наличии `mobileJoystick` вызывается `playerInput.AttachJoystick(mobileJoystick)`.

Порядок инициализации (и, следовательно, порядок `Tick` в `Update` каждого кадра):

```
GameStateMachine → Platform → Save → Pool → Catalog → InputService
   → PlayerInput → Economy → Stock → Customers → PlayerController → UI → LevelManager
```

`PlayerInput` тикается **раньше** `PlayerController`, поэтому `PlayerController.Tick`
в этом же кадре читает уже обновлённый сглаженный вектор.

## 6. PlayerConfig

Создаётся через `Assets > Create > ShelfRush > Player`:

| Поле | По умолчанию | Описание |
|---|---|---|
| `moveSpeed` | `4` | Скорость движения, units/сек |
| `carryCapacity` | `4` | Макс. число товаров в руках |
| `pickupRadius` | `1.2` | Радиус подбора товара |

Если конфиг не назначен, используются встроенные дефолты (`MoveSpeed = 4`,
`CarryCapacity = 4`).

## 7. Контракт IPlayerController

```csharp
public interface IPlayerController : IGameService, ITickable
{
    Transform View { get; set; }           // визуальный объект игрока
    IReadOnlyList<ProductData> Carried { get; }
    int CarryCapacity { get; }
    bool TryPickUp(ShelfData shelf);
    bool TryDeliver(Customers.CustomerOrder order);
}
```

## 8. Привязка View (вручную, на сцене — без изменения архитектуры)

`PlayerController` не создаёт объект игрока (архитектура запрещает God Object/статику).
Когда появится сцена, visual-объект игрока (`PolyOne`-модель или простой capsule)
привязывается так:

```csharp
var player = services.Get<IPlayerController>();
player.View = playerTransform;   // Transform игрока в сцене
```

`PlayerController.Tick` начинает двигать `View` по нормализованному вводу.

## 9. Анти-паттерны (запрещено)

- Двигать `Transform` прямой формулой внутри `PlayerController` (это делает `PlayerMovement`).
- Читать ввод в `PlayerController`/`PlayerMovement` (это делает `PlayerInput`).
- Хранить позицию/скорость статически.
- Вызывать `FindObjectOfType`/`GetComponent` в `Update`.