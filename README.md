# Timberborn — Keystone × Academic's Rainborn Patch

Standalone compatibility patch for **Timberborn 1.1**, **Academic's Rainborn 0.2.0.4** and **Keystone 1.3.3**.

**Why:** Rainborn's `RainbornExcavationFlag.OnEnterFinishedState` removes one terrain voxel using `ITerrainService.UnsetTerrain`. Vanilla `Dynamite.LowerTerrainBelow` does the same. However Rainborn then creates a temporary **Loose Dirt** obstruction in `OnDeleted` *only if* the game's `BlockValidator.BlocksValid` passes. A Keystone/vanilla natural decoration spawned during this gap can prevent Loose Dirt from appearing. Excavation can also leave unsupported decorations.

## Changes

- Keeps Rainborn's one-voxel excavation and Loose Dirt follow-up, without changing the upstream assemblies or weather behavior.
- Compares **Excavation Flag** placements with the game's vanilla **Dynamite** block validator (when the template is resolved). Enforces a terrain block directly underneath and prevents placing on the bottom terrain level. Vanilla validator remains authoritative for ordinary placement failures.
- After excavation and immediately before Rainborn's Loose Dirt creation, checks only the removed terrain's column. Deletes only ground-dependent *natural resources* (Keystone or vanilla), never player-built structures or paths.
- If a player-built obstruction prevents Loose Dirt placement, leaves it to the native Rainborn `BlocksValid` check. **Never force-spawns Loose Dirt into an occupied cell.**
- No Miners of Mountains requirement, no changes to Keystone's own ecology scheduler, no scanning entire maps every tick.

## Requirements and load order

Required: `Harmony` >=2.4.1.0, `academicunwinds.Rainborn` >=0.2.0.4, `SylvanGames.Keystone` >=1.3.3.0. `TimberUi` and `eMka.ModSettings` are mirrored optional requirements from the supplied parent manifests (also required transitively by Keystone and Rainborn). The exact IDs come from the installed 2026-10-14 reference archive; no manual load order needed.

## Install

Download the build artifact from GitHub Actions, unzip into local Timberborn `Mods` directory. The artifact contains `TBKSARPatch/version-1.1/manifest.json` and `Scripts/TBKSARPatch.dll`. **Disable or remove any earlier experimental RainbornKeystoneCleanup mod** to avoid duplicate patches. Back up your save before the first run.

## Test matrix (requires user gameplay)

1. New game, MoM disabled. Place an Excavation Flag on terrain next to a Keystone stone. Verify only legal vanilla-dynamite locations are accepted.
2. Complete excavation: terrain block removed, Loose Dirt appears, then can be demolished. No floating rock or missing loose dirt.
3. Excavate under a vanilla natural decoration. Verify the surface resource doesn't remain suspended.
4. Spawn a natural decoration at the cleared cell before flag deletion (if reproducible). Ensure no `BlocksValid` failures, collisions or crash.
5. Test underwater, atop paths, against unsupported upper structures, and at map minimum height. Verify dynamite-placement parity.
6. Reload a save mid-construction and after excavating. Verify no stale services, no duplicates, normal Loose Dirt demolition.
7. Verify no changed behavior in dynamite, rain, weather, Keystone spawning or with MoM absent.

**Validation status:** source/API inspection against the user's 2026-10-14 game/ref ZIP. Compilation will be checked via GitHub Actions. Actual in-game behavior **not yet verified**.

## Diagnostics

All warnings/errors use `[TB-KS-AR]` in `Player.log`. Only the Rainborn excavation lifecycle and the Rainborn-specific placement candidate are patched. Any unexpected API failure fails safely without globally changing excavation.

Author: **tharbad08**.
