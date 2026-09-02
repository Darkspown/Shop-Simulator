# CARRY_SETUP — Shelf Rush: настройка подбора и переноски (Pickup & Carry)

> Дата: 30.08.2026 | Автор: Cline.
> Задача 06 (Documentation/CLINE_TASKS.md). Как настроить систему переноски товаров
> через Prefab, ScriptableObject и runtime-инициализацию. **Сцена не изменяется.**

---

## 0. Общая схема

```
ProductData (SO)
   ├── prefab      → товар на полке (Product + ProductVisual + Collider)
   └── boxPrefab   → «коробка» в руках игрока (Product, опц. ProductVisual)
                ↓
PlayerCarry.TryAdd(productData)  → пул LeanPool → на carryAnchor → DOTween-стопка
```

Ключевые точки настройки:
1. **Префаб игрока** — компонент `PlayerCarry` (якорь, `stackOffset`).
2. **ProductData** — `boxPrefab` для визуала в руках.
3. **Интерактив (полка/клиент)** — реализует `IInteractable`, при `Interact` дёргает
   `player.Carry.TryAdd / TryRemove / TryDrop`.
4. **LevelConfig** — `carryCapacity` (прогрессия L1=1 … L5=7).
5. **GameBootstrap** — зарегистрирован `IPoolService`, `ILevelManager`, `PlayerConfig`.

Геймплей-поток: `Approach → Detect → Pickup → Add To Carry → Carry → Deliver → Remove From Carry`.

---

## 1. Настройка префаба Player (вью)

На корневом объекте игрока должны быть (View-слой):
`PlayerController` (оркестратор), `PlayerMovement`, `PlayerInteraction`, `PlayerCarry`, `PlayerAnimator`.

Для **`PlayerCarry`** в инспекторе:

| Поле | Тип | Описание |
|---|---|---|
| `Config` | `PlayerConfig` | (опц.) Конфиг. Если пусто — подхватывается автоматически через `ServiceBridge` из `GameBootstrap.playerConfig`. |
| `Carry Anchor` | `Transform` | Дочерний объект рук/спины (пустой `Transform` или слот на модели). Сюда DOTween-ом «подъезжают» коробки. |
| `Stack Offset` | `Vector3` | Смещение между товарами в стопке. По умолчанию `(0, 0.12, 0)` — стопка растёт вверх по Y. Подстраивается под размер коробок. |

> `PlayerController` (View) сам находит `PlayerCarry` через `GetComponentInChildren` — отдельная привязка не обязательна.
---

## 2. Создание «коробки» в руках (ProductData.boxPrefab)

Для переноски нужен префаб-«коробка» (обычно упрощённый вариант товара с полки):

1. Создайте `GameObject` с моделью коробки, `Collider` (необязательно для рук) и `Renderer`.
2. На корень добавьте компонент **`Product`** (обязательно — `PlayerCarry` вызывает
   `product.Setup(data)` при спавне и `product.ResetState()` перед возвратом в пул).
3. Опционально дочерний объект с **`ProductVisual`** (Renderer'ы подтянутся автоматически;
   даёт перекрас через `MaterialPropertyBlock`).
4. Перетащите в `Assets/Prefabs` → готовый префаб, например `P_Water_Box`.

Затем в **ProductData** (например, `PD_Water`):

| Поле | Значение |
|---|---|
| `prefab` | Префаб товара на полке (`P_Water`) |
| `boxPrefab` | Префаб коробки для рук (`P_Water_Box`); если пусто — `BoxPrefab` вернёт `prefab` |
| `visualSettings` | Масштаб/тон/смещения (применяются и к рукам через `Product.Setup`) |

> Если `boxPrefab` и `prefab` пусты — `PlayerCarry` не сможет показать визуал (работает только логически).

---

## 2.1 Авто-спавнер коробок (AutoBoxSpawner)

Готовый компонент `Assets/Scripts/Products/AutoBoxSpawner.cs` — автоматически спавнит
коробки при старте (без правки кода и сцены: добавляется на объект/prefab вручную).

