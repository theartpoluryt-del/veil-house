import sys,math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).parent))
from propkit import *
start()

def panel(name,x,z,w,h,y=-.045,mat='SmokedOak'):
    box(name,(x,y+.012,z),(w,.03,h),mat,.014)
    frame('Ogee panel moulding',(x,y-.017,z),w+.025,h+.025,mat,.021)

def door():
    for x in (-.65,.65):box('Vertical door stile',(x,0,1.32),(.16,.09,2.64),'SmokedOak',.013)
    for z in (.10,.91,1.81,2.55):box('Mortised door rail',(0,0,z),(1.30,.09,.17),'SmokedOak',.012)
    for z,h in ((.49,.61),(1.36,.70),(2.17,.55)):
        panel('Inset oak panel',0,z,1.15,h)
        panel('Inset reverse panel',0,z,1.15,h,.045)
    for y in (-.08,.08):
        box('Ornate lock plate',(.53,y,1.14),(.09,.028,.26),'Brass',.025)
        tube('Lever handle',[(.53,y*1.4,1.2),(.53,y*2,1.2),(.39,y*2,1.21)],.014,'Brass')
        ellipsoid('Keyhole',(.53,y*1.19,1.075),(.017,.006,.029),'Iron')
    for z in (.32,1.30,2.31):
        lathe('Hinge barrel',[(0,z),(.018,z),(.021,z+.10),(0,z+.10)],'Brass',loc=(-.727,0,0),n=20)
        for x in (-.70,-.65):ellipsoid('Hinge screw',(x,-.05,z+.04),(.01,.008,.01),'Iron')

def casing():
    for x in (-.83,.83):
        for off,w,d in ((0,.15,.20),(.03,.05,.27),(-.04,.026,.30)):
            box('Profiled architrave',(x+off,0,1.40),(w,d,2.80),'Enamel',.01)
        box('Plinth block',(x,0,.12),(.22,.33,.24),'Enamel',.015)
        box('Corner rosette block',(x,0,2.75),(.22,.33,.22),'Enamel',.014)
        ring('Carved rosette',(x,-.17,2.75),.055,.055,.009,'Enamel','XZ')
    for z,w,d in ((2.78,1.90,.29),(2.89,2.02,.32)):box('Lintel cornice',(0,0,z),(w,d,.10),'Enamel',.01)

def window():
    w=2.6
    for x in (-w/2,w/2):box('Moulded window jamb',(x,0,0),(.16,.17,1.95),'Enamel',.013)
    for z in (-.94,.94):box('Window header',(0,0,z),(w+.16,.17,.14),'Enamel',.012)
    box('Rounded deep sill',(0,-.12,-.95),(w+.33,.43,.12),'Enamel',.022)
    for j in range(2):
        for i in range(4):
            x=-w/2+(i+.5)*w/4;z=-.85+(j+.5)*.85
            box('Individual leaded glass pane',(x,.02,z),(w/4-.048,.025,.80),'GlassPatina',.003)
    for i in range(1,4):box('Sash mullion',(-w/2+i*w/4,-.028,0),(.039,.06,1.76),'Walnut',.007)
    box('Sash meeting rail',(0,-.04,0),(w,.08,.057),'Walnut',.008)
    tube('Sash latch',[(-.06,-.09,.03),(.07,-.09,.03),(.09,-.09,.08)],.009,'Brass')
    tube('Curtain pole',[(-1.89,-.19,1.14),(1.89,-.19,1.14)],.022,'Brass')
    for s in (-1,1):
        ellipsoid('Pole finial',(s*1.93,-.19,1.14),(.13,.07,.07),'Brass')
        vv=[];ff=[]
        for j in range(33):
            t=j/32;gather=.6+.4*abs(t-.64)/.64
            for i in range(33):
                u=i/32;x=s*(1.37+(u-.5)*.66*gather)
                y=-.20-.057*math.cos(u*math.tau*7)*(1+.3*t);z=1.05-2.09*t+.018*math.sin(u*17)*t*t
                vv.append((x,y,z))
        for j in range(32):
            for i in range(32):k=j*33+i;ff.append((k,k+1,k+34,k+33))
        ob=mesh('Gathered velvet curtain',vv,ff,'BurgundyFabric');bpy.context.view_layer.objects.active=ob;m=ob.modifiers.new('Velvet thickness','SOLIDIFY');m.thickness=.006;bpy.ops.object.modifier_apply(modifier=m.name)
        tube('Braided curtain tie',[(s*1.15,-.255,-.28),(s*1.4,-.30,-.34),(s*1.59,-.24,-.29)],.015,'Brass')

