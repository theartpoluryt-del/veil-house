"""Authored furniture meshes: shaped upholstery, joinery, turned legs and book spines.
Blender 5.2, deterministic generation, FBX exports for Unity. Metres; front is -Y.
"""
import bpy, math, random, json, sys
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2]
OUT=R/'Assets/Resources/Furniture';OUT.mkdir(parents=True,exist_ok=True)
rng=random.Random(1927)
only=set(sys.argv[sys.argv.index('--only')+1].split(',')) if '--only' in sys.argv else None
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
materials={}
for name,source,tint in [('SmokedOak','SmokedOak',1),('Walnut','SmokedOak',.54),('BurgundyFabric','BurgundyFabric',1),('FabricSeam','BurgundyFabric',.55),('Linen','Linen',1),('Leather','Leather',1),('Brass','Brass',1),('BookRed','BurgundyFabric',.6),('BookBlue','TealPaint',.8)]:
    m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;l=m.node_tree.links;p=n.get('Principled BSDF')
    for i,suffix in enumerate(['BaseColor','Normal','MetallicSmoothness']):
        t=n.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(R/f'Assets/Resources/SurfaceTextures/{source}_{suffix}.png'),check_existing=True);t.location=(-600,250-i*250)
        if i==0:
            mul=n.new('ShaderNodeMixRGB');mul.blend_type='MULTIPLY';mul.inputs[0].default_value=1;mul.inputs[2].default_value=(tint,tint,tint,1)
            l.new(t.outputs['Color'],mul.inputs[1]);l.new(mul.outputs[0],p.inputs['Base Color'])
        elif i==1:
            t.image.colorspace_settings.name='Non-Color';norm=n.new('ShaderNodeNormalMap');norm.inputs['Strength'].default_value=1.8;l.new(t.outputs[0],norm.inputs['Color']);l.new(norm.outputs[0],p.inputs['Normal'])
        else:
            t.image.colorspace_settings.name='Non-Color';inv=n.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1;l.new(t.outputs['Alpha'],inv.inputs[1]);l.new(inv.outputs[0],p.inputs['Roughness'])
    p.inputs['Metallic'].default_value=.85 if name=='Brass' else 0
    if 'Fabric' in name or name=='Linen':p.inputs['Sheen Weight'].default_value=.13
    materials[name]=m
parts=[]
def register(ob,mat):
    ob.data.materials.clear();ob.data.materials.append(materials[mat]);parts.append(ob);return ob
def metric_uv(ob,mat):
    if not ob.data.uv_layers:ob.data.uv_layers.new()
    cloth=mat in ['BurgundyFabric','FabricSeam','Linen','BookRed'];wood=mat in ['SmokedOak','Walnut']
    sx,sy=(.25,.25) if cloth else ((1,2) if wood else (.5,.5))
    dims=ob.dimensions;major=max(range(3),key=lambda a:dims[a])
    for poly in ob.data.polygons:
        axis=max(range(3),key=lambda a:abs(poly.normal[a]));axes=[a for a in range(3) if a!=axis]
        if wood and major in axes:axes=[a for a in axes if a!=major]+[major]
        for li in poly.loop_indices:
            co=ob.data.vertices[ob.data.loops[li].vertex_index].co
            ob.data.uv_layers.active.data[li].uv=(co[axes[0]]/sx,co[axes[1]]/sy)
def box(name,loc,size,mat='SmokedOak',bevel=.012,segments=3):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);ob=bpy.context.object;ob.name=name;ob.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    metric_uv(ob,mat);register(ob,mat)
    if bevel:
        mod=ob.modifiers.new('Crafted edge radius','BEVEL');mod.width=bevel;mod.segments=segments
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for f in ob.data.polygons:f.use_smooth=True
    mod=ob.modifiers.new('Weighted face normals','WEIGHTED_NORMAL');mod.keep_sharp=True;bpy.ops.object.modifier_apply(modifier=mod.name)
    return ob
