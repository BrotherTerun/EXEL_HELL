# EXEL HELL — NarrativeLayer v1 specification

**Status: PLANNED NEXT SYSTEM / not implemented at this checkpoint.**

The NarrativeLayer is intentionally smaller than a dialogue framework. Its job is to listen to gameplay, match authored events and route presentation effects without modifying the frozen FC2 rules.

## 1. Architectural boundary

Target flow:

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

The runner must not know how a chat window, speech bubble, glitch or cell message is rendered.

This keeps narrative content independent from the final visual pass.

## 2. Narrative event definition

Minimum data:

```text
id
levelId
trigger
once
delay
effects[]
```

Optional fields should be added only when actual content needs them. Do not build a generic condition graph during the jam.

## 3. Trigger vocabulary v1

Required:
- `LevelStart`
- `ActionNumber(N)`
- `FirstRefSpawn`
- `RefSpread`
- `CellDestroyed`
- `GoalCompleted`
- `ReportSubmitted`
- `LevelCompleted`
- `ManualDebug`

Possible later lightweight addition:
- `RandomInterval` / random-from-pool presentation events.

Do not make the first implementation depend on it.

## 4. Effect vocabulary

The router should understand effect intents even before every renderer exists:

- `CellMessage`
- `ProtagonistLine`
- `BossChatMessage`
- `DepartmentChatMessage`
- `Toast`
- `VisualGlitch`
- `PsychosisDelta`
- `Sound`

One event may dispatch several effects.

Example:

```text
trigger: FirstRefSpawn
once: true

effects:
- ProtagonistLine("...этого здесь раньше не было.", mood=Alarmed)
- BossChatMessage("Отчёт будет сегодня?")
- PsychosisDelta(+1)
```

## 5. Presentation lifetime

For effects that occupy screen space, support:

```text
DismissMode:
- Timed
- OnClick
- TimedOrClick

duration
priority
```

Dismiss click is presentation-only/free and must not count as a gameplay action.

## 6. ProtagonistLine

Recommended payload:

```text
text
mood
dismissMode
duration
priority
```

Renderer may later choose:
- sprite + speech bubble;
- sprite + subtitle;
- temporary tutorial hint;
- short reaction only.

The event layer should not care which visual representation is chosen.

## 7. TypewriterCellMessage

First required standalone renderer/effect.

Examples:
- `ПОМОГИТЕ`
- `НЕ СМОТРИ`
- `Я НЕ ХОЧУ СЧИТАТЬ`
- `ОНИ УЖЕ ЗДЕСЬ`
- `ЗАКРОЙ ФАЙЛ`
- `ПОЧЕМУ ТЫ ЕЩЁ РАБОТАЕШЬ`

Requirements:
- rendered as an overlay associated with a cell/region;
- typewriter reveal with optional pauses/jitter;
- no real DataToken created;
- must not affect SORT, SUM, MOVE, Report Goal checks or REF targeting;
- no raycast interference unless the effect explicitly supports dismissal;
- disappears/clears according to its presentation lifetime.

Narrative text is allowed to lie; real gameplay telegraphing must not.

## 8. Queue / concurrency

Runner must prevent two narrative events from fighting over the same exclusive renderer.

Minimum policy:
- triggers may enqueue immediately;
- effects with independent receivers may run concurrently;
- exclusive protagonist/cell-message presentation can queue by priority;
- presentation delays use unscaled/realtime timing where appropriate so pause behavior is explicit.

Do not serialize gameplay actions behind narrative unless a specific scripted moment requires it.

## 9. Once-only and persistence

`once=true` prevents repeated trigger dispatch during the current appropriate scope.

For events that must remain consumed across level reload/save, use an explicit narrative flag stored through `ExcelHellApplication.AddNarrativeFlag()` / progress `NarrativeFlags`.

Do not persist every cosmetic random event.

## 10. Gameplay hooks

NarrativeLayer should observe, not own:
- successful action counter;
- outbreak spawn becoming active;
- spread intent resolution;
- cell transition to Destroyed;
- goal satisfaction transition;
- Submit result;
- level completion.

Prefer explicit event/signal hooks as FC2 is touched, rather than polling text UI where feasible. Avoid changing core outcomes.

## 11. Debug receiver v1

Before final UI exists, provide `DebugNarrativeReceiver`.

Expected logs:

```text
[NARRATIVE/TRIGGER] FirstRefSpawn
[NARRATIVE/MATCH] L2_FirstRef_01
[NARRATIVE/EFFECT] ProtagonistLine "...этого здесь раньше не было."
[NARRATIVE/SKIP] L2_FirstRef_01 — once already consumed
```

Recommended categories:
- `[NARRATIVE/TRIGGER]`
- `[NARRATIVE/MATCH]`
- `[NARRATIVE/EFFECT]`
- `[NARRATIVE/SKIP]`

## 12. Debug harness

Provide a small development harness/API able to fire:
- LevelStart;
- ActionNumber(3);
- FirstRefSpawn;
- CellDestroyed(...);
- GoalCompleted(...);
- ManualDebug event.

This proves runner/matching/dispatch independently from final presentation.

## 13. Level content relationship

Narrative definitions should be keyed by `levelId` but not embedded into worksheet `TokenLayout/FormulaLayout` unless there is a strong implementation reason.

Gameplay layout and narrative timing need to be independently editable because user level authoring is happening in parallel.

After final L1–L5 layouts are accepted, authored narrative content can be bound to those stable level IDs/action moments.

## 14. Explicit non-goals v1

Do not build:
- graph/node editor;
- branching player dialogue system;
- text input;
- full localization content pipeline beyond existing language strategy;
- cinematic timeline framework;
- generalized quest system;
- psychosis implementation inside NarrativeLayer;
- audio manager inside NarrativeLayer.

The layer dispatches intents to those systems.

## 15. Acceptance criteria

NarrativeLayer v1 is ready to freeze when:
- gameplay triggers can be observed without changing turn/core results;
- event matching by level/trigger works;
- once-only works;
- delay works;
- multiple effects dispatch from one event;
- debug receiver identifies trigger/match/effect/skip clearly;
- manual harness can fire representative events;
- TypewriterCellMessage can display/clear without becoming gameplay data;
- placeholder hooks exist for chat/protagonist/psychosis/sound;
- no new frame-order/raycast blocker is introduced.
