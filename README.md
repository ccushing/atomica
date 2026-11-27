# Atomica prototype

A single-player Windows space shooter built in **Godot 4.7.2 .NET / C#**, based on AtomicaRequirements.docx.

## Play

Double-click **Play Atomica.cmd** in this folder, or run **build/Atomica-v03/Atomica.exe**. Keep the executable, PCK file and `data_Atomica_windows_x86_64` folder together. The release includes the .NET runtime; Godot and the SDK are not needed to play. Earlier builds remain in `build/Atomica` and `build/Atomica-v02`.

Select Kepler Outpost (216 × 162 cells, open space), The Foundry (288 × 216 cells, industrial corridors), or **The Labyrinth** (288 × 216 cells, narrow winding corridors and four small chambers). Use **1 / 2 / 3** in the menu or click an arena card. The Labyrinth matches The Foundry's size, with three-cell-wide corridors, alternate routes, and 15 authored enemy starts. Use the **− / +** controls to choose **1–15 enemy ships** (default 6), then click **Launch** or press **Enter**. The count carries across map selections and restarts during the session. Destroy all selected enemies to win. Losing all hull integrity ends the run. Restart from the result screen or pause menu.

| Key | Action |
| --- | --- |
| Left / Right | Rotate |
| Up / Down | Forward / reverse thrust |
| Space | Fire selected weapon / plant mine |
| 1 / 2 | Normal / ricochet rounds |
| 3 / 4 | Death Laser / mine |
| 5 / 6 | Rail Gun / Tactical Bomb |
| 7 | Side Guns |
| Escape | Pause or resume |
| R | Restart while paused or after a result |
| F1 | Flight manual |
| F11 | Toggle fullscreen |
| T | Toggle radar targeting line |
| M | Toggle sound |
| V | Toggle the original XNA ship images |

The ship gently decelerates when thrust is released and reaches a complete stop. Version 0.3 halves coasting drag to 12 pixels/second of speed each second (300 pixels/second stops in about 25 seconds). Drag applies only while neither forward nor reverse thrust is active. Rotation is 50% faster than version 0.1: 5.1 radians/second, about 292 degrees/second. Rotation changes heading, not the direction of velocity. Reverse thrust pushes opposite the ship's heading. Walls reflect velocity without damage. There is no speed cap by default. Friendly fire and self-damage are disabled. Ships can overlap one another in this prototype.

## Weapons and survival

All fired projectiles—normal rounds, ricochets, Rail Gun rounds, Tactical Bombs, Side Gun rounds, and enemy bullets—add the firing ship's full velocity to their launch velocity. This includes sideways and backward drift, sampled when fired. Later ship movement does not steer them. The Death Laser follows the ship's facing direction; planted mines remain stationary.

Press **T** to toggle the radar targeting marker (off by default). A straight line extends from the ship's nose in its current facing direction to the radar map's edge and rotates with the ship. The radar header shows whether aiming is on or off. The setting persists across restarts during the session.

| Weapon | Damage | Behavior |
| --- | --- | --- |
| Normal | 20 | Four shots/second, destroyed by walls; 3-second lifetime |
| Ricochet | 10 | Half damage from launch, three bounces, removed on fourth wall contact; 3-second lifetime |
| Death Laser | 60 | Instant beam stops at the first enemy or wall; 8-second reload |
| Mine | 40 | Round stationary mine, triggers once when an enemy crosses it; 120-second lifetime |
| Tactical Bomb | 80 | Slow projectile; one active, impact detonation, 160-pixel blast radius; 60-second recharge |
| Side Guns | 20 per bullet | Nine paired forward shots from side mounts, then the same reload duration as Rail Gun |

Select **7** and press Space for **Side Guns**. Each shot fires one bullet from each side mount, both forward along the current ship heading. A tap completes a nine-pair burst (18 bullets); holding Space repeats bursts after reload. Bullets use normal damage, muzzle speed, lifetime, and wall behavior, and inherit ship velocity. Switching away cancels any remaining shots and starts reload; switching back cannot bypass it. `SideGunBurstSize` controls pairs per burst (default 9), `SideGunShotsPerSecond` controls the rate within the burst (default 12), and `RailReload` controls both weapons' reload duration (default 3 seconds).

With **Tactical Bomb (6)** selected, press Space to launch, release it, then press Space again to detonate the active bomb at its current position. Holding Space does not trigger detonation or automatically launch another bomb after recharge. Remote detonation works during recharge and does not reset the 60-second timer. Firing another weapon does not detonate the bomb. The HUD displays **SPACE: DETONATE** while it is active. Impact detonation remains available.

Laser and mine damage remain 3× and 2× the configured normal damage. Laser range spans the entire arena until a wall or enemy blocks it; the visible beam lasts 0.3 seconds. Switching weapons does not reset the laser's reload timer. Other weapons can be used during that reload.

