# Prototype verification

Version 0.3 verified November 26, 2026. Earlier observations are identified below.

## Side Guns

- Added weapon 7, firing three pairs of normal-damage bullets perpendicular to the ship heading. Each bullet inherits ship velocity. A tap completes a burst, holding repeats after recharge, and switching weapons cancels the remaining burst and starts recharge.
- Reload duration reads `RailReload` directly. Added `SideGunBurstSize` and `SideGunShotsPerSecond` to the central JSON configuration, preserving existing overrides.
- All **80 automated checks passed**, including lateral geometry, inherited velocity, damage, walls, burst cadence, shared reload duration, switching, restart, and tuning validation. Compilation succeeded with zero warnings or errors.
- Inspected `build/qa/side-guns.png`: six bullets appear in two opposing streams, the seven-weapon selector fits, and Side Guns shows its recharge state.

## Central JSON configuration

- Expanded `Prototype/tuning.json` to 86 editable gameplay, weapon, enemy, pickup, presentation, audio, and startup settings; code defaults and validation live in `Prototype/Core/Tuning.cs`. Existing JSON overrides were preserved, including bomb speed 300/recharge 30, Rail magazine 30, fire rate 6, projectile lifetime 5, and enemy speed 130.
- Missing properties retain code defaults. Missing configuration uses defaults; malformed JSON, duplicates, unknown names, invalid types/ranges, or unsafe cross-setting combinations are rejected with an app warning and code-default fallback. JSON comments/trailing commas and case-insensitive setting names are supported.
- All **75 automated checks passed**, including override/default handling, configuration validation, weapon stats/timing/radius, mine/pickup settings, and configurable enemy limits. Existing gameplay checks still pass against code defaults.
- Inspected `build/qa/config-bomb.png` and `build/qa/config-rail.png`: JSON overrides load at startup and appear in the HUD (30-second bomb recharge and 30-round Rail magazine). HUD/help/effect sizes use the configured values.
- Compilation succeeded; NuGet emitted NU1900 warnings because its vulnerability-data endpoint was unavailable.

## Tactical Bomb remote detonation

- With weapon 6 selected, a fresh Space press launches a bomb or detonates the active one. Holding fire cannot immediately detonate or automatically relaunch; other weapons do not trigger the bomb. Impact detonation is retained.
- Remote and impact detonation share area damage and wall shielding. Detonation preserves the original recharge deadline. The active HUD prompts `SPACE: DETONATE`.
- All **69 automated checks passed**, including new coverage for second-press detonation, damage once, held-key protection, weapon switching, cooldown preservation, and remote blast shielding. Compilation succeeded with zero warnings or errors.

## Projectile velocity inheritance

- All **66 automated checks passed**, including the full simulation soak.
- Confirmed normal rounds, ricochets, Rail Gun rounds, Tactical Bombs, and enemy bullets already add their firing ship's full velocity. No gameplay change was needed. Laser direction and stationary mines are unchanged.
- Added regression coverage across all four player projectile types, three headings, and stationary/forward/sideways/backward motion; verified subsequent ship movement does not steer launched projectiles. Separate checks cover enemy bullets and the laser exception.

## Tactical Bomb velocity update

- Increased default launch speed from 68 to 204 pixels/second. The bomb adds the ship's full velocity at firing, including sideways drift.
- Updated the launch regression to verify 204 pixels/second plus both components of ship motion. A zero resulting velocity safely omits the directional visual trail.
- Earlier Tactical Bomb speed observations below describe its original behavior.

## Wider Death Laser

- Tripled beam rendering: body 7 → 21 pixels, bright core 2 → 6, glow 20 → 60. Enemy and wall collision now use the 21-pixel beam body.
- All **63 automated checks passed**, including beam-edge hit/miss coverage and updated first-hit/wall-contact checks. Build completed with zero warnings or errors. Visually inspected `build/qa/wide-laser.png`.

## Radar targeting marker

- T toggles a straight heading line from the player ship's nose to the radar map boundary. It defaults off, follows rotation, and persists across match restarts within the session.
- Build and Windows export succeeded with zero warnings or errors. Inspected `build/qa/radar-aim.png` and `build/qa/radar-aim-off.png`, captured after one and two T key events respectively; the line and header state toggle correctly.

