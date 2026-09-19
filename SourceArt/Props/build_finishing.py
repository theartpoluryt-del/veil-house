import sys,math,random
from pathlib import Path
sys.path.insert(0,str(Path(__file__).parent))
from propkit import *
start();rng=random.Random(394)

def entrance():
    for s in (-1,1):
        cx=s*.66
        for x in (cx-.57,cx+.57):box('Front door stile',(x,0,1.40),(.16,.13,2.80),'Walnut',.017)
        for z in (.12,1.0,2.67):box('Front door rail',(cx,0,z),(1.15,.13,.18),'Walnut',.013)
        for z,h in ((.55,.69),(1.83,1.45)):
            box('Solid raised entrance panel',(cx,0,z),(.98,.095,h),'SmokedOak',.025)
            frame('Raised panel moulding',(cx,-.08,z),.99,h,'Walnut',.024)
        tube('Curved entrance pull',[(cx-s*.38,-.10,1.12),(cx-s*.38,-.18,1.18),(cx-s*.38,-.18,1.42),(cx-s*.38,-.10,1.48)],.018)
        for z in (.35,1.42,2.51):box('Blackened strap hinge',(cx+s*.39,-.09,z),(.34,.025,.075),'Iron',.014)

def garage_door():
    for s in (-1,1):
        cx=s*1.11
        for i in range(10):box('Uneven vertical garage plank',(cx-.99+i*.22,0,1.37),(.212,.09,2.72),'SmokedOak',.008)
        for z in (.32,2.39):box('Forged horizontal brace',(cx,-.07,z),(2.13,.035,.09),'Iron',.009)
        tube('Diagonal steel brace',[(cx-.98,-.067,.37),(cx+.98,-.067,2.34)],.025,'Iron')
        tube('Forged garage pull',[(cx-s*.77,-.08,1.16),(cx-s*.77,-.18,1.19),(cx-s*.77,-.18,1.41),(cx-s*.77,-.08,1.44)],.015,'Iron')
        for x in (cx-.96,cx+.96):
            for z in (.32,2.39):ellipsoid('Hand forged rivet',(x,-.094,z),(.035,.018,.035),'Iron')

def matchbox():
    box('Cardboard sleeve',(0,0,.026),(.11,.18,.052),'BurgundyFabric',.003)
    box('Inner match tray',(0,.012,.028),(.097,.172,.043),'Paper',.002)
    for x in (-.055,.055):box('Abrasive striking strip',(x,0,.03),(.002,.14,.025),'Iron',.001)
    for i in range(7):
        tube('Wooden match',[(-.035+i*.011,.067,.039),(-.035+i*.011,.088,.039)],.0014,'SmokedOak')
        ellipsoid('Match head',(-.035+i*.011,.09,.039),(.004,.006,.004),'BurgundyFabric')

def cypress():
    lathe('Irregular bark trunk',[(0,0),(.20,0),(.14,1),(.085,3.2),(.018,5.8),(0,6)],'Bark',n=16,flutes=.1)
    for i in range(140):
        z=.7+(i/140)*5.4;a=i*2.4;width=(.3+.7*math.sin(z/6*math.pi))*(1-z/7)
        x=math.cos(a)*width*.55;y=math.sin(a)*width*.55
        tube('Needled branch',[(0,0,z-.2),(x,y,z),(x*1.45,y*1.45,z+.31)],.012,'Bark')
        for j in range(4):
            t=a+j*1.8;length=.36+rng.random()*.36;w=.08+rng.random()*.07
            vv=[(x-w*math.sin(t),y+w*math.cos(t),z-.04),(x+w*math.sin(t),y-w*math.cos(t),z-.04),(x+math.cos(t)*length*.4,y+math.sin(t)*length*.4,z+length),(x,y,z+.12)]
            mesh('Angular needle spray',vv,[(0,1,2),(0,3,1),(0,2,3),(1,3,2)],'Leaf')

def medicine():
    lathe('Faceted apothecary glass',[(0,0),(.057,0),(.062,.025),(.06,.19),(.031,.24),(.027,.32),(.021,.32),(.021,.25),(.05,.19),(.05,.03),(0,.025)],'GlassPatina',n=32,flutes=.04)
    lathe('Ground glass stopper',[(0,.32),(.03,.32),(.039,.35),(.035,.38),(0,.38)],'BlueGlaze',n=32)
    decal('Apothecary paper label',(0,-.063,.12),.082,.11,(.515,.759,.985,.867))

for name,fn in [('EntranceDoors',entrance),('GarageDoors',garage_door),('Matchbox',matchbox),('Cypress',cypress),('Medicine',medicine)]:export('Finishing',name,fn)
finish('Finishing')
