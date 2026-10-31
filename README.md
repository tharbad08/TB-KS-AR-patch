# Keystone × Academic's Rainborn Patch

## v1.1.3: Rainborn flag discovery fix

The original patch guessed `RainbornExcavationFlag` template names and consequently could not identify the flag spec on some installations. This revision resolves **all** excavation flag block specs using Rainborn's public `RainbornExcavationFlagSpec` marker through `ISpecService.GetSpecs<T>()` at game-scoped `Load()`, not `TemplateNameMapper` names or reflection. It preserves the existing guarded dynamite placement validation and unchanged excavation/Loose Dirt/natural-resource cleanup.

Expected `Player.log`: `[TB-KS-AR] Placement parity active for N Rainborn excavation flag blueprint(s).` A zero-count warning means the integration is not active; it is not evidence of passing gameplay tests.

Manual validation: verify flag placement versus dynamite over supported terrain, unsupported edges and occupied cells, with Folktails and IronTeeth; excavate one block under Keystone decoration and check cleanup/Loose Dirt; save/reload; then test with T3MP and TB Optimizer. Compare with the original mod for cost/timing.

## v1.1.2 refactor

The patch now uses typed Timberborn, Rainborn and Keystone APIs and a game-scoped Bindito service instead of capturing game service constructors with Harmony. A thread-local guard prevents recursive placement validation. Cleanup is guarded and limited to ground-dependent natural resources in the excavation column; the original Rainborn Loose Dirt handling is preserved.

## Reflection exception disclosed

Harmony must hook Rainborn's private `OnEnterFinishedState` and `OnDeleted` methods and obtain the `_blockObject` field. Rainborn has no public event exposing those specific lifecycle moments. These hooks run only on excavation completion/deletion; the patch has no direct runtime reflection calls. This should be re-evaluated if Rainborn exposes a supported public API.

## Build

Run `python3 scripts/prepare_refs.py /path/to/timberborn-build-refs-20261014-013425.zip`, then `dotnet build TBKSARPatch.csproj -c Release`. The real game, Rainborn and Keystone assemblies are required. Never commit or ship `refs/`. GitHub Actions performs source checks only, because genuine game/mod DLLs must remain outside the repository. Compile and package locally using the offline build script below.

## Validation

Test Excavation Flag versus dynamite placement, Keystone decoration cleanup, Loose Dirt creation, save/load, and concurrent T3MP / TB Optimizer. This source revision has not yet passed a real compile or in-game test.

Author: tharbad08.

## Private-reference build (no GitHub secret required)

On your Arch Linux machine with the .NET 8 SDK installed, run:

```bash
bash scripts/build-local.sh /path/to/timberborn-build-refs-20261014-013425.zip
```

This creates `dist/TBKSARPatch_1.1.3.zip` using real game/mod DLLs without uploading or embedding them in the distribution. The script fails on compile errors and checks for bundled third-party DLLs. The remote workflow verifies only source invariants; **it cannot certify a build**.
