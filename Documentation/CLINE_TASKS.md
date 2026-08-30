# CLINE_TASKS.md

# SHELF RUSH — CLINE IMPLEMENTATION TASKS

Единый набор задач для разработки Unity-проекта Shelf Rush.

Жанр: Hybrid Casual / Idle Arcade / Light Management Simulator
Платформы: PC / Mobile / WebGL / Yandex Games
Стиль: Stylized 3D / Premium Hybrid Casual / HyperCasual-inspired

**Порядок выполнения:** задачи выполняются строго последовательно. После каждой задачи Cline останавливается и ждёт следующей команды.

---

# 0. MASTER RULES — ДЛЯ ВСЕХ ЗАДАЧ

Ты работаешь над существующим Unity-проектом Shelf Rush.

## Критические правила

1. НЕ изменяй существующие сцены без прямого разрешения пользователя.
2. Не удаляй существующие GameObject, компоненты, скрипты, материалы, Prefab или настройки.
3. Не создавай дубликаты существующих систем.
4. Перед написанием кода изучай существующий проект.
5. Найди аналогичные системы и переиспользуй их.
6. Не переписывай работающий код без необходимости.
7. Если архитектура конфликтует с задачей — сначала объясни конфликт.
8. После изменений проверяй Unity Console и исправляй compile errors.
9. Задача не считается выполненной, пока проект не компилируется без ошибок.
10. Не изменяй сцену для удобства разработки.
11. Новые системы подключай через Prefab, ScriptableObject, configuration или runtime initialization.
12. Не изменяй Project Settings без необходимости и подробного отчёта.
13. Не добавляй новую зависимость, если существующая уже решает задачу.
14. Не используй временные hacks.
15. Не скрывай ошибки.

## Кроссплатформенность

Поддерживать:
- PC
- Mobile
- WebGL
- Yandex Games

Gameplay не должен зависеть от платформы.

Не создавать отдельную gameplay-логику для PC и Mobile.

Platform-specific код изолировать и использовать условную компиляцию Unity только там, где это необходимо.

## Input

PC:
- WASD
- Arrow Keys
- Mouse при необходимости

Mobile:
- Virtual Joystick / Touch

WebGL:
- Keyboard
- Mouse
- Touch при наличии

Yandex Games:
- WebGL-compatible implementation

PlayerMovement не должен напрямую читать Keyboard, Touch, Joystick или Input.GetAxis. Использовать абстракцию input.

## Пакеты

Использовать существующие:
- DOTween
- LeanPool
- TextMeshPro
- Yandex Games SDK / Plugin

### DOTween
Для UI, pickup/placement feedback, scale/bounce, reward animations, progress и camera feedback.

Не использовать DOTween как замену physics/movement.

### LeanPool
Для часто создаваемых объектов:
- Products
- Boxes
- Customers
- VFX
- Floating rewards
- temporary gameplay objects

### TextMeshPro
Для HUD, UI, reward text, world-space feedback и notifications.

### Yandex
Изолировать:
- initialization
- save/load
- ads
- rewarded ads
- interstitial ads
- server time
- fullscreen
- localization hooks
- game ready

## Архитектура

Использовать:
- SOLID
- separation of concerns
- ScriptableObjects для данных
- interfaces
- events
- dependency injection там, где оправдано
- object pooling
- data-driven design
- platform abstraction

Не использовать:
- FindObjectOfType в Update
- постоянный GetComponent в Update
- string для ProductCategory
- hardcoded product/shelf logic
- God Object
- огромные MonoBehaviour
- static global state без необходимости
- Instantiate/Destroy для объектов, которые должны быть pooled
- gameplay-код внутри UI
- Yandex API непосредственно в PlayerController

## Документация

Создать и поддерживать:

Documentation/
- PROJECT_OVERVIEW.md
- PROJECT_AUDIT.md
- ARCHITECTURE.md
- GAMEPLAY.md
- PLAYER.md
- PRODUCTS.md
- SHELVES.md
- LEVELS.md
- ECONOMY.md
- CUSTOMERS.md
- UI.md
- SAVE_SYSTEM.md
- YANDEX.md
- CROSS_PLATFORM.md
- WEBGL.md
- PERFORMANCE.md
- TECHNICAL_GUIDELINES.md
- QA_CHECKLIST.md
- PRODUCTION_READINESS.md
- CHANGELOG.md
- TODO.md

