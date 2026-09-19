import sys,math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).parent))
from propkit import *
start()

def knob(x,y,z,r=.035):
    ring('Knurled control knob',(x,y,z),r,r,.008,'Iron','XZ',32)
    ellipsoid('Control face',(x,y,z),(r*1.65,.025,r*1.65),'Iron')
    tube('Indicator notch',[(x,y-.015,z+r*.3),(x,y-.015,z+r*.7)],.002,'Porcelain')

def television():
    for x in (-.53,.53):
        for y in (-.21,.21):
            tube('Splayed wood leg',[(x*1.08,y*1.1,.04),(x,y,.43)],.025,'Walnut')
    box('Rounded walnut television cabinet',(0,0,.91),(1.38,.64,1.03),'SmokedOak',.065)
    box('Recessed bakelite bezel',(-.17,-.335,1.01),(.98,.075,.76),'Iron',.105)
    # Rounded rectangular glass face with a shallow convex section.
    verts=[];faces=[];uv=[];n=24
    for j in range(n+1):
        for i in range(n+1):
            x=i/n*2-1;z=j/n*2-1
            xx=x*math.sqrt(1-.24*z*z);zz=z*math.sqrt(1-.24*x*x)
            verts.append((-.17+xx*.43,-.381-.025*(1-x*x)*(1-z*z),1.01+zz*.31));uv.append((i/n,j/n))
    for j in range(n):
        for i in range(n):k=j*(n+1)+i;faces.append((k,k+1,k+n+2,k+n+1))
    mesh('Convex phosphor screen',verts,faces,'Screen',uvs=uv)
    for z in (.88,1.17):knob(.48,-.37,z,.065)
    box('Speaker textile',(-.15,-.331,.55),(.91,.018,.13),'Speaker',.017)
    for i in range(16):box('Ventilation slat',(-.56+i*.056,-.344,.55),(.012,.02,.11),'Walnut',.004)
    for s in (-1,1):tube('Telescopic antenna',[(0,0,1.43),(s*.42,0,2.03)],.006,'Chrome')
    decal('Maker badge',(.47,-.34,.62),.17,.035,(.06,.047,.45,.09))

def radio():
    # Arched cathedral cabinet, extruded polygon shell.
    outline=[(-.40,0),(.40,0),(.40,.35)]+[(.40*math.cos(a),.35+.24*math.sin(a)) for a in [i*math.pi/24 for i in range(1,25)]]
    vv=[(x,y,z) for y in (-.16,.16) for x,z in outline];n=len(outline)
    ff=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh('Arched veneer radio case',vv,ff,'Walnut',False)
    box('Rounded plinth',(0,0,.028),(.85,.37,.07),'SmokedOak',.025)
    box('Woven grille',(0,-.17,.30),(.64,.018,.33),'Speaker',.06)
    for i in range(-4,5):
        x=i*.064;z=.48-.16*(abs(i)/5)**2
        tube('Curved grille rib',[(x,-.19,.18),(x,-.19,z)],.011,'SmokedOak')
    decal('Radio station dial',(0,-.188,.125),.40,.07,(.005,.25,.495,.50))
    for x in (-.30,.30):knob(x,-.195,.11,.032)
    ellipsoid('Dial indicator',(0,-.195,.105),(.008,.009,.009),'Glow')

def stove():
    box('Rounded enamel range body',(0,0,.54),(1.24,.76,.99),'Enamel',.045)
    box('Cast iron stove top',(0,0,1.055),(1.29,.81,.07),'Iron',.025)
    for x in (-.31,.31):
        for y in (-.20,.20):
            for r in (.04,.09,.14):ring('Removable hob ring',(x,y,1.097),r,r,.014,'Iron')
            tube('Pot support',[(x-.16,y,1.12),(x+.16,y,1.12)],.009,'Iron')
    box('Oven cast surround',(0,-.40,.43),(1.02,.058,.60),'Iron',.04)
    box('Oven enamel panel',(0,-.437,.43),(.89,.034,.48),'Enamel',.024)
    box('Dark oven glass',(0,-.46,.43),(.61,.018,.29),'GlassPatina',.029)
    tube('Oven pull handle',[(-.34,-.44,.73),(-.34,-.52,.73),(.34,-.52,.73),(.34,-.44,.73)],.019,'Chrome')
    for i in range(4):knob(-.43+i*.28,-.405,.91,.036)
    for x in (-.51,.51):
        for y in (-.27,.27):box('Cast stove foot',(x,y,.045),(.10,.12,.09),'Iron',.018)