def cabinet(style,leaf=False):
    mat='Enamel' if style=='Kitchen' else 'MotorPaint' if style=='Tool' else 'Walnut'
    h=1.8 if style=='Wardrobe' else 1.2;w=1.4;d=.56
    if leaf:
        box('Framed opening cabinet leaf',(0,0,h/2),(w-.035,.065,h-.07),mat,.014)
        for x in (-w*.24,w*.24):panel('Recessed cabinet panel',x,h/2,w*.40,h-.29,-.042,mat)
        for x in (-.055,.055):
            tube('Drop handle',[(x,-.087,h*.54),(x-.023,-.10,h*.51),(x,-.11,h*.46),(x+.023,-.10,h*.51),(x,-.087,h*.54)],.005,'Brass')
        if style=='Tool':
            for j in range(7):box('Louvered ventilation',(-.30,-.081,.20+j*.043),(.37,.012,.018),'Iron',.003)
        return
    for x in (-w/2,w/2):box('Cabinet solid side',(x,0,h/2),(.09,d,h),mat,.012)
    box('Tongue and groove back',(0,d/2-.03,h/2),(w,.05,h-.08),mat,.008)
    for z in (.07,h-.04):box('Carcass horizontal',(0,0,z),(w,d,.10),mat,.012)
    for z in (.15,h+.018):box('Profiled cabinet cornice',(0,0,z),(w+.14,d+.11,.075),mat,.018)
    for i in range(1,4):box('Interior shelf',(0,0,h*i/4),(w-.1,d-.06,.04),'SmokedOak',.006)
    for x in (-.52,.52):
        for y in (-.19,.19):lathe('Turned cabinet foot',[(0,0),(.05,0),(.055,.045),(.035,.11),(.044,.17),(0,.17)],mat,loc=(x,y,0),n=24)
    if style=='Wardrobe':
        for i in range(3):cloth('Folded clothes inside',(0,-.015,.48+i*.042),.57,.38,'Linen')

def bed(teal=False):
    box('Bed frame',(0,0,.32),(1.88,2.5,.21),'Walnut',.025)
    box('Piped sprung mattress',(0,0,.55),(1.85,2.47,.29),'Linen',.10)
    cloth('Draped and creased cover',(0,-.28,.73),2.08,1.94,'Leather' if teal else 'BurgundyFabric',.24)
    for x in (-.46,.46):
        ob=box('Soft pillow',(x,.89,.77),(.77,.48,.19),'Linen',.085);ob.rotation_euler.z=x*.10
    for y in (-1.3,1.3):
        h=1.34 if y>0 else .89
        for x in (-.98,.98):
            lathe('Turned brass bedpost',[(0,.05),(.032,.05),(.025,h-.10),(.045,h-.06),(.035,h),(0,h)],'Brass',loc=(x,y,0),n=24)
        tube('Arched brass head rail',[(x,y,h-.11+.10*math.cos(x*1.6)) for x in [-.98+i*.049 for i in range(41)]],.021)
        for i in range(-3,4):tube('Decorated spindle',[(i*.24,y,.38),(i*.24,y,h-.09)],.011)

for name,fn in [('PanelDoor',door),('DoorCasing',casing),('SashWindow',window),('BrassBed',bed),('GuestBed',lambda:bed(True))]:export('Joinery',name,fn)
for style in ('Kitchen','Wardrobe','Tool','Document'):
    export('Joinery',style+'Body',lambda s=style:cabinet(s))
    export('Joinery',style+'Leaf',lambda s=style:cabinet(s,True))
finish('Joinery')
