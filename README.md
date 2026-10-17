# Keystone × Academic's Rainborn Patch

## v1.1.2 refactor

The patch now uses typed Timberborn, Rainborn and Keystone APIs and a game-scoped Bindito service instead of capturing game service constructors with Harmony. A thread-local guard prevents recursive placement validation. Cleanup is guarded and limited to ground-dependent natural resources in the excavation column; the original Rainborn Loose Dirt handling is preserved.

## Reflection exception disclosed

Harmony must hook Rainborn's private `OnEnterFinishedState` and `OnDeleted` methods and obtain the `_blockObject` field. Rainborn has no public event exposing those specific lifecycle moments. These hooks run only on excavation completion/deletion; the patch has no direct runtime reflection calls. This should be re-evaluated if Rainborn exposes a supported public API.

## Build

Run `python3 scripts/prepare_refs.py /path/to/timberborn-build-refs-20261014-013425.zip`, then `dotnet build TBKSARPatch.csproj -c Release`. The real game, Rainborn and Keystone assemblies are required. Never commit or ship `refs/`. GitHub Actions must likewise receive these refs securely; it cannot compile this version from the old stub alone.

## Validation

Test Excavation Flag versus dynamite placement, Keystone decoration cleanup, Loose Dirt creation, save/load, and concurrent T3MP / TB Optimizer. This source revision has not yet passed a real compile or in-game test.

Author: tharbad08.
