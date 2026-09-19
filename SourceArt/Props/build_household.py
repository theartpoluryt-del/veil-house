"""Small lived-in details, saved independently from the existing object categories."""
import sys,math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).parent))
from propkit import *
start()
def remote():
    box('Rounded remote shell',(0,0,.017),(.052,.17,.034),'Rubber',.016)
    for j in range(5):
        for i in range(3):ellipsoid('Recessed rubber key',((i-1)*.013,(j-2)*.023,.036),(.008,.012,.004),'Iron')
    ellipsoid('Red power key',(0,.066,.037),(.012,.012,.006),'BookRed')
def newspaper():
    for k in range(4):
        ob=box('Folded newspaper sheet',(0,0,k*.001),(.34,.45,.001),'Paper',.0003);ob.rotation_euler.z=k*.006
    # Original raster newspaper, horizontal planar UV.
    verts=[];faces=[];uv=[];nx=40;ny=24
    for j in range(ny+1):
        for i in range(nx+1):
            verts.append(((i/nx-.5)*.34,(j/ny-.5)*.45,.006+.002*math.sin(i*.16+j*.21)+.001*math.sin(j*.3+i*.19)))
            uv.append((i/nx,j/ny))
    for j in range(ny):
        for i in range(nx):
            k=j*(nx+1)+i;faces.append((k,k+1,k+nx+2,k+nx+1))
    ob=mesh('Printed news',verts,faces,'Newsprint',True,uv)
    bpy.context.view_layer.objects.active=ob
    m=ob.modifiers.new('Printed paper thickness','SOLIDIFY');m.thickness=.0005;bpy.ops.object.modifier_apply(modifier=m.name)
def napkins():
    for k in range(5):
        ob=cloth('Linen napkin',(0,0,k*.002),.19,.19,'Linen');ob.rotation_euler.z=k*.07
def tumbler():
    lathe('Thick glass rim and open cup',[(0,0),(.038,0),(.041,.008),(.045,.12),(.042,.124),(.038,.12),(.034,.012),(0,.012)],'GlassPatina',n=64,flutes=.022)
def charger():
    box('Adapter',(0,0,.017),(.046,.06,.035),'Enamel',.008)
    for x in (-.008,.008):tube('Contact pin',[(x,.025,.017),(x,.048,.017)],.002,'Chrome')
    pts=[(.015+(.09+.016*i/75)*math.cos(i*.21),-.09+(.05+.003*i/75)*math.sin(i*.21),.006) for i in range(76)]
    tube('Loosely coiled charging lead',[(0,-.03,.013)]+pts,.0025,'Rubber')
    box('Cable connector',pts[-1],(.014,.027,.007),'Rubber',.002)
def cardboard():
    box('Cardboard base',(0,0,.012),(.35,.27,.024),'Paper',.003)
    for x in (-.173,.173):box('Box side',(x,0,.135),(.008,.27,.25),'Paper',.002)
    for y in (-.132,.132):box('Box end',(0,y,.135),(.35,.008,.25),'Paper',.002)
    for s in (-1,1):
        ob=box('Bent open flap',(s*.22,0,.266),(.10,.27,.006),'Paper',.002);ob.rotation_euler.y=s*.40
    box('Old tape strip',(0,-.139,.12),(.035,.002,.21),'Enamel',.0002)
def shoes():
    for x,y,r in [(-.075,0,-.16),(.075,.045,.19)]:
        start_index=len(parts)
        ellipsoid('Worn rubber sole',(x,y,.024),(.12,.29,.046),'Rubber')
        ellipsoid('Scuffed leather upper',(x,y-.035,.062),(.115,.23,.074),'Leather')
        lathe('Open heel collar',[(.046,.035),(.05,.083),(.04,.11),(.032,.108),(.039,.076)],'Leather',loc=(x,y+.08,0),scale=(1,.8),n=32)
        for k in range(4):tube('Crossed lace',[(x-.028,y+k*.019-.03,.101),(x+.028,y+(k+1)*.019-.03,.098)],.0018,'Linen')
        for ob in parts[start_index:]:ob.rotation_euler.z=r
def socket():
    box('Bevelled ceramic plate',(0,0,0),(.135,.013,.095),'Enamel',.007)
    for x in (-.036,.036):
        ellipsoid('Socket recess',(x,-.009,0),(.041,.005,.043),'Porcelain')
        for dx in (-.009,.009):ellipsoid('Socket contact',(x+dx,-.013,0),(.006,.003,.010),'Iron')
    for x in (-.055,.055):ellipsoid('Screw',(x,-.010,0),(.005,.003,.005),'Chrome')
def switch():
    box('Light switch plate',(0,0,0),(.085,.014,.11),'Enamel',.006)
    box('Rocker key',(0,-.012,0),(.051,.016,.068),'Porcelain',.004)
