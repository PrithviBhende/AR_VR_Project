"""Prepare the imported source into named, metre-scale assemblies; retain source detail."""
import bpy, bmesh, json, math
from pathlib import Path
from mathutils import Vector, Matrix
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'FerrariSource'
ASSETS=ROOT/'My project/Assets/Ferrari/Models'
ASSETS.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Ferrari_Imported.blend'))
source=list(bpy.context.scene.objects)

def material_settings(name):
    n=name.lower()
    if 'redglass' in n or 'tailight' in n: return ((.55,.008,.006,1),.1,.2)
    if 'amber' in n: return ((1,.22,.015,1),.1,.22)
    if 'glass' in n: return ((.035,.075,.09,1),.45,.12)
    if 'rubber' in n or 'carpet' in n: return ((.018,.022,.026,1),0,.7)
    if 'carbon' in n: return ((.04,.05,.055,1),.2,.38)
    if 'leather' in n: return ((.08,.045,.034,1),0,.5)
    if 'gold' in n: return ((.65,.38,.075,1),.6,.3)
    if 'chrome' in n or 'silver' in n or 'brushed' in n: return ((.6,.65,.7,1),.8,.24)
    if 'headlight' in n or 'drl' in n: return ((.85,.92,1,1),.15,.16)
    if 'high gloss' in n or n in ('base_1','red plastic_1','default'): return ((.63,.008,.014,1),.32,.23)
    return ((.06,.065,.07,1),.25,.4)

material_rows=[]
for mat in bpy.data.materials:
    color,metal,rough=material_settings(mat.name)
    mat.diffuse_color=color
    mat.use_nodes=True
    bs=mat.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=color
    bs.inputs['Metallic'].default_value=metal
    bs.inputs['Roughness'].default_value=rough
    material_rows.append(dict(name=mat.name,color=list(color),metallic=metal,smoothness=1-rough))

def classify(n):
    l=n.lower()
    # These misleadingly named source meshes span both seats / the shared cockpit.
    if l in ('cp_rseat_badge_001','fer_enzo_02_cockpit_s_seatr_bump_metal2__001'): return 'body'
    if 'doorlf' in l or 'doorcardlf' in l or l=='cockpit_s_glasslf_int' or l=='glasslr_window_int': return 'door_L'
    if 'doorrf' in l or 'doorcardrf' in l or l=='cockpit_s_glassrf_int' or l=='glassrr_window_int': return 'door_R'
    if 'hooda' in l: return 'bonnet'
    if 'trunk' in l or l=='glassr_window_int': return 'engine_cover'
    if 'enginebay' in l: return 'body'
    if 'engine' in l: return 'engine'
    if 'steering_wheel' in l or l.startswith('sw_') or l.startswith('cp_sw_') or l in ['led_'+str(i) for i in range(5)] or l=='cp_airbag': return 'steering_wheel'
    if 'seatl' in l or 'l_seat' in l or 'lseat' in l: return 'seat_L'
    if 'seatr' in l or 'r_seat' in l or 'rseat' in l: return 'seat_R'
    if 'wing' in l: return 'rear_wing'
    if 'bumperf' in l or 'bumperfa' in l or 'front_bumper' in l: return 'bumper_F'
    if 'bumperr' in l or 'bumperra' in l or l.startswith('rl_') or 'exhausta' in l or l=='ext_diff_blackplane': return 'bumper_R'
    return 'body'

groups={}
mapping=[]
mid=-.584
for obj in source:
    if obj.type!='MESH': continue
    name=obj.name
    wheel=name.startswith(('Default','mom_ten','Solid1_DC','_DC_Shell'))
    if wheel:
        world=[obj.matrix_world@v.co for v in obj.data.vertices]
        axle='F' if sum(v.x for v in world)/len(world)<0 else 'R'
        # Source wheel objects contain both sides. Verify no polygon bridges the centre plane.
        assert not any(min(world[i].y for i in p.vertices)<mid-.01 and max(world[i].y for i in p.vertices)>mid+.01 for p in obj.data.polygons), name
        for side,negative in [('L',True),('R',False)]:
            clone=obj.copy();clone.data=obj.data.copy();bpy.context.collection.objects.link(clone)
            bm=bmesh.new();bm.from_mesh(clone.data)
            remove=[v for v in bm.verts if ((clone.matrix_world@v.co).y>mid if negative else (clone.matrix_world@v.co).y<mid)]
            bmesh.ops.delete(bm,geom=remove,context='VERTS');bm.to_mesh(clone.data);bm.free()
            clone.name=f'{name}_{side}'
            groups.setdefault('wheel_'+axle+side,[]).append(clone)
            mapping.append(dict(source=name,mesh=clone.name,part='wheel_'+axle+side))
        bpy.data.objects.remove(obj,do_unlink=True)
    else:
        part=classify(name);groups.setdefault(part,[]).append(obj)
        mapping.append(dict(source=name,mesh=name,part=part))

