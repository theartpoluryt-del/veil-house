import sys,math,random
from pathlib import Path
sys.path.insert(0,str(Path(__file__).parent))
from propkit import *
start();rng=random.Random(1834)

def painting(kind):
    w=1.2;h=.85
    box('Canvas stretcher',(0,0,0),(w,.045,h),'Walnut',.005)
    frame('Carved gilt frame',(0,-.036,0),w,h,'Brass',.046)
    rect=(.006,.506,.494,.994) if kind==0 else (.506,.506,.994,.994)
    decal('Oil canvas',(0,-.027,0),w-.04,h-.04,rect)
    parts[-1].data.materials[0]=material('ArtAtlas')
    if kind==2:parts[-1].data.materials[0]=material('FamilyPortrait');uv=parts[-1].data.uv_layers.active
    if kind==2:
        for p in parts[-1].data.polygons:
            for li,co in zip(p.loop_indices,[(0,0),(1,0),(1,1),(0,1)]):uv.data[li].uv=co
    for x in (-w/2,w/2):
        for z in (-h/2,h/2):
            for i in range(4):ellipsoid('Frame acanthus bead',(x+(i-1.5)*.018,-.07,z),(.028,.015,.035),'Brass')

def photo(kind):
    box('Velvet backing',(0,0,0),(.265,.022,.33),'Walnut',.009)
    frame('Engraved photo frame',(0,-.021,0),.242,.305,'Brass',.014)
    rect=(.01,.01,.49,.49) if kind==0 else (.51,.01,.99,.49)
    decal('Archival photograph',(0,-.025,0),.227,.289,rect,'ArtAtlas')
    ob=box('Folding easel leg',(0,.075,-.07),(.04,.012,.24),'Walnut',.004);ob.rotation_euler.x=-.52

def lamp(banker=False,pendant=False):
    if pendant:
        lathe('Ceiling rose',[(0,0),(.16,0),(.17,-.022),(.10,-.04),(.03,-.055),(0,-.055)],'Brass')
        tube('Pendant drop',[(0,0,-.04),(0,0,-.33)],.011)
        lathe('Fluted opal shade',[(.04,-.30),(.10,-.35),(.19,-.45),(.20,-.52),(.18,-.59),(.10,-.64),(0,-.65)],'Glow',n=64,flutes=.022)
        ring('Shade gallery',(0,0,-.42),.166,.166,.007)
        return
    lathe('Stepped weighted base',[(0,0),(.14,0),(.16,.013),(.151,.029),(.13,.043),(.06,.05),(.029,.075),(0,.075)],'Brass')
    lathe('Turned stem',[(0,.05),(.03,.05),(.035,.1),(.022,.14),(.019,.26),(.038,.285),(.026,.31),(0,.31)],'Brass',n=32)
    if banker:
        tube('Adjustable brass yoke',[(-.18,0,.32),(-.18,0,.43),(.18,0,.43),(.18,0,.32)],.008)
        vv=[];ff=[]
        for j in range(25):
            a=j*math.pi/24
            for x in (-.215,.215):vv.append((x,.13*math.cos(a),.40+.13*math.sin(a)))
        for j in range(24):ff.append((j*2,j*2+1,j*2+3,j*2+2))
        ob=mesh('Green bankers shade',vv,ff,'BlueGlaze');bpy.context.view_layer.objects.active=ob;m=ob.modifiers.new('Glass thickness','SOLIDIFY');m.thickness=.006;bpy.ops.object.modifier_apply(modifier=m.name)
        ellipsoid('Lamp bulb',(0,0,.43),(.22,.07,.07),'Glow')
        tube('Pull chain',[(.17,-.015,.42),(.172,-.018,.29)],.002,'Brass')
    else:
        lathe('Pleated silk shade',[(.24,.31),(.238,.32),(.143,.59),(.142,.60),(.134,.60),(.23,.31),(.24,.31)],'Glow',n=96,flutes=.035)
        for z,r in ((.31,.241),(.60,.142)):ring('Rolled shade piping',(0,0,z),r,r,.003,'Linen')
        for a in (0,2.094,4.189):tube('Shade support',[(0,0,.32),(.21*math.cos(a),.21*math.sin(a),.315)],.003)

def chandelier():
    lathe('Central baluster',[(0,-.2),(.065,-.2),(.08,-.35),(.03,-.47),(.06,-.53),(0,-.55)],'Brass')
    for i in range(5):
        a=i*math.tau/5
        tube('Swept chandelier arm',[(math.cos(a)*r,math.sin(a)*r,z) for r,z in ((.04,-.46),(.20,-.56),(.38,-.57),(.54,-.47),(.59,-.38))],.011)
        lathe('Tulip glass',[(.03,-.4),(.05,-.39),(.10,-.31),(.12,-.22),(.115,-.19),(.104,-.20),(.094,-.29),(.044,-.38)],'Glow',loc=(math.cos(a)*.59,math.sin(a)*.59,0),n=40,flutes=.04)