После каждой задачи:
1. Обновить соответствующую документацию.
2. Добавить изменения в CHANGELOG.md.
3. Добавить оставшиеся TODO в TODO.md.
4. Обновить инструкции подключения при необходимости.
5. Документация должна соответствовать фактическому коду.

## Формат отчёта Cline

После каждой задачи вывести:
1. Что изучено.
2. Что изменено.
3. Какие файлы созданы.
4. Какие файлы изменены.
5. Какие зависимости используются.
6. Как подключить систему.
7. Inspector settings.
8. Настройка Prefab.
9. Настройка ScriptableObject.
10. Как проверить.
11. Найденные ошибки.
12. Исправленные ошибки.
13. Что обновлено в документации.
14. Оставшиеся TODO.

После отчёта остановиться.

---

# ЗАДАЧА 01 — АУДИТ ПРОЕКТА

Изучи существующий Unity-проект Shelf Rush. Пока ничего не изменяй.

Изучи:
- Assets
- Scripts
- Scenes
- Prefabs
- ScriptableObjects
- Packages
- asmdef
- Project Settings
- Input System
- Tags/Layers
- материалы
- UI
- существующую архитектуру
- Player systems
- gameplay systems

Найди:
1. Player Controller
2. Player Movement
3. Input
4. Interaction
5. Product systems
6. Shelf systems
7. Level systems
8. Economy
9. Save
10. UI
11. Yandex integration
12. DOTween
13. LeanPool
14. TMPro

Проверь:
- compile errors
- missing references
- duplicate systems
- архитектурные проблемы
- WebGL проблемы
- Mobile проблемы
- Yandex проблемы

Создай:
Documentation/PROJECT_AUDIT.md

Обнови:
Documentation/ARCHITECTURE.md
Documentation/TODO.md

Опиши реализованное, отсутствующее, проблемы, риски и рекомендации.

Не исправляй найденные проблемы на этой задаче.

---

# ЗАДАЧА 02 — БАЗОВАЯ АРХИТЕКТУРА

На основе PROJECT_AUDIT.md разработай базовую архитектуру.

Не изменяй сцену.

Разделы:
- Core
- Player
- Input
- Products
- Shelves
- Levels
- Economy
- Customers
- UI
- Save
- Platform
- Pooling

Определи:
- ответственность модулей
- зависимости
- interfaces
- events
- data flow
- lifecycle
- initialization order

Обнови Documentation/ARCHITECTURE.md.

Не создавай лишние MonoBehaviour.

Проверь compile errors, namespaces и assembly references.

---

# ЗАДАЧА 03 — CROSS-PLATFORM INPUT

Реализуй единую систему управления.

Не изменяй сцену.

Создай:
IPlayerInput

Архитектура:
Input Provider
→ IPlayerInput
→ PlayerController
→ PlayerMovement

PlayerMovement не должен напрямую читать input.

PC:
- WASD
- Arrow Keys

Mobile:
- Virtual Joystick / Touch

WebGL:
- Keyboard
- Mouse/Touch при необходимости

Если Unity Input System уже используется — переиспользуй его. Не создавай второй input framework.

Если нужен Mobile joystick:
- создать Prefab
- не добавлять его в сцену
- описать ручное подключение

Добавить:
- dead zone
- sensitivity
- input smoothing
- normalized input
- platform-independent movement

Обновить:
- CROSS_PLATFORM.md
- PLAYER.md
- CHANGELOG.md

---

# ЗАДАЧА 04 — PLAYER CONTROLLER

Реализуй Player Controller.

Не изменяй сцену.

Архитектура:
- PlayerController
- PlayerMovement
- PlayerInteraction
- PlayerCarry
- PlayerAnimator

PlayerMovement:
- movement
- speed
- acceleration
- deceleration
- rotation
- stopping

