import json
from pathlib import Path
root = Path(__file__).resolve().parents[1]
m = json.loads((root / "manifest.json").read_text())
s = (root / "Source/ModStarter.cs").read_text()
assert m["Version"] == "1.1.2"
assert m["Id"] == "tharbad08.TBKSARPatch"
assert "RainbornExcavationFlag" in s
assert "OnEnterFinishedState" in s and "OnDeleted" in s
assert "[Context(\"Game\")]" in s
assert "[ThreadStatic]" in s
for name in ("AccessTools", "BindingFlags", "GetMethods(", "MethodInfo", "UnsetTerrain("):
    assert name not in s, name
assert "ProjectReference" not in (root / "TBKSARPatch.csproj").read_text()
assert "reflection" in (root / "AGENTS.md").read_text().lower()
print("Source and dependency invariants OK")
