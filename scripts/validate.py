import json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
m=json.loads((root/'manifest.json').read_text())
assert m['Id']=='tharbad08.TBKSARPatch'
assert m['Version'].startswith('1.1.')
ids=[r['Id'] for r in m['RequiredMods']]
assert ids==['Harmony','academicunwinds.Rainborn','SylvanGames.Keystone']
assert len(set(ids+[x['Id'] for x in m['OptionalMods']]))==len(ids)+len(m['OptionalMods'])
assert not any('Mom' in i or 'MoM' in i for i in ids)
s=(root/'Source/ModStarter.cs').read_text()
assert 'RainbornExcavationFlag' in s
assert 'OnEnterFinishedState' in s and 'OnDeleted' in s
assert 'UnsetTerrain(' not in s, 'Patch must not re-excavate terrain'
assert 'IsNaturalDecoration' in s and 'IsGroundDependentAt' in s
print('Manifest and source invariants validated')