PlayerInteraction:
- interaction radius
- detection
- interaction state

PlayerCarry:
- capacity
- add
- remove
- clear

PlayerAnimator:
- idle
- walk
- carry
- interaction

Создай PlayerConfig : ScriptableObject:
- moveSpeed
- acceleration
- rotationSpeed
- interactionRadius
- carryCapacity
- pickupDuration
- placementDuration

Не хранить значения hardcoded.

DOTween использовать только для визуальных feedback/анимаций.

Подробно описать Player Prefab, компоненты, ссылки, Inspector и проверку PC/Mobile.

Обновить PLAYER.md.

---

# ЗАДАЧА 05 — PRODUCT SYSTEM

Реализуй data-driven Product System.

Не изменяй сцену.

Создай:
- ProductCategory
- ProductData
- Product
- ProductVisual
- ProductSpawner

ProductData — ScriptableObject.

Поля:
- ID
- displayName
- category
- icon
- prefab
- boxPrefab
- rewardValue
- visual settings

Категория type-safe. Не использовать string.

Примеры:
- Water
- Juice
- Pasta
- Milk
- Detergent
- Chips

Каждый Product использует ProductData.

LeanPool использовать для runtime spawning там, где это соответствует архитектуре.

Подробно описать создание ProductData, нового продукта, prefab, категории и reward.

Обновить PRODUCTS.md.

---

# ЗАДАЧА 06 — PICKUP / CARRY SYSTEM

Реализуй подбор и переноску.

Не изменяй сцену.

Flow:
Approach → Detect → Pickup → Add To Carry → Carry → Deliver → Remove From Carry

Capacity:
- Level 1 = 1
- Level 2 = 2
- Level 3 = 3
- Level 4 = 5
- Level 5 = 7

Progression не хранить внутри PlayerCarry.

Поддержать:
- pickup
- drop
- remove
- capacity check
- multiple products
- visual stack

DOTween использовать для визуального перемещения.
LeanPool — для pooled objects.

Перед Despawn:
- Kill tweens
- reset state
- unsubscribe events
- reset transform
- clear product data

Обновить PLAYER.md и PRODUCTS.md.

---

# ЗАДАЧА 07 — SHELF SYSTEM

Реализуй Shelf System. Это критическая система.

Не изменяй сцену.

Создай:
- ShelfData
- ShelfController
- ShelfSlot
- ShelfInteraction

Shelf:
- AllowedCategory
- Capacity
- CurrentAmount
- Slots
- InteractionRadius

Главная проверка:
ProductData.Category == ShelfData.AllowedCategory

TRUE → allow placement
FALSE → block placement

Неправильный товар:
- не удалять
- не уничтожать
- не размещать
- не выдавать reward
- не увеличивать progress
- оставить у игрока

Правильный:
- найти свободный slot
- разместить
- CurrentAmount++
- OnProductPlaced
- update LevelProgress
- reward

Если Shelf Full — placement запрещён.

При нескольких товарах размещать только доступное количество, остаток оставить у игрока.

Events:
- OnProductPlaced
- OnPlacementFailed
- OnCategoryMismatch
- OnShelfFull
- OnShelfCompleted

Feedback:
- correct = highlight / bounce
- wrong = короткий негативный feedback

Gameplay logic не должна зависеть только от UI/цветов.

Обновить SHELVES.md.

---

# ЗАДАЧА 08 — PRODUCT CATEGORY VALIDATION

Реализуй отдельную проверку категорий.

Примеры:
- Water → Drinks = VALID
- Milk → Dairy = VALID
- Pasta → Grocery = VALID
- Water → Grocery = INVALID
- Milk → Drinks = INVALID
- Detergent → Grocery = INVALID

Создай:
- IProductPlacementValidator
- ProductPlacementValidator

Метод:
CanPlace(ProductData product, ShelfData shelf)

Вернуть структурированный результат:
- allowed
- reason
- category mismatch
- shelf full
- invalid product
- invalid shelf

Архитектура должна позволять:
- VIP shelves
- special products
- temporary shelves
- quests
- bonuses

