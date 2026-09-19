import sys,math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).parent))
from propkit import *
start()

def plate(blue=False):
    lathe('Thrown porcelain dish',[(0,0),(.065,0),(.075,.003),(.13,.012),(.152,.026),(.151,.034),(.143,.039),(.12,.022),(.07,.014),(0,.014)],'BlueGlaze' if blue else 'Porcelain',n=64,flutes=.008)
    ring('Fine rim',(0,0,.034),.15,.15,.002,'Brass')
    for a in range(12):
        t=a*math.tau/12
        tube('Glaze brushstroke',[(.125*math.cos(t),.125*math.sin(t),.028),(.133*math.cos(t+.025),.133*math.sin(t+.025),.033),(.14*math.cos(t),.14*math.sin(t),.036)],.0017,'Porcelain' if blue else 'BlueGlaze')

def vase(tall=False):
    if tall:p=[(0,0),(.07,0),(.082,.015),(.055,.055),(.093,.13),(.086,.24),(.044,.30),(.043,.39),(.060,.42),(.061,.43),(.051,.43),(.035,.39),(.036,.30),(.077,.24),(.08,.13),(.05,.055),(0,.022)]
    else:p=[(0,0),(.09,0),(.12,.05),(.135,.13),(.12,.22),(.065,.28),(.060,.34),(.068,.36),(.058,.36),(.05,.33),(.055,.28),(.107,.22),(.12,.13),(.10,.055),(0,.022)]
    lathe('Fluted hollow ceramic',p,'BlueGlaze' if tall else 'Porcelain',n=64,flutes=.025)
    ring('Gilt vase collar',(0,0,.39 if tall else .34),.043 if tall else .061,.043 if tall else .061,.0025,'Brass')

def bottle(kind):
    jar=kind.startswith('Jar');perfume=kind=='Perfume';milk=kind=='Milk';oil=kind=='Oil'
    h=.22 if kind=='JarSpice' else .27 if jar else .24 if perfume else .40;r=.067 if kind=='JarSpice' else .08 if jar else .065 if perfume else .075
    neck=r*.75 if jar else r*.36
    p=[(0,0),(r*.84,0),(r,.018),(r,h*.57),(r*.95,h*.68),(neck,h*.77),(neck,h*.97),(neck*1.08,h),(neck*.82,h),(neck*.80,h*.78),(r*.83,h*.64),(r*.85,.028),(0,.024)]
    mat='Enamel' if milk else 'MotorPaint' if oil else 'GlassPatina' if perfume or jar else 'BlueGlaze'
    lathe('Moulded bottle with shoulder and inner neck',p,mat,n=48,flutes=.06 if perfume else 0)
    lathe('Threaded screw cap' if jar else 'Fitted stopper',[(0,h),(neck*1.12,h),(neck*1.13,h+.022),(neck*.95,h+.026),(0,h+.026)],'Copper' if jar else 'Brass' if perfume else 'Walnut')
    for z in (.003,.010,.017):ring('Cap grip bead',(0,0,h+z),neck*1.13,neck*1.13,.0015,'Iron' if oil else 'Brass')
    row=3 if oil else 2 if milk else 1 if perfume else 0
    decal('Printed bottle label',(0,-r-.001,h*.38),r*1.35,h*.30,(.515,1-(row+1)*.125+.009,.985,1-row*.125-.008))
    if oil:tube('Fine oil spout',[(0,0,h+.023),(.025,0,h+.065),(.07,0,h+.10)],.007,'Copper')

def candle():
    lathe('Turned candlestick',[(0,0),(.085,0),(.089,.012),(.08,.025),(.041,.032),(.024,.065),(.020,.11),(.033,.125),(.028,.141),(.04,.15),(.049,.17),(.044,.181),(0,.181)],'Brass')
    lathe('Irregular wax candle',[(0,.176),(.029,.176),(.032,.19),(.03,.30),(.027,.317),(.018,.31),(0,.31)],'Wax',n=40,flutes=.025)
    for i in range(5):
        a=i*1.27;r=.031
        tube('Cooled wax drip',[(r*math.cos(a),r*math.sin(a),.31),((r+.002)*math.cos(a),(r+.002)*math.sin(a),.29-i*.008)],.003,'Wax')
    tube('Charred wick',[(0,0,.31),(.001,0,.328)],.0018,'Iron')

def kettle_complete():
    lathe('Hammered copper kettle',[(0,0),(.13,0),(.157,.027),(.17,.09),(.16,.17),(.125,.235),(.10,.25),(.087,.245),(.11,.22),(.145,.165),(.15,.085),(.135,.025),(0,.02)],'Copper',n=64)
    lathe('Domed lid',[(0,.24),(.105,.24),(.108,.25),(.088,.269),(.03,.285),(0,.285)],'Copper')
    ellipsoid('Lid grip',(0,0,.30),(.065,.05,.05),'Iron')
    tube('Arch handle',[(math.cos(a)*.14,0,.22+math.sin(a)*.16) for a in [i*math.pi/32 for i in range(33)]],.015,'Iron')
    # Tapered open spout following an ascending curved centreline.
    vv=[];ff=[]
    for j in range(13):
        t=j/12;cx=.12+.14*t;cz=.08+.19*t*t;r=.04-.019*t
        for i in range(24):
            a=i*math.tau/24;vv.append((cx,math.cos(a)*r,cz+math.sin(a)*r))
    for j in range(12):
        for i in range(24):
            a=j*24+i;b=j*24+(i+1)%24;ff.append((a,b,b+24,a+24))
    ob=mesh('Open formed spout',vv,ff,'Copper');bpy.context.view_layer.objects.active=ob;m=ob.modifiers.new('Spout wall','SOLIDIFY');m.thickness=.003;bpy.ops.object.modifier_apply(modifier=m.name)

def pan():
    lathe('Copper pan with well',[(0,0),(.12,0),(.15,.025),(.15,.072),(.141,.076),(.139,.03),(.115,.012),(0,.012)],'Copper')
    tube('Forged pan handle',[(0,.14,.05),(0,.23,.055),(0,.40,.05)],.015,'Iron')
    ring('Hanging eye',(0,.41,.05),.025,.033,.006,'Iron')

for name,fn in [('PlateIvory',lambda:plate()),('PlateBlue',lambda:plate(True)),('VasePear',lambda:vase()),('VaseFluted',lambda:vase(True)),('Candlestick',candle),('Kettle',kettle_complete),('CopperPan',pan)]:export('Vessels',name,fn)
for kind in ('Wine','Milk','Perfume','Oil','JarTea','JarSpice'):export('Vessels',kind,lambda k=kind:bottle(k))
finish('Vessels')
