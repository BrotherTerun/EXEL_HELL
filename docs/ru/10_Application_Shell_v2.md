# EXEL HELL — архитектура приложения / сцен v2

**Статус: АКТУАЛЬНАЯ production-архитектура.**  
Заменяет `10_Application_Shell.md` в части старого одно-сценового runtime-потока.

## 1. Разделение сцен

Production использует три явные сцены:

- `Assets/Scenes/Menu.unity`
- `Assets/Scenes/Gameplay.unity`
- `Assets/Scenes/LevelConstructor.unity`

`SampleScene.unity` — legacy/reference и больше не production boot path.

Каждая production-сцена имеет контекст `PrototypeSceneArchitecture` с ролью:
- `Menu`;
- `Gameplay`;
- `Constructor`.

Контекст имеет execution order `-10000`, чтобы подготовить runtime-сервисы до первого отрисованного кадра листа.

## 2. Постоянная оболочка Application

`ExcelHellApplication` создаётся через `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)` и сохраняется через `DontDestroyOnLoad` в нормальном игровом потоке.

Она владеет:
- главным меню;
- меню паузы;
- экранами settings/load/help;
- сохранением чекпоинтов;
- состояниями `GameplayActive` / `Paused`;
- навигацией между Menu и Gameplay.

Текущая оболочка генерируется рантаймом через uGUI. Финальный пиксельный интерфейс может менять скин/композицию без изменения контракта постоянства и навигации.

## 3. Нормальный production-поток

### NEW GAME
1. удалить прошлый прогресс;
2. сохранить уровень 0;
3. `PrototypeLevelRuntime.SetCurrentIndex(0)`;
4. выставить `GameplayActive=true`;
5. загрузить `Gameplay`;
6. Gameplay создаёт/инициализирует лист для уже выбранного уровня.

### CONTINUE / RESUME FROM MAIN MENU
1. прочитать сохранённый `CurrentLevelIndex`;
2. выставить runtime index;
3. выставить `GameplayActive=true`;
4. загрузить `Gameplay`.

### LOAD
Та же архитектура, но используется выбранный save payload.

**Важно:** Menu больше не рендерит и не создаёт временный лист. Оно только выбирает состояние и меняет сцену.

## 4. Инициализация Gameplay

`PrototypeSceneArchitecture.InitializeGameplayRuntime()`:

1. находит авторский объект `ExcelHellPrototype` и при необходимости временно оставляет его неактивным;
2. готовит level dataset / FormulaCells / compatibility services;
3. добавляет REF telegraph + level flow для обычного Gameplay;
4. активирует/создаёт worksheet core;
5. синхронно применяет `PrototypeLevelRuntime.Current` через `PrototypeLevelDatasetAdapter.Apply()` до первого production-render.

Это убирает старую видимую последовательность:

`legacy hardcoded seed board → один кадр → authored level`.

У адаптера остаётся LateUpdate-путь совместимости; синхронное применение production-сценой гарантирует корректный первый кадр.

## 5. Reset и Main Menu

### Reset Level
Reset из меню паузы перезагружает `Gameplay`. Индекс текущего уровня сохраняется, сцена заново строит авторское стартовое состояние.

### Main Menu
Application выставляет `GameplayActive=false` и загружает `Menu`.

Лист не должен переживать переход между сценами.

## 6. Сцена конструктора

`LevelConstructor` использует тот же лист/ядро взаимодействия, но другой набор сервисов:

- без обычного `PrototypeLevelFlow`;
- без слоя REF telegraph;
- активен `PrototypeAuthoringGuard`;
- активен `PrototypeLevelConstructor`.

AuthoringGuard постоянно нейтрализует:
- счётчик ходов/дедлайн;
- finished-state;
- pending spawn intent;
- active anomaly intent;
- Corrupted/Destroyed состояния поля.

Так сцена становится пространственной песочницей авторинга, а не игровым забегом.

См. `13_Level_Constructor_Authoring.md`.

## 7. Main Camera

Production-контекст гарантирует `Main Camera`, если её нет:
- tag `MainCamera`;
- orthographic;
- чёрный clear background;
- `AudioListener`.

Текущий gameplay UI использует Screen Space Overlay, поэтому камера пока в основном является фундаментом будущего пиксельного офисного фона и устраняет Unity-оверлей `No cameras rendering`.

## 8. Совместимость старого bootstrap

Некоторые старые prototype-helper'ы всё ещё содержат `RuntimeInitializeOnLoadMethod(AfterSceneLoad)` ради совместимости с прежними сценами.

Production-архитектура явно удаляет/переопределяет несовместимые helper'ы, особенно старое контекстное обучение, прежде чем они смогут попасть в стабильный пользовательский кадр.

Не добавлять новые финальные системы через независимый глобальный auto-bootstrap без явной межсценовой причины. Теперь предпочтительны scene-hosts.

Будущие Narrative/UI/Psychosis-компоненты должны иметь понятные хосты внутри Gameplay.

## 9. Прямой запуск `Gameplay.unity` в Editor

Это намеренный разработческий шорткат и он отличается от production boot:

- `ExcelHellApplication` создаётся BeforeSceneLoad с `GameplayActive=false`;
- Gameplay обнаруживает прямой запуск и отключает/удаляет shell, чтобы меню не закрывало лист;
- сцена использует сериализованный `startLevelIndex`;
- standalone legacy-helper'ы могут видеть `ShellAvailable=false`.

Наблюдаемые последствия:
- может появиться старый fallback HELP;
- gameplay-кнопка MENU вызывает `ExcelHellApplication.OpenGameplayMenu()`, но живого app instance нет, поэтому это no-op;
- возможен небольшой bootstrap-flicker.

Это **не** релизный путь. Исправлять паритет прямого запуска только если разработческий процесс действительно в нём нуждается.

## 10. Сохранение

`AppPersistence` использует:
- `JsonUtility`;
- `Application.persistentDataPath`;
- `System.IO`;
- схему временной записи с заменой файла.

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

Save — **чекпоинт уровня**, а не mid-turn snapshot листа.

Continue/Load возвращает к авторскому старту сохранённого текущего уровня. Для джемовой сборки это сознательное упрощение.

## 11. Приёмка production-навигации

Перед релизом проверить:
- загрузка начинается с Menu;
- New Game ведёт в L1 без вспышки legacy-board;
- Continue ведёт на сохранённый уровень;
- Load ведёт в выбранное сохранение;
- Esc/pause/resume сохраняют текущий лист внутри Gameplay;
- Reset чисто перестраивает текущий уровень;
- Main Menu действительно загружает Menu;
- settings сохраняются;
- финальное завершение сохраняется один раз;
- fallback tutorial/help не просачиваются на production-экран.
