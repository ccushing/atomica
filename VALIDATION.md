# Prototype verification

Version 0.3 verified November 26, 2026. Earlier observations are identified below.

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
