# EXEL HELL — LevelConstructor authoring guide

**Status: CURRENT technical workflow.**

The constructor is a runtime spatial authoring sandbox for quickly creating FC2 starting layouts from existing levels. It is intentionally smaller than a full Unity editor tool.

## 1. Scene

Open:

`Assets/Scenes/LevelConstructor.unity`

The scene uses the same worksheet interaction as Gameplay but enables `PrototypeAuthoringMode`.

`PrototypeAuthoringGuard` continuously:
- forces turn to 0;
- clears finished/deadline state;
- clears pending/current `#REF!` intent;
- restores cells to Normal;
- keeps tokens accessible;
- hides the ordinary turn display;
- shows authoring status instead of live anomaly information.

There is no manual REF placement. Real gameplay chooses outbreak cells dynamically.

## 2. Panel / F2

Constructor panel opens in Play Mode.

`F2` hides/shows the panel. Hiding the panel **does not leave authoring mode**; turns and anomaly remain disabled so the full board can be manipulated freely.

Panel background is intentionally fully opaque for readability.

## 3. Templates

Current buttons load clean snapshots of runtime catalog L1–L4.

A clean snapshot is captured at constructor startup. Switching back to a template restores that template within the current Play session rather than preserving previous edits.

## 4. Selecting/editing cells

Select a worksheet coordinate with the normal game interaction. The panel tracks the selected row/column.

Authoring operations can:
- clear token/formula;
- place Data;
- place RecordKey;
- place FieldKey;
- change selected Data value;
- place/remove SUM;
- place/remove SORT;
- assign/remove Report Goal;
- toggle REF enabled;
- change MaxTurns / first outbreak / respawn / active-outbreak delay.

Semantic Data/Key placement is unique by identity: placing the same semantic token elsewhere acts as relocation rather than duplication.

Placing a formula clears conflicting token state and vice versa so the exported layout does not contain illegal token/formula overlap.

## 5. Normal FC2 dragging is an authoring tool

This is critical:

> **ordinary gameplay MOVE operations performed while authoring are captured as starting layout state.**

Before Export/Rebuild and relevant panel mutations, constructor reads the live `CellModel[,]` and synchronizes:
- Data positions;
- key positions;
- labels;
- Formula property positions.

Therefore:

`drag C3 → E4`

means the export starts that token at E4.

Moving an empty `=SORT()` with the normal drag likewise changes its exported start coordinate.

### What is not captured as authored source

Computed runtime `AggregateToken` occupants are intentionally ignored when converting live board back to authored token layout. Accidentally solving part of a report in authoring mode should not bake computed intermediate results into the level start.

Dataset numeric values are edited through the constructor Data value control.

## 6. Report Goal authoring modes

Constructor does not automatically force every aggregate target to be a SUM formula.

Valid designs:
- DirectValue goal → plain ReportCell;
- aggregate goal → ReportCell + SUM;
- aggregate goal → plain ReportCell used as delivery target.

This supports experiments with fewer FormulaCells.

The level adapter warns only about genuinely suspicious combinations such as SORT on an aggregate report target or formula on a direct-value target.

## 7. Export

`EXPORT → CLIPBOARD` generates a C# block:

```csharp
new PrototypeLevelConfig
{
    ...
}
```

It includes:
- ID/name;
- board dimensions;
- ReportGoals flags;
- REF/formula mode;
- turn/anomaly parameters;
- dataset values;
- TokenLayout;
- FormulaLayout;
- GoalLayout.

Current cosmetic limitation: combined ReportGoals are emitted numerically, e.g. `(PrototypeReportGoals)52`. This is valid but less readable than explicit flag ORs.

The export is designed to be pasted into/converted to a builder method inside `PrototypeLevelCatalog`.

## 8. Import from clipboard

`IMPORT FROM CLIPBOARD` accepts the same C# initializer format emitted by Export.

Workflow:
1. export a level;
2. save/share/edit that text;
3. put initializer back into clipboard;
4. load another template if desired;
5. Import;
6. constructor parses dataset/layout/goals/parameters and rebuilds working board.

This provides a lightweight round-trip without inventing a separate JSON level format during the jam.

## 9. Rebuild

After structural edits constructor may request worksheet rebuild. The old worksheet core is destroyed and recreated; scene-level authoring services remain alive.

This is why `Worksheet Core`, `Scene Context` and runtime services are separate objects in the new scene architecture.

## 10. Fixed schema limitations

Current constructor is built around the existing prototype schema:

Records:
- ivanov;
- petrov;
- sidorov;
- volkova;
- kim.

Fields:
- hours;
- salary;
- overtime;
- bonus.

Current Report Goal vocabulary:
- SalaryTotal;
- OvertimeTotal;
- BonusTotal;
- BonusAtLeastFour (implementation currently means threshold >=5);
- SalaryOfMaxOvertime;
- SalaryForHoursBelowForty.

It is **not** currently a generic schema editor. Do not add arbitrary IDs/resize/content pipelines unless final level design actually requires them.

## 11. Recommended authoring workflow

For each candidate level:

1. pick closest L1–L4 template or clipboard import;
2. arrange keys/Data/Formulas using direct drag + panel;
3. assign goal cells/modes;
4. set REF/timing budget;
5. Export and save the initializer;
6. calculate a no-REF legal route / `C0`;
7. smoke the layout in real Gameplay with REF enabled;
8. adjust layout/timings in constructor;
9. when accepted, commit config into `PrototypeLevelCatalog`;
10. update `mvp05_levels/` + current balance artifacts.

The constructor is disposable authoring infrastructure; the committed `PrototypeLevelConfig` remains the runtime level definition.