## Tactical Bomb

- Added key 6: 4× normal damage, one active projectile, 68 pixels/second world speed, 160-pixel blast radius (five nominal ship lengths), and a 60-second recharge from launch.
- Detonates on enemy or wall contact; walls shield enemies, and the player is immune. An active bomb blocks further launches even after recharge. Switching weapons preserves recharge; restarting clears bomb state.
- All **62 automated checks passed**, including seven bomb checks covering area damage/boundaries, shielding, slow launch, cooldown, active limit, reset, fast enemy crossings, and tuning validation.
- Inspected `build/qa/bomb.png` and `build/qa/bomb-blast.png`: all six weapon options fit, active/recharge status renders, and the blast radius is visible. Capture logs contain no errors.
- Build and Windows export succeeded. NuGet reported NU1900 warnings because its vulnerability-data service was unavailable. Manual bomb combat balancing remains untested.

## The Labyrinth

- Added a third built-in arena at 288 × 216 cells, matching The Foundry, with three-cell-wide corridors, connecting loops, four small chambers, and 15 authored enemy starts.
- All **55 automated checks passed**. Bundled-map path checks and every enemy-count option now include The Labyrinth; a dedicated check verifies every floor cell is reachable.
- Build and release export succeeded with zero build warnings or errors. Inspected `build/qa/labyrinth-map.png` and `build/qa/labyrinth-menu.png`; all three arena cards fit at 1280 × 800.
- Select the map by its menu card or key 3. Manual combat balancing on this map remains untested.

## Rail Gun verification

- Rail Gun is selected with key 5: 10 damage (half normal), 12 shots per second (three times normal), and 1020 pixels of unobstructed range from a stationary ship (half normal).
- Its 12-round magazine recharges in 3 seconds after depletion; switching weapons preserves the recharge timer.
- All **54 automated checks passed**, including six Rail Gun checks and the existing simulated 30-minute soak.
- C# compilation succeeded. NuGet emitted NU1900 warnings because its package vulnerability service was unavailable.

## Version 0.3 changes and checks

- Added a 1.2-second player death explosion followed by Game Over and Continue. Continue restarts the same arena and selected enemy count with fresh match state.
- Added Death Laser: 3× normal damage, an 8-second reload that survives weapon switching, and an instantaneous beam to the first enemy or wall.
- Added mines: 2× normal damage, at most four active, stationary round placement, 120-second lifetime, and one detonation on enemy contact. Movement sweeps detect contacts even when ships cross a mine between frames.
- Halved default coasting drag from 24 to 12 pixels per second squared.
- Added up to 15 green energy boosts in random reachable cells. Each heals 10% of maximum health, capped at full health. Missing pickups refill only at five-minute gameplay boundaries. Swept pickup detection handles fast movement. Full-health players leave pickups in place.
- All **48 automated checks passed**, including the existing regression and simulated 30-minute soak. Results: `build/qa/tests-v03.txt`.
- New checks cover laser range, wall occlusion, first-hit damage and reload; mine cap, non-stacking, expiry, damage, owner safety and high-speed triggers; random pickup placement, healing/capping, fast collection and refill boundaries; halved drag; death effects and reset state; invalid timing settings.
- C# compilation completed with zero warnings and zero errors. Windows build: `build/Atomica-v03/Atomica.exe`. Earlier versions remain available; the launcher now uses version 0.3.
- The build script's export-process variable was renamed to avoid collision with PowerShell's case-insensitive Export switch.
- The corrected build/export script completed successfully. Release export logs are `build/qa/export-v03.log` and `build/qa/export-v03-errors.log`.
- Inspected three controlled visual scenarios from the packaged executable at 1280 × 800: `arsenal-v03.png` (laser, round mine, green pickups, reload HUD), `explosion-v03.png` (player destruction), and `gameover-v03.png` (Game Over and Continue). These files are under `build/qa`. Fixtures run only when an explicit capture path and QA argument are supplied; ordinary play starts normally.
- Continue's fresh simulation state is covered by the automated restart check. Full manual button interaction and combat balancing remain part of playtesting.

## Version 0.2 changes and checks