def line(name,pts,mat='FabricSeam',radius=.0016):
    curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.resolution_u=1;curve.bevel_depth=radius;curve.bevel_resolution=2
    spline=curve.splines.new('POLY');spline.points.add(len(pts)-1)
    for p,c in zip(spline.points,pts):p.co=(*c,1)
    ob=bpy.data.objects.new(name,curve);bpy.context.collection.objects.link(ob)
    bpy.context.view_layer.objects.active=ob;ob.select_set(True);bpy.ops.object.convert(target='MESH');ob=bpy.context.object
    metric_uv(ob,mat);return register(ob,mat)
def welt(loc,size,plane='XY',mat='FabricSeam',cushion_ob=None):
    w,h=size;r=min(.055,w*.16,h*.16);pts=[]
    for cx,cy,a in [(w/2-r,h/2-r,0),(-w/2+r,h/2-r,90),(-w/2+r,-h/2+r,180),(w/2-r,-h/2+r,270)]:
        for i in range(13):
            t=math.radians(a+i*90/12);x=cx+r*math.cos(t);y=cy+r*math.sin(t)
            pts.append((loc[0]+x,loc[1]+y,loc[2]) if plane=='XY' else (loc[0]+x,loc[1],loc[2]+y))
    if cushion_ob is not None:
        bpy.context.view_layer.update()
        half=Vector(cushion_ob['cushion_size'])*.5;rad=min(half)*.54
        projected=[]
        for pt in pts:
            local=Vector((pt[0]-loc[0],pt[1]-loc[1],half.z)) if plane=='XY' else Vector((pt[0]-loc[0],-half.y,pt[2]-loc[2]))
            q=upholstery_point(local,half,rad,cushion_ob['cushion_back'],cushion_ob['cushion_seed'])
            q.z+=.0006 if plane=='XY' else 0;q.y-=.0006 if plane!='XY' else 0
            projected.append(tuple(cushion_ob.matrix_world@q))
        pts=projected
    pts.append(pts[0]);return line('Hand sewn piping',pts,mat)
def upholstery_point(p,half,rad,back,seed):
    inner=half-Vector((rad,rad,rad))
    core=Vector([max(-inner[k],min(inner[k],p[k])) for k in range(3)]);delta=p-core
    q=core+delta.normalized()*rad
    nx=q.x/half.x;nz=q.z/half.z;ny=q.y/half.y
    if not back:
        edge=max(abs(nx),abs(ny));wr=.006*math.sin(nx*43+ny*11+seed)*math.exp(-((edge-.78)/.15)**2)
        q.z+=(.014*(1-nx*nx)*(1-ny*ny)+wr-.018*math.exp(-((nx+.14)**2+(ny-.1)**2)/.23))*max(0,nz)
    else:
        edge=max(abs(nx),abs(nz));wr=.006*math.sin(nx*34+nz*17+seed)*math.exp(-((edge-.78)/.18)**2)
        q.y+=(-.018*(1-nx*nx)*(1-nz*nz)+wr)*max(0,-ny)
        for bx in [-.38,.38]:
            d=(nx-bx)**2+(nz-.15)**2
            q.y+=.016*math.exp(-d/.025)*max(0,-ny)
    return q
def cushion(name,loc,size,mat='BurgundyFabric',back=False,seed=0):
    # A six-face grid projected onto a filleted box, with soft compression and edge wrinkles.
    dims=Vector(size);half=dims*.5;rad=min(size)*.27;inner=half-Vector((rad,rad,rad))
    verts=[];faces=[];res=18
    for axis in range(3):
        a,b=[j for j in range(3) if j!=axis]
        for sign in [-1,1]:
            start=len(verts)
            for j in range(res+1):
                for i in range(res+1):
                    p=Vector((0,0,0));p[axis]=sign*half[axis];p[a]=(i/res*2-1)*half[a];p[b]=(j/res*2-1)*half[b]
                    q=upholstery_point(p,half,rad,back,seed)
                    verts.append(tuple(q))
            for j in range(res):
                for i in range(res):
                    x=start+j*(res+1)+i;quad=(x,x+1,x+res+2,x+res+1)
                    # Keep outward normals for both axis parities.
                    faces.append(quad if sign*(1 if axis!=1 else -1)>0 else quad[::-1])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    ob=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(ob);ob.location=loc
    bpy.context.view_layer.update();metric_uv(ob,mat);register(ob,mat)
    for p in mesh.polygons:p.use_smooth=True
    ob['cushion_size']=size;ob['cushion_back']=back;ob['cushion_seed']=seed
    return ob
