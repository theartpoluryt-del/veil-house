import bpy,json,math,sys
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
category=sys.argv[sys.argv.index('--')+1]
folder=root/'Assets/Resources/Props'/category
manifest=json.loads((folder/'manifest.json').read_text(encoding='utf-8'))
checks=[]
for item in manifest['models']:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(folder/(item['id']+'.fbx')))
    obs=[o for o in bpy.context.scene.objects if o.type=='MESH'];tris=0
    assert obs,item['id']
    mats=set()
    for ob in obs:
        m=ob.data;m.calc_loop_triangles();tris+=len(m.loop_triangles)
        assert m.uv_layers,item['id']+' UV missing'
        assert all(math.isfinite(c) for v in m.vertices for c in v.co)
        assert all(math.isfinite(c) for u in m.uv_layers.active.data for c in u.uv)
        assert m.polygons,item['id']+' empty mesh'
        mats.update(s.material.name.split('.')[0] for s in ob.material_slots)
    assert tris==item['triangles'],(item['id'],tris,item['triangles'])
    assert mats==set(item['materials']),(item['id'],mats)
    points=[o.matrix_world@Vector(p) for o in obs for p in o.bound_box]
    dims=[max(v[k] for v in points)-min(v[k] for v in points) for k in range(3)]
    assert max(dims)<10 and min(dims)>.001,(item['id'],dims)
    checks.append(dict(id=item['id'],status='PASS',triangles=tris,dimensions=dims))
Path(__file__).with_name(category+'-check.json').write_text(json.dumps(dict(category=category,checks=checks),indent=2),encoding='utf-8')
print('CATEGORY_CHECK_PASS '+category+' models='+str(len(checks)),flush=True)