- Kepler Outpost expanded from 72 × 54 to 216 × 162 cells (6912 × 5184 world pixels).
- The Foundry expanded from 96 × 72 to 288 × 216 cells (9216 × 6912 world pixels).
- Added a 1–15 enemy selector. Spawn selection uses distinct reachable cells, preferring authored markers. Match totals and victory use the selected count.
- Player turn rate increased from 3.4 to 5.1 radians per second.
- Added coasting drag of 24 pixels per second squared, applied only without thrust, clamped to an exact zero velocity.
- C# build succeeded with zero warnings and zero errors. All **34 automated checks passed**; output is in `build/qa/tests-v02.txt`.
- New checks cover all 15 enemy counts on both enlarged maps, unique placements, victory and restart counts, custom-map supplementation, insufficient custom-map space, drag at several simulation step sizes, exact stopping, active-thrust behavior and faster rotation.
- The undamped inertia regression explicitly uses `CoastDrag = 0`; default gameplay now uses drag.
- Historical Windows release is in `build/Atomica-v02`. The prior build in `build/Atomica` is retained.
- Export log: `build/qa/export-v02.log`. Menu and gameplay captures: `build/qa/menu-v02.png` and `build/qa/game-v02.png`.
- Inspected both version 0.2 captures from the packaged executable: the enemy selector, new map dimensions, chosen-count HUD and full-map radar render correctly at 1280 × 800. Manual click-through of all selector values has not been performed; count logic is covered by the automated tests.
- Godot's console wrapper remained running after the engine finished exporting. The wrapper was stopped after confirming the engine had exited; the produced executable then ran and captured both views successfully. Build.ps1 now invokes the engine directly to avoid that wrapper.

## Version 0.1 build and package

- Godot 4.7.2 stable .NET, Compatibility renderer, C# targeting .NET 8.
- Final debug compilation completed with zero warnings and zero errors.
- Final Windows x64 release export completed without `ERROR:` messages. See `build/qa/export-final.log`.
- Release folder includes `Atomica.exe`, `Atomica.pck`, loose maps and tuning, and a self-contained .NET 8.0.5 runtime in `data_Atomica_windows_x86_64` (including `coreclr.dll`).
- The standalone executable was launched successfully on this Windows PC. Rendering reports an NVIDIA GeForce GTX 1660 SUPER.

## Automated checks

Version 0.1: all **28 checks passed**. Full output: `build/qa/tests.txt`. Version 0.2 reran these checks plus six new checks described above.

Covered CSV first-row preservation, whitespace/BOM, malformed grids, invalid perimeter, missing/duplicate starts, disconnected enemies, dimension limits, inertia, rotation independent of velocity, forward/reverse thrust, high-speed swept wall collision, glancing and corner reflection, harmless repeated wall contacts, normal projectile destruction, half-damage ricochets, bounce limits, projectile lifetime, single-hit damage, wall shielding, self/friendly-fire immunity, inherited projectile velocity, fire-rate bound, victory/defeat/restart, radar coordinates, sample-map reachability, invalid/nonfinite tuning, and overlapping-ship numerical stability.

The soak check advances 108,000 fixed steps (30 minutes of simulated time) with turning, thrust, continuous firing, weapon changes and repeated restarts. It checks finite player positions and bounded projectile counts. It is an accelerated simulation test, not a 30-minute manual playtest or a memory profiler run.

## Version 0.1 visual and interaction checks

- Captured and inspected the menu and active gameplay render at 1280 × 800; arena cards, launch controls, ship drawing, walls, health display, weapon display and radar are visible and legible.
- Inspected the standalone application window showing a progressed match, ship movement, enemy damage, radar positions and the pause overlay.
- The game window began receiving user input during verification; further automated control was stopped to avoid interfering with play.

## Remaining release validation

This is a playable prototype, not the full production release envisioned in the proposal. Not yet certified: five-minute 1920 × 1080 performance benchmark with 256 × 256 cells, 20 enemies and 500 simultaneous projectiles; 95th-percentile frame timing; map load timing on agreed reference hardware; a separate clean PC; manual combat balancing and full-match victory; long real-time memory profiling; all resize/aspect-ratio combinations; native file-picker interaction and sound quality across devices.

The CSV loader, weapons and result transitions are tested in the simulation. Full end-to-end user-interface testing of every branch remains for the next playtest. No online multiplayer is implemented.