Как настроить:
1. Создайте/выберите объект-точку выдачи (или пустой `BoxSpawner`).
2. Добавьте компонент **`Auto Box Spawner`** (меню `Add Component > ShelfRush > Products > Auto Box Spawner`) — `ProductSpawner` подтянется автоматически.
3. В **`Data`** назначьте ваш `ProductData` (коробка берётся из `ProductData.boxPrefab`).
4. В **Spawn Offsets** укажите локальные смещения от объекта, где появятся коробки (пусто = одна коробка в позиции объекта).
5. Флаги:
   - `Respawn On Enable` (default true) — спавнить при каждом `OnEnable`;
   - `Auto Despawn On Disable` (default true) — возвращать свои коробки в пул при выключении (без дублей).

Поведение:
- На `OnEnable` компонент вызывает `SpawnAll()` → `ProductSpawner.SpawnBox(data, world)` (пул LeanPool).
- Уже подбранные игроком коробки (ушли в пул/неактивны) он не трогает при `DespawnAll()`
  (guard `activeSelf`), двойного возврата в пул нет.
- `SpawnAll()` / `DespawnAll()` доступны и для ручного вызова из кода/кнопок.

---

## 3. Интерактив «взять товар» (полка) → Add To Carry

`PlayerCarry` — это только инвентарь. Чтобы товар попал в руки, нужен объект с
`IInteractable` (обычно `ShelfView`). Минимальный пример
(по образцу `DebugShelfInteractable` из `PREFAB_PLAYER.md`):

```csharp
using ShelfRush.Player.View;
using ShelfRush.Products;
using UnityEngine;

public sealed class ShelfInteractable : InteractableComponent
{
    [SerializeField] private ProductData product;

    // Полка доступна, если в руках есть место (capacity check).
    public override bool CanInteract(PlayerController player) => player.Carry.CanAdd();

    // Авто-подбор: товар берётся сам при приближении, без нажатия кнопки/тапа.
    public override bool AutoInteractOnApproach => true;

    public override void Interact(PlayerController player)
    {
        if (player.Carry.TryAdd(product))
        {
            // опционально: убрать товар с визуала полки (ProductSpawner.Despawn),
            // уменьшить запас полки (IStockService.TryTakeProduct) и т.п.
        }
    }
}
```

На объекте должны быть: компонент-наследник `InteractableComponent` (реализует
`IInteractable`) + **Collider**. `PlayerInteraction` найдёт его в радиусе
`PlayerConfig.interactionRadius` и, если `AutoInteractOnApproach == true` и
`PlayerConfig.autoPickup` включён — **возьмёт товар автоматически при приближении**,
без нажатия **E / Enter** (PC) / тапа (Mobile).

- **Detect**: `PlayerInteraction` (OverlapSphere + `CanInteract`).
- **Pickup / Add To Carry**: автоматически при приближении → товар попадёт в руки одним из
  двух способов:
  - **Коробка-`Product` на сцене** (спавн через `ProductSpawner`) — подбирается напрямую
    при подходе: `player.Carry.TryAdd(product.Data)` + возврат коробки в пул. Отдельный
    интерактив не требуется, только чтобы у коробки был Collider и компонент `Product`
    с заданным `Data`.
  - **Интерактив `IInteractable`** (например, `ShelfInteractable` ниже) —
    при `AutoInteractOnApproach == true` подбор тоже автоматический.
- Повторяется по одному товару, пока игрок рядом и есть место/запас.
- При `Carry.IsFull == true` → `CanInteract` вернёт `false`, и «взять» блокируется.
- Доставка (разгрузка) обычно НЕ авто — оставляем `AutoInteractOnApproach = false`
  и разгружаем по кнопке/тапу.

