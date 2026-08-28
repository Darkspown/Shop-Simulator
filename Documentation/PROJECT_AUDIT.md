# PROJECT AUDIT — Shelf Rush (Shop Simulator)

> Полный технический аудит проекта.
> Дата аудита: 28.08.2026 | Автор: Cline (по запросу)
> Статус: **аудит без изменений кода** (режим plan — ничего не правилось, кроме документации).

---

## 1. Сводка (Executive Summary)

Проект находится в состоянии **«чистого старта» (greenfield)**: игрового кода нет вообще.
Установлено стороннее «фундаментальное» окружение (DOTween, LeanPool, Yandex SDK, модель
персонажа PolyOne), но ни одного игрового объекта, скрипта, системы или UI в проекте нет.

Сцена `SampleScene` — это пустая сцена по умолчанию (Main Camera + Directional Light).
`EditorBuildSettings` не содержит ни одной сцены.

> Терминология: раздача называется **Shelf Rush**, проект (PlayerSetting) называется
> **Shop Simulator**. Далее в документации используется рабочее имя **Shelf Rush**.

---

## 2. Окружение / Версии

| Параметр | Значение | Источник |
|---|---|---|
| Unity Editor | `6000.2.15f1` (Unity 6.2) | `ProjectSettings/ProjectVersion.txt` |
| Company | `DefaultCompany` | `ProjectSettings.asset` |
| Product Name | `Shop Simulator` | `ProjectSettings.asset` |
| Render Pipeline | **Built-in** (нет URP/HDRP) | `GraphicsSettings.asset` → `m_CustomRenderPipeline: 0` |
| Цветовое пространство | Linear (`m_ActiveColorSpace: 1`) | `ProjectSettings.asset` |
| Input System | **New Input System только** (`activeInputHandler: 1`) | `ProjectSettings.asset` |
| API Compatibility | по умолчанию Unity 6 (совр. .NET profile) | `ProjectSettings.asset` |
| Scripting Backend | Mono (по умолчанию, `scriptingBackend` пуст) | `ProjectSettings.asset` |
| `runInBackground` | `0` | `ProjectSettings.asset` |
| `scriptingDefineSymbols` | пусто | `ProjectSettings.asset` |
| WebGL Memory | `16` (МБ) | `ProjectSettings.asset` |
| Сцены в Build | **нет** (`m_Scenes: []`) | `EditorBuildSettings.asset` |
| Git CLI | **не установлен** в системе (папка `.git` есть) | проверка окружения |

---

## 3. Структура Assets

```
Assets/
├── Scenes/
│   └── SampleScene.unity            (пустая сцена по умолчанию)
├── Scripts/                         (ПУСТО — нет ни одного игрового скрипта)
├── Plugins/
│   ├── Demigiant/DOTween/           (DOTween: DOTween.dll + modules)
│   └── CW/
│       ├── LeanCommon/              (LeanCommon + asmdef)
│       ├── LeanPool/                (LeanPool + asmdef: LeanPool, CW.Common)
│       └── Shared/Common/           (CW.Common asmdef)
├── PluginYourGames/                 (Yandex Games SDK — плагин YG2)
├── PolyOne/Free Stickman/           (модель персонажа + анимации)
└── WebGLTemplates/YandexGames/      (WebGL-шаблон с интеграцией Yandex SDK)
```

---

## 4. Существующие системы (что уже есть)

Игровых систем **нет**. Есть только готовая инфраструктура от сторонних ассетов:

### 4.1 Сцены
- Единственная сцена `Assets/Scenes/SampleScene.unity` (7055 байт):
  - `Main Camera` (Perspective, FOV 60, pos 0,1,-10) + `AudioListener`
  - `Directional Light`
  - Никакого UI, игрока, полок, товаров и т.п.

### 4.2 DOTween (`Assets/Plugins/Demigiant/DOTween`)
- Собран как DLL: `DOTween.dll`, `Editor/DOTweenEditor.dll`, `Editor/DOTweenUpgradeManager.dll`.
- Есть модули-скрипты: UI, Physics, Physics2D, Sprite, Audio, UIToolkit, UnityVersion, Utils.
- **Нет** `asmdef` → компилируется в глобальную `Assembly-CSharp`.
- **Нет** `DOTweenSettings` (не запущен мастер настройки/Setup Wizard) — рекомендуется инициализировать (доступ в меню `Tools > Demigiant > DOTween Utility Panel`).

