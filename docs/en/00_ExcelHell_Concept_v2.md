# EXEL HELL — current concept v2

**Status: CURRENT design target.**  
Supersedes the original `00_ExcelHell_Concept.md` where it conflicts with this file.

## Hook

**EXEL HELL is a turn-based spatial tactical puzzle about finishing an ordinary office report inside a spreadsheet that is gradually becoming hostile.**

The player is an employee/accountant. At first the worksheet behaves like a boring corporate tool. Then familiar spreadsheet concepts become literal physical rules:

- `SORT` gathers semantically related data into space;
- `SUM` physically collapses selected numeric data into an aggregate;
- `#REF!` becomes a spreading structural error that corrupts and destroys cells;
- later, rows/columns/interface elements become unreliable as psychosis escalates;
- the boss still expects the report before the end of the workday.

The game's identity comes from one rule:

> **office/spreadsheet semantics are the source of game mechanics, not a skin over unrelated abilities.**

## Core loop

`read report requirements → inspect layout + telegraphed threat → choose computation/reposition/defence → spend one action → resolve #REF! → replan → submit correct report before the workday ends`

The player is solving two interlocked problems:

1. **semantic work** — understand which employees/fields/values satisfy each report goal;
2. **spatial work** — place/rearrange tokens and FormulaCells so those computations remain possible while the worksheet deteriorates.

## Canonical time model

The game remains **turn-based**.

A successful gameplay action costs one turn. Selection, inspection, changing selection and invalid drops are free. The player may think without the game advancing.

The final presentation replaces `turn 3/10` with an office clock:

`09:00 → 18:00`

For a level with `maxTurns = B`:

`displayMinutes = 540 + round(540 * currentTurn / B)`

The last allowed turn maps exactly to `18:00`. An early clear shows the actual current derived time.

This preserves deterministic tactical planning while giving the fiction a real workday deadline.

## Main design pillars

### 1. Familiar semantics become physical rules

The player should often be able to predict a mechanic because they understand the spreadsheet word attached to it. We use a small authored function vocabulary rather than implementing a real spreadsheet parser.

### 2. Known threat, not hidden RNG

`#REF!` telegraphs both future outbreak spawn and active spread. The player should lose because they chose an order/position badly, not because the game secretly rolled a bad result.

Spawn is dynamic, but the selected future cell is shown before resolution.

### 3. Multiple simple goals create the encounter

A single `SalaryTotal` is an atomic task, not necessarily a whole level. Difficulty comes from composition:

- shared data;
- ordering constraints;
- filtering;
- limited FormulaCells;
- geometry;
- anomaly pressure.

The best encounter is not a huge formula; it is several understandable requirements competing for the same worksheet state.

### 4. FormulaCells are spatial infrastructure

`=SUM()` and `=SORT()` exist as properties of worksheet cells. Their coordinates matter. Empty FormulaCells can move; occupied ones first expose their occupant. This lets the player manipulate the computational infrastructure itself.

Formula scarcity is valid only while it creates placement/order decisions. If it becomes repeated maintenance tax, add/reposition formulas or redesign the level.

### 5. The interface becomes the horror

Psychosis escalates through the tool the player has learned to trust. Early distortion is presentation-only; late levels may introduce a very small set of telegraphed gameplay distortions.

Readability of real rules/telegraphs always wins over visual chaos.

## Report fiction

Report Goals use semantic data such as:

- Salary Total;
- Overtime Total;
- Bonus Total;
- Bonuses meeting a threshold;
- salary of the employee with maximum overtime;
- salaries of employees whose hours satisfy a condition.

SUBMIT validates the target report cells. Current gameplay treats correct final values as the formal requirement; provenance remains available as data but is not a victory constraint.

## Escalation across the five-level jam build

### L1 — normal work
Teach FC2 grammar. No `#REF!`.

### L2 — something is wrong
Same interaction language under a light, readable anomaly. `#REF!` may be safely ignored by a good route but must be noticed.

### L3 — the worksheet fights the plan
First level where threat should force meaningful replanning/evacuation/order changes.

### L4 — tactical pressure
Several report goals, meaningful dependency/geometry and multiple anomaly windows. The player must actively plan around failure.

### L5 — office psychosis
Do **not** simply add many more semantic actions. Keep computational complexity near L4 and escalate through interface unreliability, narrative events and the limited psychosis mechanics.

## Planned narrative layer

Narrative is delivered through the workplace rather than separate cutscenes:

- messages typed inside cells (`ПОМОГИТЕ`, `НЕ СМОТРИ`, ...);
- boss chat;
- department chat;
- incoming-message toasts;
- protagonist reactions;
- glitches and psychosis state;
- sound.

Gameplay remains readable while narrative presentation becomes increasingly wrong.

## Final visual direction

The final target is **not** a literal desktop simulator and not a screen filled 100% by a fake Excel window.

Four visual layers:

1. **pixel-art office background** — subdued cold office, cheap ambient loops, later corruption;
2. **game/spreadsheet window** — central, readable tactical surface;
3. **pixel-UI shell** — menu, chat, clock, report/support panels;
4. **protagonist sprite** — UI/scene character with a few progression states (normal / tense / scared / broken).

Data, addresses, formula notation, selection, drop feedback and real `#REF!` telegraphs remain crisp and legible.

Palette direction:
- cold grey-blue office;
- muted green report/normal states;
- saturated blue FormulaCells;
- dirty orange/red anomaly/psychosis.

## Audio direction

Small semantic SFX set rather than dozens of bespoke sounds:

- click / pickup / drop / invalid;
- SUM;
- SORT;
- DELETE;
- REF telegraph / impact;
- chat ping;
- typewriter;
- report accepted;
- 2–3 glitch variants.

Music may use a normal office loop plus a more anxious/distorted layer crossfaded by psychosis.

## Explicit non-goals

Do not build for the jam release:

- a real Excel parser;
- arbitrary formula typing/editing;
- hundreds of spreadsheet functions;
- real-time pressure while the player thinks;
- a literal Windows/Excel clone;
- procedural level generation;
- complex AI/pathfinding;
- a full Slack simulator or player chat input;
- a large graph-based narrative editor;
- dozens of independent psychosis systems.

## References by design function

- **Baba Is You** — visible rules become manipulable spatial objects.
- **Into the Breach** — deterministic/telegraphed tactical threat.
- **Papers, Please** — the work itself is gameplay; stationary workplace as fiction.
- **Windowkill / There Is No Game / Progressbar95** — interface boundaries can become gameplay/presentation material.

The reference is always the design principle, not a requirement to mimic the reference's visual skin.
