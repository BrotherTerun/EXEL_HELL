# EXEL HELL — balance artifacts

## Current source

Use:

- `../12_MVP_0.5_Balance_Model_v2.md` — definitions/guardrails;
- `current_level_baselines.csv` — compact current L1–L4 values;
- `EXEL_HELL_FC2_Balance_Baselines.xlsx` — formula-driven workbook and blank authoring template.

## Historical workbook

`../EXEL_HELL_MVP04_Balance_Model.xlsx` is **frozen historical MVP 0.4 material**.

It belongs to the clipboard-era action economy and the MVP 0.4 test matrix. Its old action costs/bands must not be copied directly into Formula Cells 2.0 balance work.

The historical workbook is retained because it documents the transition through:
- goal-combination compatibility;
- Base Cost / Threat Margin thinking;
- smoke-test selection.

The text companion `../09_MVP_0.4_Balance_Model.md` explains the assumptions of that version.

## Current workbook sheets

### Current Levels
Current runtime baseline rows for L1–L4.

Formula-driven columns include:
- `R = B - C0`;
- `F_tax = max(0,F_extract-F_delivery)`;
- `FHL = (F_tax + 0.5*F_move)/C0`;
- approximate `N_A` outbreak opportunities.

These values must be replaced after a constructor-redesigned level is accepted.

### Model
Glossary and authoring guardrails.

### Authoring Template
Blank rows for new layouts/L5. It calculates basic derived metrics but deliberately leaves `PCI/SP/SA/WP/Slack/PL/AT` to explicit authoring analysis because those need geometry/threat information rather than guessed numbers.

## Precision policy

Do not make the sheet look more certain than the game is.

- `C0` should be exact only after writing a legal route.
- L4 baseline is marked approximate where appropriate.
- `N_A` is a cadence approximation, not spread count.
- `AT` should not be filled from intuition; use actual Unity spawn/intent traces for critical entities.
- `PL/AT/D` are diagnostics, not design objectives.

## Update workflow

Accepted constructor export → commit level config → update level authoring sheet → update CSV/workbook → smoke in Unity → then fill observed threat/replan notes.
