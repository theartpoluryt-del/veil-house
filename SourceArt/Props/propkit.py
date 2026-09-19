"""Shared mesh and export helpers. Blender coordinates: X right, -Y front, Z up."""
import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
parts=[];records=[];layouts=[]

def material(name):
    m=bpy.data.materials.get(name)
    if m:return m
    m=bpy.data.materials.new(name);m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Roughness'].default_value=.7
    source={'Walnut':'SmokedOak','Glow':'Linen','Screen':'GlassPatina','BookBlue':'TealPaint','BookRed':'Leather'}.get(name,name)
    if name in ('ArtAtlas','Decals','FamilyPortrait'):
        path=ROOT/('Assets/Resources/FamilyPortrait.png' if name=='FamilyPortrait' else 'Assets/Resources/ObjectArt/'+name+'.png')
        t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(path),check_existing=True)
        m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color']);return m
    for suffix in ('BaseColor','Normal','MetallicSmoothness'):
        path=ROOT/f'Assets/Resources/SurfaceTextures/{source}_{suffix}.png'
        if not path.exists():continue
        t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(path),check_existing=True)
        if suffix=='BaseColor':
            if name=='Walnut':
                mul=m.node_tree.nodes.new('ShaderNodeMixRGB');mul.blend_type='MULTIPLY';mul.inputs[0].default_value=1;mul.inputs[2].default_value=(.52,.46,.38,1)
                m.node_tree.links.new(t.outputs[0],mul.inputs[1]);m.node_tree.links.new(mul.outputs[0],p.inputs['Base Color'])
            else:m.node_tree.links.new(t.outputs[0],p.inputs['Base Color'])
        else:
            t.image.colorspace_settings.name='Non-Color'
            if suffix=='Normal':
                n=m.node_tree.nodes.new('ShaderNodeNormalMap');n.inputs[0].default_value=.8;m.node_tree.links.new(t.outputs[0],n.inputs['Color']);m.node_tree.links.new(n.outputs[0],p.inputs['Normal'])
            else:
                n=m.node_tree.nodes.new('ShaderNodeMath');n.operation='SUBTRACT';n.inputs[0].default_value=1;m.node_tree.links.new(t.outputs['Alpha'],n.inputs[1]);m.node_tree.links.new(n.outputs[0],p.inputs['Roughness'])
    p.inputs['Metallic'].default_value=.8 if name in ('Brass','Iron','Copper','Chrome','MirrorSilver') else 0
    if name=='Glow':p.inputs['Emission Color'].default_value=(1,.68,.32,1);p.inputs['Emission Strength'].default_value=.7
    return m

def uv_metric(ob):
    mesh=ob.data;uv=mesh.uv_layers.active or mesh.uv_layers.new(name='UVMap')
    for p in mesh.polygons:
        axis=max(range(3),key=lambda i:abs(p.normal[i]));a,b=[i for i in range(3) if i!=axis]
        for li in p.loop_indices:
            co=mesh.vertices[mesh.loops[li].vertex_index].co;uv.data[li].uv=(co[a]*2,co[b]*2)

def register(ob,mat,uv=True):
    ob.data.materials.clear();ob.data.materials.append(material(mat));parts.append(ob)
    if uv:uv_metric(ob)
    return ob

def mesh(name,verts,faces,mat,smooth=True,uvs=None):
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    ob=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(ob);register(ob,mat,uvs is None)
    if uvs:
        layer=me.uv_layers.new(name='UVMap')
        for p in me.polygons:
            for li in p.loop_indices:layer.data[li].uv=uvs[me.loops[li].vertex_index]
    for p in me.polygons:p.use_smooth=smooth
    return ob

def box(name,loc,size,mat='Walnut',bevel=.012):
    bpy.ops.object.select_all(action='DESELECT');bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    ob=bpy.context.object;ob.name=name;ob.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);register(ob,mat)
    if bevel:
        m=ob.modifiers.new('Worn manufactured edge','BEVEL');m.width=min(bevel,min(size)*.45);m.segments=3;bpy.ops.object.modifier_apply(modifier=m.name)
        for p in ob.data.polygons:p.use_smooth=True
        m=ob.modifiers.new('Weighted normals','WEIGHTED_NORMAL');m.keep_sharp=True;bpy.ops.object.modifier_apply(modifier=m.name)
    return ob

def lathe(name,profile,mat='Porcelain',loc=(0,0,0),scale=(1,1),n=48,flutes=0):
    verts=[];faces=[];uv=[]
    for j,(r,z) in enumerate(profile):
        for i in range(n+1):
            a=math.tau*i/n;rr=r*(1+flutes*math.cos(a*12))
            verts.append((loc[0]+rr*math.cos(a)*scale[0],loc[1]+rr*math.sin(a)*scale[1],loc[2]+z));uv.append((i/n,z*2))
    for j in range(len(profile)-1):
        for i in range(n):
            a=j*(n+1)+i;faces.append((a,a+1,a+n+2,a+n+1))
    ob=mesh(name,verts,faces,mat,uvs=uv)
    # Position-weld axis poles and texture seam for reliable normals. Metric UV is restored.
    bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free()
    # Planar UV on bowl floors avoids the collapsed radial seam and starburst normal maps.
    layer=ob.data.uv_layers.active
    for polygon in ob.data.polygons:
        if abs(polygon.normal.z)>.7:
            for li in polygon.loop_indices:
                co=ob.data.vertices[ob.data.loops[li].vertex_index].co
                layer.data[li].uv=((co.x-loc[0])*2,(co.y-loc[1])*2)
    return ob

