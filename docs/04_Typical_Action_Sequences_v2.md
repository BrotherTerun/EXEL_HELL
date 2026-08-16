# EXEL HELL — typical action sequences v2 / FC2

**Status: CURRENT action-economy reference.**

Count only successful turn-spending actions.

## Free interaction
- click/select;
- `Shift+Drag` rectangular selection;
- selection changes;
- read/hover/formula bar;
- SUBMIT check;
- invalid drop.

## One-action primitives
- token MOVE;
- selected-range MOVE;
- empty FormulaCell MOVE;
- key → empty SORT;
- numeric selection → empty SUM;
- DELETE/quarantine.

## Core sequences

| Sequence | Cost | Notes |
|---|---:|---|
| Single token relocation | 1 | `token → destination` |
| Range relocation | 1 | any number of selected movable tokens, relative offsets preserved |
| Direct Data/direct-value goal → ReportCell | 1 | e.g. found salary → direct report target |
| Direct aggregate into report-SUM | 1 | after operands are already in a valid selection |
| Workspace SUM → plain aggregate ReportCell | 2 | SUM computation + aggregate MOVE delivery |
| FieldKey/RecordKey → SORT | 1 | full valid spill required |
| SORT + inline report SUM | 2 | after key/data assumptions are satisfied |
| SORT + workspace SUM + plain report delivery | 3 | common scarce-SUM variant |
| Move empty formula first | +1 | meaningful only if placement changes geometry/threat |
| Move one spill blocker + SORT + SUM | 3 | blocker MOVE + two formula actions |
| Quarantine + SORT + SUM | 3 | DELETE + two formula actions |
| Evacuate threatened critical token + SORT + SUM | 3 | defensive MOVE + two formula actions |
| Extract occupied SUM result | +1 | if moved into final ReportCell, this is useful delivery, not pure tax |
| Extract occupied SORT key | +1 | parking-only extraction is formula handling tax |
| Reuse one SORT for two keys | 3 before computations | SORT A → key A MOVE out → SORT B |

## Report target modes and action cost

### DirectValue
No formula required in target. Deliver the correct Data token.

### InlineSUM
Report target owns a SUM formula. After preparation, aggregation itself costs 1 action and pins the result/formula together until occupant is moved out.

### PlainDelivery
Aggregate target has no formula. Compute elsewhere, then MOVE result into ReportCell.

This normally adds one action compared with an equivalent inline report-SUM, but the delivery MOVE also frees the workspace SUM. Therefore it can be an intentional way to reuse a scarce formula rather than pure overhead.

When comparing formula inventories, classify that MOVE as `F_delivery`, not pure `F_tax`.

## Multi-goal examples after preparation

### Two independent aggregate goals

Two dedicated report-SUMs:

`range A → report SUM A → range B → report SUM B` = **2 computation actions**.

One reusable workspace-SUM + two plain report targets:

`range A → SUM → aggregate A MOVE to report → range B → SUM → aggregate B MOVE to report` = **4 actions**.

The +2 is acceptable only if sharing the formula creates a meaningful placement/threat/order decision.

### One workspace-SUM, one inline target

Depending on order, a mixed setup may spend 3 actions for two aggregate computations: one inline SUM, one workspace SUM + delivery. This is a useful middle point for formula-scarcity experiments.

### Two SORT-dependent aggregate goals

Sufficient infrastructure (`2 SORT + 2 inline report-SUM`) gives four useful formula activations after any filtering:

`SORT A → SUM A → SORT B → SUM B`.

One shared SORT adds an extraction between SORT activations. Whether that is good design depends on what the extracted key can usefully do next.

## Current semantic lower bounds

These are **lower bounds**, not ready-made levels. Geometry, filtering, formula inventory and anomaly may increase legal `C0`.

| Goal | `C_sem` | Reasoning |
|---|---:|---|
| `SalaryTotal` | 2 | SORT Salary + SUM |
| `OvertimeTotal` | 2 | SORT Overtime + SUM |
| `BonusTotal` | 2 | SORT Bonus + SUM |
| `BonusAtLeastFour`* | 4 | SORT Bonus + two filter MOVE + SUM |
| `SalaryOfMaxOvertime` | 3 | SORT Overtime + SORT Salary + direct MOVE |
| `SalaryForHoursBelowForty` | 5 | SORT Hours + SORT Salary + two filter MOVE + SUM |
| LowHours + OT Total + Bonus Total | 9 | 4 SORT + 2 filter MOVE + 3 SUM |

`*` Historical enum name; current implementation threshold is `bonus >= 5`.

## Formula scarcity comparison

For a proposed level variant:

1. record `C_sem` with sufficient infrastructure;
2. record exact legal `C0` for the authored inventory/layout;
3. calculate `Δ = (C0 - C_sem) / C_sem`;
4. separate meaningful spatial moves from formula maintenance;
5. calculate FHL from the balance model.

Authoring warning:

> If formula scarcity raises `C0` more than roughly 35% above `C_sem` without at least one real placement/order/threat choice, it is probably action tax rather than depth.

Plain aggregate ReportCells are therefore a **tool for experiments**, not automatically better design. Lower formula count is valuable only if it changes decisions.

## Difficulty bands by useful route length

- **L0: 0** — answer already committed; not a puzzle.
- **L1: 1–2** — atomic operation / tutorial fragment.
- **L2: 3–4** — filtering, one relocation, one delivery/defence.
- **L3: 5–7** — several projections, dependency, meaningful reorder/reuse.
- **L4: 8+** — late multi-goal encounter; avoid as a solo goal.

Do not make final L5 difficult merely by pushing action count upward. Its late-game identity should come mainly from psychosis/presentation and controlled interface distortion.