Не изменять сцену.

Обновить SHELVES.md.

---

# ЗАДАЧА 09 — FIRST LEVEL

Реализуй gameplay logic первого уровня.

Не изменяй сцену.

Создать:
- LevelData
- LevelManager
- LevelProgress
- LevelObjective

Level 1:
- Goal: Fill Drinks Shelf
- Target: 10
- Product: Water
- Shelf: Drinks

Flow:
Pickup → Carry → Navigate → Place → Reward → Progress → Repeat → Complete

Progress:
0/10 ... 10/10

После 10/10:
Shelf Complete → Level Complete → Reward

Все параметры в LevelData.

Не хардкодить уровень в MonoBehaviour.

Подробно описать создание и подключение LevelData.

Обновить LEVELS.md.

---

# ЗАДАЧА 10 — ПЕРВЫЕ 5 УРОВНЕЙ

Создай data-driven систему для 5 уровней.

Не изменяй сцену.

Level 1:
Drinks, 10 products

Level 2:
Drinks, 20 products, Carry upgrade

Level 3:
Drinks + Grocery, 30 products

Level 4:
Drinks + Grocery + HouseholdChemicals, 40 products, Category Validation

Level 5:
Multiple categories, First Customers, Sales System

Создать:
- LevelData
- LevelObjectiveData
- RewardData

Один LevelManager работает со всеми уровнями.

Не создавать отдельную gameplay-логику для каждого уровня.

Обновить LEVELS.md.

---

# ЗАДАЧА 11 — ECONOMY

Реализуй Economy System.

Валюта:
Coins

Создать:
- CurrencyManager
- RewardCalculator
- RewardData
- EconomyConfig

Источники:
- Product Placement
- Shelf Completion
- Level Completion
- Customer Purchase
- Mission
- Idle Income

Начальные значения:
- Product Placement = 10
- Shelf Complete = 100
- Level Complete = 500

Не hardcode.

CurrencyManager:
- AddCoins
- RemoveCoins
- CanAfford
- GetBalance

Events:
- OnCurrencyChanged
- OnRewardGranted

UI не должен напрямую управлять Economy.

Обновить ECONOMY.md.

---

# ЗАДАЧА 12 — UPGRADES

Реализуй Upgrade System.

Апгрейды:
- Speed
- CarryCapacity
- StockingSpeed
- InteractionRadius

Создать:
- UpgradeData
- UpgradeManager
- UpgradeLevelData

Каждый Upgrade:
- ID
- maxLevel
- cost per level
- value per level
- description
- icon

UpgradeManager не должен знать UI.

Пример Speed:
- Level 1 = 3
- Level 2 = 3.5
- Level 3 = 4
- Level 4 = 4.5
- Level 5 = 5

Carry:
1 → 2 → 3 → 5 → 7

Все значения конфигурируемые.

Обновить ECONOMY.md и PLAYER.md.

---

# ЗАДАЧА 13 — UI

Реализуй базовый UI.

Использовать TextMeshPro.

Не изменяй существующую сцену.

HUD:
- Level
- Coins
- Objective
- Progress
- Carry amount

Screens:
- Level Complete
- Reward
- Upgrade

UI получает данные через events.

Не делать:
UI → Player

Делать:
Gameplay → Event → UI

DOTween:
- popup
- reward
- progress
- coin animation
- level complete
- button feedback

Подробно описать подключение.

Обновить UI.md.

---

# ЗАДАЧА 14 — CUSTOMERS

Реализуй Customer System.

Не изменяй сцену.

Создать:
- CustomerData
- CustomerController
- CustomerSpawner
- CustomerNeed
- CustomerState

States:
- Spawn
- WalkToShelf
- CheckProduct
- Buy
- Leave
- Satisfied
- Unavailable

Покупатель ищет нужный товар/категорию.

Если товар есть → Purchase.
Если нет → Leave.

Использовать LeanPool.

Не использовать Instantiate/Destroy на каждый spawn.

Обновить CUSTOMERS.md.

---

# ЗАДАЧА 15 — IDLE ECONOMY

Реализуй базовую Idle Economy.