def lathe(name,x,y,height,base=.025,mat='Walnut',radius=.045):
    profile=[(0,.7),(.025,.84),(.06,1.0),(.11,.79),(.22,.61),(.45,.54),(.62,.80),(.70,1.08),(.77,.87),(.84,.64),(.91,.76),(1,.86)]
    verts=[];faces=[];steps=20
    for z,r in profile:
        for i in range(steps):
            a=i*math.tau/steps;verts.append((x+math.cos(a)*r*radius,y+math.sin(a)*r*radius,base+z*height))
    for j in range(len(profile)-1):
        for i in range(steps):faces.append((j*steps+i,j*steps+(i+1)%steps,(j+1)*steps+(i+1)%steps,(j+1)*steps+i))
    faces.extend([tuple(range(steps-1,-1,-1)),tuple((len(profile)-1)*steps+i for i in range(steps))])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();ob=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(ob)
    bpy.context.view_layer.update();metric_uv(ob,mat);register(ob,mat)
    for f in mesh.polygons:f.use_smooth=True
    return ob
def sofa(single=False):
    w=1.15 if single else 2.9;count=1 if single else 3
    for x in [-w/2+.10,w/2-.10]:
        for y in [-.38,.37]:lathe('Carved walnut foot',x,y,.18,radius=.061)
    box('Exposed timber seat rail',(0,-.01,.245),(w+.035,.96,.14),'Walnut',.018)
    box('Lower upholstered body',(0,0,.36),(w-.055,.99,.21),'BurgundyFabric',.06)
    box('Timber back frame',(0,.425,.76),(w,.095,.93),'Walnut',.017)
    for x in [-w/2,w/2]:
        box('Shaped arm timber rail',(x,0,.64),(.105,1.0,.17),'Walnut',.025)
        arm=cushion('Tailored arm pad',(x,-.015,.80),(.235,1.015,.28),seed=2)
        welt((x,-.015,.82),(.225,.985),cushion_ob=arm)
        for y in [-.38,.34]:box('Arm support',(x,y,.46),(.085,.09,.40),'Walnut',.01)
    innerw=w-.25;cw=innerw/count
    for i in range(count):
        x=(i-(count-1)/2)*cw
        seat=cushion('Compressed seat cushion',(x,-.095,.565),(cw-.025,.80,.205),seed=i*2)
        welt((x,-.095,.605),(cw-.035,.775),cushion_ob=seat)
        b=cushion('Upholstered back cushion',(x,.333,.952),(cw-.025,.22,.57),back=True,seed=i+5)
        b.rotation_euler.x=math.radians(-5+i)
        welt((x,.206,.96),(cw-.04,.54),'XZ',cushion_ob=b)
        for bx in [-.19,.19]:
            if cw>.6:
                box('Covered tuft button',(x+bx,.207,1.0),(.027,.014,.026),'FabricSeam',.009)
    if not single:
        p=cushion('Loose linen scatter pillow',(-.93,-.005,.85),(.43,.24,.42),'Linen',True,12);p.rotation_euler=(.15,-.14,-.12)