# Source centimetre-like extents: convert to metres, centre width/length, ground tyres.
allmesh=[o for os in groups.values() for o in os]
bpy.context.view_layer.update()
points=[o.matrix_world@Vector(c) for o in allmesh for c in o.bound_box]
cx=(min(v.x for v in points)+max(v.x for v in points))/2
cy=(min(v.y for v in points)+max(v.y for v in points))/2
floor=min(v.z for v in points)
normalise=Matrix.Diagonal((.01,.01,.01,1)) @ Matrix.Translation((-cx,-cy,-floor))
for o in allmesh:
    o.data.transform(normalise@o.matrix_world);o.matrix_world=Matrix.Identity(4)
    # CAD brake hardware dominates the source; reduce only that hidden high-density detail.
    if 'DC_Shell' in o.name and len(o.data.polygons)>8000:
        bpy.context.view_layer.objects.active=o
        dec=o.modifiers.new('Brake hardware reduction','DECIMATE');dec.ratio=.25
        bpy.ops.object.modifier_apply(modifier=dec.name)

bpy.context.view_layer.update()
root=bpy.data.objects.new('FerrariEnzo',None);bpy.context.collection.objects.link(root)
parts=[]
for key,objects in sorted(groups.items()):
    parent=bpy.data.objects.new(key,None);bpy.context.collection.objects.link(parent);parent.parent=root
    pts=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
    lo=Vector([min(p[i] for p in pts) for i in range(3)]);hi=Vector([max(p[i] for p in pts) for i in range(3)])
    pivot=(lo+hi)/2
    parent.location=pivot
    for o in objects:
        o.data.transform(Matrix.Translation(-pivot));o.parent=parent;o.location=(0,0,0)
    parts.append(dict(id=key,mesh_count=len(objects),faces=sum(len(o.data.polygons) for o in objects),pivot_blender=list(pivot),minimum=list(lo),maximum=list(hi)))

bpy.context.view_layer.update()
bpy.context.scene.unit_settings.system='METRIC'
bpy.context.scene.unit_settings.scale_length=1
bpy.ops.object.select_all(action='DESELECT')
root.select_set(True)
for o in root.children_recursive:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(ASSETS/'FerrariEnzo_Prepared.fbx'),use_selection=True,object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,add_leaf_bones=False)
(OUT/'prepared_manifest.json').write_text(json.dumps(dict(parts=parts,materials=material_rows,source_mapping=mapping,notes=['Replacement materials: source MTL missing','Blender +Z up; vehicle front -X; Unity FBX importer converts axes','Wheel left/right determined by source door LF/RF side','Hidden CAD brake detail reduced to 25%; original retained']),indent=2))
(ASSETS.parent/'prepared_manifest.json').write_text((OUT/'prepared_manifest.json').read_text())

# Save editable car before adding render-only staging.
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'FerrariEnzo_Prepared.blend'))
def simplemat(name,color,metal=0):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Base Color'].default_value=(*color,1);b.inputs['Metallic'].default_value=metal;b.inputs['Roughness'].default_value=.35
    return m
bpy.ops.mesh.primitive_cylinder_add(vertices=128,radius=3,depth=.14,location=(0,0,-.075))
bpy.context.object.data.materials.append(simplemat('Preview base',(.045,.065,.085),.35))
scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=1400;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.world.color=(.22,.22,.22)
for location,power,size in [((-3,-4,7),1800,5),((1,4,5),2200,4),((5,-1,4),1500,3)]:
    bpy.ops.object.light_add(type='AREA',location=location);light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=size;light.rotation_euler=(Vector((0,0,.5))-light.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(-6,-7,4.5));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,.5))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=7.7;scene.camera=camera
scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(ROOT/'Evidence/Ferrari_Member1_Assembled_Blender.png');bpy.ops.render.render(write_still=True)
print('FERRARI_PREPARED',len(parts),sum(p['faces'] for p in parts),flush=True)
