# SHELVES_SETUP — Shelf Rush: настройка полок (Shelf System)

> Дата: 05.09.2026 | Автор: Cline.
> Задача 07 (Documentation/CLINE_TASKS.md). Пошаговое руководство: создать ассет полки,
> ProductCategory, товар; собрать префаб полки и настроить родительские данные уровня.
> **Сцена не изменяется** — всё делается через ассеты и префабы.

---

## 0. Что нужно для работы Shelf System

1. **`ProductCategory`** (SO) — категория, по которой проверяется `Category == AllowedCategory`.
2. **`ProductData`** (SO) — товар с полным составом (`category`, `prefab`, `rewardValue`, `visualSettings`).
3. **`ShelfData`** (SO) — настройки полки (`AllowedCategory`, `Capacity`, `Slots`, `InteractionRadius`).
4. **Префаб полки** — объект с компонентами `ShelfController` + `ShelfInteraction` + `Collider`.
5. **`LevelConfig`** — массив полок (для прогресса уровня) и `availableProducts`.

Поток игрового момента:
`Игрок подходит в радиус → (E/Tap) → ShelfInteraction.Interact → ShelfController.TryPlace`
`→ категория ок и есть слот? → разместить + reward + progress | иначе отказ (feedback).`

---

## 1. ProductCategory (категория)

1. Меню **Assets > Create > ShelfRush > Products > Category**.
2. Назовите, например `C_Food`, `C_Drinks`.
3. В инспекторе задайте `Display Name` (для UI) и `Color` (для UI — не геймплей).

> Уже существуют в проекте: `C_Food`, `C_Drinks` (Assets/Settings/ProductCategory).

---

## 2. ProductData (товар)

1. Меню **Assets > Create > ShelfRush > Products > Product**.
2. В `category` назначьте нужную `ProductCategory`.
3. В `prefab` — префаб товара (с компонентом `Product`, опц. `ProductVisual`); в `boxPrefab` — коробку в руках.
4. `rewardValue` — награда в монетах, выдаётся за успешное размещение.
5. `visualSettings` — масштаб/тон/`ShelfOffset` (смещение товара над слотом).

> Критично: без `prefab` визуал на полке не появится (размещение пройдёт «в логике», без картинки).

---

## 3. ShelfData (полка) — основной ассет

1. Меню **Assets > Create > ShelfRush > Shelf**.
2. В секции **Placement (Shelf System)**:
   - **Allowed Category** → ваша `ProductCategory` (например `C_Food`);
   - **Slots** → массив локальных позиций слотов от корня полки
     (например `(0,0.6,0)`, `(0.4,0.6,0)`, `(-0.4,0.6,0)`). Если пусто — слоты
     раскидываются автоматически по линии.
   - **Interaction Radius** → радиус досягаемости (обычно 2–3, как у игрока).
3. **Common → Capacity** → сколько слотов задействуется (не больше числа `Slots`, минимум 1).
4. Секцию **Stock (legacy)** — `Product` — не заполняйте, если полка используется для размещения.

Правило: полка в режиме **Placement** определяется `AllowedCategory`; в режиме **Stock** — `Product`.
Не смешивайте обе роли в одном ассете без необходимости.

---

## 4. Префаб полки (ShelfController + ShelfInteraction)

1. Создайте `GameObject` (модель полки, короб/мебель).
2. Добавьте **Add Component > ShelfRush > Shelves > Shelf Controller**:
   - **Data** → ваш `ShelfData`;
   - (опц.) **Slots Root** → пустой дочерний `Transform` (родитель слотов);
   - (опц.) **Spawner** → оставьте пустым, `ProductSpawner` подтянется/создастся автоматически.
   - **Use Manual Slots** → `false` (авто) или `true` (ручная настройка, см. §4.1).
3. Добавьте **Add Component > ShelfRush > Shelves > Shelf Interaction**:
   - **Controller** → оставьте пустым (подтянется `ShelfController` с этого объекта);
   - **Interaction Prompt** → текст подсказки UI.
4. Добавьте **Collider** (Box/Sphere, `Is Trigger` опционально), чтобы
   `PlayerInteraction` находил полку через `Physics.OverlapSphere` и `GetComponent<IInteractable>`.

> По умолчанию (`Use Manual Slots = false`) `ShelfController` создаёт дочерние `ShelfSlot_{i}`
> сам в `Awake` из данных полки — вручную их создавать не нужно.
> Для ручной настройки см. §4.1.

