"""Generate three authored cup variants for Veil House. Blender 5.2, metres."""
import bpy, bmesh, json, math
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/Resources/Props/Mugs'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)

def material(name):
    found=bpy.data.materials.get(name)
    if found:return found
    m=bpy.data.materials.new(name);m.use_nodes=True
    nodes=m.node_tree.nodes;links=m.node_tree.links;p=nodes.get('Principled BSDF')
    for suffix in ('BaseColor','Normal','MetallicSmoothness'):
        image=bpy.data.images.load(str(ROOT/f'Assets/Resources/SurfaceTextures/{name}_{suffix}.png'),check_existing=True)
        t=nodes.new('ShaderNodeTexImage');t.image=image
        if suffix=='BaseColor':links.new(t.outputs['Color'],p.inputs['Base Color'])
        else:
            image.colorspace_settings.name='Non-Color'
            if suffix=='Normal':
                n=nodes.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=.7
                links.new(t.outputs['Color'],n.inputs['Color']);links.new(n.outputs['Normal'],p.inputs['Normal'])
            else:
                inv=nodes.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1
                links.new(t.outputs['Alpha'],inv.inputs[1]);links.new(inv.outputs[0],p.inputs['Roughness'])
    p.inputs['Metallic'].default_value=.8 if name in ('Brass','Iron') else 0
    return m

M={n:material(n) for n in ('Porcelain','BlueGlaze','Brass','Enamel','Iron')}

def assign(ob,mat):
    ob.data.materials.clear();ob.data.materials.append(M[mat]);return ob

def lathe(name,profile,mat,segments=48):
    """Closed surface of revolution. Profile is [(radius,z), ...], including inner wall."""
    verts=[];faces=[];uv=[]
    for j,(r,z) in enumerate(profile):
        for i in range(segments):
            a=math.tau*i/segments
            # Tiny handmade variation avoids a mathematically perfect silhouette.
            row=0 if j==len(profile)-1 and profile[-1]==profile[0] else j
            wobble=1+.004*math.sin(a*3+row*.71)+.002*math.sin(a*7-row*.37)
            verts.append((math.cos(a)*r*wobble,math.sin(a)*r*wobble,z))
    rows=len(profile)
    for j in range(rows-1):
        for i in range(segments):
            n=(i+1)%segments
            faces.append((j*segments+i,j*segments+n,(j+1)*segments+n,(j+1)*segments+i))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    bm=bmesh.new();bm.from_mesh(mesh)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free();mesh.update()
    layer=mesh.uv_layers.new(name='UVMap')
    for poly in mesh.polygons:
        coords=[]
        for li in poly.loop_indices:
            v=mesh.vertices[mesh.loops[li].vertex_index].co
            coords.append(((math.atan2(v.y,v.x)/math.tau)%1,v.z/.15))
        seam=max(c[0] for c in coords)-min(c[0] for c in coords)>.5
        for li,(u,v) in zip(poly.loop_indices,coords):layer.data[li].uv=(u+1 if seam and u<.5 else u,v)
        poly.use_smooth=True
    ob=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(ob);assign(ob,mat)
    return ob

def torus(name,major,minor,z,mat,scale=(1,1,1)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=48,minor_segments=10,location=(0,0,z))
    ob=bpy.context.object;ob.name=name;ob.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    assign(ob,mat)
    for p in ob.data.polygons:p.use_smooth=True
    return ob

def handle(name,x,z,width,height,mat,tilt=0):
    bpy.ops.object.select_all(action='DESELECT')
    curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.resolution_u=3
    curve.bevel_depth=.009;curve.bevel_resolution=4;curve.resolution_u=12;curve.use_fill_caps=True
    spline=curve.splines.new('BEZIER');spline.bezier_points.add(5)
    pts=[(x-.006,0,z+height*.38),(x+width*.37,0,z+height*.50),(x+width,0,z+height*.22),(x+width,0,z-height*.25),(x+width*.36,0,z-height*.5),(x-.004,0,z-height*.34)]
    for bp,co in zip(spline.bezier_points,pts):
        bp.co=co;bp.handle_left_type='AUTO';bp.handle_right_type='AUTO'
    ob=bpy.data.objects.new(name,curve);bpy.context.collection.objects.link(ob);ob.rotation_euler.y=tilt
    bpy.context.view_layer.objects.active=ob;ob.select_set(True);bpy.ops.object.convert(target='MESH');assign(ob,mat)
    for p in ob.data.polygons:p.use_smooth=True
    uv=ob.data.uv_layers.new(name='UVMap') if not ob.data.uv_layers else ob.data.uv_layers.active
    for poly in ob.data.polygons:
        for li in poly.loop_indices:
            co=ob.data.vertices[ob.data.loops[li].vertex_index].co;uv.data[li].uv=(co.x/.15,co.z/.15)
    return ob

