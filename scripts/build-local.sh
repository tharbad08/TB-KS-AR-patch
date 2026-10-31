#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [[ $# -ne 1 ]]; then
  echo "Usage: scripts/build-local.sh /path/to/timberborn-build-refs-20261014-013425.zip" >&2
  exit 2
fi
if ! command -v dotnet >/dev/null 2>&1; then
  echo "Install the .NET 8 SDK locally, then rerun this script (Arch: sudo pacman -S dotnet-sdk-8.0)." >&2
  exit 2
fi
python3 scripts/validate.py
python3 scripts/prepare_refs.py "$1"
dotnet build TBKSARPatch.csproj --configuration Release --nologo
outdir="dist/TBKSARPatch/version-1.1"
mkdir -p "$outdir/Scripts"
cp manifest.json "$outdir/manifest.json"
cp bin/Release/netstandard2.1/TBKSARPatch.dll "$outdir/Scripts/TBKSARPatch.dll"
(cd dist && zip -qr -FS TBKSARPatch_1.1.3.zip TBKSARPatch)
if unzip -Z1 dist/TBKSARPatch_1.1.3.zip | grep -E '(^|/)(refs|0Harmony|Code|Keystone\.Core|AcademicsRainborn)\.dll$'; then
  echo "ERROR: package contains third-party DLLs" >&2; exit 1
fi
echo "Installable mod: $(pwd)/dist/TBKSARPatch_1.1.3.zip"