Проверьте раскладку:
- Ассет валиден: `AllowedCategory` != null;
- `Slots` достаточно для `Capacity` (или Slots пусто → авто-раскидка);
- префаб полки имеет `Collider` в пределах `InteractionRadius` от игрока.

### 4.1 Ручная настройка слотов (Use Manual Slots)

Включите **`Use Manual Slots = true`** на `ShelfController`, если хотите расставить точки размещения
товаров **вручную в сцене/префабе** (а не получать их автоматически из `ShelfData.Slots`).

1. Убедитесь, что у `ShelfController` в инспекторе есть кнопки **«+ Добавить слот (child)»** и
   **«Синхрон. порядок / имена слотов»** (появляются при выбранной полке).
2. Нажмите **«+ Добавить слот (child)»** ровно `Capacity` раз — создастся дочерние объекты
   `ShelfSlot_0..N` под `Slots Root` (или под полкой) с компонентом `ShelfSlot`.
3. В окне Scene переместите каждый `ShelfSlot_*` на нужное место на поверхности полки (куда ложатся
   товары). Отладочный gizmo/имя объекта помогает ориентироваться.
4. Нажмите **«Синхрон. порядок / имена слотов»** — приведёт имена и порядок (индексы `0..N`)
   к порядку в иерархии.
5. В `Awake` `ShelfController` использует эти вручную расставленные слоты (их позиции из сцены),
   до `ShelfData.Capacity` штук. Ручные слоты при пересборке не удаляются.

> Удобство ручного режима: слоты видны и расставляются в редакторе заранее (авто-слоты видны только
> в Play Mode). Это помогает точно «посадить» товары на модель полки.
---

## 5. LevelConfig (уровень)

1. Откройте/создайте `LevelConfig` (Assets > Create > ShelfRush > Level).
2. **Shelves** → добавьте `ShelfData` полок этого уровня (для прогресса и количества).
3. **Available Products** → товары уровня (для каталога/заказов).
4. **Carry Capacity** → сколько товаров игрок может нести (прогрессия, см. CARRY_SETUP).
5. Добавьте все `LevelConfig` в **GameBootstrap.levels**, а `ProductData` — в **GameBootstrap.products**.

Прогресс уровня (`LevelManager.PlacementProgress`) автоматически = `PlacedProducts / TotalShelfCapacity`
(сумма `ShelfData.SlotCapacity` по полкам уровня). Обновляется событием `PlacementProgressChanged`.

---

## 6. Как работает взаимодействие (точки настройки)

- Игрок сканирует радиус `PlayerConfig.InteractionRadius` и ищет `IInteractable`
  (`PlayerInteraction.SearchForTarget`). Полка находит её через компонент `ShelfInteraction`
  (Collider обязателен).
- `ShelfInteraction.CanInteract` возвращает true, если полка не полна и у игрока что-то есть в руках.
- По кнопке/тапу `ShelfInteraction.Interact` размещает товары из рук, снимая с игрока
  только реально размещённые (остаток остаётся).

---

## 7. Чек-лист/проверка

1. **Категории**: `C_Food`, `C_Drinks` существуют.
2. **ProductData**: у товара назначен `category`, `prefab`, `rewardValue`.
3. **ShelfData**: `AllowedCategory` совпадает с категорией товара; `Capacity <= Slots.Length` (или Slots пусто).
4. **Префаб полки**: `ShelfController` + `ShelfInteraction` + `Collider`.
   (Опц. зона: дочерний `InteractionZone` из **Plane** + `ShelfInteractionZone (Plane)`.)
5. **Уровень**: `ShelfData` добавлены в `LevelConfig.shelves`; `LevelConfig` — в `GameBootstrap.levels`.
6. **В рантайме**:
   - Подойти с правильным товаром → разместилось, `CurrentAmount++`, зелёный highlight + bounce, +монеты.
   - Подойти с неверным товаром → отказ (негативный «тряс»), товар остался в руках.
   - Заполнить полку → `OnShelfCompleted`; попытка дальше → `OnShelfFull`, остаток в руках.

---

## 8. Частые проблемы

