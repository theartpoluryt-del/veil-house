"""Original seamless PBR surfaces authored with Blender Python/NumPy; no external assets.
Run: blender --background --factory-startup --python generate_materials.py
Pixels are sampled at texel centres; noise and every pattern are periodic in UV.
"""
from pathlib import Path
import json, math
import bpy
import numpy as np
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]
DEST=ROOT/'Assets/Resources/SurfaceTextures'
DEST.mkdir(parents=True,exist_ok=True)
N=2048
u,v=np.meshgrid((np.arange(N,dtype=np.float32)+.5)/N,(np.arange(N,dtype=np.float32)+.5)/N)
rng=np.random.default_rng(1927)
def noise(nx,ny):
    grid=rng.random((ny,nx),dtype=np.float32)
    x=u*nx;y=v*ny;ix=x.astype(np.int32);iy=y.astype(np.int32)
    fx=x-ix;fy=y-iy;fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy)
    return ((grid[iy%ny,ix%nx]*(1-fx)+grid[iy%ny,(ix+1)%nx]*fx)*(1-fy)+
            (grid[(iy+1)%ny,ix%nx]*(1-fx)+grid[(iy+1)%ny,(ix+1)%nx]*fx)*fy).astype(np.float32)
def mix(a,b,t):return np.array(a,dtype=np.float32)[None,None,:]*(1-t[:,:,None])+np.array(b,dtype=np.float32)[None,None,:]*t[:,:,None]
def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)
def save(name,channel,data,color=False):
    if data.ndim==2:data=np.repeat(data[:,:,None],3,axis=2)
    rgba=np.ones((N,N,4),np.float32);rgba[:,:,:data.shape[2]]=np.clip(data,0,1)
    # Byte-backed Blender images accept channel values in their declared encoding.
    # Palette values already describe sRGB PNG bytes; do not gamma-decode them twice.
    path=DEST/(name+'_'+channel+'.png')
    img=bpy.data.images.new(name+'_'+channel,width=N,height=N,alpha=data.shape[2]==4,float_buffer=False)
    img.colorspace_settings.name='sRGB' if color else 'Non-Color'
    img.pixels.foreach_set(rgba.ravel());img.filepath_raw=str(path);img.file_format='PNG';img.save()
    bpy.data.images.remove(img)
    return path
manifest=[];materials=[]
def surface(name,title,col,h,rough,meters,metal=0):
    dx=(np.roll(h,-1,axis=1)-np.roll(h,1,axis=1))*N/(2*meters[0])
    dy=(np.roll(h,-1,axis=0)-np.roll(h,1,axis=0))*N/(2*meters[1])
    normal=np.stack((-dx,-dy,np.ones_like(dx)),axis=2)
    normal/=np.linalg.norm(normal,axis=2)[:,:,None]
    packed=np.zeros((N,N,4),np.float32);packed[:,:,0]=metal;packed[:,:,3]=1-np.clip(rough,0,1)
    paths=[save(name,'BaseColor',col,True),save(name,'Normal',normal*.5+.5),save(name,'MetallicSmoothness',packed)]
    mat=bpy.data.materials.new(title);mat.use_nodes=True
    ns=mat.node_tree.nodes;ls=mat.node_tree.links;p=ns.get('Principled BSDF')
    for i,path in enumerate(paths):
        t=ns.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(path));t.location=(-650,300-i*280)
        if i==0:ls.new(t.outputs['Color'],p.inputs['Base Color'])
        elif i==1:
            t.image.colorspace_settings.name='Non-Color';n=ns.new('ShaderNodeNormalMap');n.location=(-320,0)
            ls.new(t.outputs['Color'],n.inputs['Color']);ls.new(n.outputs['Normal'],p.inputs['Normal'])
        else:
            t.image.colorspace_settings.name='Non-Color';inv=ns.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1
            ls.new(t.outputs['Alpha'],inv.inputs[1]);ls.new(inv.outputs[0],p.inputs['Roughness']);p.inputs['Metallic'].default_value=metal
    manifest.append(dict(id=name,title=title,width=meters[0],height=meters[1],resolution=N))
    materials.append(mat);print('MATERIAL_SAVED '+name,flush=True)