> Для авто-подбора коробок на коробке (или префабе `ProductData.boxPrefab`) должен быть
> **Collider** (обычный или Trigger) и компонент **`Product`** с привязанным `Data`.
>
> **Два способа задать `Data` коробки:**
> 1. **Через пул** — `ProductSpawner.Spawn/SpawnBox` вызывает `product.Setup(data)`
>    (Data заполняется автоматически).
> 2. **Ручной объект на сцене** — добавьте компонент `Product` и в поле **`Initial Data`**
>    (Product.initialData) назначьте ваш `ProductData`. В `Awake` компонент сам вызовет
>    `Setup(initialData)` и свяжет `Data`. Такой коробке `ProductSpawner` не нужен —
>    достаточно Collider + `Product` + назначенный `Initial Data`.
>
> Если на объекте есть и `Product`, и `Data == null` (например, коробка без
> `Initial Data`, созданная вручную) — она авто-подбором игнорируется (guard в
> `PlayerInteraction.FindCollectibleProduct`).
---

## 4. Доставка (клиент) → Remove From Carry

Доставка — тоже `IInteractable`, но снимающее:

```csharp
public override bool CanInteract(PlayerController player) => player.Carry.CanRemove();

public override void Interact(PlayerController player)
{
    if (player.Carry.TryRemove(out var product))
    {
        // завершить заказ клиента (ICustomerService.TryCompleteOrder) и т.п.
    }
    // либо точечно: player.Carry.TryDrop(productData) — снять конкретный товар по типу
}
```

Поведение методов:
- `TryAdd(product)` — pickup, если есть место;
- `TryRemove(out product)` — снимает «верхний» (последний взятый);
- `TryDrop(productData)` — снимает конкретный товар по типу;
- `Clear()` — мгновенно снимает всё (например, на старте уровня).

После снятия стопка уплотняется (`CompactStack`), визуал уезжает DOTween-shrink и
возвращается в пул по despawn-контракту.

---

## 5. Прогрессия вместимости (LevelConfig)

`PlayerCarry` **не хранит** прогрессию — она задана в данных уровня.

1. Откройте каждый ассет `LevelConfig` (меню `Assets > Create > ShelfRush > Level`).
2. В секции **Progression** поле **Carry Capacity**:
   - L1 = 1, L2 = 2, L3 = 3, L4 = 5, L5 = 7.
3. Добавьте все `LevelConfig` в `GameBootstrap.levels`.

Во время уровня `PlayerCarry.Capacity` и `PlayerController.CarryCapacity` (plain C#
сервис) читают `ILevelManager.Current.CarryCapacity`. Если уровень не активен — фолбэк
на `PlayerConfig.carryCapacity` (по умолчанию 4).

> На старте уровня (`LevelStartedEvent`) `PlayerCarry` очищает руки (визуалы в пул),
> capacity перечитывается для нового уровня.

---

## 6. GameBootstrap (регистрация сервисов)

Используются уже существующие сервисы, регистрируемые в `GameBootstrap.Build()`:
- **`IPoolService`** → `LeanPoolService` (пул коробок);
- **`ILevelManager`** → `LevelManager` (источник capacity);
- **`PlayerConfig`** (данные), **`IEventBus`**.

В `GameBootstrap` дополнительно назначить (поля-ссылки на ассеты):
- **playerConfig** → ваш `PlayerConfig`;
- **levels** → массив всех `LevelConfig` (L1…L5);
- **products** → массив `ProductData` (для каталога).

Код менять не нужно — только заполнить ссылки.

---

## 7. Чек-лист / проверка

1. **Без сцены**: на префабе Player есть `PlayerCarry` + `CarryAnchor`; на `ProductData`
   заполнен `boxPrefab` с компонентом `Product`.
2. Рядом с игроком — тестовый объект с `ShelfInteractable` (+ Collider). Подойти в радиус,
   нажать **E / Enter** → `Count` растёт, на `carryAnchor` появляется коробка (DOTween-подъём из пула).
3. Заполнить руки до `Capacity` → следующие «взять» игнорируются (`IsFull`),
   `CanInteract` возвращает `false`.
4. `TryRemove`/`TryDrop` → коробка уезжает, возвращается в пул, стопка уплотняется.
5. Сменить уровень (L2 с `carryCapacity=2`) → можно нести 2 товара.
6. Профилировщик: многократный pickup/deliver не даёт утечек объектов (LeanPool переиспользует коробки).