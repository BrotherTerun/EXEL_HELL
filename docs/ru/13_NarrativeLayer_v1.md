# NarrativeLayer v1

## Назначение

Независимый от рендерера событийный слой между состоянием gameplay и presentation. v1 не меняет отрисовку листа, экономику ходов, формулы, цели или поведение `#REF!`.

Runtime-поток:

`gameplay state -> NarrativeGameplayProbe -> NarrativeSignals -> NarrativeEventRunner -> presentation receiver`

## Триггеры v1

- `LevelStart`
- `ActionNumber`
- `FirstRefSpawn`
- `RefSpread`
- `CellDestroyed`
- `GoalCompleted`
- `ReportSubmitted`
- `LevelCompleted`
- `ManualDebug`

Определения событий при необходимости фильтруются по `levelId`, `triggerNumber` (для `ActionNumber`) и `triggerSubjectId` (конкретная цель/ячейка/сущность).

## Зарезервированные эффекты API v1

- `CellMessage`
- `ProtagonistLine`
- `BossChatMessage`
- `DepartmentChatMessage`
- `Toast`
- `VisualGlitch`
- `PsychosisDelta`
- `Sound`

Presentation-эффекты имеют собственную политику времени жизни:

- `Timed`
- `OnClick`
- `TimedOrClick`

Это свойство конкретного экземпляра эффекта. Ни один тип эффекта не привязан навсегда к одному способу закрытия.

`ProtagonistLine` дополнительно содержит настроение `Normal / Tired / Alarmed / Psychotic` для будущего рендера аватара.

## Контракт очереди

Runner отправляет один ticket эффекта одному совместимому presentation receiver за раз. Receiver вызывает `ticket.Complete()`, когда показ завершён. Поэтому будущий `OnClick`-эффект героя или ячейки может блокировать narrative presentation queue до закрытия игроком, не расходуя игровое действие.

Эффекты без рендера безопасно пропускаются. Debug receiver завершает ticket сразу. Настоящий renderer всегда имеет приоритет над debug fallback. Уничтоженные Unity receiver определяются через семантику времени жизни Unity, чтобы пересозданный/закрытый UI не мог навсегда остановить очередь.

## Проверка авторинга

`NarrativeDefinitionValidator` запускается при загрузке определений событий в Editor/Development Build. Он сообщает об ошибках авторских данных, не меняя gameplay.

Проверки включают:
- повторяющиеся ID событий;
- `once=true` без стабильного ID;
- отрицательную задержку;
- недопустимый номер `ActionNumber`;
- отсутствующие effects;
- отсутствующий текст у текстовых effects;
- отсутствующую координату для `CellMessage`;
- недопустимый timed lifetime (`duration <= 0`);
- предупреждение для нулевого `PsychosisDelta`;
- отсутствующий audio key у `Sound`.

Корректная база выдаёт:

```text
[NARRATIVE/VALIDATION] OK — no authoring issues found.
```

## Self-test в Play Mode

Открыть Gameplay и войти в Play Mode.

В Editor/Development Build runtime bootstrap устанавливает `DebugNarrativeReceiver` и `NarrativeDebugHarness`. Release build smoke-harness не устанавливает.

Harness добавляет два синтетических события и сам проверяет результат. Ключевая финальная строка:

```text
[NARRATIVE/SELF-TEST] PASS — matches=2, onceSkips=1, effects=3, missingReceivers=0.
```

При несоответствии выводится `[NARRATIVE/SELF-TEST] FAIL` с фактическими и ожидаемыми счётчиками.

Детальный поток также включает:

```text
[NARRATIVE/TEST] BEGIN synthetic smoke test.
[NARRATIVE/TRIGGER] ManualDebug ...
[NARRATIVE/MATCH] debug_once_protagonist <- ManualDebug
[NARRATIVE/TRIGGER] ManualDebug ...
[NARRATIVE/SKIP] debug_once_protagonist — once event already consumed.
[NARRATIVE/TRIGGER] ActionNumber number=3 ...
[NARRATIVE/MATCH] debug_action_three <- ActionNumber
[NARRATIVE/EFFECT] ... ProtagonistLine ... dismiss=TimedOrClick ...
[NARRATIVE/RECEIVER] ProtagonistLine accepted ...
[NARRATIVE/EFFECT] ... CellMessage ... text="ПОМОГИТЕ" ... dismiss=OnClick ...
[NARRATIVE/RECEIVER] CellMessage accepted ...
[NARRATIVE/EFFECT] ... PsychosisDelta ... value=1
[NARRATIVE/RECEIVER] PsychosisDelta accepted ...
```

Точный порядок строк может отличаться на кадр, потому что у одного тестового события есть короткая задержка.

## Наблюдение реального gameplay

Во время игры Console также должна показывать:
- `LevelStart` после bind прототипа;
- `ActionNumber` при изменении gameplay turn;
- `FirstRefSpawn` для первой новой Corrupted-клетки;
- `RefSpread` для последующих новых Corrupted-клеток;
- `CellDestroyed` при переходе клетки в Destroyed;
- `GoalCompleted` один раз на выполненную цель отчёта;
- `ReportSubmitted` при нажатии Submit;
- `LevelCompleted` после принятого/завершённого состояния прототипа.

## Критерии приёмки v1

1. Проект компилируется в Unity 6000.3.1f1.
2. Автоматический self-test выдаёт `SELF-TEST PASS`.
3. Валидация авторинга выдаёт `OK` для синтетической базы.
4. Нормальный gameplay публикует ожидаемые read-only trigger logs.
5. Видимое поведение листа/UI не меняется.
6. Триггер или dismiss сами по себе не увеличивают число игровых ходов.
7. Reset/смена сцены перебинживают probe без дублирования narrative roots.

После этого debug sample database можно заменить авторскими событиями уровней и настоящими receiver: протагонистом, cell manifestations, chat/toast UI, psychosis и audio.
