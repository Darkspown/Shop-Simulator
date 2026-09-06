# SHELVES — Shelf Rush: Shelf System

> Дата: 05.09.2026 | Автор: Cline.
> Задача 07 (Documentation/CLINE_TASKS.md). Архитектура и правила Shelf System.
> Пошаговая настройка ассетов/префабов — в `Documentation/SHELVES_SETUP.md`.

---

## 0. Резюме

Shelf System — критическая система, которая решает, **какие товары можно разместить на полке**.
Всё решение принимается по данным (`ProductData.Category == ShelfData.AllowedCategory`) и
**НЕ зависит от UI/цветов.** UI/цвета — только feedback после принятого решения.

Созданные компоненты:

| Файл | Тип | Ответственность |
|---|---|---|
| `Shelves/ShelfData.cs` | ScriptableObject (Data) | Поля полки: `AllowedCategory`, `Capacity`, `Slots`, `InteractionRadius` (+ legacy `Product` для режима Stock). |
| `Shelves/ShelfController.cs` | MonoBehaviour | Ядро логики: `CanPlace`/`TryPlace`/`TryPlaceRange`, `CurrentAmount`, события, feedback, спавн товара. |
| `Shelves/ShelfSlot.cs` | MonoBehaviour | Слот размещения (точка на полке, содержимое). Создаётся автоматически из `ShelfData.Slots`. |
| `Shelves/ShelfInteraction.cs` | MonoBehaviour (`IInteractable`) | Связывает игрока с контроллером: размещает товары из рук, оставляя остаток у игрока. |

События (через `EventBus`, файл `Core/GameEvents.cs`):
`ShelfProductPlacedEvent`, `ShelfPlacementFailedEvent`, `ShelfCompletedEvent`.
---

## 1. Главная проверка

```csharp
bool allowed = product.Category == shelf.AllowedCategory;   // type-safe, по ссылке на SO
```

- `TRUE` → **allow placement** (если есть свободный слот).
- `FALSE` → **block placement** (товар не трогается).

Проверка инкапсулирована в `ShelfController.ValidatePlacement(out reason)` и обёрнута в
`TryPlace(product, out reason)`:

```csharp
public bool TryPlace(ProductData product, out ShelfPlacementFailReason reason)
{
    // 1. product != null
    // 2. shelf.IsFull (CurrentAmount >= Capacity) → иначе reason = ShelfFull
    // 3. product.Category == AllowedCategory       → иначе reason = CategoryMismatch
    // 4. есть свободный слот                       → иначе reason = ShelfFull
    // 5. разместить: slot.SetPlaced, CurrentAmount++, события, reward, feedback
}
```

`ShelfPlacementFailReason`: `NoData`, `CategoryMismatch`, `ShelfFull`.

---

## 2. Поведение

### Неправильный товар (категория не совпала)
- не удалять, не уничтожать, не размещать;
- не выдавать `reward`;
- не увеличивать `LevelProgress`;
- **остаётся у игрока**;
- события: `OnCategoryMismatch` → `OnPlacementFailed(reason=CategoryMismatch)` → `ShelfPlacementFailedEvent`;
- feedback: короткий негативный (лёгкий «тряс» полки).

### Правильный товар
- найти свободный слот;
- разместить (занять слот, спавн визуала через `ProductSpawner`/LeanPool);
- `CurrentAmount++`;
- `OnProductPlaced` (view-событие);
- `ShelfProductPlacedEvent` → **reward** (EconomyService) и **update LevelProgress** (LevelManager);
- feedback: highlight (зелёный tint через `ProductVisual`) + bounce (`DOPunchScale`);
- если полка заполнилась → `OnShelfCompleted` + `ShelfCompletedEvent`.

### Полка полная (`IsFull`)
- размещение запрещено;
- события: `OnShelfFull` → `OnPlacementFailed(reason=ShelfFull)` → `ShelfPlacementFailedEvent`;
- feedback: негативный. Остаток (не поместившийся) остаётся у игрока.
---

## 4. Runtime-слоты