Создать:
- StoreIncomeData
- StoreIncomeManager

Параметры:
- coinsPerMinute
- maxOfflineTime
- incomeMultiplier

Игрок получает offline reward.

Не использовать только Time.deltaTime для offline progression.

Архитектура должна позволять использовать серверное время Yandex.

Не делать полноценный anti-cheat в MVP.

Обновить ECONOMY.md и SAVE_SYSTEM.md.

---

# ЗАДАЧА 16 — SAVE SYSTEM

Реализуй Save System.

Сохранять:
- current level
- coins
- upgrades
- unlocked stores
- player customization
- idle progression
- completed missions

Создать:
- SaveData
- SaveManager
- ISaveProvider

Архитектура:
Game → ISaveProvider → LocalSave / YandexSave

Editor/PC:
Local Save

Yandex:
Yandex cloud save, если SDK доступен

Не вызывать Yandex API из PlayerController.

Обновить SAVE_SYSTEM.md и YANDEX.md.

---

# ЗАДАЧА 17 — YANDEX GAMES PLATFORM

Реализуй platform abstraction.

Не изменяй сцену.

Создать:
- IPlatformService
- YandexPlatformService
- LocalPlatformService
- PlatformServiceFactory

Поддержать архитектурно:
- initialization
- save/load
- ads
- rewarded ads
- interstitial ads
- server time
- fullscreen
- localization hooks
- game ready

Gameplay обращается к IPlatformService, а не напрямую к Yandex API.

Editor → LocalPlatformService
Yandex/WebGL → YandexPlatformService

Использовать conditional compilation только при необходимости.

Подробно описать:
1. Установку Yandex plugin.
2. Проверку SDK.
3. Настройки.
4. Подключение PlatformService.
5. Editor testing.
6. WebGL testing.
7. Yandex testing.

Обновить YANDEX.md.

---

# ЗАДАЧА 18 — ADS

Реализуй архитектуру рекламы.

Не изменяй сцену.

Создать:
- IAdService
- AdService

API:
- ShowInterstitial()
- ShowRewarded(Action<bool> callback)

Rewarded:
- Double Reward
- Extra Coins
- Instant Restock
- Offline Bonus

Gameplay не должен зависеть от Yandex API.

Архитектура:
IAdService
→ YandexAdService
/
EditorMockAdService

EditorMockAdService должен позволять тестировать rewarded rewards без SDK.

Обновить YANDEX.md.

---

# ЗАДАЧА 19 — LEANPOOL AUDIT

Проведи аудит runtime объектов.

LeanPool использовать для:
- Products
- Boxes
- Customers
- VFX
- Floating rewards
- temporary gameplay objects

Не использовать pooling для одноразовых persistent объектов.

Проверить:
- Spawn
- Despawn
- Reset state
- Event unsubscribe
- DOTween Kill
- references cleanup

Перед возвратом pooled object:
- Kill/stop DOTween
- clear state
- unsubscribe events
- reset transform
- clear ProductData
- reset visual state

Не изменяй сцену.

Обновить ARCHITECTURE.md.

---

# ЗАДАЧА 20 — DOTWEEN AUDIT

Проведи аудит DOTween.

Использовать для:
- UI
- pickup
- placement
- reward popup
- shelf completion
- button feedback
- character feedback
- camera feedback

Не использовать как physics/movement system.

Каждая tween-анимация должна корректно завершаться при:
- disable
- despawn
- scene unload

Перед возвратом pooled object:
DOTween.Kill(target)
или безопасный эквивалент.

Не создавать бесконечные tweens без cleanup.

Обновить TECHNICAL_GUIDELINES.md.

---

# ЗАДАЧА 21 — FULL GAMEPLAY INTEGRATION

Свяжи:
- Player
- Product
- Carry
- Shelf
- Category Validation
- Level
- Economy
- Customer

Не переписывай существующие системы.

Flow:
Player
→ Pickup Product
→ Carry
→ Shelf Detection
→ Placement Validator
→ Shelf
→ Product Placed
→ Level Progress
→ Reward
→ Customer Purchase
→ Economy

