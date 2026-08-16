# EXEL HELL — current production plan

**Checkpoint:** 2026-08-16 ~08:30 (+03:00)  
**Deadline:** jam release window morning 17 August.  
The exact hour allocation may slide; the **dependency order and freeze rules** matter more than preserving an obsolete timetable.

## Completed critical path

- FC2 core approved/frozen;
- REF telegraph blocker fixes;
- `jam/final-build` production branch;
- LevelConstructor;
- aggregate plain ReportCell delivery rule;
- Menu / Gameplay / LevelConstructor scene split;
- Menu NEW/CONTINUE/LOAD routing into Gameplay;
- first-frame authored level application;
- production camera/bootstrap cleanup;
- documentation refresh (this pass).

## Parallel work now

### Level-authoring lane
User:

`LevelConstructor → rebuild/tune L1–L5 → no-REF legal routes → smoke with REF → accept layouts → exports`

### Infrastructure lane
Assistant:

`NarrativeLayer v1 → debug hooks/receiver → TypewriterCellMessage`

These lanes are deliberately independent. Narrative architecture should not wait for final coordinates; narrative **content timing** should.

## Convergence order

### 1. Accepted L1–L5 + NarrativeLayer API
Exit criteria:
- five playable authored configs or accepted candidate configs;
- core untouched except blockers;
- narrative triggers/effects dispatch reliably.

### 2. Narrative/UI shell
Implement together to avoid multiple layout rewrites:
- workday clock;
- chat button;
- Boss/Department channels;
- history/unread;
- incoming toast;
- protagonist presentation container;
- NarrativeLayer receivers for those systems.

### 3. First visual pass
- pixel office background;
- unified menu/game/chat frame;
- fonts/spacing/borders;
- protagonist first states;
- replace prototype-looking shell without changing interaction contracts.

### 4. Psychosis
- progression 0–4;
- presentation manifestations;
- maximum three gameplay distortion primitives;
- telegraph/readability first.

### 5. Narrative content integration
Once level action timing is stable:
- L1 tutorial/reassurance;
- L2 first wrongness;
- L3 reactions/replans;
- L4 pressure/chat conflict;
- L5 final narrative/psychosis arc.

### 6. Animation / presentation pass
Reusable helpers only; no bespoke animation architecture.

### 7. SFX + music
Small semantic sound set; normal/anxious music state.

### 8. Integration / release
- L1→L5 full pass;
- restart;
- menu/continue/load/save;
- clock/chat/narrative/psychosis;
- resolution/aspect;
- RU/EN;
- candidate build;
- screenshots/GIF;
- credits/licences/AI disclosure as required;
- itch upload/regression.

## Feature freeze rule

Once the late integration window begins, **no new architecture/system**.

After freeze, only:
- content;
- art/skin;
- animation on existing hooks;
- audio;
- tuning;
- blocker/obvious UX fixes.

If a planned system does not exist in a basic working form by freeze, cut its depth rather than inventing it at night.

## Cut order if behind

Cut **depth before entire identity systems**:

1. fewer random chat messages;
2. fewer protagonist frames;
3. fewer psychosis manifestations;
4. only two gameplay distortion primitives instead of three;
5. two music states instead of three;
6. simpler office animation;
7. fewer decorative transitions.

Do not cut first:
- five playable levels;
- readable REF/Formula gameplay;
- workday deadline presentation;
- minimum narrative arc;
- final escalation/psychosis identity;
- build/regression buffer.

## Things we explicitly do not spend time reopening

- FC2 interaction redesign;
- realtime conversion;
- true Excel parser;
- generic level-editor architecture beyond current constructor;
- new major spreadsheet functions;
- full mid-turn save snapshot;
- direct Gameplay editor-launch parity unless it blocks development;
- complex narrative graph;
- procedural psychosis framework.

## Definition of jam-ready

The build is shippable when a clean player can:

`Menu → L1 → L2 → L3 → L4 → L5/finale`

with:
- no blocker/softlock on intended routes;
- clear controls/report goals;
- REF pressure escalating intelligibly;
- narrative/presentation escalating from normal office to psychosis;
- save/continue/load/reset functioning at checkpoint level;
- no old prototype UI/tutorial leaking into production flow;
- stable standalone build at target resolution.

Polish beyond that is optional; release buffer is not.