def leaf(loc,a,length,width,mat='Leaf'):
    vv=[];ff=[]
    for j in range(9):
        t=j/8;spread=max(.0003,width*math.sin(math.pi*t)**.8)
        for side in (-1,0,1):
            along=length*t;across=side*spread;z=length*(.5*t-.7*t*t)+abs(side)*.013*math.sin(t*math.pi)
            vv.append((loc[0]+along*math.cos(a)-across*math.sin(a),loc[1]+along*math.sin(a)+across*math.cos(a),loc[2]+z))
    for j in range(8):
        for k in range(2):p=j*3+k;ff.append((p,p+1,p+4,p+3))
    ob=mesh('Curved veined leaf',vv,ff,mat);bpy.context.view_layer.objects.active=ob
    m=ob.modifiers.new('Leaf thickness','SOLIDIFY');m.thickness=.0005;bpy.ops.object.modifier_apply(modifier=m.name)

def plant(fern=False):
    lathe('Glazed fluted planter',[(0,0),(.17,0),(.19,.045),(.23,.38),(.25,.40),(.25,.44),(.21,.44),(.205,.39),(.17,.06),(0,.045)],'Copper' if fern else 'BlueGlaze',flutes=.015)
    lathe('Uneven soil',[(0,.39),(.208,.39),(.207,.407),(0,.411)],'Soil')
    for i in range(17 if fern else 11):
        a=i*2.399;h=.62+(i%4)*.12
        tube('Arched stem',[(0,0,.40),(.04*math.cos(a),.04*math.sin(a),h),(.12*math.cos(a),.12*math.sin(a),h+.11)],.004,'Bark')
        leaf((.1*math.cos(a),.1*math.sin(a),h+.1),a,.43+(i%3)*.07,.055 if fern else .10)
        if fern:
            for j in range(3):
                leaf((.15*math.cos(a),.15*math.sin(a),h+.03-j*.045),a+.7,.20,.045)
                leaf((.15*math.cos(a),.15*math.sin(a),h+.03-j*.045),a-.7,.20,.045)

def flowerbed():
    for x in (-.85,.85):box('Weathered planter end',(x,0,.15),(.08,.55,.30),'SmokedOak')
    for y in (-.25,.25):box('Weathered planter plank',(0,y,.15),(1.75,.06,.30),'SmokedOak')
    box('Garden soil',(0,0,.23),(1.66,.43,.10),'Soil')
    for i in range(15):
        x=rng.uniform(-.75,.75);y=rng.uniform(-.16,.16);z=rng.uniform(.38,.66)
        tube('Flower stem',[(x,y,.26),(x+.04,y,z)],.003,'Leaf');leaf((x,y,.35),i*1.3,.17,.033)
        for k in range(7):
            a=k*math.tau/7;leaf((x+.04,y,z),a,.05,.019,'BurgundyFabric')

def clock():
    for z,w,d,h in ((.12,.90,.55,.24),(.27,.8,.48,.08),(2.37,.84,.49,.10),(2.47,.92,.56,.11)):
        box('Clock case moulding',(0,0,z),(w,d,h),'Walnut',.02)
    box('Panelled walnut case',(0,.08,1.30),(.70,.35,2.15),'Walnut',.018)
    for x in (-.31,.31):
        lathe('Fluted clock column',[(0,.32),(.036,.32),(.029,.41),(.027,1.60),(.04,1.67),(0,1.67)],'SmokedOak',loc=(x,-.18,0),n=24,flutes=.08)
    frame('Pendulum door',(0,-.14,1.0),.43,1.25,'SmokedOak',.036)
    box('Pendulum recess',(0,-.157,1.0),(.40,.025,1.19),'Iron',.006)
    tube('Pendulum shaft',[(0,-.20,1.55),(0,-.20,.65)],.007)
    ellipsoid('Pendulum bob',(0,-.21,.71),(.27,.046,.27),'Brass')
    decal('Roman clock face',(0,-.198,2.0),.60,.60,(.008,.508,.492,.992))
    ring('Dial bezel',(0,-.205,2.0),.306,.306,.012,'Brass','XZ')
    tube('Hour hand',[(0,-.222,2),(-.09,-.222,2.12)],.010,'Iron')
    tube('Minute hand',[(0,-.224,2),(.17,-.224,2.10)],.006,'Iron')
    ring('Broken arch crown',(0,0,2.46),.34,.18,.031,'Walnut','XZ')

for name,fn in [('Landscape',lambda:painting(0)),('StillLife',lambda:painting(1)),('PortraitFrame',lambda:painting(2)),('PhotoCouple',lambda:photo(0)),('PhotoManor',lambda:photo(1)),('TableLamp',lamp),('BankerLamp',lambda:lamp(True)),('Pendant',lambda:lamp(False,True)),('Chandelier',chandelier),('PalmPlanter',plant),('FernPlanter',lambda:plant(True)),('Flowerbed',flowerbed),('GrandfatherClock',clock)]:export('Decor',name,fn)
finish('Decor')
