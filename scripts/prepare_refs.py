import sys, zipfile
from pathlib import Path

if len(sys.argv) != 2:
    raise SystemExit("Usage: python3 scripts/prepare_refs.py reference-archive.zip")
root = Path(__file__).resolve().parents[1] / "refs"
root.mkdir(exist_ok=True)
with zipfile.ZipFile(sys.argv[1]) as archive:
    paths = archive.namelist()
    game = [p for p in paths if "/game/" in p and p.endswith(".dll")]
    if not game:
        raise SystemExit("No game DLLs in archive")
    for path in game:
        (root / Path(path).name).write_bytes(archive.read(path))
    additions = {
        "AcademicsRainborn.dll": "/3736240438/Scripts/AcademicsRainborn.dll",
        "Keystone.Core.dll": "/3726908702/Keystone.Core.dll",
        "Code.dll": "/3726908702/Code.dll"
    }
    for target, suffix in additions.items():
        found = [p for p in paths if p.endswith(suffix)]
        if len(found) != 1:
            raise SystemExit("Missing or ambiguous dependency " + target)
        (root / target).write_bytes(archive.read(found[0]))
print("Extracted real compile-only references. Do not commit or distribute refs/.")
