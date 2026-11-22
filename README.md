# ostora vivi

Among Us cheat / mod for the Steam copy. Provides ESP (role/impostor info) and cheat features (force impostor, kill, teleport).

## Features

- **ESP**: Shows each player's role (Impostor/Crewmate) and name above their character
- **Force Impostor**: Spoof self or named friends to appear as Impostor (local view; host-authoritative for kills)
- **Kill Nearest**: Auto-kill the nearest player to cursor
- **Kill All**: Attempt to kill all other players
- **Teleport To Cursor**: Move local player to mouse position
- **Kill Button Enabler**: Ensures kill button is always enabled

## Project Structure

```
OstoraVivi/
│
├─ OstoraVivi.csproj       # MSBuild project file (net6.0, BepInEx references)
├─ src/
│   └─ Plugin.cs           # Main plugin + injected MonoBehaviour (ESP + cheats)
├─ build.ps1               # Build script (dotnet build + copy DLL to BepInEx/plugins)
├─ install.ps1             # Installation guide
└─ README.md               # This file
```

## Build & Install

1. **Install dotnet SDK 6.0** (or newer) if not already present.

2. **Run build.ps1** (PowerShell, ExecutionPolicy Bypass):
   ```powershell
   powershell -ExecutionPolicy Bypass -File build.ps1
   ```
   This will:
   - `dotnet restore` and `dotnet build -c Release`
   - Copy the resulting `OstoraVivi.dll` to `BepInEx/plugins/`

3. **Start Among Us** (Steam copy). BepInEx should load the plugin automatically.

4. **Configure** (first run auto-generates config):
   - Edit `BepInEx/config/ostora.vivi.cfg`
   - Set `FriendNames` to comma-separated list of names to force as Impostor
   - Toggle cheats on/off via the in-game overlay

5. **Uninstall**: Remove `OstoraVivi.dll` from `BepInEx/plugins/` and delete the config file.

## Configuration

Default config (`BepInEx/config/ostora.vivi.cfg`):

```ini
[General]
ForceImpostorSelf=false
ForceImpostorFriends=false
FriendNames=

[Cheats]
KillNearest=false
KillAll=false
TeleportCursor=false
```

- `ForceImpostorSelf`: When true, local player's role is spoofed to Impostor in the client view.
- `ForceImpostorFriends`: When true, players whose names match `FriendNames` are spoofed to Impostor.
- `FriendNames`: Comma-separated names (e.g., `Alice,Bob`) to target with Force Impostor Friends.
- `KillNearest`: When true, automatically kills the nearest player to cursor each frame.
- `KillAll`: When true, attempts to kill all other players each frame.
- `TeleportCursor`: When true, teleports local player to mouse cursor position each frame.

## Technical Notes

- **Plugin type**: BepInEx 6 IL2CPP `BasePlugin` with injected MonoBehaviour via `ClassInjector.RegisterTypeInIl2Cpp<T>()`.
- **Target framework**: `net6.0` (compatible with `BepInEx.Unity.IL2CPP.dll`).
- **Interop assemblies**: Plugin compiles against game's `Assembly-CSharp.dll` and `Il2Cppmscorlib.dll` from `BepInEx/interop/`. These regenerate on game updates — rebuild the plugin after any game/patch update.
- **Role spoof**: `NetworkedPlayerInfo.RoleType` and `RoleWhenAlive` have setters, allowing local role modification. Non-host spoof is view-only; kills still require host authority.
- **Kill replication**: `RoleManager.SetRole()` works when `AmongUsClient.Instance.AmHost`. Non-host uses `RpcSetRole()` attempt which may not replicate.
- **ESP**: Uses `Camera.WorldToScreenPoint()` (UICamera or Camera.main) and IMGUI `GUI.Box`/`GUI.Label` for overlays.
- **Kill path**: `PlayerControl.SetKillTimer(0f)` + `KillButton.SetTarget()` + `KillButton.DoClick()`. Also attempts `PlayerControl.RpcMurderPlayer()` and `PlayerControl.MurderPlayer()`.
- **Teleport**: Uses `UICamera.ScreenToWorldPoint(Input.mousePosition)` + transform write. Also attempts `CustomNetworkTransform.RpcSnapTo()`.
- **Caution**: This mod modifies game state. Use in single-player or with friends who consent. Host authority limits some features in multi-player.

## Rebuilding After Game Updates

After any Among Us update that changes the interop DLLs:

1. Re-run `build.ps1` to rebuild against the new interop assemblies.
2. Verify the plugin loads correctly (check BepInEx.log for errors).

## License

Unlicense / public domain. Use at your own risk.