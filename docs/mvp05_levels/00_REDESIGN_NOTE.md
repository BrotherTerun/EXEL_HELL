# MVP 0.5 redesigned progression

Implementation note for the next formula-cell playtest pass.

The experiment now uses five levels:

1. Formula Reconciliation — tutorial, Salary Total + Overtime Total, no #REF!.
2. Routine Report — original MVP 0.4 goals Salary Total + Bonus >= 5, no #REF!.
3. Urgent Reconciliation — the same goal structure under light #REF! pressure, with a spare SORT coordinate.
4. Inconsistent Data — original semantic goals Salary of max-overtime employee + Salary for Hours < 40. The first report target is a plain protected ReportCell; the second is a protected ReportCell + SUM. Report values are persistent SUM operands.
5. Final Reconciliation — original final goals Salary for Hours < 40 + Overtime Total + Bonus Total. Bonus starts as an already contiguous range; Hours/Salary/Overtime use three SORT lanes plus one spare SORT lane. #REF! begins later than in the previous stress-test layout.

This note is intentionally short; runtime LevelConfig is the source of truth for exact coordinates and timings.