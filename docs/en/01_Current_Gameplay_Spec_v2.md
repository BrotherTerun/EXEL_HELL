# EXEL HELL — current gameplay specification v2

**Status: CURRENT / FC2 frozen core.**  
Core changes after this point are blocker fixes only.

## 1. Turn economy

### Free
- click/select one cell;
- `Shift + Drag` rectangular selection;
- change/clear selection;
- hover/read data and Formula Bar;
- invalid drop/action;
- `SUBMIT` check.

### Costs 1 turn on success
- MOVE one movable token;
- MOVE selected token payload as one rectangular translation;
- MOVE an empty FormulaCell property;
- drop FieldKey/RecordKey into empty `=SORT()`;
- drop a valid selected numeric range into empty `=SUM()`;
- DELETE/quarantine.

After a successful action the anomaly resolves once.

## 2. Input grammar

`Drag = MOVE`  
`Shift+Drag = SELECT RANGE`

There is no Ctrl/disjoint selection. Selection is a contiguous rectangle.

The Formula Bar is read-only/generated. There is no formula typing.

## 3. Cell layers

A worksheet coordinate can simultaneously own independent properties:

1. **Report property** — permanent authored report infrastructure.
2. **Formula property** — none / SUM / SORT; movable when empty.
3. **Occupant token** — Data, FieldKey, RecordKey, Aggregate or Label.
4. **Anomaly state** — Normal / Corrupted / Destroyed.

Operational priority when dragging from a formula cell:

`occupant first → formula property only when empty`

Report property does not move when formula/occupant moves.

## 4. MOVE

### Single token

Drag a movable occupant to a legal target. Distance/path does not cost extra; only final legality matters.

A plain ReportCell is a legal destination when empty.

### Range MOVE

After `Shift+Drag`, dragging a movable occupied cell inside a multi-cell selection moves the selected movable token payload as one action.

Rules:
- relative positions are preserved;
- ordinary empty source cells are not payload;
- own moving source occupants may overlap their translated footprint;
- destination coordinates must be on board, Normal, non-Formula for token placement and free of foreign occupants;
- successful range MOVE clears selection.

Dragging a cell outside the current selection moves only the source top layer.

### Formula MOVE

An **empty** FormulaCell may be dragged to another legal empty coordinate, including an empty ReportCell.

If the FormulaCell contains an occupant, the first drag moves the occupant and leaves the formula property behind. Only then can the formula move.

This lifecycle is shared by SUM and SORT:

`empty formula → input drop → occupied formula → occupant MOVE out → empty formula → formula MOVE`

## 5. SUM

Activation only by DROP:

`selected numeric rectangle → drag/drop → empty =SUM()`

Requirements:
- at least two numeric operands;
- operands are Data or Aggregate tokens;
- source cells must be Normal;
- ordinary empty Normal cells inside the rectangle are allowed/ignored;
- keys/labels invalidate the range;
- formula target cannot be inside its own source selection;
- target formula must be empty/available.

Resolution:
1. calculate the sum;
2. record provenance/source IDs;
3. ordinary numeric sources are destructively consumed;
4. numeric occupants of ReportCells are **persistent operands**: read but not consumed;
5. resulting Aggregate becomes occupant of the SUM FormulaCell;
6. Formula Bar may show generated `=SUM(A1:B3)`;
7. spend one action.

## 6. SORT

Activation only by DROP:

`one FieldKey/RecordKey → drag/drop → empty =SORT()`

Rules:
- FieldKey spill direction = **down**;
- RecordKey spill direction = **right**;
- no fallback direction;
- full semantic schema positions are respected;
- spill must fit legal Normal/non-Formula destination cells;
- moving tokens already belonging to the spill may overlap their own source positions;
- a foreign occupant blocks the spill;
- invalid spill commits no action;
- key becomes occupant of the SORT FormulaCell;
- associated Data tokens move into spill positions;
- Formula Bar may show generated `=SORT(sourceAddress)`;
- spend one action.

To reuse the same SORT, MOVE its key occupant out first.

## 7. Report goals and ReportCells

Report property is protected infrastructure. A target may also carry a formula property.

There are three authoring modes:

### DirectValue
Example: `SalaryOfMaxOvertime`.

Target is a **plain ReportCell**. The required Data token is moved directly there.

A formula on a direct-value target is considered suspicious and produces a validation warning.

### Aggregate / InlineSUM
Target ReportCell also owns `=SUM()`.

The selected operands are dropped directly into the report formula. The result is computed and stored there in one formula action.

### Aggregate / PlainDelivery
Target is a **plain ReportCell without formula**.

The aggregate is computed elsewhere, then MOVE'd into the report target. This is intentionally valid: level design may reduce formula inventory and test whether shared/workspace formulas create real spatial decisions.

This rule replaced the earlier warning that every aggregate report target must itself be `=SUM()`.

An aggregate report target must not be `=SORT()`.

## 8. Report persistence

A numeric ReportCell occupant can be selected as an operand in a later SUM without being consumed.

This allows report-mediated dependencies: satisfy goal A, then use its committed value as part of goal B.

Persistence belongs to the **ReportCell**, not to a special SUM mode.

## 9. DELETE

DELETE costs one action on a legal worksheet cell.

Use cases:
- sacrifice an ordinary cell/data;
- quarantine a Corrupted cell.

Report header/active ReportCells are protected. Attempting to DELETE them is invalid and spends no turn.

Formula property protection follows FC2 implementation rules; DELETE is not a general formula-removal tool.

## 10. #REF! anomaly

`#REF!` is a persistent, telegraphed process.

Level config controls:
- `FirstOutbreakTurn`;
- `RespawnDelayTurns`;
- `ActiveOutbreakDelayTurns`;
- `CorruptionStepsBeforeDestroy`;
- `SpawnPreferredDistance`;
- `SpawnDistanceVariation`;
- `SpawnCandidatePoolSize`.

Spawn positions are selected dynamically from the current board around report-critical data. Authors do not place spawn points.

### Telegraphs
- future outbreak spawn = translucent amber fill + bright border;
- active spread intent = translucent red fill, no border;
- both can exist simultaneously;
- if both point to the same target, spawn visual wins;
- overlays are non-raycasting and live above FormulaCell presentation.

### Resolution
A successful player action advances the pending outbreak/spread/corruption process. Selection and invalid actions do not.

Report interface cannot be chosen as outbreak/spread target.

## 11. Win / lose

Win:
- all authored Report Goals are satisfied by their target cells;
- player submits the report.

Current SUBMIT primarily validates the required final value/token semantics encoded by the goal. Provenance exists but is not a victory condition.

Lose:
- current runtime deadline/turn budget expires before successful submission;
- particular board states may become practically or mathematically unrecoverable before the formal deadline.

The final presentation maps the turn budget onto `09:00–18:00`; it does not change core timing.

## 12. Formula-level UI compatibility

On FC2 levels the old toolbar SUM/SORT/CUT/PASTE/clipboard workflow is not the gameplay grammar. Legacy controls/tutorials are hidden/disabled by compatibility systems.

Kept:
- DELETE;
- SUBMIT;
- RESET;
- formula bar;
- table/report/status information until final UI shell replaces presentation.

## 13. Core invariants

Do not change without reopening core design:

- turn-based deterministic action economy;
- Drag=MOVE / Shift+Drag=SELECT;
- formula activation by DROP only;
- empty formula mobility;
- occupied formula exposes occupant first;
- fixed SORT directions;
- destructive ordinary SUM sources;
- persistent numeric ReportCell operands;
- aggregate report target may be inline-SUM or plain delivery;
- telegraphed #REF!;
- no real Excel parser / formula typing.
