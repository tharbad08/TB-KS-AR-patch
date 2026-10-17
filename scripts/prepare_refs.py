from pathlib import Path
import sys
import zipfile

if len(sys.argv) != 2:
    raise SystemExit("Usage: python3 scripts/prepare_refs.py /path/to/timberborn-build-refs.zip")
archive_path = Path(sys.argv[1])
root = Path(__file__).resolve().parents[1] / "refs"
root.mkdir(exist_ok=True)
with zipfile.ZipFile(archive_path) as archive:
    names = archive.namelist()
    game = [name for name in names if "/game/" in name and name.endswith(".dll")]
    assert len(game) > 100, "Game references missing or archive malformed"
    for name in game:
        (root / Path(name).name).write_bytes(archive.read(name))
    required = {
        "AcademicsRainborn.dll": "/3736240438/Scripts/AcademicsRainborn.dll",
        "Keystone.Core.dll": "/3726908702/Keystone.Core.dll",
        "Code.dll": "/3726908702/Code.dll",
        "0Harmony.dll": "/3677595742/0Harmony.dll",
    }
    for target, suffix in required.items():
        matches = [name for name in names if name.endswith(suffix)]
        if len(matches) != 1:
            raise SystemExit(f"Expected exactly one {target} ({suffix}); found {len(matches)}")
        (root / target).write_bytes(archive.read(matches[0]))
print(f"Prepared {len(game) + len(required)} real references locally. Never commit or upload refs/.")