# Oak: slowly wandering fibres, varied early/late growth, pores. Periodic at 1 x 2 m.
warp=.008*np.sin(2*np.pi*v)+.018*(noise(5,3)-.5)
grain=.5+.5*np.sin(2*np.pi*(u*115+warp*115))
long=noise(95,7);broad=noise(9,3);pores=np.maximum(0,.43-noise(480,40))
wood=np.clip(.52+.42*(broad-.5)+.27*(long-.5)+.12*(grain-.5)-pores*1.2,0,1)
oak=mix((.18,.091,.039),(.59,.38,.19),wood)
height=.00012*grain+.00014*long-.0011*pores
surface('SmokedOak','01 • Smoked oak',oak,height,.58+.30*noise(22,8),(1,2))
# Six narrow boards, staggered end joints, inset grooves and individual tint.
bu=u*6;bv=v*2+((np.floor(bu).astype(int)%2)*.5)
fu=bu%1;fv=bv%1
edge=np.minimum(np.minimum(fu,1-fu)*.2,np.minimum(fv,1-fv)*1.2)
joint=1-smooth(.0006,.0024,edge)
boardid=np.floor(bu)+6*(np.floor(bv)%2)
tint=.91+.12*np.sin(boardid*12.9898+3)
col=oak*tint[:,:,None]*(1-joint[:,:,None]*.73)
surface('Parquet','02 • Staggered oak boards',col,height-joint*.0014,.52+.25*noise(33,6)+joint*.2,(1.2,2.4))
# Mineral plaster: broad trowel marks, fine granular bump and sparse pores.
trowel=noise(12,12);sand=noise(700,700);pit=np.maximum(0,.30-sand)
surface('Plaster','03 • Warm lime plaster',mix((.61,.58,.50),(.77,.74,.66),.4+.35*trowel+.13*noise(80,80)),
        (trowel-.5)*.0009+(sand-.5)*.00035-pit*.0018,.87+.09*sand,(1,1))
# Paint retains substrate texture; distressed pinholes reveal a lighter undercoat.
chips=smooth(.79,.91,noise(155,155))*(1-smooth(.35,.65,noise(5,5)))
paint=mix((.13,.235,.235),(.235,.355,.335),.25+.6*noise(14,14))
paint=paint*(1-chips[:,:,None]*.65)+np.array([.57,.55,.44])*chips[:,:,None]*.65
surface('TealPaint','04 • Aged teal paint',paint,(sand-.5)*.00008-chips*.0002,.58+.12*trowel,(1,1))
# Repeating botanical print: curved stems, paired pointed leaves, small rosettes.
x=(u-.5)*2;y=v
stemx=.10*np.sin(2*np.pi*y)
ink=np.exp(-((x-stemx)/.012)**2)*.8
for cy in [.18,.40,.64,.85]:
    for side in [-1,1]:
        cx=.10*math.sin(2*math.pi*cy)+side*.16
        xx=(x-cx)*side;yy=y-cy
        along=xx*.75+yy*.66;across=-xx*.66+yy*.75
        leaf=np.maximum(0,1-(along/.22)**2-(across/.048)**2)
        vein=np.exp(-(across/.004)**2)
        ink=np.maximum(ink,smooth(0,.18,leaf)*(.65-.24*vein))
for cy in [.06,.54]:
    cx=.1*math.sin(2*math.pi*cy);xx=x-cx;yy=(y-cy)*1.6
    theta=np.arctan2(yy,xx);r=np.sqrt(xx*xx+yy*yy)
    flower=1-smooth(.064,.075,r/(.8+.2*np.cos(theta*6)))
    ink=np.maximum(ink,flower*.62)
papergrain=noise(600,600);fade=.93+.07*noise(9,9)
wall=mix((.62,.65,.51),(.24,.34,.245),ink)*fade[:,:,None]
surface('Wallpaper','05 • Olive botanical wallpaper',wall,(papergrain-.5)*.00007+ink*.000025,.86+.05*papergrain,(.6,1.2))
# Glazed Victorian checker tile, contrasting inlaid borders and grout.
tx=(u*2)%1;ty=(v*2)%1;d=np.minimum(np.minimum(tx,1-tx),np.minimum(ty,1-ty))
grout=1-smooth(.006,.014,d);dark=((np.floor(u*2)+np.floor(v*2))%2)
tilecol=mix((.73,.70,.60),(.20,.32,.31),dark)
border=(smooth(.055,.061,d)-smooth(.068,.074,d))*.6
tilecol=tilecol*(1-border[:,:,None])+.42*border[:,:,None]
tilecol*=((.96+.04*noise(10,10))[:,:,None])
tilecol=tilecol*(1-grout[:,:,None])+np.array([.32,.30,.26])*grout[:,:,None]
surface('Tile','06 • Victorian glazed tile',tilecol,-grout*.0011+.0001*noise(80,80),.40+.17*noise(12,12)+grout*.35,(.8,.8))
# Woven upholstery with alternating warp/weft highlights, fibre variation.
weft=.5+.5*np.cos(2*np.pi*u*180);warp=.5+.5*np.cos(2*np.pi*v*180)
check=(np.floor(u*180)+np.floor(v*180))%2
weave=weft*(1-check)+warp*check
fibres=noise(700,700)
for name,title,a,b in [('BurgundyFabric','07 • Burgundy woven upholstery',(.20,.032,.05),(.37,.10,.125)),('Linen','08 • Cream woven linen',(.54,.48,.37),(.79,.73,.61))]:
    surface(name,title,mix(a,b,.35+.35*weave+.22*noise(24,24)),weave*.000075+fibres*.000012,.88+.09*fibres,(.25,.25))
