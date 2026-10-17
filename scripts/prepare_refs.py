"""Prepare exact local compile-only references from a Timberborn reference archive.

This script never uploads or checks game/mod assemblies into Git.
It generates refs/ReferenceAssemblies.props with explicit MSBuild references.
"""
from pathlib import Path
from xml.etree import ElementTree
import sys
import zipfile

if len(sys.argv) != 2:
    raise SystemExit("Usage: python3 scripts/prepare_refs.py /path/to/timberborn-build-refs.zip")

archive_path = Path(sys.argv[1]).expanduser().resolve()
root = Path(__file__).resolve().parents[1] / "refs"
root.mkdir(exist_ok=True)

with zipfile.ZipFile(archive_path) as archive:
    names = archive.namelist()
    game = [name for name in names if "/game/" in name and name.endswith(".dll")]
    if len(game) < 100:
        raise SystemExit("Missing Timberborn game DLLs or malformed reference archive")

    required = {
        "AcademicsRainborn.dll": "/3736240438/Scripts/AcademicsRainborn.dll",
        "Keystone.Core.dll": "/3726908702/Keystone.Core.dll",
        "Code.dll": "/3726908702/Code.dll",
        "0Harmony.dll": "/3677595742/0Harmony.dll",
    }

    selected = {}
    for name in game:
        filename = Path(name).name
        if filename in selected:
            raise SystemExit(f"Ambiguous game assembly: {filename}")
        selected[filename] = name

    for filename, suffix in required.items():
        matches = [name for name in names if name.endswith(suffix)]
        if len(matches) != 1:
            raise SystemExit(f"Expected one {filename} matching {suffix}, found {len(matches)}")
        selected[filename] = matches[0]

    # Clear only our own prior extracted reference DLLs; avoid stale version mismatches.
    for prior in root.glob("*.dll"):
        prior.unlink()
    for filename, name in selected.items():
        (root / filename).write_bytes(archive.read(name))

# Generate real MSBuild assembly references, not a custom ReferenceDll item.
# Each HintPath is the local DLL path. Private=false prevents copying the
# game's, Harmony's, Rainborn's, or Keystone's binaries to the build output.
project = ElementTree.Element("Project")
items = ElementTree.SubElement(project, "ItemGroup")
for dll in sorted(root.glob("*.dll")):
    reference = ElementTree.SubElement(items, "Reference", Include=dll.stem)
    ElementTree.SubElement(reference, "HintPath").text = str(dll.resolve())
    ElementTree.SubElement(reference, "Private").text = "false"

ElementTree.indent(project, space="  ")
props = root / "ReferenceAssemblies.props"
ElementTree.ElementTree(project).write(props, encoding="unicode", xml_declaration=False)
print(f"Prepared {len(selected)} DLLs and {len(items)} explicit MSBuild references.")
print(f"Private reference file: {props}")
