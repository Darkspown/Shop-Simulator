# LEVELS — Shelf Rush: система уровней (Задача 09)

> Дата: 14.09.2026 | Автор: Cline.
> Задача 09 (Documentation/CLINE_TASKS.md). Gameplay-логика первого уровня.
> **Сцена не изменялась.** Вся логика — plain C# сервисы + данные в `ScriptableObject`.

---

## 0. Резюме

Реализована **data-driven** система уровней: конкретный уровень полностью описан в
`LevelData` (ScriptableObject), а игровая логика в `LevelManager` / `LevelProgress`
**не хардкодит** ни товар, ни полку, ни цель, ни награды.

| Класс | Файл | Роль |
|---|---|---|
| `LevelData` | `Levels/LevelData.cs` | `ScriptableObject` — все параметры уровня (цели, награды, carry). Единый источник. |
| `LevelObjective` | `Levels/LevelObjective.cs` | Определение одной цели (данные): тип, товар, полка, `TargetCount`, `ShelfCompleteReward`. |
| `LevelProgress` | `Levels/LevelProgress.cs` | Runtime-прогресс по целям (0/10 … 10/10), без UI. |
| `LevelManager` | `Levels/LevelManager.cs` | Gameplay-логика: старт/финиш, подсчёт прогресса, награды, события. |
| `ILevelManager` | `Levels/ILevelManager.cs` | Контракт для UI/ввода/bootstrap. |

> Примечание: прежний `LevelConfig` переименован в `LevelData` и расширен целями
> (скрипт и GUID ассетов сохранены — существующие `LevelData` L1–L5 остаются связанными).

---

## 1. Level 1 (текущие параметры — в `Assets/Settings/LevelConfig/L1.asset`)

```
Goal:  Fill Drinks Shelf       (goalDescription)
Type:  FillShelf               (LevelObjectiveType)
Product: Water                 (LevelObjective.TargetProduct  → PD_Water)
Shelf:  Drinks                 (LevelObjective.TargetShelf    → ShelfDataWater, AllowedCategory C_Drinks)
Target: 10                     (LevelObjective.TargetCount)
```

Прочие параметры уровня:

| Параметр | Поле `LevelData` | Значение L1 |
|---|---|---|
| Индекс | `levelIndex` | 0 |
| Награда за завершение цели (Shelf Complete) | `LevelObjective.shelfCompleteReward` | 100 |
| Награда за прохождение уровня (Level Complete) | `completionReward` | 500 |
| Вместимость переноски | `carryCapacity` | 1 |
| Регистрируемые полки | из `Objectives[].TargetShelf` | Drinks (`ShelfDataWater`, capacity 10) |

> Чтобы полку можно было **физически** наполнить до цели, `ShelfData` полки должен иметь
> `Capacity >= TargetCount` (для L1 Drinks `capacity = 10`).

---

## 2. Gameplay Flow (Level 1)

```
Pickup → Carry → Navigate → Place → Reward → Progress → Repeat → Complete
```

1. **Pickup / Carry** — игрок берёт товар (Water) (см. `CARRY_SETUP.md`, `PlayerCarry`).
2. **Navigate** — подходит к полке Drinks (`ShelfInteraction`/`ShelfInteractionZone`).
3. **Place** — `ShelfController.TryPlace` проверяет `Product.Category == Shelf.AllowedCategory`
   и наличие свободного слота; при успехе публикует `ShelfProductPlacedEvent`.
4. **Reward (за товар)** — `EconomyService` на `ShelfProductPlacedEvent` начисляет монеты
   (`ProductData.RewardValue`).
5. **Progress** — `LevelManager.OnShelfProductPlaced` находит цель по `(TargetShelf, TargetProduct)`,
   вызывает `LevelProgress.Advance` → прогресс `1/10 … 10/10`, событие `PlacementProgressChanged`.
6. **Repeat** — пока `Progress.IsComplete == false`.
7. **Complete** — при `10/10`:
   - **Shelf Complete** — `Advance` возвращает `true` → награда `LevelObjective.ShelfCompleteReward`;
   - **Level Complete** — `LevelManager.FinishLevel` начисляет `LevelData.CompletionReward` и
     публикует `LevelCompletedEvent` (UI, экономика).

> Прочие события: interface `ILevelManager.PlacedProducts` (всего размещено), `CompletedShelves`
> (число завершённых целей), `PlacementProgress` (0..1).

---

## 3. Создание и подключение LevelData (пошагово)

### 3.1 Создать ассет уровня
1. В Project: `Assets > Create > ShelfRush > Level` (или ПКМ → `Create > ShelfRush > Level`).
   Создаётся `LevelData`-ассет (файл `*.asset`), например `L1.asset`.
2. Ассет уже использует новый скрипт `LevelData` (`ShelfRush.Levels.LevelData`) — сцена не нужна.

### 3.2 Настроить цели (Objectives)
1. В инспекторе `LevelData` раскрыть список **Objectives** и добавить элемент (**+**).
2. Заполнить **одну цель** `FillShelf`:
   - **Goal Description** — отображаемая формулировка (напр. `Fill Drinks Shelf`);
   - **Target Product** — перетащить `ProductData` (напр. `PD_Water`);
   - **Target Shelf** — перетащить `ShelfData` (напр. `ShelfDataWater`, AllowedCategory=C_Drinks);
   - **Target Count** — сколько разместить (напр. `10`);
   - **Shelf Complete Reward** — награда за заполнение полки (напр. `100`).
3. Поля `Level Index`, **Completion Reward** (за уровень), **Carry Capacity** — по желанию.

### 3.3 Подключить LevelData в проект
1. Открыть объект с компонентом **`GameBootstrap`** (на сцене/префабе).
2. В поле **`Levels`** (массив `LevelData[]`) добавить созданные ассеты (L1…L5). Порядок =
   порядок уровней (`StartLevel(index)`).
3. Убедиться, что `GameBootstrap.products` содержит `ProductData` (для каталога),
   а на полке сцены используется `ShelfData`-ассет из `TargetShelf`.

### 3.4 Проверка целиком
- `LevelManager` регистрирует `TargetShelf`-полки в `IStockService` при `StartLevel`.
- Прогресс цели идёт по `ShelfProductPlacedEvent` (публикуется `ShelfController`) —
  никакой дополнительной настройки не требуется.

---

## 4. Добавление нового уровня (без правки кода)

```
Create LevelData → Set Objectives → Set Products → Set Shelves → Set Rewards → Done
```

`LevelManager` менять не требуется: он читает цель/награды/полки из данных. Для нескольких
объективов добавляются элементы в **Objectives** — прогресс и завершение считаются по всем.

---

## 5. Анти-паттерны (запрещено)

- Хардкод товара/полки/цели/награды в `LevelManager` или MonoBehaviour — только `LevelData`.
- Хранение runtime-прогресса в `LevelData` (ScriptableObject не мутируется в рантайме —
  прогресс живёт в `LevelProgress`).
- Хранение `CurrentAmount` полки в ассете — это состояние `ShelfController`.
- Сравнение категорий по строке — только по ссылке `ProductCategory`.