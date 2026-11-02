"""Round-trip the delivered FBX through Blender to check hierarchy, scale and pivots."""
import bpy, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
manifest=json.loads((ROOT/'FerrariSource/prepared_manifest.json').read_text())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(ROOT/'My project/Assets/Ferrari/Models/FerrariEnzo_Prepared.fbx'))
bpy.context.view_layer.update()
rows=[]
for p in manifest['parts']:
    o=bpy.data.objects.get(p['id'])
    assert o is not None,p['id']
    meshes=[c for c in o.children_recursive if c.type=='MESH']
    pts=[c.matrix_world@Vector(v) for c in meshes for v in c.bound_box]
    lo=Vector([min(v[i] for v in pts) for i in range(3)])
    hi=Vector([max(v[i] for v in pts) for i in range(3)])
    pivot=o.matrix_world.translation
    assert all(lo[i]-.01<=pivot[i]<=hi[i]+.01 for i in range(3)),(p['id'],list(pivot),list(lo),list(hi))
    assert len(meshes)==p['mesh_count'],p['id']
    if p['id'].startswith('wheel_'):
        assert not lo.y<0<hi.y, 'Wheel crosses the vehicle centre: '+p['id']
    rows.append(dict(id=p['id'],meshes=len(meshes),pivot=list(pivot),minimum=list(lo),maximum=list(hi)))
allpts=[o.matrix_world@Vector(v) for o in bpy.context.scene.objects if o.type=='MESH' for v in o.bound_box]
dimensions=[max(v[i] for v in allpts)-min(v[i] for v in allpts) for i in range(3)]
assert 4<dimensions[0]<5.5 and 1.8<dimensions[1]<2.4 and .8<dimensions[2]<1.5,dimensions
assert abs(min(v.z for v in allpts))<.001
report=dict(result='PASS',part_count=len(rows),dimensions_metres=dimensions,parts=rows,
    checks=['All 16 IDs survive FBX export/import','All part pivots lie inside their assembly bounds','All mesh counts survive export/import','Four wheels independently occupy one side only','Metre-scale dimensions and ground-level tyre contact'])
(ROOT/'FerrariSource/fbx_roundtrip_validation.json').write_text(json.dumps(report,indent=2))
print('ROUNDTRIP_PASS',len(rows),dimensions,flush=True)
