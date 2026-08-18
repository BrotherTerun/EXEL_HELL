# EXEL HELL — application shell pass

Branch: `agent/application-shell`

## Goal

Add the reusable game-level boilerplate around the existing turn-based prototype without redesigning the puzzle core:

- main menu;
- pause menu;
- New Game / Continue / Load;
- checkpoint save;
- persistent settings;
- stable hook points for later narrative/audio systems;
- keep `agent/prototype-core` untouched until manual verification.

## Persistence backend

The first integration attempt used **BayatGames / Save Game Free** as a pinned UPM git dependency. Manual Unity verification exposed a packaging incompatibility: the package subtree contains an `.npmignore` rule excluding `*.meta`, so Unity resolves the immutable Git package without the metadata required to import its runtime assembly. Unity then ignores the package assets and `Bayat.Unity.SaveGameFree` is unavailable to compilation.

Decision: remove that runtime dependency rather than require a manual ZIP import or maintain a fork for two small JSON documents.

`AppPersistence` now uses Unity/.NET built-ins only:

- `JsonUtility` for serialization;
- `Application.persistentDataPath` as the save root;
- `System.IO` for directory/file operations;
- write-to-`.tmp` then replace for a simple safer-write path.

Files are stored under the application's persistent data directory in `Saves/`:

- `excel_hell_progress.json`
- `excel_hell_settings.json`

### Progress payload

Currently stores:

- save schema version;
- current level index;
- highest unlocked level index;
- campaign-completed flag;
- timestamp;
- extensible `NarrativeFlags` list.

The list is deliberately present before narrative production starts so cell-overlay events, chat events and later story gates can attach flags without replacing the save schema.

### Save scope

This pass implements **level/checkpoint persistence**, not an arbitrary mid-turn snapshot of the rearranged worksheet.

Reasons:

1. the current graybox state is concentrated inside `ExcelHellPrototype` and was not designed as a serializable domain snapshot;
2. serializing its private transient state now would tightly couple final save data to a prototype that is about to be refactored visually and structurally;
3. levels are short puzzle units, so restoring the current level is a safe jam-build fallback.

`SAVE` in the pause menu therefore stores the current level checkpoint. Level advance autosaves the new current level. The final accepted report marks the current campaign as completed.

A future board snapshot should be implemented only after the final worksheet model is separated from its view/runtime UI.

## Application flow

`ExcelHellApplication` is created with `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)` and builds a high-priority uGUI canvas at runtime.

This avoids editing Unity scene YAML/prefab references during the integration pass and matches the current prototype, which also builds its UI at runtime.

Flow:

`boot -> Main Menu -> New/Continue/Load -> prototype gameplay -> Esc -> Pause -> gameplay/menu`

Screens:

- Main Menu
- Pause
- Settings
- Load Game

The small internal screen stack handles temporary navigation (for example Main -> Load -> Back) without adding a full UI navigation framework.

## Prototype compatibility

The old prototype still contains a legacy `AfterSceneLoad` auto-bootstrap. `PrototypeShellGuard` removes that automatically created prototype while the application is in menus. This avoids invasive edits to the large prototype class and makes the entire shell easy to drop if the branch is rejected.

`PrototypeLevelRuntime.SetCurrentIndex()` was added as the smallest explicit API needed for Continue/Load.

`PrototypeLevelFlow` now:

- hides its IMGUI playtest footer while application menus are active;
- saves after a successful level advance;
- records final campaign completion.

## Settings

Persisted and functional now:

- master volume (applied through `AudioListener.volume`);
- music volume value;
- SFX volume value;
- fullscreen/windowed;
- resolution;
- VSync.

Music/SFX are intentionally separate persisted values already, but there is no project AudioMixer/music/SFX routing yet, so those two values cannot change real channel volume until audio is added. They are ready to be consumed by the future audio service.

The current settings UI uses simple click-to-cycle controls rather than polished sliders/dropdowns. It is a functional shell, not final visual design.

## Evaluated OSS UI framework

`Haruma-K/UnityScreenNavigator` was evaluated (MIT, uGUI, Unity 2021.3+), but not added. It provides Page/Modal/Sheet prefab lifecycles, transition animations, resource loading and history. For the current four runtime-generated jam screens, adopting its prefab/resource model would create a larger migration than the UI it replaces.

Decision: reuse the screen-stack pattern, not the package. Reconsider it only if production UI moves to authored prefabs with animated transitions and more screens.

## Manual Unity verification checklist

This environment cannot launch Unity, so the branch is not considered production-ready until these are checked in the editor/build:

1. project refreshes with no SaveGameFree package warnings/errors;
2. no compile errors after refresh;
3. build opens on Main Menu rather than exposing the prototype underneath it;
4. New Game starts level 1;
5. Esc opens Pause and Resume returns to the same puzzle state;
6. Save -> Main Menu -> Continue restores the saved level;
7. progressing to level 2, quitting to menu and Continue starts level 2;
8. Load screen displays/delete/reloads the single slot correctly;
9. Settings survive restart;
10. fullscreen/resolution/VSync apply on the intended Windows target;
11. Quit exits a standalone build;
12. final level completion is written without repeated disk writes.

## Not included in this pass

- exact mid-turn worksheet save/restore;
- multiple player save slots (UI currently exposes one slot);
- key rebinding;
- localization infrastructure for shell strings;
- AudioMixer routing for Music/SFX;
- production art/animation/transitions;
- chat UI;
- narrative cell overlay messages;
- gameplay hotkey/context-menu redesign;
- Steam/cloud saves.

These are intentionally separated from the reusable application shell so gameplay/UI direction can continue changing without invalidating persistence and application flow.
