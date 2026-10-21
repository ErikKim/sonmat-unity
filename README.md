# sonmat-unity

A part of 손맛 햅틱 / Sonmat Haptics (`sonmat`): SonmatUnity.dll.

The BepInEx 5 plugin for Unity games on Mono (Overcooked! 2): logs the sounds the game plays and the buttons it reads, in the line format the app's engine reads.

The app does not carry this in its installer for good: it fetches the release zip that `sonmat-games/parts.json` names
(address + SHA-256) into `<app>/parts/unity/` when a game needs it (`sonmat/tools/partfetch.py`). A change here reaches
players when a new tag has been released and `parts.json` points at it.

Build: dotnet build -c Release -o out. Every push to main builds it on GitHub (artifact); a tag `v*` makes the release.
