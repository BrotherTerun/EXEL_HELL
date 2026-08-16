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

Effects with no renderer are skipped safely. Debug receiver completes immediately.

## Play Mode smoke test

Checkout `feature/narrative-layer-v1`, open the Gameplay scene and enter Play Mode.

In Editor/Development Build the runtime bootstrap installs `DebugNarrativeReceiver` and `NarrativeDebugHarness`. Release builds do not install the smoke harness.

Expected console sequence includes:

```text
[NARRATIVE] Runtime bootstrap complete...
[NARRATIVE] Loaded 2 event(s)...
[NARRATIVE/TEST] BEGIN...
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
2. Automatic smoke test emits MATCH / EFFECT / RECEIVER and duplicate-once SKIP logs.
3. Normal gameplay emits the expected read-only trigger logs.
4. No visible worksheet/UI behaviour changes.
5. No trigger or dismiss operation increments gameplay turns by itself.
6. Reset/scene changes rebind the probe without duplicate narrative roots.

After this passes, the debug sample event database can be replaced by authored per-level events and real receivers: protagonist renderer, cell manifestation renderer, chat/toast UI, psychosis and audio.