### 4.3 LeanPool + LeanCommon (`Assets/Plugins/CW`)
- `LeanPool/Required/Scripts`: `IPoolable.cs`, `LeanClassPool.cs`, `LeanGameObjectPool.cs`, `LeanPool.cs`.
- Есть собственные `asmdef`: `CW.Common`, `LeanCommon`, `LeanPool`.
- Есть примеры (сцены `01–06 Pool*`).
- **Готовая система объектного пула** — переиспользовать для товаров/коробок/клиентов/VFX.

### 4.4 PluginYourGames — Yandex SDK (YG2 v2) (`Assets/PluginYourGames`)
- Готовый интеграционный слой для Yandex Games:
  - `Scripts/Basic/YG2.cs` — статическая точка входа (`YG2`): инициализация SDK,
    `isSDKEnabled`, события окна/паузы, язык, реклама (`onAdvNotification`,
    `nowRewardAdv`, `nowInterAdv`), сейвы (cloud/local), `platform`.
  - `Scripts/InfoYG/InfoYG.cs` — настройки плагина (`InfoYG.Inst()`, Resources).
  - `Platforms/YandexGames/YandexGames.asset` — конфиг платформы
    (showFirstAdv, interAdvInterval 60, saveCloud 1, selectWebGLTemplate 1 и др.).
  - `Resources/SettingsYG2.asset` — общий конфиг `InfoYG` (language `ru`, autoApplySettings, logInEditor и т.д.).
  - `Scripts/Basic/{GameplayAPI,GameReadyAPI,GamePause,WindowGame,CallAction}.cs`.
  - `Platforms/YandexGames/Scripts/YandexGamePlatform.cs` — реализация под `#if YandexGamesPlatform_yg`.
  - `Scripts/EditorScr/DefineSymbols.cs` — автоматическое управление defines
    (`PLUGIN_YG_2`, `TMP_YG2`, `NJSON_YG2`, платформенных `*_yg`).
  - `Scripts/Utils/LocalStorage_yg.cs`, `JsonYG.cs`.
  - Пример: `Example/Scenes/ExampleYG2.unity`, примеры prefabs/скриптов.
- **Нет** `asmdef` → код плагина попадает в глобальную `Assembly-CSharp`
  (подтверждено `m_EditorClassIdentifier: Assembly-CSharp::YG.InfoYG` в `SettingsYG2.asset`).
- WebGL-шаблон `Assets/WebGLTemplates/YandexGames/index.html` уже содержит подключение
  Yandex SDK (`/sdk.js`), обработку паузы/ресума, fullscreen, `SendMessage` на `YG2Instance`.

### 4.5 PolyOne Free Stickman (`Assets/PolyOne/Free Stickman`)
- Модель персонажа: `Prefabs/Free Pack - Stick Man.prefab` (скелет: Root/Spine/Head/руки/ноги).
- Аниматор-контроллер: `Animation/Controler/Stickman_Controler.controller`.
- Доп. `Prefabs/SM_Plane.prefab`, демо-сцена `Scene/Free Stickman - Demo.unity`.
- В prefab **нет** скриптов (только модель+анимация) — подходит как визуальная часть игрока.

---

## 5. Пакеты (Packages/manifest.json)

```
com.unity.collab-proxy          2.10.2
com.unity.feature.development   1.0.2
com.unity.inputsystem           1.16.0   (New Input System)
com.unity.multiplayer.center    1.0.0
com.unity.timeline              1.8.9
com.unity.ugui                  2.0.0    (uGUI + TextMeshPro в Unity 6.2)
com.unity.visualscripting       1.9.8
+ стандартные встроенные модули (physics, ui, animation, etc.)
```

- **TextMeshPro**: НЕ отдельный пакет. В Unity 6.2 TMP встроен в `com.unity.ugui 2.0.0`
  (`Runtime/TMP/*`, namespace **`TMPro`**, класс `TMP_Text`). **Доступен и готов к использованию.**
- Newtonsoft.Json: НЕ установлен (плагин YG2 умеет работать без него / через встроенный JSON).

---

## 6. asmdef

