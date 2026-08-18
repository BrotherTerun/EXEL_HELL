# EXEL HELL — руководство по авторингу LevelConstructor

**Статус: АКТУАЛЬНЫЙ технический процесс.**

Constructor — runtime-песочница пространственного авторинга для быстрой сборки стартовых раскладок FC2 на основе существующих уровней. Это намеренно не полноценный Unity-editor tool.

## 1. Сцена

Открыть `Assets/Scenes/LevelConstructor.unity`.

Сцена использует те же взаимодействия листа, что Gameplay, но включает `PrototypeAuthoringMode`.

`PrototypeAuthoringGuard` постоянно:
- принудительно держит turn = 0;
- сбрасывает finished/deadline;
- очищает pending/current `#REF!` intent;
- возвращает клетки в Normal;
- сохраняет доступ к токенам;
- скрывает обычный счётчик ходов;
- показывает авторский статус вместо живой информации аномалии.

Ручной расстановки REF нет. В Gameplay очаг выбирается динамически.

## 2. Панель / F2

Панель конструктора открывается в Play Mode.

`F2` скрывает/показывает панель. Скрытие **не выключает authoring mode**: ходы и аномалия остаются отключены, чтобы полем можно было свободно манипулировать.

Фон панели намеренно непрозрачный ради читаемости.

## 3. Шаблоны

Текущие кнопки загружают чистые снимки runtime-каталога L1–L4.

Чистый снимок фиксируется при старте конструктора. Возврат к шаблону восстанавливает именно его, а не предыдущие правки текущей Play-сессии.

## 4. Выбор/редактирование ячеек

Выбрать координату обычным игровым взаимодействием. Панель отслеживает row/column.

Можно:
- очистить token/formula;
- поставить Data;
- поставить RecordKey;
- поставить FieldKey;
- изменить числовое значение Data;
- поставить/убрать SUM;
- поставить/убрать SORT;
- назначить/убрать Report Goal;
- включить/выключить REF;
- изменить MaxTurns / first outbreak / respawn / active-outbreak delay.

Semantic Data/Key уникальны по identity: установка того же токена в другом месте означает перенос, а не дублирование.

Установка формулы очищает конфликтующее содержимое и наоборот, чтобы экспорт не создавал незаконного overlap.

## 5. Обычный FC2 drag как инструмент авторинга

Критически важно:

> **обычные gameplay MOVE, выполненные в authoring mode, фиксируются как стартовая раскладка.**

Перед Export/Rebuild и соответствующими изменениями панель читает живой `CellModel[,]` и синхронизирует:
- позиции Data;
- позиции key;
- labels;
- позиции Formula property.

Поэтому `drag C3 → E4` означает, что экспорт начнёт токен в E4.

Перемещение пустой `=SORT()` обычным drag также меняет её стартовую координату в экспорте.

### Что не фиксируется как authored source

Вычисленные runtime `AggregateToken` намеренно игнорируются при обратной сборке авторской раскладки. Случайно решённая часть отчёта не должна запекаться в старт уровня.

Числовые значения dataset редактируются через Data value control конструктора.

## 6. Режимы Report Goal

Constructor не заставляет каждую aggregate-цель быть SUM FormulaCell.

Допустимы:
- DirectValue → обычная ReportCell;
- aggregate → ReportCell + SUM;
- aggregate → обычная ReportCell как delivery target.

Так можно проверять варианты с меньшим запасом FormulaCell.

Адаптер предупреждает только о действительно подозрительных комбинациях: SORT на aggregate report target или formula на direct-value target.

## 7. Export

`EXPORT → CLIPBOARD` генерирует C#-блок `new PrototypeLevelConfig { ... }`.

Включает:
- ID/name;
- размеры поля;
- ReportGoals flags;
- REF/formula mode;
- параметры ходов/аномалии;
- dataset values;
- TokenLayout;
- FormulaLayout;
- GoalLayout.

Косметическое ограничение: комбинированные ReportGoals сейчас выводятся числом, например `(PrototypeReportGoals)52`. Это валидно, но менее читаемо, чем явные OR-флаги.

Экспорт предназначен для вставки/преобразования в builder method внутри `PrototypeLevelCatalog`.

## 8. Import из буфера

`IMPORT FROM CLIPBOARD` принимает тот же C# initializer, который выдаёт Export.

Процесс:
1. экспортировать уровень;
2. сохранить/передать/изменить текст;
3. вернуть initializer в clipboard;
4. при необходимости загрузить другой template;
5. Import;
6. constructor разбирает dataset/layout/goals/parameters и перестраивает рабочее поле.

Это даёт лёгкий round-trip без отдельного JSON-формата уровней в рамках джема.

## 9. Rebuild

После структурных изменений constructor может запросить перестройку листа. Старый worksheet core уничтожается и создаётся заново; сценовые сервисы авторинга остаются живы.

Поэтому `Worksheet Core`, `Scene Context` и runtime services разделены в новой архитектуре сцен.

## 10. Ограничения фиксированной схемы

Текущий constructor привязан к существующей prototype-схеме.

Records: `ivanov`, `petrov`, `sidorov`, `volkova`, `kim`.

Fields: `hours`, `salary`, `overtime`, `bonus`.

Report Goals:
- SalaryTotal;
- OvertimeTotal;
- BonusTotal;
- BonusAtLeastFour (в реализации сейчас порог >=5);
- SalaryOfMaxOvertime;
- SalaryForHoursBelowForty.

Это **не** универсальный schema editor. Не добавлять произвольные ID/resize/content pipelines, пока финальный level design реально этого не требует.

## 11. Рекомендуемый процесс

Для каждого кандидата:
1. выбрать ближайший шаблон L1–L4 или clipboard import;
2. расставить keys/Data/Formulas прямым drag + панелью;
3. назначить goal cells/modes;
4. настроить REF/turn budget;
5. Export и сохранить initializer;
6. посчитать законный no-REF маршрут / `C0`;
7. smoke в настоящем Gameplay с REF;
8. поправить layout/timings в constructor;
9. после приёмки закоммитить config в `PrototypeLevelCatalog`;
10. обновить `mvp05_levels/` и актуальные balance artifacts.

Constructor — одноразовая инфраструктура авторинга; за runtime-определение уровня отвечает закоммиченный `PrototypeLevelConfig`.
