# CI BFL

Server-only Flag Domination helpers for Bannerlord Skirmish, Battle and Captain. Ordinary clients do not need this installed. It depends on Native/Multiplayer and retains module ID `CI_BFL`.

Features: timed flag collapse (one random flag remains), prevention of enemy HP synchronization while preserving real HP for teammates/spectators, distinct mirror-match faction colors and optional corpse fadeout. No database, web service or account credentials are required.

## Build and install

Use the .NET SDK and your own game/server reference assemblies. Source was compiled against installed v1.4.8-compatible assemblies; validate your target runtime version.

```powershell
dotnet build .\CI_BFL.sln -c Release -p:BannerlordPath="D:\Games\Bannerlord"
```

Copy generated `dist/` content into `<DedicatedServer>/Modules/CI_BFL`. Enable `_MODULES_*Native*Multiplayer*CI_BFL*_MODULES_` and restart the server. Windows/Linux module DLLs are staged by the build; game dependencies and generated release outputs are not stored in the public source repository.

## Configure behavior

| Server command | Meaning |
|---|---|
| `ci_bfl_flag_removal_time 60` | Collapse delay in seconds; 0 disables collapse. |
| `ci_bfl_prevent_hp_sync True` | Prevent opponents seeing real HP. |
| `ci_bfl_corpse_fadeout_timer 15` | Optional corpse fadeout delay. |

Admin chat `!setflagtimer <seconds>` adjusts future rounds; `!flagtimer` reports the current setting. The random surviving flag avoids the previous choice when possible. Skirmish Play can be loaded alongside this module for map/warmup commands.

## Source and verification

`src/CI_BFL` contains the server behaviors/commands; `module/SubModule.xml` is the manifest; `Directory.Build.props` contains build path defaults. No production server configs, host addresses, deployment credentials or database details are included. Build success does not prove live flag timing, HP visibility or mirror-match colors; test those on a dedicated server before deployment.
