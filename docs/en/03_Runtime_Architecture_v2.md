# EXEL HELL — runtime architecture map v2

**Status: CURRENT technical map.**

This is the shortest answer to “where does the game actually live now?” after the scene/constructor split.

## 1. Production scenes

```text
Menu.unity
  └─ PrototypeSceneArchitecture(role=Menu)

Gameplay.unity
  ├─ Gameplay Scene Context
  │   └─ PrototypeSceneArchitecture(role=Gameplay)
  ├─ Worksheet Core
  │   └─ ExcelHellPrototype
  └─ Gameplay Runtime Services
      ├─ PrototypeLevelDatasetAdapter
      ├─ PrototypeFormulaCells
      ├─ PrototypeFormulaLevelCompatibility
      ├─ PrototypeRefTelegraphLayer
      └─ PrototypeLevelFlow

LevelConstructor.unity
  ├─ Level Constructor Scene Context
  │   └─ PrototypeSceneArchitecture(role=Constructor)
  ├─ Worksheet Core
  │   └─ ExcelHellPrototype
  └─ Authoring Runtime Services
      ├─ PrototypeLevelDatasetAdapter
      ├─ PrototypeFormulaCells
      ├─ PrototypeFormulaLevelCompatibility
      ├─ PrototypeAuthoringGuard
      └─ PrototypeLevelConstructor
```

Some components may still have legacy runtime auto-bootstrap code for compatibility, but production scenes explicitly own/normalize the required service set.

## 2. Application shell

`ExcelHellApplication`

Cross-scene responsibility:
- boot/menu/pause/load/settings;
- level checkpoint persistence;
- transition state (`GameplayActive`, `Paused`);
- selected saved level → `PrototypeLevelRuntime`;
- narrative flags persistence hook.

Normal release path:

```text
BeforeSceneLoad: ExcelHellApplication
        ↓
Menu scene
        ↓ NEW / CONTINUE / LOAD
set PrototypeLevelRuntime.CurrentIndex
        ↓
Gameplay scene
        ↓
SceneArchitecture creates/applies current level
```

## 3. Level data

`PrototypeLevelConfig.cs`

Contains:
- `PrototypeLevelDataset`;
- token/formula/goal placement records;
- anomaly/turn parameters;
- static `PrototypeLevelCatalog`;
- `PrototypeLevelRuntime.CurrentIndex` access.

Current catalog baseline: L1–L4.

Final level authoring happens through constructor, but accepted source ultimately returns to this catalog for runtime.

## 4. Worksheet core

`ExcelHellPrototype.cs`

Still contains much of the prototype domain + runtime UI in one component:
- `CellModel[,]`;
- selection;
- goals;
- turns/deadline;
- anomaly intents/state;
- generated worksheet/sidebar UI;
- legacy action methods.

Do not undertake a late full domain/view refactor. FC2 and production adapters deliberately wrap this core.

Future NarrativeLayer should observe explicit hooks around it rather than make UI text the canonical state.

## 5. Level adapter

`PrototypeLevelDatasetAdapter.cs`

Responsibilities:
- clear legacy seed state;
- apply authored dataset/tokens/formulas/goals;
- rebuild required-source semantics;
- validate report/formula combinations;
- initialize anomaly/refresh.

Production scene invokes its apply path before first render; compatibility LateUpdate remains.

Current report authoring rules:
- DirectValue → plain ReportCell;
- Aggregate → inline SUM **or plain delivery**;
- aggregate target with SORT invalid/suspicious.

## 6. FC2 interaction layer

`PrototypeFormulaCells.cs`

Owns current player grammar over the prototype board:
- Drag=MOVE;
- Shift+Drag=SELECT;
- formula overlays/interaction;
- range MOVE;
- SUM/SORT drop activation;
- formula mobility;
- drag ghost;
- formula bar interaction/presentation.

Treat this as frozen gameplay code except blockers.

## 7. Compatibility layer

`PrototypeFormulaLevelCompatibility.cs`

Keeps old prototype UI assumptions from leaking into FC2 levels:
- hides obsolete toolbar formula controls;
- disables legacy tutorial assumptions;
- restores threat/required semantic flags where needed.

This is transitional but intentionally retained for jam stability.

## 8. REF presentation

`PrototypeRefTelegraphLayer.cs`

Dedicated stable overlay above full cell/FormulaCell presentation.

Spawn:
- amber translucent fill;
- bright border.

Spread:
- red translucent fill;
- no border.

No raycast interception.

## 9. Level progression

`PrototypeLevelFlow.cs`

Responsibilities include:
- current-level completion/progression integration;
- REF-disabled compatibility behavior;
- saving progression/completion through Application shell.

Final narrative completion signals should hook cleanly around this flow rather than replace it.

## 10. Constructor

`PrototypeLevelConstructor.cs`

Runtime authoring UI/config synchronizer/import/export. See `13_Level_Constructor_Authoring.md`.

`PrototypeAuthoringGuard.cs`

Ensures constructor scene cannot accidentally advance turns/anomaly while using normal FC2 manipulation.

## 11. Planned next hosts

Under Gameplay runtime/scene context, add small explicit hosts for:

```text
NarrativeEventRunner
NarrativePresentationRouter
Narrative/UI Shell
PsychosisController
Audio/Presentation hooks (only as needed)
```

Avoid a new global bootstrap per feature. Scene ownership is now the preferred architecture.

## 12. Stability rule

The project intentionally contains prototype-era technical debt. During the jam, prefer adapters/explicit scene hosts over a clean-room rewrite.

Refactor only when technical debt creates a concrete blocker such as:
- wrong first frame;
- duplicated input;
- service surviving the wrong scene;
- untestable narrative trigger;
- broken save/progression.

Do not refactor solely because a class is large or reflection is inelegant during finalization.
