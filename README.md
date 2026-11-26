# Atomica prototype

A single-player Windows space shooter built in **Godot 4.7.2 .NET / C#**, based on AtomicaRequirements.docx.

## Play

Double-click **Play Atomica.cmd** in this folder, or run **build/Atomica-v03/Atomica.exe**. Keep the executable, PCK file and `data_Atomica_windows_x86_64` folder together. The release includes the .NET runtime; Godot and the SDK are not needed to play. Earlier builds remain in `build/Atomica` and `build/Atomica-v02`.

Select Kepler Outpost (216 × 162 cells, open space) or The Foundry (288 × 216 cells, corridors). Both maps now have nine times their original area. Use the **− / +** controls to choose **1–15 enemy ships** (default 6), then click **Launch** or press **Enter**. The count carries across map selections and restarts during the session. Destroy all selected enemies to win. Losing all hull integrity ends the run. Restart from the result screen or pause menu.

| Key | Action |
| --- | --- |
| Left / Right | Rotate |
| Up / Down | Forward / reverse thrust |
| Space | Fire selected weapon / plant mine |
| 1 / 2 | Normal / ricochet rounds |
| 3 / 4 | Death Laser / mine |
| Escape | Pause or resume |
| R | Restart while paused or after a result |
| F1 | Flight manual |
| F11 | Toggle fullscreen |
| M | Toggle sound |
| V | Toggle the original XNA ship images |

The ship gently decelerates when thrust is released and reaches a complete stop. Version 0.3 halves coasting drag to 12 pixels/second of speed each second (300 pixels/second stops in about 25 seconds). Drag applies only while neither forward nor reverse thrust is active. Rotation is 50% faster than version 0.1: 5.1 radians/second, about 292 degrees/second. Rotation changes heading, not the direction of velocity. Reverse thrust pushes opposite the ship's heading. Walls reflect velocity without damage. There is no speed cap by default. Friendly fire and self-damage are disabled. Ships can overlap one another in this prototype.

## Weapons and survival

| Weapon | Damage | Behavior |
| --- | --- | --- |
| Normal | 20 | Four shots/second, destroyed by walls; 3-second lifetime |
| Ricochet | 10 | Half damage from launch, three bounces, removed on fourth wall contact; 3-second lifetime |
| Death Laser | 60 | Instant beam stops at the first enemy or wall; 8-second reload |
| Mine | 40 | Round stationary mine, triggers once when an enemy crosses it; 120-second lifetime |

Laser and mine damage remain 3× and 2× the configured normal damage. Laser range spans the entire arena until a wall or enemy blocks it; the visible beam lasts 0.3 seconds. Switching weapons does not reset the laser's reload timer. Other weapons can be used during that reload.

Select **4**, then press Space to plant a mine at the ship's current position. Up to four can be active. Holding Space plants as you move with a half-second placement delay; mines cannot be stacked in the same spot. Exploding or expiring mines free slots. Mines do not hurt the player. The HUD shows active mine count; amber radar dots mark mines.

Green circular pickups with a plus sign restore **10% of maximum hull** (10 points by default), capped at full health. They remain in place when the player already has full health. Up to 15 are placed randomly in reachable empty cells at launch. Missing pickups are replenished on five-minute gameplay boundaries; collecting one does not immediately spawn a replacement. Existing uncollected pickups remain. Pausing freezes the timer. Green radar dots locate the pickups; the HUD shows the next refill countdown. Very small maps can have fewer than 15 when there is not enough safe space.

When the player dies, a visible explosion plays for about 1.2 seconds, followed by a **Game Over** dialog. **Continue** (or Enter) restarts the current arena with the same selected enemy count, full health, fresh pickups, no planted mines, and reset weapon timers.

## Custom maps

Choose **Load CSV** from the arena selector. The map is validated before replacing the selected arena; an invalid file displays an error and keeps the previous map available.

- UTF-8, comma-delimited rectangular grid, 3–512 rows and columns, no header.
- `0` empty, `1` wall, `2` player start, `3` enemy start.
- Exactly one player start, at least one enemy, closed outer wall perimeter.
- All enemy starts must be reachable through empty cells from the player.
- Each cell is 32 world pixels. Spawns are centered in cells; the conservative ship collision hull has a 12-pixel half-width.
- First CSV row is part of the map. Rows progress down; columns progress right.

Sample maps are in `Prototype/Maps` and copied to `build/Atomica-v03/Maps`. The packaged game reads the loose maps beside the executable first, so those copies can be edited without rebuilding. To create a new map, save a CSV and load it with the selector. Godot's CSV import sidecars use `importer="keep"` to prevent translation import. Enemy selection prefers authored markers and spreads additional ships across distinct reachable empty cells when more ships are requested than the CSV contains. A tiny custom map without enough empty cells reports an error at launch; choose a smaller count.

## Tune gameplay

Edit `build/Atomica-v03/tuning.json` and restart the game. For development, edit `Prototype/tuning.json`. Settings include acceleration, turn rate, coasting drag, health, damage, fire rate, muzzle speed, lifetime, bounce limit, enemy speed and fire interval. New settings are `LaserReload` (8 seconds), `MineLifetime` (120 seconds), and `EnergyRefillSeconds` (300 seconds). `CoastDrag: 0` restores undamped drift; larger values stop the ship sooner. `SpeedCap: 0` means unlimited speed. Invalid tuning falls back to the defaults and reports a message in the menu. Changes are read at application startup.

## Develop and build

- Source: `Prototype/` with separate simulation, collision, map parsing and presentation code.
- Engine: official Godot **4.7.2 stable .NET**, downloaded and SHA-512 verified under `.tools/godot`.
- Export templates: matching official **4.7.2 .NET** templates under `.tools/templates`.
- SDK: **.NET 8.0.300**, pinned in `global.json` with latest-feature roll-forward within .NET 8.
- Use **Open Atomica Editor.cmd** to open the project. It selects the .NET 8 runtime because this machine also has a newer runtime without a matching SDK.
- Run `./Build.ps1 -Test -Export` in PowerShell to compile, test and export. Package restore may need internet access on a clean checkout.
- Run `dotnet run --project Tests/Atomica.Tests.csproj` for the pure C# regression tests. The build script sets local caches automatically.

Keep `.tools` when rebuilding on this machine. A clean checkout needs the matching official engine/templates at the paths above, or adjustments to the launcher, NuGet.Config and export preset. Download sources: https://github.com/godotengine/godot-builds/releases/tag/4.7.2-stable and https://dotnet.microsoft.com/download/dotnet/8.0.

## Prototype scope

Implemented: two arenas, runtime custom CSV loading, inertial movement, continuous wall sweeps, four weapons, green energy pickups, pursuing/patrolling enemies with A* routes and line-of-sight fire, radar, health, death explosion and Continue dialog, win/lose/restart, pause on focus loss, sound, fullscreen, and original-art toggle.

The physics model uses conservative axis-aligned square hulls for ship-to-wall collision and circles for projectile hits. At extreme velocities, a 16-contact-per-tick safety bound discards unresolved movement instead of allowing tunneling. This is a deliberate prototype tradeoff. Enemy steering uses assisted velocity control. Player flight preserves inertia while allowing gentle coasting drag.

Presentation uses a single Godot scene with a custom-drawn view; gameplay is separated into C# modules. Reusable player/enemy scenes, advanced AI, ship-to-ship impacts, save files, key rebinding, gamepad support, an in-game map editor, and online multiplayer are not part of this prototype.

See `VALIDATION.md` for completed checks and remaining release validation. This build has not completed the proposal's reference-hardware performance certification or testing on a separate clean PC.
