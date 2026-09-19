import bpy,json,math
from pathlib import Path
root=Path(__file__).resolve().parents[2];folder=root/'Assets/Resources/Furniture'
records=[]
for item in json.loads((folder/'manifest.json').read_text())['models']:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(folder/(item['id']+'.fbx')))
    obs=[o for o in bpy.context.scene.objects if o.type=='MESH'];tris=0
    assert obs
    for o in obs:
        m=o.data;m.calc_loop_triangles();tris+=len(m.loop_triangles)
        assert m.uv_layers and all(math.isfinite(c) for v in m.vertices for c in v.co)
        assert all(math.isfinite(c) for uv in m.uv_layers.active.data for c in uv.uv)
    assert tris==item['triangles'],(item['id'],tris,item['triangles'])
    records.append(dict(id=item['id'],triangles=tris,status='PASS'))
Path(__file__).with_name('Furniture-check.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
print('FURNITURE_CHECK_PASS',len(records))
