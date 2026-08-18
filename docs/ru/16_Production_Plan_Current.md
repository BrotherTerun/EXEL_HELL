# EXEL HELL — актуальный производственный план

**Контрольная точка:** 2026-08-16 ~08:30 (+03:00)  
**Дедлайн:** релизное окно джема утром 17 августа.  
Точные часы могут сдвигаться; **порядок зависимостей и правила freeze** важнее устаревшего расписания.

## Закрытый критический путь

- ядро FC2 одобрено/заморожено;
- blocker fixes телеграфии REF;
- production-ветка `jam/final-build`;
- LevelConstructor;
- правило доставки aggregate в обычную ReportCell;
- разделение Menu / Gameplay / LevelConstructor;
- маршрутизация NEW/CONTINUE/LOAD из Menu в Gameplay;
- применение авторского уровня до первого кадра;
- production camera/bootstrap cleanup;
- обновление документации.

## Параллельная работа

### Ветка авторинга уровней
Пользователь:

`LevelConstructor → пересобрать/настроить уровни → законные no-REF маршруты → smoke с REF → принять layout → exports`

### Ветка инфраструктуры
Ассистент:

`NarrativeLayer v1 → debug hooks/receiver → TypewriterCellMessage`

Эти ветки намеренно независимы. Narrative architecture не должна ждать финальных координат; **тайминг narrative content** — должен.

## Порядок схождения

### 1. Принятые уровни + API NarrativeLayer
Критерии выхода:
- финальные playable authored configs или принятые кандидаты;
- core не тронут кроме blockers;
- narrative triggers/effects надёжно dispatch'ятся.

### 2. Narrative/UI shell
Реализовать одним блоком, чтобы не перевёрстывать экран несколько раз:
- часы рабочего дня;
- кнопку чата;
- каналы Boss/Department;
- history/unread;
- incoming toast;
- presentation-container протагониста;
- receiver NarrativeLayer для этих систем.

### 3. Первый visual pass
- пиксельный офис;
- единая рамка menu/game/chat;
- fonts/spacing/borders;
- первые состояния героя;
- убрать вид прототипа, не меняя контракты взаимодействий.

### 4. Psychosis
- прогрессия 0–4;
- presentation manifestations;
- максимум три gameplay distortion primitive;
- в первую очередь телеграфия/читаемость.

### 5. Интеграция narrative content
После стабилизации таймингов действий:
- L1 tutorial/reassurance;
- L2 первая неправильность;
- L3 реакции/replans;
- L4 давление/конфликт чата;
- финальная narrative/psychosis дуга.

### 6. Animation / presentation pass
Только переиспользуемые helper'ы, без отдельной bespoke animation architecture.

### 7. SFX + музыка
Маленький семантический набор звуков; normal/anxious музыкальное состояние.

### 8. Integration / release
- полный проход кампании;
- restart;
- menu/continue/load/save;
- clock/chat/narrative/psychosis;
- resolution/aspect;
- RU/EN;
- candidate build;
- screenshots/GIF;
- credits/licences/AI disclosure при необходимости;
- itch upload/regression.

## Правило feature freeze

После начала позднего integration window **не начинать новую архитектуру/систему**.

После freeze разрешены только:
- content;
- art/skin;
- animation на существующих hooks;
- audio;
- tuning;
- blocker/очевидные UX fixes.

Если планируемой системы нет хотя бы в базовом рабочем виде к freeze, урезать её глубину вместо ночного строительства архитектуры.

## Порядок урезания при отставании

Резать **глубину раньше систем, формирующих идентичность**:
1. меньше случайных сообщений чата;
2. меньше кадров героя;
3. меньше psychosis manifestations;
4. два gameplay distortion primitive вместо трёх;
5. два музыкальных состояния вместо трёх;
6. проще анимация офиса;
7. меньше декоративных transitions.

Не резать первыми:
- проходимую кампанию;
- читаемые REF/Formula gameplay;
- подачу рабочего дедлайна;
- минимальную narrative arc;
- финальную эскалацию/идентичность психоза;
- буфер build/regression.

## Что явно не открываем заново

- редизайн взаимодействий FC2;
- переход к realtime;
- настоящий Excel parser;
- generic level-editor сверх текущего constructor;
- новые крупные spreadsheet functions;
- полноценный mid-turn save snapshot;
- паритет прямого запуска Gameplay в Editor, если он не блокирует работу;
- сложный narrative graph;
- процедурный psychosis framework.

## Определение jam-ready

Сборка готова к отправке, когда чистый игрок может пройти:

`Menu → кампания → finale`

при этом:
- нет blocker/softlock на предполагаемых маршрутах;
- controls/report goals понятны;
- давление REF эскалирует читаемо;
- narrative/presentation идут от обычного офиса к психозу;
- save/continue/load/reset работают на уровне чекпоинтов;
- старый prototype UI/tutorial не просачивается в production flow;
- standalone build стабилен на целевом разрешении.

Всё сверх этого — polish; релизный буфер — нет.