| asmdef | Путь |
|---|---|
| `LeanCommon.asmdef` | `Assets/Plugins/CW/LeanCommon/LeanCommon.asmdef` |
| `LeanPool.asmdef` | `Assets/Plugins/CW/LeanPool/LeanPool.asmdef` |
| `CW.Common.asmdef` | `Assets/Plugins/CW/Shared/Common/CW.Common.asmdef` |

- **DOTween** → без asmdef (глобальная сборка).
- **PluginYourGames** → без asmdef (глобальная сборка).
- **`Assets/Scripts`** → **нет** asmdef (папка пустая).

> Риск: сторонний код в глобальной `Assembly-CSharp` — слабая изоляция. При написании
> игрового кода рекомендуется задать собственный asmdef для `Assets/Scripts`.


---

## 7. Input System

- **New Input System активен** (`activeInputHandler: 1`, legacy отключён).
- В корне Assets лежит сгенерированный дефолтный ассет `InputSystem_Actions.inputactions`
  (мапы: Player, UI; группы: Keyboard&Mouse, Gamepad, Touch, Joystick, XR;
  экшены Move/Jump/Look/Sprint и т.д.).
- Это **дефолтный шаблон** Unity, а не настроенная под Shelf Rush схема.
- `InputManager.asset` содержит legacy-оси по умолчанию (неактивны).

---

## 8. Tags / Layers / Sorting

`ProjectSettings/TagManager.asset`:
- **Tags**: кастомных нет (пусто).
- **Layers**: только стандартные (Default, TransparentFX, Ignore Raycast, Water, UI).
  Все кастомные слои (3 и 6–31) **пустые**.
- **Sorting Layers**: только `Default`.

> Для игры потребуется добавить слои (Player, Products, Shelves, Interactable и т.п.).
> Единственный слой UI занят по умолчанию.

---

## 9. Материалы / Рендер

- Проект на **Built-in Render Pipeline** (URP-ресурсов нет).
- Всего 15 `.mat` — только в сторонних ассетах/примерах. Своих материалов нет.
- Качество: 6 уровней (Very Low … Ultra), по умолчанию High; платформенной настройки нет.
- Linear color space включён.

---

## 10. Проверка искомых систем (подробно)

| # | Система | Статус | Комментарий |
|---|---|---|---|
| 1 | Player Controller | Отсутствует | Нет ни одного скрипта |
| 2 | Player Movement | Отсутствует | — |
| 3 | Input (игровой) | Только дефолтный шаблон | Нет игровых карт/биндингов |
| 4 | Interaction | Отсутствует | — |
| 5 | Product systems | Отсутствует | — |
| 6 | Shelf systems | Отсутствует | — |
| 7 | Level systems | Отсутствует | Сцена пустая |
| 8 | Economy | Отсутствует | — |
| 9 | Save (игра) | Отсутствует (игрового) | Yandex cloud save готов (`saveCloud: 1`) |
| 10 | UI | Отсутствует | Нет Canvas/EventSystem |
| 11 | Yandex integration | Плагин готов и настроен | Нужен наш код-обёртка вокруг `YG2` |
| 12 | DOTween | Установлен, **не инициализирован** | Нет `DOTweenSettings` |
| 13 | LeanPool | Установлен, готов | Есть asmdef |
| 14 | TMPro | **Доступен** (в составе ugui 2.0.0) | namespace `TMPro` |

---

## 11. Проверка ошибок компиляции / ссылок

- `Assets/Scripts` пуст → **игровых compile-errors нет** (нечему компилироваться).
- Лога ошибок сборки в `Logs/` нет (только `Packages-Update.log` и
  шейдерные логи (`shadercompiler-*.log`) — это штатные логи импорта).
- **Missing references**: в единственной сцене нет ссылок на скрипты → отсутствуют.
- **Duplicate systems**: дублей нет (систем нет вообще).
- Потенциальный риск компиляции: у плагина YG2 авто-определение defines
  (`DefineSymbols.cs`) выполнится при первом импорте в редакторе; до открытия редактора
  defines в `ProjectSettings.asset` пусты — это нормально и разберётся редактором.


---

## 12. Архитектурные проблемы / риски

1. **Отсутствует игровая архитектура** — стартовая точка нулевая, всё нужно строить
   с соблюдением SOLID, разделения ответственности, Data-driven (SO), интерфейсов и событий.
