"""Reimport the exported FBX files, validate geometry, then render the source library."""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
folder=ROOT/'Assets/Resources/Props/Mugs'
report={'category':'mugs','models':[]}
manifest=json.loads((folder/'manifest.json').read_text(encoding='utf-8'))
for item in manifest['models']:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(folder/(item['id']+'.fbx')))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    assert meshes,item['id']+' missing meshes'
    triangles=0;issues=[];materials=set();closed=0
    for ob in meshes:
        mesh=ob.data;mesh.calc_loop_triangles();triangles+=len(mesh.loop_triangles)
        assert mesh.uv_layers,ob.name+' missing UVs'
        assert all(math.isfinite(c) for v in mesh.vertices for c in v.co),'Nonfinite vertex'
        assert all(math.isfinite(c) for u in mesh.uv_layers.active.data for c in u.uv),'Nonfinite UV'
        assert all(t.area>1e-12 for t in mesh.loop_triangles),ob.name+' degenerate triangles'
        materials.update(s.material.name.split('.')[0] for s in ob.material_slots if s.material)
        bm=bmesh.new();bm.from_mesh(mesh)
        # FBX can split identical positions at hard-normal or UV seams.
        bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001)
        boundary=sum(e.is_boundary for e in bm.edges)
        nonmanifold=sum(not e.is_manifold for e in bm.edges)
        if nonmanifold:issues.append({'mesh':ob.name,'boundary_edges':boundary,'nonmanifold_edges':nonmanifold})
        else:
            closed+=1
            assert bm.calc_volume(signed=True)>0,ob.name+' inward normals'
        bm.free()
    assert not issues,str(issues)
    assert triangles==item['triangles'],(triangles,item['triangles'])
    assert materials==set(item['materials']),(materials,item['materials'])
    world=[ob.matrix_world@Vector(c) for ob in meshes for c in ob.bound_box]
    size=[max(v[k] for v in world)-min(v[k] for v in world) for k in range(3)]
    # Blender reimport restores Z-up; all cups are hand-sized, not centimetre-scaled.
    assert .08<size[2]<.15 and .10<size[0]<.30,(item['id'],size)
    report['models'].append({'id':item['id'],'triangles':triangles,'closed_meshes':closed,'materials':sorted(materials),'size_m':size,'status':'PASS'})
    print('MUG_CHECK_PASS',item['id'],triangles,flush=True)
bpy.ops.wm.open_mainfile(filepath=str(Path(__file__).with_name('Mugs.blend')))
missing=[]
for img in bpy.data.images:
    if img.source=='FILE' and not Path(bpy.path.abspath(img.filepath)).exists():missing.append(img.filepath)
assert not missing,missing
report['missing_textures']=missing
Path(__file__).with_name('Mugs-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=48
scene.cycles.use_denoising=True
scene.render.resolution_x=1400;scene.render.resolution_y=850;scene.render.resolution_percentage=100
scene.world.color=(.3,.3,.3)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.001))
floor=bpy.context.object;floor.name='Inspection surface'
mat=bpy.data.materials.new('Neutral inspection stone');mat.diffuse_color=(.17,.19,.20,1);floor.data.materials.append(mat)
for loc,power,size in [((.0,-.5,.8),35,.8),((-.5,.35,.5),22,.65),((.5,.2,.7),30,.5)]:
    bpy.ops.object.light_add(type='AREA',location=loc);light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=size
    light.rotation_euler=(Vector((0,0,.035))-light.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(.39,-.82,.58))
camera=bpy.context.object;camera.rotation_euler=(Vector((.01,0,.03))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO';camera.data.ortho_scale=.98;scene.camera=camera
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(Path(__file__).with_name('Mugs-preview.png'))
bpy.ops.render.render(write_still=True)
print('MUG_VALIDATION_COMPLETE',flush=True)
