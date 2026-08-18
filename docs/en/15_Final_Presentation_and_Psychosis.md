# EXEL HELL — final presentation / chat / psychosis target

**Status: PLANNED final jam presentation.**  
This file describes the target state after NarrativeLayer and accepted levels; not every subsystem exists yet.

## 1. Visual lock

EXEL HELL is **not** a literal Excel/Windows simulator.

Target composition:

1. subdued pixel-art office background;
2. stylized game/spreadsheet window as the central tactical surface;
3. unified pixel-UI shell for menu/chat/clock/panels;
4. small animated protagonist sprite as a reactive UI/scene character.

The spreadsheet remains the focal interaction area and must preserve clarity of:
- numbers;
- keys;
- addresses;
- `=SUM()` / `=SORT()`;
- selected range;
- drag/drop validity;
- report targets;
- real `#REF!` telegraphs.

Psychosis may lie visually around those systems, but cannot make actual tactical information unreadable.

## 2. Office background

Mandatory low-cost atmosphere layer:
- cold grey-blue office;
- monitor/lamp/vent/silhouette loops;
- little or no detailed NPC animation;
- later levels progressively corrupted/incorrect.

Office exists to frame the game and give protagonist/chat/presentation a physical context, not to become an explorable environment.

## 3. Protagonist

Not controllable.

A small sprite/UI character with roughly 2–4 authored states:
- normal/tired;
- tense;
- scared/alarmed;
- broken/psychotic.

Narrative events can trigger expressions/reactions. Do not build a large skeletal/animation state machine for the jam.

## 4. Workday clock

Visible turn counter is replaced by electronic office time:

`09:00 → 18:00`

Calculation:

`totalMinutes = 540 + round(540 * currentTurn / maxTurns)`

Rules:
- successful gameplay action advances time;
- selection/UI inspection/invalid drop do not;
- Reset starts 09:00;
- last turn = exactly 18:00;
- early completion displays actual derived time;
- underlying turn remains canonical for gameplay/NarrativeLayer triggers.

## 5. Chat

Chat is **not permanently open**.

Main gameplay has a separate chat button/unread state.

Full chat window contains two channels:
- `НАЧАЛЬНИК`
- `ОТДЕЛ`

Each channel has history.

Player has no text input.

Message sources:
- scripted narrative events;
- reactive gameplay events;
- optional random authored pool.

## 6. Toast notifications

Incoming messages produce a small toast:
- short enough to read without opening chat;
- does not cover tactical table area;
- click opens relevant channel/history;
- unread state remains until appropriate read policy;
- presentation click is free.

Examples:

Boss:
- `Отчёт должен быть готов до 18:00.`
- `Почему файл ещё открыт?`
- `Не удаляйте строки самостоятельно.`

Department:
- `у кого-нибудь тоже формулы сами двигаются?`
- `я перезапустил файл, вроде прошло`
- `ребят у меня в ячейке написано помогите`

Late messages may become obviously impossible/wrong.

## 7. Psychosis scale 0–4

One global/progression value. Do not build dozens of unrelated systems.

### 0 — normal
- ordinary office/table;
- tiny optional visual imperfection only.

### 1 — suspicious
Presentation-only:
- small text/border jitter;
- brief color shift;
- wrong hover for a moment;
- strange cell/protagonist/chat messages.

### 2 — clearly wrong
Still mostly presentation:
- ghost cells;
- flicker;
- visual row/column offset;
- transient wrong labels;
- UI elements momentarily desync and recover.

### 3 — gameplay distortion begins
Only after clear telegraph:
- temporary disabled cell;
- real row offset;
- real column offset;
- optional temporary formula rejection/noise.

### 4 — final
- several controlled distortions;
- UI/chat/table visually conflict;
- office/protagonist strongly corrupted;
- real game remains solvable/readable.

## 8. Maximum gameplay psychosis primitives

Prefer only these three:

### `DisableCell(cell, turns)`
Temporarily unavailable coordinate with clear duration/telegraph.

### `ShiftRow(row, delta)`
Controlled physical/semantic displacement with clear before/after presentation.

### `ShiftColumn(column, delta)`
Same for column.

If one primitive proves expensive/unreadable, ship with two. Presentation effects can create much more variety without multiplying rules.

## 9. Psychosis design rule

> Presentation can be chaotic. Gameplay effects must be small, authored/telegraphed and learnable.

Never falsify the real orange/red REF telegraph in a way that makes the player unable to distinguish truth. A fake/noisy telegraph can exist only if real threat remains independently legible.

## 10. Level escalation mapping

- L1: psychosis 0, narrative/tutorial only.
- L2: 0→1, first suspicious events around REF.
- L3: 1→2, strong presentation abnormalities, almost no rule changes.
- L4: 2→3, first limited gameplay distortion after telegraph.
- L5: 3→4, primary identity from interface unreliability rather than much larger `C0`.

Exact narrative moments should be authored only after final level routes/timings are accepted.

## 11. Animation/presentation library

Prefer reusable helpers for:
- hover/select;
- token pick-up / drag ghost;
- valid/invalid drop;
- SUM collapse;
- SORT spill;
- REF spawn/spread/impact;
- corruption/death;
- report accepted;
- toast in/out;
- typewriter message;
- menu/chat transition;
- protagonist reaction;
- glitch manifestation/dismiss.

PrimeTween is the preferred existing presentation layer.

## 12. Audio target

Small semantic SFX set (~12–15 events) + minimal music states.

Possible music structure:
- normal office loop;
- anxious/drone/distorted layer;
- crossfade/intensity controlled by psychosis.

Better two well-directed states than five unrelated tracks.

## 13. Accessibility/readability guardrails

- avoid persistent heavy CRT/noise over data;
- no font distortion that makes numbers/formulas ambiguous;
- report goals remain identifiable;
- color is not the only distinction for key state if another clear marker is cheap;
- psychosis dismiss/notification interaction must not consume gameplay actions;
- UI scaling must be tested at intended 16:9/16:10 resolutions.