def table(w,d,h):
    # Individual planks and a profiled edge catch light without balloon-like rounding.
    box('Table top substrate',(0,0,h-.017),(w-.04,d-.04,.055),'Walnut',.012)
    count=max(3,round(d/.23));bw=d/count
    for i in range(count):
        board=box('Fitted oak top board',(0,-d/2+(i+.5)*bw,h+.012),(w,bw-.0015,.070),'SmokedOak',.008)
    for x in [-w/2+.12,w/2-.12]:
        for y in [-d/2+.12,d/2-.12]:lathe('Turned and tapered leg',x,y,h-.095,radius=.05)
    for y in [-d/2+.105,d/2-.105]:box('Shaped long apron',(0,y,h-.12),(w-.19,.055,.14),'Walnut',.008)
    for x in [-w/2+.105,w/2-.105]:box('End apron',(x,0,h-.12),(.055,d-.19,.14),'Walnut',.008)
    if h>.6:
        front=box('Inset drawer front',(0,-d/2+.067,h-.125),(min(.56,w-.3),.042,.105),'SmokedOak',.009)
        for x in [-min(.17,w*.19),min(.17,w*.19)]:
            line('Aged brass drawer pull',[(x-.032,-d/2+.041,h-.124),(x-.022,-d/2+.005,h-.125),(x+.022,-d/2+.005,h-.125),(x+.032,-d/2+.041,h-.124)],'Brass',.004)
def chair():
    for x in [-.225,.225]:
        for y in [-.225,.225]:lathe('Tapered chair leg',x,y,.37,radius=.035)
    box('Seat frame',(0,0,.407),(.59,.58,.10),'Walnut',.015)
    seat=cushion('Fitted leather seat',(0,-.005,.49),(.55,.53,.13),'Leather',seed=3)
    welt((0,-.005,.515),(.53,.51),mat='Leather',cushion_ob=seat)
    for x in [-.247,.247]:
        ob=box('Curved back stile',(x,.243,.85),(.06,.08,.85),'Walnut',.016);ob.rotation_euler.x=math.radians(-5)
    back=cushion('Inset back upholstery',(0,.239,.94),(.45,.105,.50),'Leather',True,4)
    welt((0,.18,.94),(.43,.47),'XZ','Leather',cushion_ob=back)
    box('Carved crest rail',(0,.284,1.25),(.62,.11,.12),'SmokedOak',.035)
    for y in [-.2,.2]:box('Leg stretcher',(0,y,.22),(.46,.035,.035),'Walnut',.005)
def bookcase(w):
    box('Panelled cabinet back',(0,.185,1.33),(w,.065,2.50),'Walnut',.008)
    for x in [-w/2,w/2]:
        box('Carcass side',(x,0,1.35),(.10,.47,2.61),'Walnut',.012)
        box('Front pilaster',(x,-.235,1.39),(.13,.095,2.52),'SmokedOak',.018)
        for off in [-.035,0,.035]:box('Pilaster fluting',(x+off,-.288,1.38),(.008,.008,2.22),'Walnut',.003)
    for z,width,depth,height in [(.07,w+.12,.55,.14),(.175,w+.035,.49,.07),(2.59,w+.10,.52,.08),(2.66,w+.19,.57,.075),(2.715,w+.24,.61,.04)]:
        box('Profiled moulding',(0,-.01,z),(width,depth,height),'SmokedOak',.013)
    for k in range(5):
        z=.24+k*.47;box('Bevelled shelf',(0,-.015,z),(w-.09,.48,.045),'SmokedOak',.007)
        x=-w/2+.13;stacked=False
        while x<w/2-.17:
            if k==1 and x>-.16 and not stacked:
                for j in range(3):
                    mat=['BookRed','BookBlue','Leather'][j]
                    box('Horizontal book pages',(x+.15,-.045,z+.045+j*.055),(.30,.25,.035),'Linen',.003)
                    for dz in (-.023,.023):box('Horizontal cloth cover',(x+.15,-.045,z+.045+j*.055+dz),(.32,.265,.008),mat,.003)
                    box('Horizontal book spine',(x+.15,-.181,z+.045+j*.055),(.32,.015,.047),mat,.004)
                x+=.37;stacked=True;continue
            bw=rng.uniform(.04,.095);height=rng.uniform(.265,.375);depth=rng.uniform(.22,.33);mat=rng.choice(['BookRed','BookBlue','Leather','Walnut'])
            if rng.random()<.12:x+=.045
            first=len(parts)
            box('Paper page block',(x,-.045,z+.025+height/2),(bw-.008,depth-.015,height-.018),'Linen',.003,2)
            for xx in [x-bw/2,x+bw/2]:box('Clothbound cover',(xx,-.05,z+.025+height/2),(.005,depth,height),mat,.002,2)
            box('Rounded book spine',(x,-.05-depth/2,z+.025+height/2),(bw,.018,height),mat,.009,3)
            for zz in [z+.08,z+height-.04]:box('Spine foil band',(x,-.061-depth/2,zz),(bw*.82,.002,.004),'Brass',.001,1)
            tilt=rng.uniform(-.035,.035)
            pull=.065 if rng.random()<.16 else rng.uniform(-.006,.01)
            for ob in parts[first:]:ob.rotation_euler.y=tilt;ob.location.y-=pull
            x+=bw+.012
