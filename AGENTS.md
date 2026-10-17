# AI contributor guide — TB-KS-AR-patch

Scope: one standalone compatibility patch for Timberborn 1.1, Academic's Rainborn and Keystone. Do not fork or repackage upstream mods. Never require Miners of Mountains.

1. Version as `<game version>.<build number>` (`1.1.1`, `1.1.2`). Install under `version-1.1`.
2. Confirm game/mod API names, signatures, and accessibility against the actual installed DLLs. Follow the target mod's lifecycle and code conventions. Never guess an API.
3. **Avoid reflection by default.** Prefer compile-time typed references, direct public API calls, normal C# generics, Bindito dependency injection and targeted Harmony attributes. Do not use reflection merely to avoid adding required game/mod DLL references. Do not use runtime method/field discovery in regularly executed gameplay callbacks when a typed alternative exists.
4. **If reflection is genuinely unavoidable, tell the user before release.** Identify the exact target member, why public/typed APIs cannot do the job, its invocation frequency and performance/reentrancy risks. Document the exception in the source/README, validate it against the actual DLL, and avoid broad patches. A Harmony patch to a private lifecycle method is a legitimate boundary, but it still requires documentation.
5. Keep Rainborn's one-voxel excavation, Loose Dirt placement, demolition and original resource cost/timing. Use the vanilla dynamite placement rules where applicable; never recurse indefinitely or circumvent native validation.
6. Respect player-built objects. Only delete ground-dependent natural resources of the removed cell, never unrelated paths/structures. Snapshot changing collections and guard indirect reentry.
7. Game state must be scene-scoped and cleared on unload; use Bindito over constructor interceptions. Never run full-map or every-tick scans for a rare excavation event.
8. Use required dependencies for Rainborn, Keystone and Harmony; optional deps for optional integration only. Document the effective ordering and keep MoM absent.
9. Log actionable errors to Player.log with the [TB-KS-AR] prefix; rate-limit repeated warnings. Catch expected mod incompatibilities without silencing unrelated game faults.
10. CI must validate sources and compile using real version-matched references when available. Keep game/mod DLLs outside the repository and **do not distribute reference DLLs**, compile-time stubs, Harmony, personal information or identifying paths. Distinguish source checks, compile checks and manual gameplay tests in all releases.
11. Use only **tharbad08** as public attribution. Review Git commit attribution for privacy.
12. Document manual test cases: terrain removal, natural resource cleanup, Loose Dirt, dynamite placement parity, save/load, other performance mods, and alternate factions. Do not claim in-game testing without evidence.
