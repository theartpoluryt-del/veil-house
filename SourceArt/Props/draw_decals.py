"""Original typography, instrument dials and schematic globe cartography."""
from PIL import Image,ImageDraw,ImageFont,ImageFilter
from pathlib import Path
import math,random
root=Path(__file__).resolve().parents[2];out=root/'Assets/Resources/ObjectArt';out.mkdir(exist_ok=True)
r=random.Random(1927)
im=Image.new('RGB',(2048,2048),(211,195,157));d=ImageDraw.Draw(im)
def font(n):return ImageFont.truetype('C:/Windows/Fonts/times.ttf',n)
def txt(x,y,s,n=42,fill=(46,40,30)):d.text((x,y),s,font=font(n),fill=fill,anchor='mm')
for i in range(95000):
    x=r.randrange(2048);y=r.randrange(2048);v=r.randrange(-15,12);d.point((x,y),fill=(211+v,195+v,157+v))
cx=cy=512
for rad,w in [(470,5),(451,2),(393,2),(285,3)]:d.ellipse((cx-rad,cy-rad,cx+rad,cy+rad),outline=(55,49,33),width=w)
for i in range(60):
    a=i*math.tau/60-math.pi/2;ro=443;ri=415 if i%5 else 399
    d.line([(cx+math.cos(a)*ri,cy+math.sin(a)*ri),(cx+math.cos(a)*ro,cy+math.sin(a)*ro)],fill=(31,29,21),width=4 if i%5 else 7)
for i,t in enumerate(['XII','I','II','III','IV','V','VI','VII','VIII','IX','X','XI']):
    a=i*math.tau/12-math.pi/2;txt(cx+math.cos(a)*348,cy+math.sin(a)*348,t,65)
txt(512,455,'VEIL HOUSE',43);txt(512,590,'LONDON  •  1894',24)
labels=['DARJEELING','APOTHECARY','LAIT FRAIS','MACHINE OIL']
for j,s in enumerate(labels):
    y=j*256;d.rounded_rectangle((1050,y+14,2021,y+242),radius=16,outline=(69,57,34),width=7)
    d.rectangle((1065,y+28,2006,y+228),outline=(104,76,42),width=2)
    txt(1536,y+65,'VEIL & SONS · EST. 1894',28);txt(1536,y+126,s,64);txt(1536,y+195,'FINE QUALITY  •  ORIGINAL BLEND',24)
d.rectangle((0,1024,1023,1536),fill=(38,34,27))
for i in range(51):
    x=65+i*18;d.line((x,1170,x,1230 if i%5 else 1260),fill=(209,171,93),width=3)
    if i%5==0:txt(x,1300,str(550+i*20),24,(224,200,141))
txt(512,1110,'VALVE RECEIVER',47,(224,200,141));txt(512,1440,'LONG WAVE     MEDIUM WAVE',28,(224,200,141))
for i in range(10):txt(60+i*99,1700,str(i),57)
txt(512,1850,'VEIL   MOTOR WORKS',49);txt(512,1940,'1927 · TOURING',38)
# Deliberately schematic hand-drawn antique map; no real-time geographic dataset.
d.rectangle((1024,1024,2047,2047),fill=(72,108,111))
polys=[[(.07,.16),(.18,.08),(.29,.17),(.26,.27),(.18,.37),(.13,.30)],[(.23,.38),(.32,.45),(.34,.58),(.28,.80),(.24,.62)],[(.44,.19),(.53,.12),(.63,.18),(.67,.30),(.55,.34),(.47,.28)],[(.46,.32),(.56,.33),(.59,.46),(.53,.65),(.47,.54),(.43,.40)],[(.59,.13),(.78,.10),(.91,.19),(.82,.31),(.76,.36),(.68,.29),(.64,.41),(.58,.29)],[(.79,.58),(.89,.56),(.93,.68),(.84,.73),(.78,.66)],[(.05,.90),(.31,.88),(.55,.91),(.81,.88),(.99,.92),(.99,.96),(.04,.96)]]
for p in polys:
    pts=[(1024+x*1024,1024+y*1024) for x,y in p];d.polygon(pts,fill=(170,155,107));d.line(pts+[pts[0]],fill=(215,190,129),width=3)
for x in range(1024,2048,85):d.line((x,1024,x,2047),fill=(117,143,134),width=1)
for y in range(1024,2048,85):d.line((1024,y,2047,y),fill=(117,143,134),width=1)
im.save(out/'Decals.png')