`ShelfController.Awake` создаёт дочерние объекты `ShelfSlot_{i}` для каждой позиции
`ShelfData.Slots` (или синтетических позиций) и вызывает `slot.Init(this, i, offset)`.
Ничего руками на сцене создавать не нужно — сцена не изменяется.

`ShelfSlot` хранит: индекс, локальное смещение, `Contents` (`ProductData`) и `Visual`
(`Product`). Проверки занятости: `IsOccupied` / `IsAvailable`.

---

## 5. Интеграция (обработчики событий)

| Система | Файл | Реакция |
|---|---|---|
| Награда | `Economy/EconomyService.cs` | на `ShelfProductPlacedEvent` → `AddCurrency(Coins, product.RewardValue)`. Только на успешное размещение. |
| Прогресс уровня | `Levels/LevelManager.cs` | на `ShelfProductPlacedEvent` → `PlacedProducts++`; на `ShelfCompletedEvent` → `CompletedShelves++`; `PlacementProgress` = `PlacedProducts / LevelConfig.TotalShelfCapacity`. Событие `PlacementProgressChanged`. |
| UI | (future `IHUDView`) | подписка на события + `ILevelManager.PlacementProgressChanged`. |

`ILevelManager` расширен: `PlacedProducts`, `CompletedShelves`, `PlacementProgress`,
`PlacementProgressChanged`.
`LevelConfig` расширен: `TotalShelfCapacity`, `TotalShelfCount`.

---

## 6. Feedback

- **correct** = `ProductVisual.SetTint(зелёный)` + `DOPunchScale` (bounce) на слоте;
- **wrong** = `DOPunchPosition` (короткий «тряс») на корне полки.

Feedback — только визуальный слой. Решение и события не зависят от того, показывается ли он.

---

## 7. Анти-паттерны (запрещено)

- Сравнение категорий по строке — только по ссылке `ProductCategory`.
- Хранение `CurrentAmount` в `ScriptableObject` — это runtime-состояние (`ShelfController`).
- Принятие решения «по цвету/UI» — сначала `TryPlace`, потом feedback.
- Прямой спавн `Instantiate/Destroy` товара — через `ProductSpawner`/LeanPool.
- `FindObjectOfType`/статическое состояние для сервисов — через `GameBootstrap`/`ServiceBridge`.

---

## 8. Связь с legacy (IStockService)

Существующий режим «забор товара с полки» (`StockService`, `PlayerController.TryPickUp`)
оставлен без изменений и использует поля `ShelfData.Product` + `ShelfData.Capacity`.
Новая Shelf System (placement) использует `AllowedCategory` + `Slots` + `SlotCapacity`.
Оба режима сосуществуют; при настройке конкретного ассета полки используйте либо
назначение `Product` (Stock), либо `AllowedCategory` (Placement).

### Несколько товаров
`TryPlaceRange(IEnumerable<ProductData>)` размещает **только доступное количество**
(пока есть свободные слоты и подходит категория), остаток **не** размещается и остаётся у игрока.
`ShelfInteraction.Interact` снимает с игрока только реально размещённые товары
(`Carry.TryDrop` в цикле по снимку `Carry.Items`).

---

## 3. Данные (ShelfData)

| Поле | Тип | Смысл |
|---|---|---|
| `AllowedCategory` | `ProductCategory` (SO) | какая категория допускается (type-safe, не строка) |
| `Capacity` | int | сколько слотов задействуется (не менее 1) |
| `Slots` / `Placements` | `Vector3[]` | локальные позиции слотов от корня полки |
| `InteractionRadius` | float | радиус, с которого игрок может взаимодействовать |
| `Product` | `ProductData` | ТОЛЬКО legacy режим Stock (`IStockService`) — товар, выдаваемый с полки |

`SlotCapacity` = `min(Capacity, Slots.Length)`, минимум 1. Если `Slots` пуст —
слоты раскидываются автоматически по `Capacity` по строке (`SyntheticOffset`).

Текущее количество (`CurrentAmount`) — **runtime-состояние** в `ShelfController`,
в ассете не хранится (SO не должен мутироваться в рантайме).