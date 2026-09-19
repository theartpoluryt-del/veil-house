import sys,math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).parent))
from propkit import *
start()

def tire():
    # True annular tyre with alternating chevron tread relief.
    profile=[(.255,-.11),(.32,-.14),(.39,-.12),(.423,-.07),(.430,0),(.423,.07),(.39,.12),(.32,.14),(.255,.11),(.255,-.11)]
    verts=[];faces=[];n=128
    for j,(r,z) in enumerate(profile):
        for i in range(n):
            a=i*math.tau/n;tread=.006 if 2<j<7 and (i+j*2)%8<3 else 0
            verts.append(((r-tread)*math.cos(a),(r-tread)*math.sin(a),z))
    for j in range(len(profile)-1):
        for i in range(n):a=j*n+i;b=j*n+(i+1)%n;faces.append((a,b,b+n,a+n))
    mesh('Chevron tread and rounded sidewall',verts,faces,'Rubber')
    for z in (-.124,.124):ring('Raised sidewall bead',(0,0,z),.352,.352,.004,'Rubber')

def wheel(loc):
    begin=len(parts);tire()
    for z in (-.10,.10):
        ring('Rolled steel wheel rim',(0,0,z),.26,.26,.012,'Chrome')
        for i in range(24):
            a=i*math.tau/24
            tube('Laced spoke',[(.07*math.cos(a+.3),.07*math.sin(a+.3),z*.3),(.252*math.cos(a),.252*math.sin(a),z)],.004,'Chrome')
    lathe('Wheel hub',[(0,-.13),(.075,-.13),(.085,-.07),(.08,.10),(.055,.15),(0,.15)],'Chrome',n=32)
    for ob in parts[begin:]:
        ob.rotation_euler.y=math.pi/2;ob.location=loc

def car():
    box('Steel ladder chassis',(0,0,.41),(1.75,3.74,.16),'Iron',.025)
    # Curved bonnet surface across its width, with folded side walls.
    vv=[];ff=[]
    for j in range(17):
        y=-1.75+j*.087
        for i in range(25):
            a=i*math.pi/24;vv.append((.66*math.cos(a),y,.76+.48*math.sin(a)))
    for j in range(16):
        for i in range(24):k=j*25+i;ff.append((k,k+1,k+26,k+25))
    ob=mesh('Curved long bonnet',vv,ff,'MotorPaint');bpy.context.view_layer.objects.active=ob;m=ob.modifiers.new('Coachwork thickness','SOLIDIFY');m.thickness=.012;bpy.ops.object.modifier_apply(modifier=m.name)
    for s in (-1,1):
        box('Low passenger door',(s*.82,.45,.84),(.10,1.93,.65),'MotorPaint',.06)
        box('Rubber running board',(s*1.02,.04,.48),(.32,2.0,.095),'Rubber',.019)
        for y in (-1.2,1.2):
            wheel((s*1.0,y,.47))
            vv=[];ff=[]
            for j in range(33):
                a=j*math.pi/32
                for x in (s*.78,s*1.20):vv.append((x,y+.58*math.cos(a),.48+.58*math.sin(a)))
            for j in range(32):ff.append((j*2,j*2+1,j*2+3,j*2+2))
            f=mesh('Sweeping separate mudguard',vv,ff,'MotorPaint');bpy.context.view_layer.objects.active=f;m=f.modifiers.new('Rolled fender edge','SOLIDIFY');m.thickness=.018;bpy.ops.object.modifier_apply(modifier=m.name)
        for i in range(10):tube('Bonnet cooling louver',[(s*.662,-1.54+i*.104,.78),(s*.657,-1.54+i*.104,1.04)],.011,'Iron')
        tube('Door upper trim',[(s*.82,-.38,1.17),(s*.82,1.34,1.17)],.014,'Chrome')
        tube('Door handle',[(s*.89,.66,1.04),(s*.94,.66,1.04),(s*.94,.81,1.04)],.012,'Chrome')
        ellipsoid('Headlamp housing',(s*.76,-1.70,.91),(.28,.24,.28),'Brass')
        ellipsoid('Fluted headlamp lens',(s*.76,-1.827,.91),(.235,.023,.235),'Porcelain')
        tube('Windscreen frame',[(s*.78,-.36,1.17),(s*.78,-.27,1.98)],.022,'Chrome')
        tube('Rear roof bow',[(s*.79,1.28,1.15),(s*.79,1.28,1.99)],.018,'Iron')
    box('Rear curved body',(0,1.38,.87),(1.75,.17,.68),'MotorPaint',.07)
    for y in (.20,1.02):
        box('Leather bench cushion',(0,y,.87),(1.46,.62,.18),'Leather',.08)
        box('Pleated leather seat back',(0,y+.28,1.12),(1.46,.16,.46),'Leather',.075)
        for i in range(12):tube('Seat stitched channel',[(-.66+i*.12,y+.18,.97),(-.66+i*.12,y+.18,1.29)],.003,'Walnut')
    cloth('Tensioned canvas roof',(0,.54,2.04),1.78,2.0,'Rubber',.04)
    box('Windscreen glass',(0,-.30,1.65),(1.49,.022,.62),'GlassPatina',.015)
    tube('Windscreen top rail',[(-.8,-.27,1.99),(.8,-.27,1.99)],.021,'Chrome')
    box('Radiator surround',(0,-1.77,.88),(1.12,.10,.78),'Chrome',.055)
    box('Radiator core',(0,-1.826,.88),(.96,.014,.65),'Iron',.015)
    for i in range(23):tube('Radiator fin',[(-.45+i*.041,-1.842,.58),(-.45+i*.041,-1.842,1.17)],.004,'Chrome')
    tube('Front curved bumper',[(-.94,-1.91,.46),(-.74,-1.98,.46),(.74,-1.98,.46),(.94,-1.91,.46)],.025,'Chrome')
    decal('Vehicle plate',(0,-2.01,.45),.35,.075,(.10,.008,.40,.075))
    ring('Steering wheel',(.42,-.15,1.36),.16,.16,.016,'Walnut','XZ')
    for a in range(3):
        t=a*math.tau/3;tube('Steering spoke',[(.42,-.15,1.36),(.42+.15*math.cos(t),-.15,1.36+.15*math.sin(t))],.007,'Chrome')

