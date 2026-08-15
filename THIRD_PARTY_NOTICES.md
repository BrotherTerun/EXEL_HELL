# Third-party notices

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
