# EXEL HELL — карта архитектуры рантайма v2

**Статус: АКТУАЛЬНАЯ техническая архитектура.**

## 1. Production-сцены

Текущая сборка использует три явные сцены:

- `Assets/Scenes/Menu.unity`;
- `Assets/Scenes/Gameplay.unity`;
- `Assets/Scenes/LevelConstructor.unity`.

`SampleScene.unity` — legacy/reference и больше не является production-путём запуска.

Каждая production-сцена содержит контекст `PrototypeSceneArchitecture` с ролью `Menu`, `Gameplay` или `Constructor`. Контекст имеет порядок выполнения `-10000`, чтобы подготовить сервисы до первого кадра листа.

## 2. Постоянная оболочка приложения

`ExcelHellApplication` создаётся через `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)` и сохраняется через `DontDestroyOnLoad` в обычном игровом потоке.

Она отвечает за:
- главное меню;
- паузу;
- settings/load/help;
- сохранение прогресса и настроек;
- состояния `GameplayActive` / `Paused`;
- навигацию между Menu и Gameplay.

Текущая оболочка генерируется рантаймом через uGUI. Финальный пиксельный интерфейс может менять скин и композицию, не ломая контракт навигации и постоянства.

## 3. Нормальный production-поток

### NEW GAME
1. удалить предыдущий прогресс;
2. сохранить прогресс на уровне 0;
3. `PrototypeLevelRuntime.SetCurrentIndex(0)`;
4. выставить `GameplayActive=true`;
5. загрузить `Gameplay`;
6. Gameplay создаёт/инициализирует лист уже для выбранного уровня.

### CONTINUE / RESUME
1. прочитать сохранённый `CurrentLevelIndex`;
2. выставить runtime-index;
3. выставить `GameplayActive=true`;
4. загрузить `Gameplay`.

### LOAD
Та же схема, но индекс берётся из выбранного сохранения.

Menu больше не создаёт временный лист. Оно только выбирает состояние и переключает сцену.

## 4. Инициализация Gameplay

`PrototypeSceneArchitecture.InitializeGameplayRuntime()`:

1. находит авторский объект `ExcelHellPrototype` и при необходимости временно держит его неактивным;
2. подготавливает dataset, FormulaCells и сервисы совместимости;
3. добавляет REF telegraph и level flow;
4. активирует/создаёт ядро листа;
5. синхронно применяет `PrototypeLevelRuntime.Current` через `PrototypeLevelDatasetAdapter.Apply()` до первого production-render.

Это убирает старую видимую последовательность:

`legacy hardcoded seed board → один кадр → authored level`.

У адаптера остаётся LateUpdate-путь совместимости, но синхронное применение в production-сцене гарантирует первый кадр.

## 5. Reset и Main Menu

### Reset Level
Перезагрузка `Gameplay`. Индекс текущего уровня сохраняется; сцена заново строит авторское начальное состояние.

### Main Menu
Application выставляет `GameplayActive=false` и загружает `Menu`.

Лист между сценами не сохраняется.

## 6. Сцена конструктора

`LevelConstructor` использует тот же лист и базовые взаимодействия, но другой набор сервисов:

- нет обычного `PrototypeLevelFlow`;
- нет REF telegraph;
- активен `PrototypeAuthoringGuard`;
- активен `PrototypeLevelConstructor`.

AuthoringGuard постоянно нейтрализует:
- счётчик ходов/дедлайн;
- finished-state;
- pending spawn intent;
- active anomaly intent;
- состояния Corrupted/Destroyed.

Поэтому сцена является пространочной песочницей авторинга, а не игровым забегом.

См. `13_Level_Constructor_Authoring.md`.

## 7. Main Camera

Production-контекст гарантирует `Main Camera`, если её нет:
- tag `MainCamera`;
- orthographic;
- чёрный clear background;
- `AudioListener`.

Gameplay UI пока работает в Screen Space Overlay, поэтому камера в основном служит фундаментом для будущего пиксельного офиса и устраняет оверлей Unity `No cameras rendering`.

## 8. Совместимость со старым bootstrap

Некоторые ранние helper-классы всё ещё имеют `RuntimeInitializeOnLoadMethod(AfterSceneLoad)` ради старых prototype-сцен.

Production-архитектура явно удаляет/переопределяет несовместимые legacy-helper'ы, прежде всего старое контекстное обучение, до того как они смогут попасть в стабильный пользовательский кадр.

Не добавлять новые финальные системы через независимый глобальный auto-bootstrap без явной межсценовой причины. При наличии production-сцен предпочитать scene-hosts.

Будущие Narrative/UI/Psychosis-компоненты должны иметь понятные хосты в Gameplay.

## 9. Прямой запуск `Gameplay.unity` в Editor

Прямой запуск сцены — разработческий шорткат и отличается от production boot:

- `ExcelHellApplication` создаётся BeforeSceneLoad с `GameplayActive=false`;
- Gameplay распознаёт прямой запуск и отключает/удаляет оболочку, чтобы меню не закрыло лист;
- используется сериализованный `startLevelIndex`;
- legacy-helper'ы могут видеть `ShellAvailable=false`.

Следствия:
- может появиться старый fallback HELP;
- кнопка gameplay MENU вызывает `ExcelHellApplication.OpenGameplayMenu()`, но без живого Application это no-op;
- возможен небольшой bootstrap-flicker.

Это не релизный путь. Паритет прямого запуска исправлять только если он реально нужен разработке.

## 10. Сохранение

`AppPersistence` использует:
- `JsonUtility`;
- `Application.persistentDataPath`;
- `System.IO`;
- запись во временный файл с последующей заменой.

Прогресс хранит:
- версию схемы;
- текущий индекс уровня;
- максимальный открытый индекс;
- флаг завершения кампании;
- timestamp;
- список `NarrativeFlags`.

Настройки хранят:
- master/music/SFX;
- fullscreen;
- resolution;
- VSync;
- language.

### Объём сохранения

Сохранение — это **чекпоинт уровня**, а не снимок листа посреди хода.

Continue/Load возвращает в авторское стартовое состояние сохранённого текущего уровня. Для джемовой сборки это сознательное упрощение.

## 11. Приёмка production-навигации

Перед релизом проверить:
- boot начинается с Menu;
- New Game ведёт в L1 без вспышки legacy-board;
- Continue ведёт на сохранённый уровень;
- Load ведёт в выбранное сохранение;
- Esc/pause/resume сохраняют текущий лист внутри Gameplay;
- Reset чисто перестраивает текущий уровень;
- Main Menu действительно загружает Menu;
- settings сохраняются;
- финальное завершение сохраняется один раз;
- fallback tutorial/help не просачивается в production-экран.
