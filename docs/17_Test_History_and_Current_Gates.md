# EXEL HELL — test history and current validation gates

**Status: CURRENT interpretation of historical evidence.**  
Raw protocols under `tests/` are preserved unchanged.

## 1. Why old tests still matter

The mechanics changed substantially from MVP 0.3/0.4 to FC2, so old turn counts and button ratings cannot validate the current build directly.

They remain valuable because they identified recurring design problems:
- anomaly that arrives after useful data are no longer needed is decorative;
- repeated opening SORTs become routine;
- semantic goals are more interesting in composition than as isolated arithmetic;
- overlapping goals can create either useful dependencies or structural conflicts;
- deterministic telegraphing is understandable and should be preserved;
- CUT/PASTE relocation became repetitive action tax;
- DELETE only becomes naturally useful under real spatial pressure;
- tutorial/report-target readability strongly affects perceived difficulty.

## 2. MVP 0.2 → semantic data

Early prototype proved basic worksheet manipulation worked but exposed two failures:
- one DELETE could effectively end the REF threat;
- names/numbers were mostly decoration rather than meaningful data.

This led to persistent outbreaks and semantic Report Goals in MVP 0.3.

## 3. MVP 0.3 math discovery

Balance work introduced:
- Base Cost `C`;
- rough threat horizon `H`;
- data last-use `U_i`;
- threat arrival `T_i`;
- Slack `T_i-U_i`.

Key conclusion:

> Do not ask whether REF is interesting in a configuration where the peaceful task mathematically finishes before REF can matter.

This principle remains current, even though FC2 action costs changed.

## 4. MVP 0.3 smoke evidence

Repeated smoke tests 01–08 showed:
- many combinations were too routine;
- DELETE and RecordKey SORT were often unused;
- opening FieldKey SORTs were repeatedly described as routine;
- REF often changed only order, not strategy.

### Smoke-test 09
`SalaryTotal + OvertimeTotal + BonusTotal`, 8 turns in that build.

Important observation: dynamic REF forced immediate reordering/evacuation instead of the habitual “SORT everything, SUM everything” route. This was one of the first convincing pressure examples.

### Smoke-test 10
`SalaryForHoursBelowForty + OvertimeTotal + BonusTotal`, 14 turns in that build.

Important observations:
- first natural DELETE use to control REF;
- geometry/order were genuinely disrupted;
- REF pressure rated much higher than simpler tests.

This test strongly supported the idea that multi-goal composition + threat, rather than one complex formula, is the core encounter structure.

## 5. MVP 0.4 evidence

MVP 0.4 fixed:
- single-cell SUM exploit;
- report protection;
- fresh data;
- some goal-overlap issues;
- telegraph/UI problems.

Smoke 11–15 still showed several configurations with weak REF impact and the recurring “SORT first” tax. Clipboard relocation also remained repetitive.

These observations helped motivate FormulaCells rather than another timing-only anomaly buff.

## 6. External 4-level playtests (pre-FC2)

### Player 1
Notable raw results:
- L1 2:34, first try; task clarity 3; asked for tutorial;
- L2 REF plan influence 4, REF clarity 5, tension 2;
- L3 6:01, 5 restarts; data rearrangement strong but perceived alternatives low; replay desire low.

### Player 2
Notable raw results:
- L1 task clarity 2 despite interest in table task;
- L2 REF influence 5 / clarity 5 but tension 2;
- L3 rearrangement 5, alternatives 2, replay desire 2; DELETE unused;
- L4 was invalidated by a bug that simplified the level; REF influence 1 and little meaningful defeat risk.

Shared interpretation:
- telegraphing itself was understandable;
- onboarding/report context needed improvement;
- threat did not consistently create multiple viable responses;
- repeated relocation/tool ceremony was hurting replay value.

Do not use these exact ratings as FC2 acceptance scores.

## 7. FormulaCells transition

The first FormulaCell proposal still treated formula positions mostly as fixed activation infrastructure. FC2 then adopted:
- Drag=MOVE;
- Shift+Drag=SELECT;
- formula activation by DROP;
- movable empty FormulaCells;
- range MOVE;
- persistent Report operands.

This directly addresses the old routine CUT/PASTE and fixed toolbar tax while preserving semantic/spatial planning.

## 8. What is actually validated now

Validated by internal implementation/smoke reasoning:
- FC2 action grammar exists;
- formula movement/occupant layering works;
- report persistence works;
- REF telegraph render blockers were fixed;
- dynamic spawn/report protection architecture exists;
- current L1–L4 have explicit legal no-REF baseline routes in docs/code.

**Not yet honestly validated by the old external profiles:**
- final FC2 onboarding clarity;
- replay desire under FC2;
- whether current formula scarcity is fun rather than tax;
- final L3/L4 pressure after constructor redesign;
- L5 psychosis gameplay;
- final pixel-UI/chat/narrative readability.

Do not write “external playtest confirms FC2” until a current build is actually tested.

## 9. Current technical smoke gates

### Scene/application
- production boot starts Menu;
- New Game → L1 Gameplay;
- Continue/Load → saved level Gameplay;
- no old worksheet/tutorial one-frame flash on production route;
- Pause/Resume preserve current puzzle;
- Reset reloads current authored start;
- Main Menu loads Menu.

### Constructor
- direct LevelConstructor opens authored board;
- F2 hides/shows panel without enabling turns/REF;
- normal token/formula drag is reflected in Export;
- Export → clipboard → Import round-trip restores layout/goals/parameters;
- Aggregate runtime results are not baked into TokenLayout.

### FC2 rules
- invalid drop = 0 actions;
- range MOVE correct;
- occupied formula moves occupant first;
- empty formula moves;
- SORT direction/blocked spill correct;
- SUM consumes ordinary operands but preserves ReportCell numeric operands;
- plain aggregate ReportCell accepts delivered Aggregate;
- aggregate ReportCell without SUM produces no false validation warning;
- report interface protected;
- spawn and spread telegraphs simultaneously readable.

## 10. Current level-design gates

For every accepted final level:
- explicit legal no-REF route and `C0`;
- turn reserve `R` recorded;
- Formula Handling Load checked;
- no catastrophic exposure before reasonable player reaction;
- at least one intended decision actually depends on spatial/order/threat state;
- no single hidden hard-lock route/password.

Target roles:
- L1 teaches FC2 without REF;
- L2 makes REF visible but does not demand expert defence;
- L3 produces at least one meaningful replan in real play;
- L4 supports several responses to pressure and remains recoverable;
- L5 remains computationally comparable to L4 and gets escalation mainly from psychosis.

## 11. New external playtest questions for FC2

Prefer short targeted questions over the old CUT/PASTE profile:

1. When did Drag=MOVE and Shift+Drag=SELECT become clear?
2. Did the player understand that an occupied FormulaCell contains a movable occupant and an underlying reusable formula?
3. Did they intentionally move a FormulaCell? Why?
4. How many actions were taken only because of REF?
5. How many times did REF change **route**, not just order?
6. Was any formula reuse obviously maintenance/busywork?
7. Did plain ReportCell delivery vs inline SUM feel like a meaningful layout distinction?
8. Did the player see >1 reasonable action at pressure moments?
9. Did failures feel attributable/learnable?
10. After failure, did they want to retry a different route?

Those answers, plus action trace/restarts, are the current core-production evidence we actually need.
