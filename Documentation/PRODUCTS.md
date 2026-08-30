# PRODUCTS — Shelf Rush: data-driven Product System

> Дата: 30.08.2026 | Автор: Cline.
> Задача 05 (Documentation/CLINE_TASKS.md). Реализован **data-driven** Product System:
> данные в `ScriptableObject`, логика спавна/визуала — в тонких вью-компонентах,
> пулинг — через LeanPool. Сцена **не изменена**.

---

## 1. Обзор и архитектура

Продукт — центральная игровая сущность (категория → товар → runtime-экземпляр). Путь данных:

```
ProductCategory (SO, type-safe категория)
      │
      ▼
ProductData    (SO, статические данные товара)
      │
      ├── prefab / boxPrefab  ──► ProductSpawner ──(LeanPool)──► Product (runtime)
      │                                                                │
      └── visualSettings ─────────────────────────────────────────────► ProductVisual (визуал)
```

Принципы (см. `Documentation/ARCHITECTURE.md`, §10 — анти-паттерны):
- **Категория type-safe**: `ProductCategory` — это ссылка на `ScriptableObject`,
  **не строка**. Строковые поля (`displayName`, `Id`) существуют, но не используются
  для идентификации категории в игровой логике.
- **Data-driven**: все данные — в `ScriptableObject`; новый товар = новый ассет, без правки кода.
- **Runtime-спавн через LeanPool**: `ProductSpawner` использует `IPoolService`
  (обёртка над LeanPool, `ShelfRush.Pooling.LeanPoolService`). Если пул недоступен
  (prefab открыт без `GameBootstrap`) — аккуратный fallback на `Instantiate`/`Destroy`.

### File map (Assets/Scripts/Products/)

| Файл | Класс | Роль |
|---|---|---|
| `ProductCategory.cs` | `ProductCategory` (SO) | Type-safe категория (ссылка, не строка); имя+цвет для UI |
| `ProductData.cs` | `ProductData` (SO) + `VisualSettings` | Статические данные товара (см. §3) |
| `Product.cs` | `Product` (MonoBehaviour) | Runtime-экземпляр: ссылка на `ProductData`, сброс перед Despawn |
| `ProductVisual.cs` | `ProductVisual` (MonoBehaviour) | Визуал: масштаб+тон через `MaterialPropertyBlock` |
| `ProductSpawner.cs` | `ProductSpawner` (MonoBehaviour) | Спавн/деспавн товаров через `IPoolService` |
| `IProductCatalog.cs` | `IProductCatalog` | Каталог: `All`, `GetByCategory`, `TryFind`, `TryFindById` |
| `ProductCatalog.cs` | `ProductCatalog` | Реализация каталога (построена на бустрапе) |

---

## 2. ProductCategory (категория, type-safe)

```csharp
[CreateAssetMenu(fileName = "ProductCategory", menuName = "ShelfRush/Products/Category")]
public sealed class ProductCategory : ScriptableObject
```

Категория — это **отдельный ассет** (`ScriptableObject`). Идентификация категории — по ссылке
на объект (`ReferenceEquals`), поэтому завести новую категорию можно без компиляции/кода.
Строка `DisplayName` — только для UI (ценники, фильтры), не для маппингов.

Поля:
- `displayName` — название для UI;
- `color` — цвет категории (подсветка полок/ценников).

> Новая категория надёжно работает и при переименовании ассета: `displayName` на `OnValidate`
> берётся из имени файла, если пуст.

---