def tube(name,pts,r=.01,mat='Brass',closed=False):
    cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.bevel_depth=r;cu.bevel_resolution=2;cu.use_fill_caps=True
    s=cu.splines.new('POLY');s.points.add(len(pts)-1);s.use_cyclic_u=closed
    for p,co in zip(s.points,pts):p.co=(*co,1)
    bpy.ops.object.select_all(action='DESELECT');ob=bpy.data.objects.new(name,cu);bpy.context.collection.objects.link(ob)
    ob.select_set(True);bpy.context.view_layer.objects.active=ob;bpy.ops.object.convert(target='MESH');return register(bpy.context.object,mat)

def ring(name,loc,rx,ry,r=.01,mat='Brass',plane='XY',n=48):
    pts=[]
    for i in range(n):
        a=math.tau*i/n;x=rx*math.cos(a);y=ry*math.sin(a)
        pts.append((loc[0]+x,loc[1]+y,loc[2]) if plane=='XY' else (loc[0]+x,loc[1],loc[2]+y) if plane=='XZ' else (loc[0],loc[1]+x,loc[2]+y))
    return tube(name,pts,r,mat,True)

def ellipsoid(name,loc,size,mat='Porcelain'):
    bpy.ops.object.select_all(action='DESELECT');bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,location=loc)
    ob=bpy.context.object;ob.name=name;ob.scale=tuple(v/2 for v in size);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for p in ob.data.polygons:p.use_smooth=True
    return register(ob,mat)

def decal(name,loc,w,h,rect=(0,0,1,1),mat='Decals'):
    x,y,z=loc;u,v,s,t=rect
    return mesh(name,[(x-w/2,y,z-h/2),(x+w/2,y,z-h/2),(x+w/2,y,z+h/2),(x-w/2,y,z+h/2)],[(0,1,2,3)],mat,False,[(u,v),(s,v),(s,t),(u,t)])

def frame(name,loc,w,h,mat='Brass',thick=.035):
    x,y,z=loc
    for d,width in ((0,thick),(.018,thick*.45)):
        for s in (-1,1):
            box(name+' stile',(x+s*(w/2+d),y-d*.5,z),(width,.036,h+2*d+width),mat,.009)
            box(name+' rail',(x,y-d*.5,z+s*(h/2+d)),(w+2*d,width,width),mat,.009)

def cloth(name,loc,w,d,mat='Towel',drop=0):
    verts=[];faces=[];nx=40;ny=24
    for j in range(ny+1):
        for i in range(nx+1):
            x=(i/nx-.5)*w;y=(j/ny-.5)*d
            z=.008*math.sin(i*.9+j*.21)+.013*math.sin(j*.8+i*.19)-drop*(abs(x)/(w/2))**8
            verts.append((loc[0]+x,loc[1]+y,loc[2]+z))
    for j in range(ny):
        for i in range(nx):
            k=j*(nx+1)+i;faces.append((k,k+1,k+nx+2,k+nx+1))
    ob=mesh(name,verts,faces,mat)
    bpy.context.view_layer.objects.active=ob;m=ob.modifiers.new('Fabric thickness','SOLIDIFY');m.thickness=.005;bpy.ops.object.modifier_apply(modifier=m.name)
    return ob

def export(category,name,builder):
    global parts
    parts.clear();builder();bpy.context.view_layer.update()
    groups={}
    for ob in parts:groups.setdefault(ob.data.materials[0].name,[]).append(ob)
    merged=[]
    for mat,objs in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for ob in objs:ob.select_set(True)
        bpy.context.view_layer.objects.active=objs[0]
        if len(objs)>1:bpy.ops.object.join()
        ob=bpy.context.object;ob.name=name+'_'+mat
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);merged.append(ob)
    bpy.ops.object.select_all(action='DESELECT')
    for ob in merged:ob.select_set(True)
    out=ROOT/'Assets/Resources/Props'/category;out.mkdir(parents=True,exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',object_types={'MESH'},bake_space_transform=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
    for ob in merged:ob.data.calc_loop_triangles()
    record={'id':name,'triangles':sum(len(o.data.loop_triangles) for o in merged),'materials':list(groups)}
    records.append(record);layouts.append(merged);print('PROP_SAVED '+name+' triangles='+str(record['triangles']),flush=True)

def finish(category):
    out=ROOT/'Assets/Resources/Props'/category
    (out/'manifest.json').write_text(json.dumps({'category':category,'models':records},indent=2),encoding='utf-8')
    for i,objects in enumerate(layouts):
        for ob in objects:ob.location+=Vector(((i%5)*5,(i//5)*5,0))
    bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).with_name(category+'.blend')))
    bpy.ops.file.make_paths_relative();bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
    print('CATEGORY_SAVED '+category+' models='+str(len(records)),flush=True)

def start():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