| Симптом | Причина / решение |
|---|---|
| Полка не находится игроком | Нет `Collider` на префабе полки / вне `InteractionRadius`. |
| Вхожу в зону, но товары не выкладываются | Зона раньше полагалась на `OnTriggerEnter` (физику) — теперь она сама проверяет близость. Проверьте: `Auto Place On Enter = true`, зона это `ShelfInteractionZone (Plane)`, `Controller` указывает на `ShelfController`, у игрока есть товары в руках, позиция игрока внутри BoxCollider зоны. |
| `ShelfController` не размещает | `ShelfData.AllowedCategory` = null или не совпадает с категорией товара. |
| Нет визуала на полке | `ProductData.prefab` пуст; проверьте `product.VisualSettings.ShelfOffset` (товар может быть «в полке»). Ранее товар мог «не рисоваться», т.к. стартовый масштаб был 0 в ожидании твина — теперь товар спавнится сразу с правильным масштабом (`VisualSettings.Scale`). |
| Товар невидим во время «перелёта» | Это нормально было раньше (scale=0 в полёте). Сейчас товар виден всегда — от позиции игрока до слота. |
| Не начисляется reward | Точно не `CustomerOrderCompleted` — reward за размещение выдаётся на `ShelfProductPlacedEvent`; проверьте `rewardValue` > 0. |
| Прогресс уровня не растёт | `ShelfData` не добавлены в `LevelConfig.shelves`; `TotalShelfCapacity` = 0. |
| Слоты не видны вручную | Это норм: `ShelfSlot` создаются автоматически в `Awake` (runtime-дети полки). |

---

## 9. Interaction Zone — «наступил → товар перелетел на полку» (опционально)

Вместо/в дополнение к кнопочному `ShelfInteraction` можно добавить **зону из объекта Plane** перед
полкой: игрок просто заходит в неё, и товары из рук **перелетают** на полку
(`ShelfController.TryPlaceFlying` → DOTween от игрока к свободному слоту с bounce). Подсветки нет.

### 9.1 Как настроить
1. Создайте **`GameObject > 3D Object > Plane`** и расположите его на полу **перед полкой**
   (плоскость = область зоны). Размер зоны настраивается **масштабом** Plane (ширина/глубина).
2. Добавьте `Add Component > ShelfRush > Shelves > Shelf Interaction Zone (Plane)`.
3. Компонент заменит `MeshCollider` плоскости (он concave и не может быть Trigger) на
   `BoxCollider` по габаритам меша с небольшой толщиной по высоте и пометит его `IsTrigger=true`.
   Размер зоны при этом по-прежнему настраивается **масштабом** Plane.

Инспектор `ShelfInteractionZone`:
| Поле | Значение |
|---|---|
| `Controller` | пусто → авто `GetComponentInParent<ShelfController>` |
| `Auto Configure Collider` | `true` — заменить MeshCollider на BoxCollider (concave не может быть Trigger) |
| `Auto Place On Enter` | `true` — выкладывать, когда игрок стоит внутри зоны |
| `Fly Delay Step` | `0.12` — задержка между перелётами отдельных товаров |
| `Scan Interval` | `0.1` — как часто проверять, что игрок внутри (независимо от физики) |

### 9.2 Как работает
- Зона **периодически** (по умолчанию каждые `0.1 с`) проверяет, находится ли позиция игрока
  внутри её `BoxCollider` (точность не привязана к физическим триггерам, поэтому **Rigidbody на
  игроке не требуется** — игрок движется transform'ом).
- Когда игрок внутри и в руках есть товар (`PlayerCarry` не пуст) — товары «перелетают» на полку:
  `ShelfController.TryPlaceFlying(item, playerPos, delay)` для каждого товара в руках.
- Подходящий товар рисуется у игрока и DOTween-ом (перелёт + рост `OutBack`) садится в свободный
  слот; размещённое снимается с рук (`PlayerCarry.TryDrop`). Полка/reward/progress — как обычно.
- Неподходящая категория / полка заполнилась → товар **остаётся в руках**. Остаток после
  частичной выкладки можно донести, выйдя и зайдя снова (или взяв новые товары внутри зоны).
- Игровое решение принимает `ShelfController` по данным (никакой подсветки/цветов).

### 9.3 Итоговая иерархия префаба полки (с зоной)
```
Shelf_Food
├── ShelfController        (Data = ShelfData)
├── ShelfInteraction        (кнопка, опционально)
├── [model mesh + Collider]
└── InteractionZone (Plane)   ← GameObject > 3D Object > Plane, перед полкой
    └── ShelfInteractionZone (Plane)  (+ MeshCollider, IsTrigger)
```