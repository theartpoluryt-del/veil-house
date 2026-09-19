import sys,math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).parent))
from propkit import *
start()

def basin(name,rx,ry,z,height,mat='Porcelain'):
    profile=[(0,z),(.62,z),(.78,z+height*.1),(.98,z+height*.88),(1,z+height),(.98,z+height+.025),(.90,z+height+.025),(.88,z+height*.86),(.69,z+.035),(0,z+.035)]
    lathe(name,profile,mat,scale=(rx,ry),n=80)
    lathe('Metal drain grate',[(0,z+.038),(.035,z+.038),(.035,z+.041),(0,z+.041)],'Chrome',n=32)
    for i in range(5):box('Drain slit',((i-2)*.01,0,z+.043),(.004,.04,.002),'Iron',.0005)

def tap(tall=False):
    h=1.02 if tall else .42
    lathe('Tap mounting flange',[(0,0),(.06,0),(.065,.012),(.045,.026),(.026,.045),(0,.045)],'Chrome',loc=(0,.18,0),n=32)
    tube('Swept faucet',[(0,.18,0),(0,.18,h-.13),(0,.175,h-.06),(0,.14,h),(0,.07,h+.025),(0,-.01,h+.015),(0,-.065,h-.02)],.018,'Chrome')
    for x in (-.23,.23):
        tube('Mixer pipe',[(x,.18,.06),(x,.18,.16)],.02,'Chrome')
        tube('Cross tap handle',[(x-.055,.18,.18),(x+.055,.18,.18)],.009,'Chrome')
        tube('Cross tap handle',[(x,.13,.18),(x,.23,.18)],.009,'Chrome')
        ellipsoid('Porcelain hot cold cap',(x,.18,.186),(.029,.029,.018),'Porcelain')

def sink():
    basin('Deep oval washbasin',.47,.30,-.13,.18)
    box('Splash ledge',(0,.23,.04),(.89,.16,.06),'Porcelain',.025)
    tap()

def pedestal():
    lathe('Tapered ceramic pedestal',[(0,0),(.20,0),(.21,.06),(.15,.10),(.085,.24),(.08,.57),(.15,.78),(.18,.84),(0,.84)],'Porcelain',n=64,flutes=.018)

def bathtub():
    basin('Rolled enamel bathtub',1.20,.53,.31,.60,'Enamel')
    for x in (-.76,.76):
        for y in (-.29,.29):
            tube('Curved claw foot',[(x,y,.37),(x*1.04,y*1.12,.23),(x*1.10,y*1.30,.10)],.042,'Brass')
            ellipsoid('Foot pad',(x*1.10,y*1.30,.085),(.18,.14,.10),'Brass')
            for k in (-1,0,1):tube('Carved claw',[(x*1.1+k*.04,y*1.3,.12),(x*1.1+k*.04,y*1.3-.05,.045)],.009,'Brass')
    tube('Overflow pipe',[(-.93,0,.34),(-1.14,0,.48),(-1.16,0,.82)],.018,'Chrome')

def toilet():
    lathe('Pedestal base',[(0,0),(.20,0),(.21,.045),(.15,.09),(.125,.27),(.21,.40),(0,.40)],'Porcelain',scale=(1,1.25),n=64)
    basin('Open toilet bowl',.31,.42,.31,.28)
    ring('Dark hinged seat',(0,-.025,.625),.285,.37,.029,'Walnut')
    box('Rounded cistern',(0,.38,.90),(.58,.25,.55),'Porcelain',.055)
    box('Cistern fitted lid',(0,.38,1.195),(.62,.28,.055),'Porcelain',.02)
    tube('Flush lever',[(.27,.255,1.08),(.32,.255,1.08),(.32,.22,1.01)],.013,'Chrome')
    for x in (-.12,.12):box('Seat hinge',(x,.27,.645),(.06,.09,.02),'Chrome',.009)

def mirror():
    # Mirror substrate with patinated edge. Unity supplies a planar room reflection.
    box('Silvered glass',(0,0,0),(.97,.018,1.12),'MirrorSilver',.045)
    frame('Profiled mirror surround',(0,-.022,0),1.0,1.16,'Brass',.04)
    for s in (-1,1):
        tube('Scrolled crest',[(s*x,-.045,z) for x,z in ((0,.61),(.12,.67),(.25,.69),(.31,.65),(.27,.61))],.012,'Brass')
        for z in (-.48,-.24,0,.24,.48):ellipsoid('Patina fleck',(s*.469,-.011,z),(.018,.002,.042),'Iron')

def towel():
    for k in range(3):cloth('Folded terry layer',(0,0,.012+k*.015),.48,.40,'Towel')
    for y in (-.16,.16):tube('Woven towel border',[(x,y,.055+.005*math.sin(x*25)) for x in [i*.012-.23 for i in range(40)]],.002,'BlueGlaze')
    for i in range(22):tube('Loose towel fringe',[(-.225+i*.021,.20,.04),(-.222+i*.021,.225,.028)],.0014,'Towel')

def soap():
    box('Used oval soap',(0,0,.025),(.16,.22,.05),'Wax',.028)
    for i in (-1,0,1):box('Embossed soap mark',(i*.023,0,.050),(.012,.047,.001),'Porcelain',.0004)

for name,fn in [('Sink',sink),('Pedestal',pedestal),('Bathtub',bathtub),('BathTap',lambda:tap(True)),('Toilet',toilet),('Mirror',mirror),('FoldedTowel',towel),('Soap',soap),('BathMat',lambda:cloth('Terry bath mat',(0,0,.025),.85,2.1,'Towel'))]:export('Bathroom',name,fn)
finish('Bathroom')