records=[];layouts=[]
def export(name,fn):
    global parts
    if only is not None and name not in only:
        bpy.ops.import_scene.fbx(filepath=str(OUT/(name+'.fbx')))
        objs=[ob for ob in bpy.context.selected_objects if ob.type=='MESH']
        for ob in objs:
            for slot in ob.material_slots:slot.material=materials[slot.material.name.split('.')[0]]
        records.append(dict(id=name,triangles=sum(sum(len(p.vertices)-2 for p in ob.data.polygons) for ob in objs),meshes=len(objs)))
        layouts.append((name,objs));return
    parts=[];fn()
    bpy.ops.object.select_all(action='DESELECT')
    for p in parts:p.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]
    # Merge per material to keep FBX hierarchy compact; preserve all authored vertices/UVs.
    grouped={}
    for p in parts:grouped.setdefault(p.data.materials[0].name,[]).append(p)
    merged=[]
    for mat,objs in grouped.items():
        bpy.ops.object.select_all(action='DESELECT')
        for ob in objs:ob.select_set(True)
        bpy.context.view_layer.objects.active=objs[0];bpy.ops.object.join();ob=bpy.context.object;ob.name=name+'_'+mat
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);merged.append(ob)
    bpy.ops.object.select_all(action='DESELECT')
    for ob in merged:ob.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
    tris=sum(sum(len(p.vertices)-2 for p in ob.data.polygons) for ob in merged)
    records.append(dict(id=name,triangles=tris,meshes=len(merged)));layouts.append((name,merged))
    print('FURNITURE_EXPORTED '+name+' triangles='+str(tris),flush=True)
export('Sofa',lambda:sofa(False));export('Armchair',lambda:sofa(True));export('DiningChair',chair)
for dims in [(2.5,1.24,.49),(.76,.76,.71),(1.65,.56,.82),(3.4,1.65,.81),(3.2,1.35,.82),(.85,.85,.78),(.7,.7,.65),(1.5,.58,.84),(.6,.6,.47),(.72,.68,.67),(2.3,.8,.83),(.72,.72,.62),(3.2,.9,.91),(1.2,.54,.79)]:
    name='Table_'+'_'.join(str(round(a*1000)) for a in dims);export(name,lambda dims=dims:table(*dims))
for w in [2.6,3.2,3.7]:export('Bookcase_'+str(round(w*1000)),lambda w=w:bookcase(w))
(OUT/'manifest.json').write_text(json.dumps(dict(models=records),indent=2),encoding='utf-8')
# Keep a clearly arranged editable library; FBX pivots remain at floor level at origin.
for i,(name,objects) in enumerate(layouts):
    offset=Vector(((i%5)*4.5,(i//5)*4,0))
    for ob in objects:ob.location+=offset
bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).with_name('Furniture.blend')))
bpy.ops.file.make_paths_relative();bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print('FURNITURE_COMPLETE models='+str(len(records)),flush=True)
