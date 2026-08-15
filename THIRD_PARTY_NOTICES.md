# Third-party notices

## Save Game Free

- Project: Save Game Free
- Author: Bayat Games
- Source: https://github.com/BayatGames/SaveGameFree
- Integrated as a pinned Unity Package Manager git dependency from `Assets/BayatGames/SaveGameFree`.
- Pinned revision: `1a1a4c4e9873667272a5fc889b27429e4c09cdd7`
- Package id: `io.bayat.unity.savegamefree`
- License: MIT

Copyright (c) 2025 Bayat Games

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## Evaluated but not integrated: UnityScreenNavigator

- Project: UnityScreenNavigator
- Author: Haruma-K
- Source: https://github.com/Haruma-K/UnityScreenNavigator
- License: MIT
- Decision: not added as a runtime dependency. EXEL HELL currently creates its UI entirely at runtime, while UnityScreenNavigator is optimized around Page/Modal/Sheet prefabs and their lifecycle. Pulling it in for four jam-build screens would add more adaptation surface than it removes. The application shell uses the same simple stack-navigation concept without copying UnityScreenNavigator source code.