def throw():
    # Draped over the sofa seat and front edge; irregular hem and transverse folds.
    verts=[];faces=[];nx=56;ny=48
    for j in range(ny+1):
        t=j/ny
        for i in range(nx+1):
            x=(i/nx-.5)*.86;y=.38-t*.95
            z=.010*math.sin(i*.43+j*.21)+.018*math.sin(i*.71+j*.035)+(.032*math.sin(t*math.pi))+.008*math.sin(j*.32+i*.10)
            if t>.68:z-=.50*((t-.68)/.32)**.8;y=-.27-(t-.68)*.12
            verts.append((x+.025*math.sin(j*.18),y,z))
    for j in range(ny):
        for i in range(nx):
            k=j*(nx+1)+i;faces.append((k,k+1,k+nx+2,k+nx+1))
    ob=mesh('Rumpled wool throw',verts,faces,'Towel');bpy.context.view_layer.objects.active=ob
    m=ob.modifiers.new('Woven thickness','SOLIDIFY');m.thickness=.004;bpy.ops.object.modifier_apply(modifier=m.name)
    for i in range(28):
        x=(i/27-.5)*.84;tube('Loose throw fringe',[(x,-.312,-.49),(x+.008,-.315,-.555)],.0015,'Linen')
def pillow():
    ob=ellipsoid('Crushed linen cushion',(0,0,.13),(.48,.43,.25),'Linen')
    for v in ob.data.vertices:
        q=v.co;dent=.045*math.exp(-((q.x-.03)**2+(q.y+.02)**2)/.018)
        if q.z>0:q.z-=dent
        q.z+=.010*math.sin(q.x*64+q.y*13)*math.exp(-abs(q.x)*5)
    ring('Stitched pillow edge',(0,0,.12),.233,.206,.0022,'Towel')
def radiator():
    for x in [i*.077-.385 for i in range(11)]:
        box('Cast iron radiator section',(x,0,.37),(.060,.12,.64),'Enamel',.024)
    for z in (.13,.62):tube('Radiator manifold',[(-.45,0,z),(.45,0,z)],.025,'Iron')
    for x in (-.31,.31):box('Cast foot',(x,0,.052),(.055,.19,.10),'Iron',.012)
    tube('Valve pipe',[(.46,0,.60),(.54,0,.60),(.54,0,.73)],.012,'Copper')
    ring('Valve wheel',(.54,0,.74),.033,.033,.006,'Iron')
def vent():
    box('Vent plate',(0,0,0),(.30,.025,.17),'Enamel',.008)
    for z in [i*.022-.065 for i in range(7)]:box('Dark ventilation slot',(0,-.017,z),(.255,.006,.009),'Iron',.002)
def threshold():box('Worn oak threshold',(0,0,.012),(1.58,.25,.024),'Walnut',.009)
def floorlamp():
    lathe('Weighted lamp foot',[(0,0),(.16,0),(.18,.018),(.14,.06),(0,.07)],'Iron')
    tube('Floor lamp stem',[(0,0,.05),(0,0,1.37)],.012,'Brass')
    lathe('Pleated fabric shade',[(.25,1.18),(.16,1.62),(.153,1.62),(.24,1.18)],'Linen',n=64,flutes=.04)
    ellipsoid('Warm bulb',(0,0,1.30),(.075,.075,.13),'Glow')
    tube('Pull switch cord',[(.08,0,1.32),(.08,0,1.02)],.0018,'Iron')
    ellipsoid('Pull switch grip',(.08,0,1.01),(.015,.015,.034),'Walnut')
def powerstrip():
    box('Extension block',(0,0,.017),(.055,.25,.034),'Enamel',.008)
    for y in (-.085,0,.085):
        for x in (-.009,.009):box('Plug socket',(x,y,.035),(.005,.012,.002),'Iron',.001)
def pencil():
    tube('Hexagonal pencil',[(0,-.08,.006),(0,.07,.006)],.003,'Walnut')
    tube('Graphite tip',[(0,.07,.006),(0,.084,.006)],.0014,'Iron')
def trim():
    profile=[(0,0),(.036,0),(.036,.025),(.026,.038),(.026,.12),(.043,.133),(.042,.148),(0,.155)]
    verts=[(x,y,z) for x in (-.5,.5) for y,z in profile];n=len(profile)
    faces=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh('Profiled timber skirting',verts,faces,'Walnut',False)
for name,fn in [('Remote',remote),('Newspaper',newspaper),('Napkins',napkins),('Tumbler',tumbler),('Charger',charger),('CardboardBox',cardboard),('Shoes',shoes),('Socket',socket),('WallSwitch',switch),('Throw',throw),('CrumpledPillow',pillow),('Radiator',radiator),('Vent',vent),('Threshold',threshold),('FloorLamp',floorlamp),('PowerStrip',powerstrip),('Pencil',pencil),('TrimSegment',trim)]:export('Household',name,fn)
finish('Household')
