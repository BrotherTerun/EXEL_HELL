# MVP 0.4 freeze regression

Short blocker-level pass before moving from graybox research to level/content production.

## Setup

- Use any goal set that gives at least one active Report Cell.
- Keep `showSpawnDebug = true`.
- Do not score fun/balance here; this is only a rules regression.

## 1. Future outbreak telegraph stability

1. Start the run.
2. Note the orange future-spawn cell and the `NEW #REF!` address in the sidebar.
3. Click/select several unrelated cells before spending an action.

Expected:
- board address and sidebar address stay the same;
- no `PrototypeSpawnDirector` log exists;
- only `ExcelHellPrototype` chooses spawn cells.

## 2. Two telegraphs at once

Reach a state with:
- one live Corrupted cell that has a movement intent;
- one scheduled future outbreak.

Expected:
- active-spread target is visible through the movement overlay;
- future outbreak target is also visible;
- neither telegraph changes because of selection-only clicks.

## 3. Corrupted values are inaccessible

1. Put a numerically correct result in a Report Cell.
2. If testing an older branch/state where corruption of report cells is still possible, corrupt it; otherwise verify on an ordinary numeric cell.

Expected:
- Corrupted cell displays `#REF!`, not its hidden value;
- hidden number is not accepted by numeric gameplay logic;
- a Corrupted report answer cannot satisfy SUBMIT.

## 4. SORT preserves semantic gaps

1. SORT a field into a readable column.
2. CUT the middle record value, e.g. Sidorov salary.
3. SORT the same field again.

Expected:
- schema keeps one slot per employee;
- Sidorov position is empty;
- Volkova/Kim do not shift upward and inherit the wrong identity.

Repeat once with RecordKey SORT if convenient: one slot per field must remain.

## 5. Report interface is protected

Report interface = report header (`REPORT`) + active green Report Cells only. Free cells below them remain worksheet cells.

Check:
- future outbreak never chooses report interface;
- movement intent never points into report interface;
- DELETE on report header/target shows a protected-structure message and spends no turn;
- free cells below the report targets can still be destroyed normally.

## Gate

Pass = all five rules behave as written and Unity reports no compile/runtime blocker.

After pass: freeze MVP 0.4 core behavior and start LevelConfig/content work. Only blocker-level fixes reopen this checklist.
