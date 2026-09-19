from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import random,textwrap
root=Path(__file__).resolve().parents[2]
font=root/'Assets/Resources/Fonts/Editorial.ttf'
if not font.exists():font=next((root/'Assets').rglob('Editorial.ttf'))
r=random.Random(42);im=Image.new('RGB',(1024,1024),(188,179,151));d=ImageDraw.Draw(im)
for _ in range(45000):
    x=r.randrange(1024);y=r.randrange(1024);c=r.randrange(157,207);d.point((x,y),fill=(c,c-6,c-20))
def f(size):return ImageFont.truetype(str(font),size)
d.text((70,28),'THE EVENING HERALD',font=f(57),fill=(34,37,34))
d.line((48,104,975,104),fill=(50,48,40),width=5)
d.text((50,120),'LOCAL EDITION       FRIDAY, OCTOBER 21       TWO PENCE',font=f(21),fill=(47,45,39))
d.text((50,174),'A QUIET HOUSE, A LONG NIGHT',font=f(37),fill=(37,37,31))
copy='The last visitors left before the rain. At the station a parcel waited for collection, tied with a blue ribbon. No one remembered who had delivered it. The household asked that the garden gate be kept shut until the morning. In the kitchen the clock continued to strike, although its hands had stopped.'
for col in range(3):
    x=50+col*316
    if col:d.line((x-15,242,x-15,960),fill=(109,101,85),width=2)
    lines=textwrap.wrap(copy,27)*3
    for j,line in enumerate(lines[:31]):d.text((x,248+j*23),line,font=f(17),fill=(57,53,44))
out=root/'Assets/Resources/ObjectArt/Newsprint.png';im.save(out)
print('NEWSPRINT_SAVED',out)