Проверить:
- правильный товар
- неправильный товар
- заполненную полку
- несколько товаров
- покупателя
- отсутствие товара

Не изменяй сцену.

---

# ЗАДАЧА 22 — PERFORMANCE

Проведи performance audit.

Платформы:
- PC
- Mobile
- WebGL
- Yandex Games

Проверить:
- GC allocations
- Update loops
- Instantiate/Destroy
- Find calls
- GetComponent calls
- LINQ runtime usage
- reflection
- excessive events
- DOTween
- pooling

Особое внимание Mobile и WebGL.

Не делать преждевременную оптимизацию. Исправлять реальные bottlenecks.

Создать Documentation/PERFORMANCE.md.

Описать:
- проблемы
- исправления
- рекомендации
- profiling checklist

---

# ЗАДАЧА 23 — MOBILE UX

Проведи Mobile UX audit.

Не изменяй сцену.

Проверить:
- touch targets
- joystick
- UI scaling
- Canvas Scaler
- aspect ratios
- safe areas
- orientation
- resolution independence
- accidental touches
- input conflicts

UI должен быть удобен для пальца.

Gameplay input должен оставаться единым.

Обновить CROSS_PLATFORM.md и UI.md.

---

# ЗАДАЧА 24 — WEBGL / YANDEX BUILD

Подготовь проект к WebGL/Yandex.

Не изменяй сцену.

Проверить:
- WebGL compilation
- stripping
- managed code
- unsupported APIs
- threading
- reflection
- file access
- networking
- memory
- build size
- loading

Проверить Yandex integration.

Создать Documentation/WEBGL.md.

Описать:
- Build Settings
- Player Settings
- compression
- memory
- testing
- troubleshooting

Не менять настройки без необходимости.

Если требуется ручная настройка — указать точный путь в Unity Inspector.

---

# ЗАДАЧА 25 — QA

Проведи полный QA.

## PLAYER
- movement
- stop
- rotation
- PC
- Mobile

## PRODUCT
- pickup
- carry
- capacity
- pooling

## SHELF
- correct category
- wrong category
- full shelf
- partial placement

## LEVEL
- progress
- completion
- reward

## ECONOMY
- coins
- upgrades
- rewards

## CUSTOMERS
- spawn
- purchase
- unavailable product

## SAVE
- save
- load
- restart
- offline progression

## YANDEX
- initialization
- ads
- save
- platform fallback

## WEBGL
- compile
- run
- input
- memory

Не изменять сцену.

Создать Documentation/QA_CHECKLIST.md.

Все найденные ошибки исправить и повторить проверку.

---

# ЗАДАЧА 26 — ФИНАЛЬНАЯ ДОКУМЕНТАЦИЯ

Проведи полный аудит Documentation.

Проверить наличие:
- PROJECT_OVERVIEW.md
- PROJECT_AUDIT.md
- ARCHITECTURE.md
- GAMEPLAY.md
- PLAYER.md
- PRODUCTS.md
- SHELVES.md
- LEVELS.md
- ECONOMY.md
- CUSTOMERS.md
- UI.md
- SAVE_SYSTEM.md
- YANDEX.md
- CROSS_PLATFORM.md
- WEBGL.md
- PERFORMANCE.md
- TECHNICAL_GUIDELINES.md
- QA_CHECKLIST.md
- PRODUCTION_READINESS.md
- CHANGELOG.md
- TODO.md

Документация должна соответствовать реальному коду.

Не описывать несуществующие функции.

Если документация и код расходятся:
1. определить расхождение
2. исправить документацию или код
3. объяснить изменение

Добавить:
- ASCII architecture diagrams
- dependency flow
- setup instructions
- Inspector setup
- Prefab setup
- ScriptableObject setup
- testing
- troubleshooting

Обновить CHANGELOG.md.

Не изменять сцену.

---

# ЗАДАЧА 27 — PRODUCTION READINESS AUDIT

Проведи финальный Production Readiness Audit.

Платформы:
1. Windows/PC
2. Mobile
3. WebGL
4. Yandex Games