2. **Нет asmdef для игрового кода** — сторонний код (YG2, DOTween) живёт в глобальной
   `Assembly-CSharp`; игровой код при отсутствии asmdef тоже попадёт туда →
   риск «God Object»-монолита. Рекомендуется свой asmdef для `Assets/Scripts`.
3. **`runInBackground: 0`** — на WebGL/Яндекс-играх при потере фокуса вкладки таймеры
   могут встать; шаблон Yandex частично управляет паузой сам, но флаг стоит пересмотреть
   (обычно ставят 1).
4. **Нет сцен в EditorBuildSettings** — билд не знает, какую сцену собирать.
5. **WebGL Memory 16 МБ** — под нагрузку (пулы, тексты TMP) может быть мало;
   вероятно, потребуется увеличить.
6. **DOTween не инициализирован** — до первого запуска стоит создать `DOTweenSettings`
   (мастер `DOTween Utility Panel`).
7. **Нет кастомных Layers/Tags** — потребуются для коллизий и маскирования.
8. **Linear color space на мобильных/WebGL** — допустимо, но требует аккуратной
   настройки освещения/материалов и производительности.
9. **Нет Newtonsoft.Json** — для сохранений/данных либо использовать встроенный
   `JsonUtility`, либо добавить пакет `com.unity.nuget.newtonsoft-json` (плагин YG2
   поддерживает `NJSON_YG2`).
10. **Сторонние пакеты без учёта версий в git** — `Library/` и `Logs/`, вероятно,
    не должны попадать в репозиторий (см. `.gitignore`).

---

## 13. Потенциальные проблемы платформ

### WebGL
- Скрипт-бэкенд для релиза WebGL обычно IL2CPP; `webGLMemorySize` 16 МБ может быть мал.
- `runInBackground: 0` + пауза/фокус вкладки.
- Проверить включение шаблона `YandexGames` и defines `PLUGIN_YG_2`, `YandexGamesPlatform_yg`
  (авто-настройка плагина).
- Избегать прямых обращений к файловой системе (нет диска) → использовать
  `PlayerPrefs`/Yandex cloud/local save.
- Потоковую загрузку/Data Caching настроить через плагин.

### Mobile (Android/iOS)
- New Input System активен — для Touch нужны мапы с `<Touchscreen>` (в шаблоне есть группа `Touch`).
- Билд-бэкенд/API compatibility проверить под выбранную платформу.
- Linear-RP производительность, количество динамических источников света.
- Единый gameplay-API: не дублировать логику между PC и Mobile.

### Yandex Games
- Плагин YG2 готов; нужен наш слой-обёртка: реклама (inter/rewarded), сейвы (cloud),
  `GameReadyAPI`, `GameplayStart/Stop`, язык, `onOpenAnyAdv`/пауза.
- Defines платформы включаются автоматически редактором плагина при первом импорте.
- WebGL-шаблон Yandex подключён в шаблоне, но его надо выбрать в Player Settings.

## 14. Рекомендации (приоритеты)

1. Создать игровую архитектуру: `Assets/Scripts` с собственным `asmdef`,
   ServiceLocator/DI-подход, event bus, ScriptableObjects для данных.
2. Инициализировать DOTween (DOTween Setup) и настроить `DOTweenSettings`.
3. Настроить Input System под игру (единая схема: WASD/Arrows + Touch/Joystick + Gamepad).
4. Добавить кастомные Layers/Tags.
5. Подключить игровое сохранение на базе Yandex cloud (через `YG2`) + локальный fallback.
6. Собрать сцену (или набор сцен) и добавить их в `EditorBuildSettings`.
7. Переиспользовать: LeanPool (пулы объектов), DOTween (анимации/фидбек), PolyOne (игрок),
   `YG2` (Yandex), TMPro (текст).
8. Разобраться с `runInBackground` и `webGLMemorySize` под WebGL в будущем.

---

## 15. Файлы, изменённые в рамках аудита

- **Создан** `Documentation/PROJECT_AUDIT.md` (этот файл).
- **Создан** `Documentation/ARCHITECTURE.md` (см. отдельный документ).
- **Создан** `Documentation/TODO.md` (см. отдельный документ).
- Игровой код, сцены и ассеты **не изменялись**.

