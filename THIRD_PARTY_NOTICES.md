# Third-party notices

## Integrated: Tiny5

- Project: Tiny5
- Author: Stefan Schmidt / Gissio
- Source: https://github.com/Gissio/font_Tiny5
- License: SIL Open Font License 1.1
- Runtime asset: `Game/Assets/_Project/Resources/Fonts/Tiny5-Regular.ttf`
- Use: spreadsheet values, headers, formulas and short technical controls. Long chat and protagonist dialogue keep the regular UI font for readability.
- The unmodified upstream `OFL.txt` is vendored next to the runtime font as `Tiny5-OFL.txt`.

## Evaluated but not integrated: Save Game Free

- Project: Save Game Free
- Author: Bayat Games
- Source: https://github.com/BayatGames/SaveGameFree
- License: MIT
- Decision: not shipped as a runtime dependency. Its Unity-package subtree contains an `.npmignore` rule that excludes `*.meta`; when resolved through Unity Package Manager as an immutable Git package, Unity receives package assets without their meta files and ignores the runtime assembly. EXEL HELL therefore uses Unity's built-in `JsonUtility` plus `Application.persistentDataPath` / `System.IO` for the current small checkpoint/settings documents.

## Evaluated but not integrated: UnityScreenNavigator

- Project: UnityScreenNavigator
- Author: Haruma-K
- Source: https://github.com/Haruma-K/UnityScreenNavigator
- License: MIT
- Decision: not added as a runtime dependency. EXEL HELL currently creates its UI entirely at runtime, while UnityScreenNavigator is optimized around Page/Modal/Sheet prefabs and their lifecycle. Pulling it in for four jam-build screens would add more adaptation surface than it removes. The application shell uses the same simple stack-navigation concept without copying UnityScreenNavigator source code.
