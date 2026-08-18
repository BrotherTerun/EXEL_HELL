# EXEL HELL — application / scene architecture v2

**Status: CURRENT production architecture.**  
Supersedes `10_Application_Shell.md` where it describes the old one-scene runtime flow.

## 1. Scene split

Production now uses three explicit scenes:

- `Assets/Scenes/Menu.unity`
- `Assets/Scenes/Gameplay.unity`
- `Assets/Scenes/LevelConstructor.unity`

`SampleScene.unity` is legacy/reference material and is no longer the production boot path.

Each production scene has a `PrototypeSceneArchitecture` context with a role:

- `Menu`
- `Gameplay`
- `Constructor`

The scene context has execution order `-10000` so it can prepare runtime services before the worksheet reaches its first rendered frame.

## 2. Persistent Application shell

`ExcelHellApplication` is created with `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)` and persists via `DontDestroyOnLoad` during normal game flow.

It owns:
- main menu UI;
- pause menu;
- settings/load/help screens;
- save/checkpoint persistence;
- `GameplayActive` / `Paused` state;
- navigation between Menu and Gameplay.

The shell remains runtime-generated uGUI for the current build. Final pixel-UI may reskin/recompose it without changing the persistence/navigation contract.

## 3. Normal production flow

### NEW GAME
1. delete previous progress;
2. save progress at level 0;
3. `PrototypeLevelRuntime.SetCurrentIndex(0)`;
4. set `GameplayActive=true`;
5. load `Gameplay` scene;
6. Gameplay scene creates/initializes the worksheet for the already-selected level.

### CONTINUE / RESUME FROM MAIN MENU
1. read saved `CurrentLevelIndex`;
2. set runtime index;
3. set `GameplayActive=true`;
4. load `Gameplay`.

### LOAD
Same architecture as Continue, using the selected save payload.

**Important:** Menu does not render/create a temporary worksheet anymore. It only selects state and transitions scenes.

## 4. Gameplay scene initialization

`PrototypeSceneArchitecture.InitializeGameplayRuntime()`:

1. finds the scene-authored `ExcelHellPrototype` worksheet object and temporarily keeps it inactive if needed;
2. prepares level dataset / FormulaCells / compatibility services;
3. adds REF telegraph + level flow for normal Gameplay;
4. activates/creates worksheet core;
5. applies `PrototypeLevelRuntime.Current` synchronously through `PrototypeLevelDatasetAdapter.Apply()` before the first production render.

This removes the old visible sequence:

`legacy hardcoded seed board → one frame → authored level`.

The adapter still has its LateUpdate compatibility path; the production scene's synchronous apply is the first-frame guarantee.

## 5. Reset and Main Menu

### Reset Level
Pause-menu Reset reloads `Gameplay`. Current level index is preserved; the scene reconstructs the authored start state.

### Main Menu
Application sets `GameplayActive=false` and loads `Menu`.

The worksheet is not intended to persist between scenes.

## 6. Constructor scene

`LevelConstructor` uses the same worksheet/core interaction but a different service set:

- no normal `PrototypeLevelFlow`;
- no REF telegraph layer;
- `PrototypeAuthoringGuard` active;
- `PrototypeLevelConstructor` active.

AuthoringGuard continuously neutralizes:
- turn counter/deadline;
- finished state;
- pending spawn intent;
- active anomaly intent;
- Corrupted/Destroyed board states.

This makes the scene a spatial authoring sandbox rather than a playable run.

See `13_Level_Constructor_Authoring.md`.

## 7. Main Camera

Production scene context guarantees a `Main Camera` if none exists:
- tag `MainCamera`;
- orthographic;
- black clear background;
- `AudioListener`.

Current gameplay UI is Screen Space Overlay, so the camera is mostly a rendering foundation for the future pixel-office background. It also avoids Unity's `No cameras rendering` Game View overlay.

## 8. Legacy bootstrap compatibility

Several older prototype helpers still contain `RuntimeInitializeOnLoadMethod(AfterSceneLoad)` for compatibility with older prototype scenes.

Production scene architecture explicitly removes/overrides incompatible legacy helpers, especially the old contextual tutorial, before they can produce a stable player-facing frame.

Do not add new final systems through independent global auto-bootstrap unless there is a clear cross-scene reason. Prefer scene hosts now that production has explicit scenes.

Future Narrative/UI/Psychosis components should have clear hosts under Gameplay scene context.

## 9. Direct `Gameplay.unity` launch in Editor

Direct scene launch is intentionally a developer shortcut and currently differs from production boot:

- `ExcelHellApplication` is created BeforeSceneLoad with `GameplayActive=false`;
- Gameplay scene detects a direct launch and disables/destroys the shell so the menu does not cover the worksheet;
- scene falls back to serialized `startLevelIndex`;
- standalone legacy helpers can therefore see `ShellAvailable=false`.

Observable consequences:
- old fallback HELP may appear;
- gameplay MENU button calls `ExcelHellApplication.OpenGameplayMenu()` but there is no surviving app instance, so it is a no-op;
- small bootstrap flicker may remain.

This does **not** describe the release path. Fix direct-launch parity later only if the developer workflow needs it.

## 10. Persistence

`AppPersistence` uses:
- `JsonUtility`;
- `Application.persistentDataPath`;
- `System.IO`;
- temporary-write/replace pattern.

Progress stores:
- schema version;
- current level index;
- highest unlocked level index;
- campaign completed flag;
- timestamp;
- `NarrativeFlags` list.

Settings store:
- master/music/SFX values;
- fullscreen;
- resolution;
- VSync;
- language.

### Save scope

Save is a **level checkpoint**, not a mid-turn worksheet snapshot.

Continue/Load returns to the authored start state of the saved current level. This is an intentional jam-build simplification.

## 11. Production navigation acceptance

Before release validate:
- boot starts at Menu;
- New Game reaches L1 in Gameplay with no legacy-board flash;
- Continue reaches saved level;
- Load reaches selected saved level;
- Esc/pause/resume preserve current worksheet while staying in Gameplay;
- Reset rebuilds current level cleanly;
- Main Menu actually loads Menu;
- settings persist;
- final completion persists once;
- no production-screen fallback tutorial/help leaks through.
