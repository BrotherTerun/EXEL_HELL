# Final UI Shell — production layout contract

## Scope

This branch replaces prototype screen composition without changing puzzle rules. It preserves existing worksheet/formula/button callbacks and adds presentation-only runtime adapters.

Reference resolution: **1600×900**, `ScaleWithScreenSize`, width/height match = `0.5`.

## Composition

The production screen is treated as a stylized game window, not an Excel/Windows clone.

- outer game window: inset inside the 1600×900 presentation stage;
- worksheet: primary left area;
- formula bar: directly above worksheet;
- right rail: report/task/control region;
- protagonist slot: upper-right presentation region;
- workday clock: upper-right, separate from turn mechanics;
- messages control: compact button/notification region;
- transient toast layer: above game window UI;
- footer/status reserve: lower presentation strip for later art/status treatment.

Pixel office background and final fonts/sprites are intentionally deferred to the visual pass. This branch establishes stable geometry and interaction endpoints first.

## Workday clock

Gameplay still owns integer turns. Presentation maps them to the workday:

`09:00 + 540 minutes × currentTurn / maxTurns`

- turn `0` => `09:00`;
- final turn => exactly `18:00`;
- displaying time never consumes or creates gameplay actions;
- legacy `turn N/max` remains internal and is hidden from the player-facing HUD.

## Chat shell

Read-only channels:

- `НАЧАЛЬНИК`
- `ОТДЕЛ`

Narrative effects:

- `BossChatMessage`
- `DepartmentChatMessage`
- `Toast`

Incoming chat:

1. appends to channel history;
2. increments unread count unless that channel is currently open;
3. produces a transient toast;
4. clicking the toast opens the correct channel;
5. chat interactions never consume gameplay turns.

No player text input exists in release scope.

## Protagonist presentation

`ProtagonistLine` is now a real NarrativeLayer endpoint, but still art-agnostic.

Metadata already supported:

- text;
- `Normal / Tired / Alarmed / Psychotic` mood;
- `Timed / OnClick / TimedOrClick` lifetime.

The placeholder bubble owns its raycast. A dismissal click is consumed by UI, completes the narrative ticket and does not invoke worksheet interaction or turn economy.

The later pixel-art pass should replace only avatar/bubble visuals, not NarrativeLayer contracts.

## Temporary legacy bridge

The old report/sidebar code still owns working gameplay button callbacks. `PrototypeLegacyRailAdapter` fits the whole legacy coordinate system into the new rail by transform scaling rather than rebuilding individual buttons.

This is deliberate jam-scope debt: preserve proven interaction code now, replace skin/layout internals only if the visual pass has time.

## Expected first Play Mode check

On `feature/final-ui-shell`:

1. project compiles;
2. Gameplay still accepts all worksheet/formula interactions;
3. screen uses 1600×900 production composition;
4. worksheet remains fully visible and clickable;
5. report/sidebar buttons remain clickable;
6. clock starts at `09:00` and advances with actions;
7. legacy raw turn label is hidden;
8. Narrative self-test ultimately reports `PASS`;
9. the self-test `ProtagonistLine` appears in the temporary bubble and dismisses automatically after its authored lifetime (or on click);
10. no UI click increments a gameplay turn unless it is an existing gameplay control.

Visual beauty is **not** an acceptance criterion for this branch. Geometry, readability and interaction integrity are.