# Fine pebbled leather; close cells rather than giant crocodile scales.
cell=noise(170,170);crease=(1-smooth(.22,.43,cell))*.7
surface('Leather','09 • Bottle green leather',mix((.055,.12,.10),(.15,.24,.19),.35+.30*noise(18,18)-.2*crease),
        cell*.00015-crease*.00012,.62+.20*noise(35,35),(.5,.5))
scratch=noise(8,800)
surface('Brass','10 • Brushed antique brass',mix((.37,.245,.09),(.65,.47,.22),.35+.5*noise(12,12)),
        scratch*.000014,.29+.18*noise(30,30),(.25,.25),.85)
# A complete carpet design (one rug per UV square), not a repeating floor tile.
edge=np.minimum(np.minimum(u,1-u),np.minimum(v,1-v))
rug=np.empty((N,N,3),np.float32);rug[:]=(.25,.075,.08)
def stain(mask,col):
    global rug
    rug=rug*(1-mask[:,:,None])+np.array(col,dtype=np.float32)*mask[:,:,None]
stain((edge<.09).astype(np.float32),(.59,.42,.23))
stain(((edge>.018)&(edge<.071)).astype(np.float32),(.09,.22,.205))
stain(((edge>.094)&(edge<.112)).astype(np.float32),(.085,.18,.17))
# Border rosettes distributed along all four edges.
for horizontal in [True,False]:
    axis=u if horizontal else v;perp=v if horizontal else u
    xx=((axis*24)%1-.5)*2;yy=(np.minimum(perp,1-perp)-.045)*55
    rose=(1-smooth(.38,.50,np.sqrt(xx*xx+yy*yy)))*(.75+.25*np.cos(np.arctan2(yy,xx)*6))
    stain(rose,(.68,.49,.27))
dx=(u-.5)*1.6;dy=(v-.5)*1.1
diamond=np.abs(dx)+np.abs(dy)
stain((1-smooth(.254,.261,diamond)),(.64,.45,.23))
stain((1-smooth(.236,.243,diamond)),(.08,.20,.19))
stain((1-smooth(.205,.215,diamond)),(.48,.17,.13))
petal=.5+.5*np.cos(np.arctan2(dy,dx)*8)
rosette=1-smooth(.095,.108,np.sqrt(dx*dx+dy*dy)/(.7+.3*petal))
stain(rosette,(.68,.47,.24))
xx=((u*12)%1-.5)*2;yy=((v*18)%1-.5)*2
sprig=(1-smooth(.12,.20,np.abs(xx)*.8+np.abs(yy)))*(edge>.125)*(diamond>.29)
stain(sprig,(.61,.41,.24))
rug*=((.91+.07*noise(40,40)+.025*weave)[:,:,None])
surface('PersianRug','11 • Woven heritage carpet',rug,weave*.00013+fibres*.00003,.94+.04*fibres,(3,5))

(DEST/'manifest.json').write_text(json.dumps(dict(materials=manifest),ensure_ascii=False,indent=2),encoding='utf-8')
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for i,mat in enumerate(materials):
    bpy.ops.mesh.primitive_cube_add(size=1,location=((i%6)*1.4,0,-(i//6)*1.5))
    ob=bpy.context.object;ob.name=mat.name;ob.dimensions=(1.17,.1,1.17)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for face in ob.data.polygons:
        for li in face.loop_indices:
            co=ob.data.vertices[ob.data.loops[li].vertex_index].co
            ob.data.uv_layers.active.data[li].uv=(co.x/1.17+.5,co.z/1.17+.5)
    ob.data.materials.append(mat)
    bevel=ob.modifiers.new('Rounded sample edges','BEVEL');bevel.width=.02;bevel.segments=3
    ob.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1800;scene.render.resolution_y=850;scene.render.resolution_percentage=100
bpy.ops.object.light_add(type='AREA',location=(1,-4,5));bpy.context.object.data.energy=1300;bpy.context.object.data.size=8
bpy.context.object.rotation_euler=(Vector((2,0,-.5))-bpy.context.object.location).to_track_quat('-Z','Y').to_euler()
scene.world.color=(.22,.22,.22)
bpy.ops.object.camera_add(location=(3.5,-10,.2));cam=bpy.context.object
cam.rotation_euler=(Vector((3.5,0,-.75))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=8.7;scene.camera=cam
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(ROOT.parent/'BlenderMaterials/HouseMaterials_Preview.png')
Path(scene.render.filepath).parent.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).with_name('HouseMaterials.blend')))
bpy.ops.file.make_paths_relative();bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
bpy.ops.render.render(write_still=True)
print('ALL_MATERIALS_COMPLETE',flush=True)