def decal_band(z,r,h,mat='BlueGlaze'):
    # Slightly raised, irregular hand-painted band rather than a flat color cylinder.
    profile=[(r,z-h/2),(r+.001,z),(r,z+h/2),(r-.001,z),(r,z-h/2)]
    return lathe('Hand painted glaze band',profile,mat,64)

def classic_mug():
    outer=[(0,.005),(.045,.005),(.050,.004),(.058,.008),(.061,.025),(.062,.078),(.060,.105),(.057,.112)]
    inner=[(.053,.114),(.050,.112),(.052,.105),(.053,.030),(.047,.015),(0,.015)]
    lathe('Thick stoneware body',outer+inner,'Porcelain')
    torus('Rolled drinking rim',.0545,.003,.111,'BlueGlaze')
    handle('Large ear handle',.055,.063,.065,.075,'BlueGlaze',-.035)
    decal_band(.035,.0625,.012);decal_band(.085,.0618,.006)
    # A small firing thumbprint makes the foot visibly irregular.
    torus('Uneven foot ring',.040,.004,.003,'Porcelain',(.98,1.03,1))

def floral_teacup():
    outer=[(0,.018),(.030,.018),(.043,.024),(.058,.040),(.071,.065),(.075,.083),(.073,.093)]
    inner=[(.069,.095),(.066,.093),(.068,.086),(.064,.067),(.052,.043),(.031,.027),(0,.027)]
    lathe('Fine flared teacup',outer+inner,'Porcelain',64)
    torus('Thin gilt lip',.0695,.0017,.094,'Brass')
    handle('Delicate curled handle',.066,.062,.049,.055,'Brass',.025)
    # Saucer with a real depression and raised edge.
    saucer=[(0,.006),(.055,.003),(.085,.006),(.103,.011),(.108,.016),(.103,.020),(.070,.015),(.030,.011),(0,.011)]
    lathe('Scalloped saucer',saucer,'Porcelain',64)
    torus('Saucer gilt edge',.105,.0015,.017,'Brass')
    torus('Teacup foot',.029,.004,.015,'Porcelain')
    for a in (.25,2.3,4.35):
        for k in range(6):
            angle=k*math.tau/6;da=math.cos(angle)*.08;z=.069+math.sin(angle)*.006
            r=.071+(z-.065)*.22
            bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,location=(math.cos(a+da)*r,math.sin(a+da)*r,z),scale=(.0009,.003,.005))
            petal=bpy.context.object;petal.name='Cobalt petal relief';petal.rotation_euler.z=a+da;assign(petal,'BlueGlaze')

def enamel_mug():
    outer=[(0,.004),(.046,.004),(.055,.009),(.057,.025),(.058,.088),(.056,.102)]
    inner=[(.053,.105),(.050,.102),(.052,.094),(.051,.027),(.045,.014),(0,.014)]
    lathe('Dented enamel camp mug',outer+inner,'Enamel')
    torus('Dark rolled enamel rim',.053,.0032,.102,'Iron',(.98,1.02,1))
    handle('Angular enamel handle',.052,.057,.061,.064,'BlueGlaze',.045)
    # Two small chips expose the darker substrate.
    for a,z in ((.55,.081),(3.8,.029)):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,location=(math.cos(a)*.0575,math.sin(a)*.0575,z),scale=(.005,.002,.004))
        chip=bpy.context.object;chip.name='Chipped enamel spot';chip.rotation_euler.z=a-math.pi/2;assign(chip,'Iron')

def export_model(name,builder):
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    builder();objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objects:ob.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
    for ob in objects:
        ob.data.calc_loop_triangles()
    tris=sum(len(ob.data.loop_triangles) for ob in objects)
    mats=sorted({slot.material.name for ob in objects for slot in ob.material_slots if slot.material})
    bpy.context.view_layer.update()
    world=[ob.matrix_world@Vector(c) for ob in objects for c in ob.bound_box]
    lo=Vector((min(v.x for v in world),min(v.y for v in world),min(v.z for v in world)))
    hi=Vector((max(v.x for v in world),max(v.y for v in world),max(v.z for v in world)))
    print('MUG_EXPORTED',name,'triangles='+str(tris),'size='+','.join(f'{x:.3f}' for x in hi-lo),flush=True)
    return {'id':name,'triangles':tris,'materials':mats,'size':[round(x,4) for x in hi-lo]}

records=[]
for name,builder in [('MugClassic',classic_mug),('TeacupFloral',floral_teacup),('MugEnamel',enamel_mug)]:records.append(export_model(name,builder))
(OUT/'manifest.json').write_text(json.dumps({'category':'mugs','models':records},indent=2),encoding='utf-8')

# Save an editable library with all three variants arranged for inspection.
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for offset,builder in zip((-0.24,0,.24),(classic_mug,floral_teacup,enamel_mug)):
    before=set(bpy.context.scene.objects);builder()
    for ob in set(bpy.context.scene.objects)-before:ob.location.x+=offset
bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).with_name('Mugs.blend')))
bpy.ops.file.make_paths_relative();bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print('MUG_CATEGORY_COMPLETE models=3',flush=True)