## 3. ProductData (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "ProductData", menuName = "ShelfRush/Products/Product")]
public sealed class ProductData : ScriptableObject
```

Поля (в инспекторе разбиты на секции):

| Группа | Поле | Тип | Назначение |
|---|---|---|---|
| Identity | `id` | `string` | Стабильный код **для сохранений** (авто-генерация GUID) |
| Identity | `displayName` | `string` | Отображаемое имя (UI/подсказки) |
| Identity | `category` | `ProductCategory` | **Type-safe категория** (ссылка, не строка) |
| Identity | `icon` | `Sprite` | Иконка для UI |
| Prefabs | `prefab` | `GameObject` | Runtime-префаб товара (полка/руки) |
| Prefabs | `boxPrefab` | `GameObject` | Префаб «коробки» товара; если пуст — `prefab` |
| Reward | `rewardValue` | `int` | Награда (монеты) за доставку |
| Visual Settings | `visualSettings` | `VisualSettings` | Масштаб/тон/смещения (см. ниже) |

Публичные свойства: `Id`, `DisplayName`, `Category`, `Icon`, `Prefab`, `BoxPrefab`
(возвращает `boxPrefab` или `prefab`), `RewardValue`, `VisualSettings`.

`VisualSettings` (вложенный сериализуемый класс):
- `Scale` — масштаб runtime-префаба,
- `Tint` — базовый тон (применяется через `MaterialPropertyBlock`, без клонов материалов),
- `StackOffset` — шаг стекирования между экземплярами (например, вверх по Y),
- `ShelfOffset` — базовое смещение товара от точки спавна (над полкой).

Авторасстановка в `OnValidate`: пустой `id` → GUID; пустой `displayName` → имя ассета;
`boxPrefab` без `prefab` → скопируется, и наоборот.

> Замечание: ранее поле `basePrice` **убрано** (нигде не использовалось) — заменено на
> `rewardValue` по требованиям задачи.

---

## 4. Runtime: Product, ProductVisual, ProductSpawner

### Product (MonoBehaviour)
Тонкая связка «GameObject ⇄ ProductData». Свойства: `Data`, `Id`, `DisplayName`, `Category`,
`RewardValue`, `Prefab`, `BoxPrefab`. Методы:
- `Setup(ProductData)` — назначить данные, применить визуал, переименовать объект.
- `ResetState()` — сброс перед возвратом в пул (отвязка данных, сброс тона).

### ProductVisual (MonoBehaviour)
Вью визуала: `Apply(ProductData)` (масштаб + `tint`), `SetTint(Color)` / `ResetTint()`
для подсветки. Перекраска через `MaterialPropertyBlock` — pooling-safe. В `OnDisable`
возвращается базовый тон (reset state).

### ProductSpawner (MonoBehaviour)
Единственная точка создания/уничтожения товаров. API:
- `Spawn(ProductData, Vector3 position, Transform parent=null) : Product` — спавн `prefab`,
- `SpawnBox(ProductData, position, parent)` — спавн `boxPrefab` (или `prefab`),
- `SpawnRaw(GameObject, position, parent)` — сырой спавн, `DespawnRaw(GameObject)`,
- `Despawn(Product)` — вызов `ResetState()` + возврат в пул.

Размещается на объекте-спавнере (полка/точка выдачи) как prefab; новых объектов в сцену
не добавляет. Пул резолвится лениво через `GameBootstrap.Instance.Services.TryGet<IPoolService>()`;
если бутстрап не запущен — fallback на `Instantiate`/`Destroy`.

---

## 5. Пошаговое создание

### 5.1 Создать категорию (ProductCategory)
1. В Project: `Assets/Settings` → ПКМ → `Create/ShelfRush/Products/Category`.
2. Название файла = ключ категории (например, `Drinks`, `Food`, `Household`).
3. В инспекторе задать `Display Name` (для UI) и `Color`.

Примеры категорий под товары из ТЗ:
- **Drinks** → Water, Juice, Milk
- **Food** → Pasta, Chips
- **Household** → Detergent

### 5.2 Создать префаб товара
Для **каждого** товара — свой префаб под `prefab` (и, опционально, `boxPrefab`):
1. Создайте `GameObject` (3D-примитив/модель/`PolyOne`) с `Collider` и `Renderer`.
2. Добавьте компонент **`Product`** (корень) и, если нужен собственный визуал, дочерний
   объект с **`ProductVisual`** (Renderer'ы заполнятся автоматически).
3. Перетащите в `Assets/Prefabs` → новый **Prefab**.

> `prefab` и `boxPrefab` могут быть одинаковыми, если коробка не выделяется.

### 5.3 Создать ProductData (товар)
1. В Project: ПКМ → `Create/ShelfRush/Products/Product`. Назовите, например, `PD_Water`.
2. Заполните:
   - **displayName** = «Вода» / «Water», **category** = категория (см. 5.1),
     **icon** = спрайт.
   - **prefab** = префаб из 5.2; **boxPrefab** = коробка (или пусто).
   - **rewardValue** = награда за доставку (см. §5.4).
   - **Visual Settings**: `Scale` (обычно `1`), `Tint` (белый = как в префабе),
     `StackOffset` (между товарами в стопке), `ShelfOffset`.

### 5.4 Reward (награда)
`ProductData.rewardValue` — количество монет, которое получит игрок при успешной доставке
этого товара. Выдаётся через экономику при `CustomerOrderCompletedEvent` (см.
`Documentation/ARCHITECTURE.md`, экономика/уровни). Больше за редкий/дорогой товар
(например: Water=5, Juice=7, Milk=9, Pasta=12, Chips=6, Detergent=15).

### 5.5 Зарегистрировать в каталоге
1. Откройте объект с **`GameBootstrap`** (данные).
2. В поле **`products`** (массив `ProductData[]`) добавьте все созданные ProductData.
3. `ProductCatalog` построится при старте; категории доступны через `IProductCatalog.GetByCategory`.

### 5.6 Использовать спавнер
На объекте полки/точки выдачи добавьте **`ProductSpawner`** и спавньте товары, например
`_spawner.Spawn(productData, transform.position + shelfOffset)`; при уборке —
`_spawner.Despawn(product)`.

---

## 6. Примеры товаров из ТЗ

| Товар | Категория | rewardValue | Префаб (`prefab`) |
|---|---|---|---|
| Water | Drinks | 5 | `Prefabs/P_Water` |
| Juice | Drinks | 7 | `Prefabs/P_Juice` |
| Milk | Drinks | 9 | `Prefabs/P_Milk` |
| Pasta | Food | 12 | `Prefabs/P_Pasta` |
| Chips | Food | 6 | `Prefabs/P_Chips` |
| Detergent | Household | 15 | `Prefabs/P_Detergent` |

> Конкретные `.asset`-файлы и `Prefabs/P_*` создаются в редакторе Unity по шагам выше
> (сцена не изменяется; новые префабы/ассеты кладутся в свои папки).

---

## 7. Pooling и «reset перед Despawn»

`ProductSpawner.Despawn` вызывает `Product.ResetState()` (отвязка данных) и затем
`IPoolService.Despawn`. `ProductVisual` на `OnDisable` возвращает базовый тон — это
удовлетворяет контракту «перед Despawn: reset state, reset transform, unsubscribe events».

Проверка: включить профилировщик, многократно спавн/деспавн товаров — не должно быть
утечек объектов и роста аллокаций сверх LeanPool.