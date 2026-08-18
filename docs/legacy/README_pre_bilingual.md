# EXEL HELL — documentation index

**Checkpoint:** 2026-08-16 ~08:30 (+03:00)  
**Production branch:** `jam/final-build`  
**Core status:** Formula Cells 2.0 / FC2 approved and frozen except blocker bugs.

This directory deliberately contains two layers:

1. **CURRENT** production documentation — what the game is now and what is being built for the jam release.
2. **HISTORICAL** research — earlier MVP specifications, smoke tests, playtests and devlogs that explain why the current design exists.

Do not silently merge the two. When documents disagree, use this order:

1. runtime code on `jam/final-build`;
2. documents marked **CURRENT** below;
3. accepted level authoring sheets under `mvp05_levels/`;
4. historical MVP/test/devlog material.

For exact authored coordinates and anomaly timings, `Game/Assets/_Project/Scripts/Prototype/PrototypeLevelConfig.cs` remains the technical source of truth until a constructor export is accepted and committed.

## CURRENT — read these first

| Document | Purpose |
|---|---|
| `00_ExcelHell_Concept_v2.md` | Current concept, pillars and planned final form. |
| `01_Current_Gameplay_Spec_v2.md` | Canonical FC2 rules and action economy. |
| `02_Production_Status_and_Tasks_v2.md` | Implemented / in progress / planned / explicitly cut. |
| `03_Runtime_Architecture_v2.md` | Runtime object/service map after the scene split. |
| `04_Typical_Action_Sequences_v2.md` | Current reusable action sequences and lower bounds. |
| `10_Application_Shell_v2.md` | Menu / Gameplay / LevelConstructor architecture and persistence. |
| `11_MVP_0.5_Formula_Cells_Spec_v2.md` | Implemented Formula Cells 2.0 specification. |
| `12_MVP_0.5_Balance_Model_v2.md` | Current multidimensional balance model and authoring gates. |
| `13_Level_Constructor_Authoring.md` | Constructor workflow, import/export and limitations. |
| `14_Narrative_Layer_Spec.md` | **Planned next system:** renderer-agnostic narrative events. |
| `15_Final_Presentation_and_Psychosis.md` | **Planned final target:** chat, clock, pixel-office shell, protagonist, psychosis. |
| `16_Production_Plan_Current.md` | Current critical path and cut/freeze rules. |
| `17_Test_History_and_Current_Gates.md` | What old tests proved and what still needs validating on FC2. |
| `balance/README.md` | Balance artifacts and version boundary. |
| `balance/current_level_baselines.csv` | Machine-readable current L1–L4 baseline. |
| `balance/EXEL_HELL_FC2_Balance_Baselines.xlsx` | Current workbook with formulas + authoring template. |
| `mvp05_levels/README_CURRENT.md` | Status of current level sheets and L5 target. |

## Current runtime vs final target

The runtime catalog currently contains **four authored FC2 levels**. They are a stable baseline, not a promise that their layouts are final: level design is now being iterated through `LevelConstructor`.

Final jam target: **five levels**.

1. L1 — teach FC2 without `#REF!`.
2. L2 — same interaction language under light anomaly pressure.
3. L3 — first real replan encounter.
4. L4 — full multi-goal tactical pressure.
5. L5 — roughly L4 computational load, but identity/difficulty comes from the planned psychosis/interface unreliability rather than a large increase in action tax.

## HISTORICAL — keep as evidence, not current rules

- `00_ExcelHell_Concept.md` — original concept; contains the retired real-time target.
- `01_Prototype_Features.md`, `02_Design_Tasks.md` — first SUM/CUT/PASTE prototype scope.
- `05_MVP_0.3.md`, `06_MVP_0.3_Balance_Model.md` — semantic-data / threat-horizon research.
- `07_MVP_0.4_Backlog.md`, `08_MVP_0.4.md`, `09_MVP_0.4_Balance_Model.md` — clipboard-era MVP 0.4.
- `10_Application_Shell.md` — shell before the production scene split.
- `11_MVP_0.5_Formula_Cells_Spec.md` — first FormulaCell proposal (fixed formula properties / selection-to-activation). Superseded by FC2 v2.
- `12_MVP_0.5_Balance_Model.md` — early FC2 model; v2 adds the final report-delivery rule and authoring workflow.
- `playtest-4-levels.md` — old CUT/PASTE `SampleScene` playtest build.
- `EXEL_HELL_MVP04_Balance_Model.xlsx` — frozen MVP 0.4 workbook. Do not use its action costs for FC2. The current FC2 workbook was created separately instead of mutating this historical artifact.
- `tests/**` — immutable test evidence for the version named inside each protocol.
- `devlog/**` — historical developer narrative; facts are useful, mechanics may be obsolete.

## Retired assumptions — do not reintroduce accidentally

- The game is **turn-based**. The final `09:00–18:00` workday is a presentation mapping over turns, not real-time pressure while the player thinks.
- FC2 does not use toolbar `SUM`/`SORT` or mandatory `CUT → PASTE` function input.
- `Drag = MOVE`; `Shift+Drag = SELECT`.
- Empty FormulaCell properties are movable.
- Aggregate report goals may be authored either as report `=SUM()` cells or as plain ReportCell delivery targets.
- `#REF!` spawn positions are dynamic and telegraphed; level authors do not place spawn points.
- The final visual target is a stylized game window in a pixel-art office, not a literal Windows/Excel simulator.

## Documentation maintenance rule

- Core blocker fix → update the relevant CURRENT document.
- Accepted constructor layout → update `mvp05_levels/` and `balance/current_level_baselines.csv` / workbook.
- New production subsystem → add/update its CURRENT tech spec.
- Historical test/devlog files are not rewritten retroactively.
