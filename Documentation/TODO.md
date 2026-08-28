# TODO — Shelf Rush

> Список задач по результатам аудита (28.08.2026). Проект на стартовом этапе:
> игровой код полностью отсутствует.

---

## Высокий приоритет (фундамент)

- [ ] **Инициализировать DOTween**: создать `DOTweenSettings`
      (меню `Tools > Demigiant > DOTween Utility Panel > Setup`), проверить, что анимации
      работают. DOTween собран в DLL, asmdef нет (живёт в глобальной сборке) — учесть.
- [ ] **Создать игровой asmdef** для `Assets/Scripts` для изоляции игрового кода от
      глобальной сборки и плагинов (LeanPool/CW.Common, LeanCommon, LeanPool — уже asmdef).
- [ ] **Настроить Input System под игру** (базовый единый конвейер реализован —
      см. `CROSS_PLATFORM.md`):
      - единая схема: WASD/Arrows (Keyboard) + E (Interact) — ✅ `KeyboardMouseInputProvider`
      - Touch: свайп-джойстик — ✅ `TouchInputProvider`; виртуальный джойстик —
        ✅ `VirtualJoystick` prefab (подключается вручную)
      - Gamepad (опционально, WebGL) — ✅ `GamepadInputProvider`
      - оставшееся: продублировать при необходимости через экшены из
        `InputSystem_Actions.inputactions` (сейчас провайдеры читают устройства напрямую).
- [ ] **Добавить кастомные Layers/Tags** (например: `Player`, `Product`, `Shelf`,
      `Interactable`, слой `Environment`) в `TagManager`.
- [ ] **Добавить сцену(ы) в `EditorBuildSettings`** (сейчас `m_Scenes: []`).
- [ ] **Пересмотреть `runInBackground`** (сейчас `0`) под WebGL/Яндекс-игры — обычно `1`.
- [ ] **Оценить `webGLMemorySize` (16 МБ)** — вероятно, потребуется увеличение.

## Player

- [x] `PlayerController` / `PlayerMovement` (единый API `IPlayerInput` + `PlayerMovement`,
      источники ввода по платформе).
- [ ] Использовать **PolyOne Free Stickman** (`Free Pack - Stick Man.prefab` +
      `Stickman_Controler.controller`) как визуальную часть игрока (через prefab/runtime).
- [ ] Анимации движения/игле (через Animator Controller).
- [ ] `Interaction` — выделение/подбор товара с полки.

## Products

- [ ] `ProductData` (ScriptableObject) — id/имя/цена/вид.
- [ ] `Product` prefab + **LeanPool** (SpawningPool) для переиспользования.
- [ ] `ProductSpawner` / `ProductPool`.

## Shelves

- [ ] `ShelfData` (ScriptableObject) — вместимость, набор товаров, точки размещения.
- [ ] `Shelf` / `ShelfView` / `StockService` (пополнение, снятие товара игроком).
- [ ] Взаимодействие «игрок берёт товар с полки».

## Customers / Level

- [ ] `Customer`, `CustomerSpawner`, `CustomerFlow`.
- [ ] `LevelConfig` (SO), `LevelManager`, `LevelBootstrap`.
- [ ] Очередь/запросы клиентов на товары.

## Economy

- [ ] `EconomyService` + `Wallet` (баланс, события изменения).
- [ ] `RewardService` (награды за выполненные заказы, DOTween для анимации наград).

## Save

- [ ] `ISaveService` (абстракция).
- [ ] `PlayerPrefsSaveService` (локальный fallback).
- [ ] `YandexSaveService` (cloud save через `YG2`, `saveCloud: 1` уже включён).

## UI

- [ ] Каркас UI: Canvas, EventSystem, HUD (деньги/заказ), панели магазина/уровней.
- [ ] Весь текст — **TextMeshPro** (`TMPro`).

## Yandex

- [ ] `YandexService`-обёртка над `YG2` (ads inter/rewarded, saves, `GameReadyAPI`,
      `GameplayStart/Stop`, язык, пауза при рекламе).
- [ ] Проверить выбор WebGL-шаблона `YandexGames` в Player Settings.
- [ ] Проверить, что плагин выставил defines (`PLUGIN_YG_2`, `YandexGamesPlatform_yg`,
      `TMP_YG2`) при первом импорте в редакторе.

## Общее / платформы

- [ ] Собрать и проверить на PC (Standalone), Android, WebGL.
- [ ] Проверить Яндекс-игры: загрузка, реклама, сейвы, пауза.
- [ ] Решить по Newtonsoft.Json (добавить `com.unity.nuget.newtonsoft-json` или использовать
      встроенный JSON) — плагин YG2 поддерживает `NJSON_YG2`.

## Документация

- [ ] Создать остальные документы `Documentation/`:
      `PROJECT_OVERVIEW.md`, `GAMEPLAY.md`, `PLAYER.md`, `PRODUCTS.md`, `SHELVES.md`,
      `ECONOMY.md`, `LEVELS.md`, `UI.md`, `SAVE_SYSTEM.md`, `YANDEX.md`,
      `CROSS_PLATFORM.md`, `TECHNICAL_GUIDELINES.md`.
- [ ] Вести `CHANGELOG.md`.
