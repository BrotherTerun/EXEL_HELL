# EXEL HELL — production status and tasks v2

**Checkpoint:** 2026-08-16 ~08:30 (+03:00)  
**Branch:** `jam/final-build`

## Implemented and considered stable

### Core gameplay
- Formula Cells 2.0 interaction grammar;
- single/range MOVE;
- movable empty FormulaCells;
- SUM/SORT activation by DROP;
- report persistence/dependencies;
- plain aggregate ReportCell delivery targets;
- turn-based #REF! with dynamic telegraphed spawn/spread;
- fixed REF telegraph layering and distinct spawn/spread visuals;
- 4 baseline FC2 runtime levels.

Core is frozen except blocker bugs.

### Level authoring
- dedicated `LevelConstructor` scene;
- F2 panel show/hide;
- authoring guard: turns/deadline/#REF neutralized;
- L1–L4 clean templates;
- edit/move token/formula layout using normal FC2 drag + constructor controls;
- edit Data values, Report Goals and key anomaly/turn parameters;
- sync current live board back into authored config before export/rebuild;
- export `PrototypeLevelConfig` initializer to clipboard;
- import the same initializer from clipboard as working template.

### Application / scenes
- `Menu.unity`;
- `Gameplay.unity`;
- `LevelConstructor.unity`;
- persistent application shell;
- NEW GAME / CONTINUE / LOAD select a level then load `Gameplay`;
- reset reloads Gameplay;
- main menu transition uses Menu scene;
- save/checkpoint + settings persistence;
- authored layout is applied before first production render;
- production scenes guarantee a Main Camera.

## Current parallel work

### Level design — user
Use `LevelConstructor` to rebuild/validate final L1–L5:
- formula inventory;
- token/key placement;
- Report Goal composition/modes;
- turn budget;
- #REF! cadence;
- legal baseline routes;
- smoke/balance.

Current L1–L4 catalog entries are baselines and may be replaced by accepted constructor exports.

### Narrative infrastructure — assistant next
Build `NarrativeLayer v1` as an event listener/router without changing core gameplay:
- trigger definitions;
- effect definitions;
- once/delay/queue;
- gameplay hooks;
- debug receiver/logs;
- `TypewriterCellMessage` overlay;
- API hooks for chat/psychosis/audio/protagonist.

See `14_Narrative_Layer_Spec.md`.

## Next production blocks after convergence

1. **Narrative/UI shell** — chat button, boss/department channels, history, unread, toasts, workday clock, protagonist container/renderer.
2. **Visual pass** — pixel-art office, unified pixel-UI frame/menus/chat, readable spreadsheet skin.
3. **Psychosis** — 0–4 escalation; presentation first, maximum 2–3 gameplay distortion primitives.
4. **Narrative content integration** — events per accepted L1–L5.
5. **Reusable animation/presentation** — drag/drop, SORT/SUM, REF, report, toast, typewriter, protagonist reactions, glitches.
6. **SFX/music** — small semantic library + normal/anxious music state.
7. **Full integration/regression** — L1→L5, save/load/reset, resolution, language, narrative/psychosis.
8. **itch/build/release**.

## Known non-blocking development quirks

### Direct `Gameplay.unity` launch
Direct scene play is a developer shortcut, not the production boot path.

Current behavior deliberately removes the persistent Application shell if it was created with `GameplayActive=false`. Consequences:
- old standalone fallback HELP may appear;
- gameplay `MENU` button has no Application instance to open, therefore no-op;
- a minor direct-launch bootstrap flicker may remain.

Normal release path `Menu → Gameplay` does not have these differences. Fix direct-launch parity only if it becomes useful enough to justify the time.

### Constructor export cosmetics
Exporter currently serializes combined `ReportGoals` as numeric enum flags, e.g. `(PrototypeReportGoals)52`. This is valid C# but less readable than explicit `A | B | C`. Cosmetic improvement only.

## Explicitly closed / cut directions

Do not reopen during finalization unless a blocker proves the current solution invalid:

- real-time game clock;
- old toolbar SUM/SORT workflow;
- mandatory CUT/PASTE into formulas;
- fixed immovable FormulaCells;
- genuine Excel formula parser/editor;
- procedural level generator;
- large narrative graph/editor;
- full chat input/social simulator;
- large psychosis ruleset;
- merging production back into `agent/prototype-core`.

## Definition of “current build ready for content”

The infrastructure is ready for content when:
- Unity compiles without blockers;
- Menu NEW/CONTINUE/LOAD reach the proper level in Gameplay;
- constructor import/export round-trip works;
- accepted level exports are committed to `PrototypeLevelCatalog`;
- baseline legal routes and turn budgets are recorded.

At this checkpoint the remaining uncertainty should be **level content and presentation**, not another core interaction redesign.