def hammer():
    ob=box('Worn hickory handle',(0,0,.01),(.04,.32,.045),'SmokedOak',.015);ob.rotation_euler.z=.025
    box('Forged hammer head',(0,.13,.025),(.17,.055,.075),'Iron',.012)
    for x in (-.073,-.055):box('Split claw',(x,.17,.025),(.012,.065,.03),'Iron',.008)

def wrench():
    box('Drop forged wrench shank',(0,0,0),(.041,.26,.028),'Iron',.014)
    for y in (-.14,.14):
        pts=[(.055*math.cos(a),y+.055*math.sin(a),0) for a in [math.pi*.18+i*math.pi*1.64/24 for i in range(25)]]
        tube('Open jaw',pts,.014,'Iron')
    box('Stamped size',(0,0,.016),(.02,.067,.003),'Chrome',.002)

def case(kind):
    mat='Leather' if kind=='Suitcase' else 'MotorPaint' if kind in ('Toolbox','Canister') else 'SmokedOak'
    w=.85;d=.46;h=.40
    box('Bevelled case body',(0,0,h*.46),(w,d,h*.85),mat,.035)
    box('Lid with rolled seam',(0,0,h*.91),(w+.012,d+.012,h*.17),mat,.025)
    for x in (-.27,.27):
        box('Case binding',(x,0,.205),(.046,d+.025,.39),'Walnut' if kind=='Suitcase' else 'Iron',.008)
        box('Brass latch',(x,-d/2-.023,.31),(.045,.025,.075),'Brass',.008)
    tube('Carrying handle',[(-.13,-.26,.22),(-.11,-.31,.22),(.11,-.31,.22),(.13,-.26,.22)],.014,'Leather' if kind=='Suitcase' else 'Iron')
    if kind=='Canister':
        lathe('Filler cap',[(0,.41),(.052,.41),(.052,.44),(0,.44)],'Iron',loc=(.25,0,0),n=24)
        tube('Top grip',[(-.23,0,.4),(-.23,0,.48),(.1,0,.48),(.1,0,.4)],.02,'MotorPaint')

def crate():
    for i in range(4):
        for y in (-.31,.31):box('Separated crate plank',(0,y,.08+i*.13),(.80,.055,.115),'SmokedOak',.008)
        for x in (-.38,.38):box('Crate end plank',(x,0,.08+i*.13),(.055,.6,.115),'SmokedOak',.008)
    for x in (-.31,.31):
        box('Corner batten',(x,-.35,.27),(.065,.045,.54),'Walnut',.006)
        for z in (.1,.42):ellipsoid('Rusty nail',(x,-.376,z),(.013,.005,.013),'Iron')
    for i in range(5):box('Crate lid board',(-.32+i*.16,0,.55),(.153,.66,.04),'SmokedOak',.006)

def breadbox():
    box('Breadbox base',(0,0,.018),(.65,.39,.036),'SmokedOak',.012)
    for i in range(19):
        a=i*math.pi/36;ob=box('Roll top slat',(0,.18*math.cos(a)-.02,.04+.18*math.sin(a)),(.62,.020,.025),'SmokedOak',.007);ob.rotation_euler.x=-a
    for x in (-.30,.30):box('Breadbox cheek',(x,0,.10),(.035,.35,.18),'Walnut',.022)
    tube('Breadbox pull',[(-.08,-.02,.222),(.08,-.02,.222)],.011,'Brass')

def letters():
    for i in range(7):
        ob=box('Uneven envelope',(i*.0008,0,i*.003),(.25,.18,.002),'Paper',.0004);ob.rotation_euler.z=(i-3)*.017
    tube('Envelope fold',[(-.12,-.086,.022),(0,.02,.022),(.12,-.086,.022)],.0006,'Walnut')
    box('Postage stamp',(.084,.055,.024),(.032,.037,.001),'BookBlue',.001)

def inkpot():
    box('Faceted glass inkwell',(0,0,.047),(.12,.12,.093),'GlassPatina',.024)
    lathe('Brass hinged cap',[(0,.093),(.034,.093),(.038,.11),(0,.116)],'Brass',n=32)

for name,fn in [('Motorcar',car),('SpareTire',tire),('Hammer',hammer),('Wrench',wrench),('Suitcase',lambda:case('Suitcase')),('Toolbox',lambda:case('Toolbox')),('Canister',lambda:case('Canister')),('JewelryBox',lambda:case('JewelryBox')),('WoodCrate',crate),('Breadbox',breadbox),('Letters',letters),('Inkpot',inkpot)]:export('Garage',name,fn)
finish('Garage')
