# EXEL HELL — спецификация NarrativeLayer v1

**Статус: ЗАПЛАНИРОВАННАЯ СЛЕДУЮЩАЯ СИСТЕМА / на этой контрольной точке ещё не реализована.**

NarrativeLayer намеренно меньше полноценного dialogue framework. Его задача — слушать gameplay, сопоставлять авторские события и маршрутизировать presentation-эффекты, не меняя замороженные правила FC2.

## 1. Архитектурная граница

Целевой поток:

```text
Gameplay signal
      ↓
NarrativeEventRunner
      ↓
matching NarrativeEventDefinition
      ↓
NarrativeEffect[]
      ↓
NarrativePresentationRouter
      ↓
receiver(s)
```

Runner не должен знать, как отрисовываются чат, speech bubble, glitch или cell message. Так narrative content остаётся независимым от финального visual pass.

## 2. Определение narrative event

Минимальные данные:

```text
id
levelId
trigger
once
delay
effects[]
```

Опциональные поля добавлять только по реальной потребности контента. Не строить во время джема универсальный condition graph.

## 3. Словарь триггеров v1

Обязательные:
- `LevelStart`
- `ActionNumber(N)`
- `FirstRefSpawn`
- `RefSpread`
- `CellDestroyed`
- `GoalCompleted`
- `ReportSubmitted`
- `LevelCompleted`
- `ManualDebug`

Возможное лёгкое расширение позже:
- `RandomInterval` / случайное событие из авторского пула.

Первая реализация не должна от него зависеть.

## 4. Словарь эффектов

Router должен понимать намерение эффекта даже до существования всех renderer:

- `CellMessage`
- `ProtagonistLine`
- `BossChatMessage`
- `DepartmentChatMessage`
- `Toast`
- `VisualGlitch`
- `PsychosisDelta`
- `Sound`

Одно событие может отправлять несколько эффектов.

Пример:

```text
trigger: FirstRefSpawn
once: true

effects:
- ProtagonistLine("...этого здесь раньше не было.", mood=Alarmed)
- BossChatMessage("Отчёт будет сегодня?")
- PsychosisDelta(+1)
```

## 5. Время жизни presentation

Для эффектов, занимающих экран:

```text
DismissMode:
- Timed
- OnClick
- TimedOrClick

duration
priority
```

Dismiss-click — presentation-only и бесплатный: он не считается gameplay-действием.

## 6. ProtagonistLine

Рекомендуемый payload:

```text
text
mood
dismissMode
duration
priority
```

Поздний renderer может выбрать:
- sprite + speech bubble;
- sprite + subtitle;
- временную tutorial hint;
- короткую реакцию.

Event-layer не зависит от выбранного визуального представления.

## 7. TypewriterCellMessage

Первый обязательный самостоятельный renderer/effect.

Примеры:
- `ПОМОГИТЕ`
- `НЕ СМОТРИ`
- `Я НЕ ХОЧУ СЧИТАТЬ`
- `ОНИ УЖЕ ЗДЕСЬ`
- `ЗАКРОЙ ФАЙЛ`
- `ПОЧЕМУ ТЫ ЕЩЁ РАБОТАЕШЬ`

Требования:
- overlay, привязанный к ячейке/области;
- печатное появление с опциональными паузами/jitter;
- настоящий DataToken не создаётся;
- не влияет на SORT, SUM, MOVE, Report Goal checks или REF targeting;
- не перехватывает raycast, если эффект явно не поддерживает dismiss;
- исчезает по правилам presentation lifetime.

Narrative text может лгать; настоящая gameplay-телеграфия — нет.

## 8. Очередь / конкурентность

Runner должен предотвращать конфликт двух narrative events за один exclusive renderer.

Минимальная политика:
- triggers могут сразу добавляться в очередь;
- effects с независимыми receiver могут выполняться параллельно;
- exclusive protagonist/cell-message presentation можно очередить по priority;
- presentation delays используют unscaled/realtime timing там, где поведение паузы должно быть явным.

Не блокировать gameplay narrative-событиями без конкретной сценарной необходимости.

## 9. Once-only и persistence

`once=true` предотвращает повторный dispatch в нужной области жизни события.

Если событие должно оставаться consumed после reload/save, использовать явный narrative flag через `ExcelHellApplication.AddNarrativeFlag()` / `NarrativeFlags` прогресса.

Не сохранять каждый косметический random event.

## 10. Gameplay hooks

NarrativeLayer наблюдает, но не владеет:
- счётчиком успешных действий;
- активацией outbreak spawn;
- разрешением spread intent;
- переходом клетки в Destroyed;
- переходом Report Goal в выполненное состояние;
- результатом Submit;
- завершением уровня.

Предпочитать явные event/signal hooks при касании FC2 вместо чтения текстового UI. Не менять исходы core.

## 11. Debug receiver v1

До существования финального UI предоставить `DebugNarrativeReceiver`.

Ожидаемые логи:

```text
[NARRATIVE/TRIGGER] FirstRefSpawn
[NARRATIVE/MATCH] L2_FirstRef_01
[NARRATIVE/EFFECT] ProtagonistLine "...этого здесь раньше не было."
[NARRATIVE/SKIP] L2_FirstRef_01 — once already consumed
```

Рекомендуемые категории:
- `[NARRATIVE/TRIGGER]`
- `[NARRATIVE/MATCH]`
- `[NARRATIVE/EFFECT]`
- `[NARRATIVE/SKIP]`

## 12. Debug harness

Нужен небольшой development harness/API, способный вручную вызвать:
- LevelStart;
- ActionNumber(3);
- FirstRefSpawn;
- CellDestroyed(...);
- GoalCompleted(...);
- ManualDebug event.

Так runner/matching/dispatch проверяются независимо от финального presentation.

## 13. Связь с level content

Narrative definitions должны индексироваться по `levelId`, но не встраиваться в worksheet `TokenLayout/FormulaLayout` без веской технической причины.

Gameplay-layout и narrative timing должны редактироваться независимо, потому что авторинг уровней идёт параллельно.

После приёмки финальных layout события можно привязать к стабильным level ID/моментам действий.

## 14. Явные не-цели v1

Не строить:
- graph/node editor;
- ветвящийся диалог игрока;
- text input;
- полный localization pipeline сверх существующей стратегии;
- cinematic timeline framework;
- generalized quest system;
- реализацию psychosis внутри NarrativeLayer;
- audio manager внутри NarrativeLayer.

Слой только отправляет намерения соответствующим системам.

## 15. Критерии приёмки

NarrativeLayer v1 готов к freeze, когда:
- gameplay triggers наблюдаются без изменения turn/core results;
- matching по level/trigger работает;
- once-only работает;
- delay работает;
- одно событие отправляет несколько effects;
- debug receiver ясно различает trigger/match/effect/skip;
- manual harness вызывает репрезентативные события;
- TypewriterCellMessage показывает/очищает текст, не становясь gameplay data;
- есть placeholder hooks для chat/protagonist/psychosis/sound;
- не появляется новый blocker порядка кадров/raycast.