Select **6** and press Space to launch a **Tactical Bomb**. Its launch velocity is 204 pixels/second in the ship's facing direction (3× the original launch speed), plus the ship's full velocity at firing, including sideways drift. Remote detonation or contact with an enemy or wall deals 4× configured normal damage once to each enemy whose center is within five nominal 32-pixel ship lengths (160 pixels). Walls shield enemies; the player is immune. Recharge begins at launch and lasts 60 seconds, even across weapon switches. Only one bomb can be active, including after recharge completes; it remains in flight until detonated. The HUD shows active/recharge status, and the radar marks its position. `BombReload` and `BombSpeed` can be adjusted in tuning.json.

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

**`tuning.json` is the central app configuration.** Edit `build/Atomica-v03/tuning.json` beside the executable and restart the game. For development, edit `Prototype/tuning.json`; the build copies this file into the release folder. No `app.config` or `local.settings.json` is needed.

The file lists the supported tuning settings, preserving existing JSON overrides. Code defaults are in `Prototype/Core/Tuning.cs`. You can remove any setting to use its code default, or use `{}` for all defaults. The packaged game prefers the loose JSON beside the executable, then its bundled JSON. If no file exists it uses code defaults. The editor reads `Prototype/tuning.json`. A malformed file, unknown/duplicate name, invalid type, or out-of-range value causes the entire file to be ignored, with a warning in the menu and log. The game remains playable with code defaults; settings are never partially applied after a validation error.

| Settings | Purpose and units |
| --- | --- |
| `Acceleration`, `TurnRate`, `CoastDrag`, `SpeedCap`, `Health` | Movement and hull; world pixels/second, pixels/second², radians/second. Zero drag disables braking; zero speed cap disables the cap. |
| `ShipRadius`, `ProjectileRadius`, `ProjectileMuzzleOffset`, `MuzzleFlashOffset` | Collision and launch geometry in world pixels. The ship hull must fit the 32-pixel map cells. |
| `NormalDamage`, `BouncingDamage`, `ShotsPerSecond`, `MuzzleSpeed`, `ProjectileLifetime`, `MaxBounces` | Standard projectile damage, shots/second, speed, lifetime in seconds, bounce count. |
| `Laser*`, `Rail*`, `Bomb*` | Weapon recharge times, damage multipliers, beam widths, magazine capacity, fire-rate/lifetime multipliers, bomb velocity and blast radius, effect durations. Multipliers are relative to normal rounds; damage rounds to an integer with a minimum of 1. |
| `SideGunBurstSize`, `SideGunShotsPerSecond` | Paired shots per burst and pairs/second within a burst. Reload uses `RailReload`; damage and projectile stats use normal rounds. |
| `MaxMines`, `Mine*` | Mine cap, lifetime, planting interval, damage multiplier, center-to-center placement spacing and trigger radius. |
| `MaxEnergyBoosts`, `Energy*` | Pickup cap, refill interval, heal fraction (0.1 = 10%), pickup radius and spawn clearances. A zero cap disables pickups. |
| `MaxEnemies`, `DefaultEnemyCount`, `DefaultArenaIndex` | Enemy selection cap and startup count; arena 0 = Kepler, 1 = Foundry, 2 = Labyrinth. |
| `Enemy*` | Movement, firing delay/stagger, sight/chase distances, approach/retreat behavior, aim lead, and path refresh/arrival settings. Distances are world pixels and times are seconds. |
| `WindowWidth`, `WindowHeight`, `Fullscreen`, `PhysicsTicksPerSecond` | Startup window and simulation rate. Weapon fire rates must not exceed the physics rate. |
| `MutedAudio`, `OriginalShipArt`, `RadarAimEnabled`, `ShotVolumeDb`, `HitVolumeDb` | Initial preferences and sound volume in decibels. Keyboard toggles still work during play. |
| `CameraFollowRate`, `DeathDelaySeconds`, `HitPulseSeconds`, `ShipHitFlashSeconds`, `LowHullPercent`, `*Particle*` | Camera smoothing, transition/flash timing, low-hull warning percentage, and particle counts, speeds, and lifetimes. |

Example of a smaller override file:

```json
{
  "BombSpeed": 250,
  "BombReload": 45,
  "MaxMines": 6,
  "DefaultEnemyCount": 8,
  "RadarAimEnabled": true,
  "ShotVolumeDb": -28
}
```

Names are case-insensitive; JSON comments and trailing commas are accepted. Restart to apply edits. HUD counters, weapon stats, blast visuals, and help text read the configured values. The named limits in `Tuning.cs` document accepted ranges and cross-setting constraints. Map format, UI layout/art coordinates, collision solver tolerances, and engine resource identifiers remain implementation details rather than gameplay tuning settings. The weapon stats elsewhere in this README describe the code defaults; your JSON can override them.

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
