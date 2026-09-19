"""Additional 2K PBR surfaces for the authored object library. Run in Blender."""
from pathlib import Path
base=Path(__file__).with_name('generate_materials.py')
exec(compile(base.read_text(encoding='utf-8').split('# Oak:')[0],str(base),'exec'))
existing=json.loads((DEST/'manifest.json').read_text(encoding='utf-8'))
n=noise(9,9); fine=noise(240,240); micro=noise(800,800); streak=noise(80,5)
def add(name,a,b,rough=.7,depth=.0002,metal=0,pattern=None,meters=(.5,.5)):
    t=n*.45+fine*.30+micro*.25 if pattern is None else pattern
    surface(name,name,mix(a,b,np.clip(t,0,1)),depth*(fine*.7+micro*.3),rough+.12*(fine-.5),meters,metal)
add('Porcelain',(.67,.68,.60),(.92,.89,.76),.37,.000025)
add('BlueGlaze',(.075,.20,.26),(.22,.39,.43),.34,.00004)
add('Enamel',(.65,.67,.59),(.87,.86,.73),.44,.00006)
add('Iron',(.045,.053,.050),(.18,.17,.14),.79,.0006,.65)
add('Rubber',(.022,.025,.024),(.073,.076,.068),.94,.0002)
add('MotorPaint',(.06,.10,.095),(.12,.20,.175),.43,.000035,.45)
vein=(.5+.5*np.sin(u*35+v*29+noise(6,6)*12+noise(36,36)*2))**30
surface('Marble','Veined marble',mix((.80,.80,.71),(.49,.51,.46),vein*.24+fine*.09),fine*.000025,.4+vein*.05,(1,1))
add('Limestone',(.42,.40,.33),(.76,.71,.57),.93,.0009,meters=(1,1))
joint=(np.mod(v*10,1)<.08)|(np.mod(u*5+(np.floor(v*10)%2)*.5,1)<.04)
col=mix((.24,.11,.065),(.45,.25,.14),n*.6+fine*.4);col[joint]=(.16,.15,.12)
surface('HearthBrick','Soot stained brick',col,fine*.001-joint*.006,.93+fine*.04,(1.2,1.2))
add('Wax',(.72,.61,.42),(.92,.85,.65),.68,.00014)
add('Paper',(.62,.52,.34),(.91,.85,.66),.94,.00007)
add('Soil',(.035,.025,.015),(.21,.14,.07),.98,.004)
add('Bark',(.075,.048,.027),(.29,.20,.11),.95,.002,pattern=streak*.8+micro*.2)
leafvein=np.maximum((np.cos(u*math.tau*30+v*25)>.96)*.15,(abs(u-.5)<.01)*.5)
surface('Leaf','Veined foliage',mix((.035,.10,.031),(.22,.36,.09),n*.5+fine*.2+leafvein),leafvein*.00016,.76+fine*.1,(.2,.35))
weave=(np.sin(u*math.tau*380)*np.sin(v*math.tau*360))*.5+.5
surface('Towel','Looped terry cloth',mix((.63,.65,.57),(.88,.86,.74),weave*.35+n*.3),weave*.0004+fine*.0003,.96+fine*.02,(.3,.3))
surface('Speaker','Woven speaker grille',mix((.16,.105,.052),(.43,.34,.20),weave*.8+n*.2),weave*.0003,.93+fine*.05,(.25,.25))
add('GlassPatina',(.16,.235,.24),(.31,.37,.35),.20,.000006,.35)
add('Copper',(.21,.105,.052),(.58,.32,.13),.59,.00012,.8)
add('Chrome',(.36,.39,.38),(.68,.70,.65),.28,.00002,.95)
add('MirrorSilver',(.49,.54,.52),(.68,.72,.68),.12,.000003,.95)
newids={m['id'] for m in manifest}
(DEST/'manifest.json').write_text(json.dumps({'materials':[m for m in existing['materials'] if m['id'] not in newids]+manifest},indent=2),encoding='utf-8')
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for i,mat in enumerate(materials):
    mat.use_fake_user=True
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48,ring_count=24,radius=.45,location=((i%5)*1.3,(i//5)*1.3,.45))
    ob=bpy.context.object;ob.name=manifest[i]['id'];ob.data.materials.append(mat)
    for polygon in ob.data.polygons:polygon.use_smooth=True
bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).with_name('ObjectSurfaces.blend')))
bpy.ops.file.make_paths_relative();bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print('OBJECT_SURFACES_COMPLETE '+str(len(manifest)),flush=True)