def fireplace():
    box('Sooted firebox back',(0,.08,.67),(2.20,.12,1.14),'HearthBrick',.004)
    box('Hewn hearth slab',(0,-.24,.12),(3.21,1.15,.24),'Limestone',.032)
    for x in (-1.23,1.23):
        box('Carved stone jamb',(x,-.10,.73),(.43,.52,1.24),'Limestone',.025)
        for z in (.28,1.16):box('Pillar moulding',(x,-.12,z),(.49,.58,.12),'Marble',.016)
        for i in (-1,0,1):tube('Pillar carved flute',[(x+i*.09,-.372,.39),(x+i*.09,-.372,1.08)],.017,'Marble')
    for z,w,d,h in ((1.27,3.15,.66,.18),(1.40,3.30,.74,.08)):
        box('Profiled mantel',(0,-.1,z),(w,d,h),'Marble',.025)
    for i in range(4):
        ob=lathe('Split charred log',[(0,0),(.09,0),(.1,.09),(.085,.62),(0,.62)],'Bark',n=20,flutes=.14)
        ob.rotation_euler=(1.35,0,i*.42);ob.location=(-.67+i*.37,-.35,.34)
    for i in range(9):
        tube('Basket upright',[(-.88+i*.22,-.64,.24),(-.88+i*.22,-.64,.54)],.012,'Iron')
    for z in (.29,.47):tube('Basket rail',[(-.94,-.64,z),(.94,-.64,z)],.012,'Iron')
    for i in range(7):ellipsoid('Ember bed',(-.64+i*.21,-.4,.27),(.17,.20,.035),'Glow')

def telephone():
    box('Bakelite moulded base',(0,0,.04),(.40,.29,.085),'Iron',.065)
    ring('Rotary dial rim',(0,-.05,.09),.091,.091,.009,'Brass')
    for i in range(10):
        a=i*math.tau/12-.6;ring('Finger aperture',(.064*math.cos(a),-.05+.064*math.sin(a),.098),.012,.012,.004,'Brass',n=20)
    for x in (-.13,.13):tube('Receiver fork',[(x,.07,.06),(x,.07,.17),(x,.04,.19)],.015,'Brass')
    tube('Curved receiver grip',[(-.14,.07,.18),(-.09,.07,.23),(0,.07,.25),(.09,.07,.23),(.14,.07,.18)],.023,'Iron')
    for x in (-.15,.15):ellipsoid('Receiver earpiece',(x,.07,.18),(.10,.10,.07),'Iron')
    tube('Braided telephone cable',[(.21+.018*math.cos(i*.65),.08+i*.001,.11-.001*i) for i in range(90)],.004,'Rubber')

def globe():
    lathe('Globe pedestal',[(0,0),(.16,0),(.17,.02),(.12,.045),(.04,.07),(.025,.14),(0,.14)],'Brass')
    ob=ellipsoid('Printed globe',(0,0,.36),(.44,.44,.44),'Decals')
    # Sphere creation UVs are retained by restoring spherical projection.
    uv=ob.data.uv_layers.active
    for p in ob.data.polygons:
        coords=[]
        for li in p.loop_indices:
            v=ob.data.vertices[ob.data.loops[li].vertex_index].co.normalized();coords.append(((math.atan2(v.y,v.x)/math.tau)%1,math.asin(v.z)/math.pi+.5))
        seam=max(c[0] for c in coords)-min(c[0] for c in coords)>.5
        for li,(u,v) in zip(p.loop_indices,coords):uv.data[li].uv=(.505+(u+1 if seam and u<.5 else u)*.485,.005+v*.485)
    ring('Meridian brass cradle',(0,0,.36),.245,.245,.009,'Brass','XZ')

def book(blue=False):
    box('Deckled paper block',(0,0,.033),(.283,.405,.052),'Paper',.007)
    for z in (.006,.065):box('Worn cloth cover',(0,0,z),(.32,.44,.012),'BookBlue' if blue else 'BurgundyFabric',.005)
    box('Rounded bound spine',(-.153,0,.035),(.023,.44,.066),'BookBlue' if blue else 'BurgundyFabric',.01)
    for y in (-.16,-.10,.10,.16):box('Gilt spine rib',(-.165,y,.035),(.003,.008,.063),'Brass',.001)
    for i in range(12):box('Individual page edge',(.143,0,.012+i*.004),(.001,.391,.001),'Linen',.0002)
    for x in (-.13,.13):tube('Cover gilt border',[(x,-.18,.073),(x,.18,.073)],.0018,'Brass')

def umbrella():
    lathe('Folded umbrella cloth',[(0,0),(.017,.02),(.065,.17),(.055,.73),(.014,.78),(0,.78)],'BookBlue',n=64,flutes=.13)
    tube('Curved wood grip',[(0,0,.74),(0,0,1.01),(.02,0,1.05),(.055,0,1.05),(.08,0,1.02),(.08,0,.99)],.012,'Walnut')
    ring('Folded umbrella strap',(0,0,.50),.058,.058,.009,'Leather')

for name,fn in [('Television',television),('Radio',radio),('Stove',stove),('Fireplace',fireplace),('Telephone',telephone),('Globe',globe),('BookRedDetail',book),('BookBlueDetail',lambda:book(True)),('Umbrella',umbrella)]:export('Appliances',name,fn)
finish('Appliances')