Проверить:
- Architecture
- Input
- Player
- Products
- Shelves
- Category Validation
- Levels
- Economy
- Upgrades
- Customers
- Idle
- Save
- UI
- DOTween
- LeanPool
- TMPro
- Yandex
- WebGL
- Performance

Требования:
- 0 compile errors
- 0 missing scripts
- 0 broken references
- нет дублирующих систем
- нет критических memory leaks
- нет очевидных pooling leaks
- нет gameplay зависимости от Yandex
- нет platform-specific кода внутри core gameplay
- PC input работает
- Mobile input архитектурно готов
- WebGL compile готов
- Yandex integration изолирована

Проверить, что существующая сцена не была изменена без разрешения.

Создать Documentation/PRODUCTION_READINESS.md.

В конце указать:

READY

или

NOT READY

Если NOT READY — перечислить конкретные блокеры.

Не исправлять крупные архитектурные проблемы молча.

---

# ОБЩИЙ WORKFLOW

Выполнять строго:

01 Audit
→ 02 Architecture
→ 03 Input
→ 04 Player
→ 05 Products
→ 06 Carry
→ 07 Shelves
→ 08 Category Validation
→ 09 Level 1
→ 10 Levels 1–5
→ 11 Economy
→ 12 Upgrades
→ 13 UI
→ 14 Customers
→ 15 Idle
→ 16 Save
→ 17 Yandex
→ 18 Ads
→ 19 LeanPool
→ 20 DOTween
→ 21 Integration
→ 22 Performance
→ 23 Mobile
→ 24 WebGL
→ 25 QA
→ 26 Documentation
→ 27 Production Audit

---

# СТРОГОЕ ПРАВИЛО О СЦЕНЕ

На протяжении всего проекта:

**НЕ ИЗМЕНЯТЬ EXISTING SCENE**

если пользователь явно не разрешил.

Разрешённые способы подготовки новой функциональности:
- Script
- Prefab
- ScriptableObject
- Configuration
- Runtime Initialization
- Documentation

Если без изменения сцены невозможно подключить систему:
1. остановиться;
2. объяснить причину;
3. указать конкретный GameObject/компонент;
4. описать ручное подключение;
5. дождаться разрешения пользователя.

---

# ПРИНЦИП ДОБАВЛЕНИЯ НОВОГО ТОВАРА

Новый товар должен добавляться без изменения core-кода:

Create ProductData
→ Set ID
→ Set Name
→ Set ProductCategory
→ Assign Icon
→ Assign Prefab
→ Assign Reward
→ Done

---

# ПРИНЦИП ДОБАВЛЕНИЯ НОВОЙ ПОЛКИ

Create ShelfData
→ Set AllowedCategory
→ Set Capacity
→ Create/Assign Shelf Prefab
→ Done

Core Shelf System менять не требуется.

---

# ПРИНЦИП ДОБАВЛЕНИЯ НОВОГО УРОВНЯ

Create LevelData
→ Set Objectives
→ Set Products
→ Set Shelves
→ Set Rewards
→ Done

LevelManager менять не требуется.

---

# ГЛАВНЫЙ GAMEPLAY FLOW

Player
→ Input
→ Movement
→ Interaction
→ Pickup
→ Carry
→ Shelf Detection
→ Placement Validator
→ Shelf
→ Level Progress
→ Reward
→ Economy
→ Upgrades
→ Customers
→ Store Income
→ Idle Progression

---

# PLATFORM ARCHITECTURE

Gameplay
→ Platform Abstraction
→ Local Provider / Yandex Provider

Core gameplay не должен знать, какой provider используется.

---

# FINAL PRINCIPLE

Simple Controls
+
Clear Product Categories
+
Correct Shelf Placement
+
Physical Progress
+
Satisfying Feedback
+
Economy
+
Upgrades
+
Customers
+
Idle Automation
+
Store Expansion
=
SHELF RUSH

Главная игровая фантазия:

«Я превращаю маленький хаотичный магазин в идеально организованный и прибыльный бизнес, а затем развиваю его в большую торговую сеть.»

END OF CLINE_TASKS.md
