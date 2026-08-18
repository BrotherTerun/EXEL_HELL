# MVP 0.5 / Formula Cells 2.0 — four-level playtest progression

Source of truth for runtime coordinates and timings: `PrototypeLevelConfig.cs`.

This pass intentionally uses four levels only. The progression tests one question at a time:

1. **Training Reconciliation** — learn `Drag=MOVE`, `Shift+Drag=SELECT`, DROP activation and movable FormulaCells with no anomaly.
2. **Watched Reconciliation** — exactly the same puzzle and goals; one late #REF! window adds light pressure but should usually not require a response.
3. **Inconsistent Data** — two semantic goals with a report-mediated dependency; #REF! should force roughly 2–3 plan changes over a successful run. Target: often pass on the second attempt.
4. **Final Reconciliation** — three-goal composition with real anomaly pressure. Target: several meaningful replans and roughly 3–4 attempts for a clean first-time player before mastery.

## FC2 authoring constraints

- `MOVE` costs 1 action; selection is free.
- Formula activation happens only by DROP.
- Filled FormulaCell is anchored until its occupant is moved out.
- Empty FormulaCell may move.
- ReportCell occupant is a persistent formula operand.
- Formula scarcity must not become maintenance tax: preferred `FHL <= 0.25` and scarcity should not raise `C0` more than ~35% over `C_sem` without a real spatial choice.
- Exact anomaly difficulty remains a playtest hypothesis until the deterministic spawn/intent trace is observed in Unity.

The old five-level notes are superseded by the four authoring sheets in this directory.