# EXEL HELL — current level authoring status

**Checkpoint:** 2026-08-16 ~08:30 (+03:00)

The files in this directory describe the **current runtime L1–L4 baseline** from `PrototypeLevelConfig.cs`. They are useful reference layouts, but level design is now actively being iterated with `LevelConstructor`.

If a constructor export is accepted and differs from these sheets, update/replace the matching sheet and the current balance artifacts. Runtime config wins until documentation catches up.

## Baseline files

### `01_Formula_Tutorial.md`
L1 — Training Reconciliation.

Current baseline:
- no REF;
- SalaryTotal + OvertimeTotal;
- C_sem=4, C0=5, B=8;
- teaches moving D2 SORT to the better B lane.

### `02_Spill_Reuse.md`
L2 — Watched Reconciliation.

Current baseline:
- same puzzle as L1;
- FirstOutbreak=4;
- light visible threat;
- intended A/B control against L1.

Filename is historical; content currently describes light-pressure L2, not a separate final “spill reuse” mechanic lesson.

### `03_Aggregate_Dependency.md`
L3 — Inconsistent Data.

Current baseline:
- max-overtime salary + low-hours salary;
- H2 direct-value ReportCell;
- H5 aggregate target;
- C0≈7, B=11;
- early REF / replan target.

### `04_Formula_Pressure.md`
L4 — Final Reconciliation baseline.

Current baseline:
- LowHoursSalary + OvertimeTotal + BonusTotal;
- three SORTs for four projections;
- C_sem=9, C0≈10, B=14;
- multiple outbreak windows.

This is a baseline encounter; the final jam level sequence may tune/replace it.

## Current authoring rules not reflected in every old sheet paragraph

Since the sheets were first written, aggregate report targets gained an additional valid mode:

- ReportCell + SUM (inline computation);
- **plain ReportCell (aggregate computed elsewhere and delivered by MOVE).**

This is now intentionally available for formula-scarcity experiments. When a level is rebuilt, its sheet should explicitly list target mode per goal.

## L5 target

L5 does not exist in runtime catalog at this checkpoint.

Design target:
- approximately L4 computational complexity rather than a much higher C0;
- final difficulty/identity from NarrativeLayer + Psychosis 3→4;
- controlled interface distortion, not random unreadability;
- report remains solvable under real rules;
- no new core spreadsheet function required.

Create `05_*.md` only after an actual accepted constructor layout/config exists; do not document invented coordinates in advance.

## Required sheet format after constructor acceptance

Every final level sheet should contain:
- runtime ID / RU+EN name;
- board/dataset;
- goals + answer + Report target mode;
- formula inventory/start coordinates;
- turn/anomaly parameters;
- exact starting-layout notes;
- explicit legal no-REF route;
- C_sem / C0 / B / R;
- F_act/F_move/F_extract/F_delivery/F_tax/FHL;
- meaningful formula placement choices;
- expected + observed REF interactions;
- smoke result and redesign criteria.
