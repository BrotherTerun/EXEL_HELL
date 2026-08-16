# NarrativeLayer v1

## Purpose

Renderer-agnostic event layer between gameplay state and presentation. v1 does not alter worksheet rendering, turn economy, formulas, goals or #REF! behaviour.

Runtime flow:

`gameplay state -> NarrativeGameplayProbe -> NarrativeSignals -> NarrativeEventRunner -> presentation receiver`

## Triggers in v1

- `LevelStart`
- `ActionNumber`
- `FirstRefSpawn`
- `RefSpread`
- `CellDestroyed`
- `GoalCompleted`
- `ReportSubmitted`
- `LevelCompleted`
- `ManualDebug`

Event definitions can optionally filter by `levelId`, `triggerNumber` (for `ActionNumber`) and `triggerSubjectId` (for a specific goal/cell/subject).

## Effects reserved by v1 API

- `CellMessage`
- `ProtagonistLine`
- `BossChatMessage`
- `DepartmentChatMessage`
- `Toast`
- `VisualGlitch`
- `PsychosisDelta`
- `Sound`

Presentation effects carry their own lifetime policy:

- `Timed`
- `OnClick`
- `TimedOrClick`

This is intentionally per effect instance. No effect type is permanently tied to one dismissal mode.

`ProtagonistLine` additionally carries `Normal / Tired / Alarmed / Psychotic` mood metadata for the later avatar renderer.

## Queue contract

The runner sends one effect ticket to one compatible presentation receiver at a time. A receiver calls `ticket.Complete()` when presentation is finished. This allows a future `OnClick` protagonist/cell manifestation to block the narrative presentation queue until the player dismisses it, without spending a gameplay action.

Effects with no renderer are skipped safely. Debug receiver completes immediately. A real renderer always has priority over the debug fallback. Destroyed Unity receivers are detected through Unity object lifetime semantics so a recreated/closed UI cannot permanently stall the queue.

## Authoring validation

`NarrativeDefinitionValidator` runs when event definitions are loaded in Editor/Development Build. It reports malformed authoring data without mutating gameplay.

Current checks include:

- duplicate event IDs;
- `once=true` without stable ID;
- negative delay;
- invalid `ActionNumber` trigger number;
- missing effects;
- missing text for text-based effects;
- missing cell coordinate for `CellMessage`;
- invalid timed lifetime (`duration <= 0`);
- zero `PsychosisDelta` warning;
- missing audio key for `Sound`.

A valid database emits:

```text
[NARRATIVE/VALIDATION] OK — no authoring issues found.
```

## Play Mode self-test

Checkout `feature/narrative-layer-v1`, open the Gameplay scene and enter Play Mode.

In Editor/Development Build the runtime bootstrap installs `DebugNarrativeReceiver` and `NarrativeDebugHarness`. Release builds do not install the smoke harness.

The harness installs two synthetic events and verifies its own result. The important final line is:

```text
[NARRATIVE/SELF-TEST] PASS — matches=2, onceSkips=1, effects=3, missingReceivers=0.
```

A failed expectation produces `[NARRATIVE/SELF-TEST] FAIL` as an error with actual/expected counters.

The underlying detailed sequence also includes:

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

The exact interleaving can differ by a frame because one sample event has a short delay.

## Real gameplay observation

While playing, Console should also report:

- `LevelStart` after the prototype binds;
- `ActionNumber` when the gameplay turn changes;
- `FirstRefSpawn` for the first newly corrupted cell;
- `RefSpread` for later newly corrupted cells;
- `CellDestroyed` when a cell reaches Destroyed state;
- `GoalCompleted` once per satisfied report goal;
- `ReportSubmitted` when the existing Submit button is clicked;
- `LevelCompleted` when the prototype reaches its accepted/finished state.

## v1 acceptance criteria

1. Project compiles in Unity 6000.3.1f1.
2. Automatic self-test emits `SELF-TEST PASS`.
3. Authoring validation reports `OK` for the synthetic database.
4. Normal gameplay emits the expected read-only trigger logs.
5. No visible worksheet/UI behaviour changes.
6. No trigger or dismiss operation increments gameplay turns by itself.
7. Reset/scene changes rebind the probe without duplicate narrative roots.

After this passes, the debug sample event database can be replaced by authored per-level events and real receivers: protagonist renderer, cell manifestation renderer, chat/toast UI, psychosis and audio.
