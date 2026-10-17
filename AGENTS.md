# AI contributor guide — TB-KS-AR-patch

Scope: **one standalone compatibility patch** for Timberborn 1.1, Academic's Rainborn and Keystone. Source belongs here; do not edit, repackage, or fork the upstream mods to fix their behavior. Never require Miners of Mountains.

1. Use game-version.build-number convention (`1.1.1`, `1.1.2`, ...). The install folder is `version-1.1`.
2. Verify all third-party and game API names against supplied game/mod DLLs, and follow the targeted mod's naming and lifecycle. Do not guess an interface signature or use broad terrain-wide Harmony patches.
3. Reuse vanilla dynamite's mechanics where practical. Preserve Rainborn's one-voxel excavation, flag deletion, Loose Dirt creation and demolition, resource costs and build time.
4. Respect player-built structures. Any automatic cleanup is limited to natural resources supported by the removed terrain, and never deletes unrelated objects; a player-built obstruction must be left to the game's placement validator.
5. Use explicit dependency metadata and correct ordering. Include Rainborn, Keystone, Harmony as required, mirror optional dependencies, and keep MoM absent. Validate against actual installed manifests (2026-10-14 user build refs).
6. Scope runtime patches to Rainborn and its placement validation. Protect exceptions, rate-limit repetitive messages, and log diagnostics to Player.log. Do not let a compatibility patch crash the colony.
7. CI must compile a standalone .NET assembly and upload a complete version-1.1 installable ZIP. Do not ship game DLLs, Harmony, compile-time stubs, personal names or identifying file paths.
8. Document manual game tests and candidly distinguish source/API review, CI compile, and actual in-game verification. Never assert game behavior was tested when it was not.
9. Use only **tharbad08** in attribution, manifests and docs. Review Git commit attribution for privacy.
10. Every patch or PR must have a clear problem statement, compatibility rationale and tests; no unrelated optimizations or sweeping cleanup.
