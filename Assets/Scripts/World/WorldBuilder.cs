using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VeilHouse {
    /// <summary>Original, self-contained manor set. All geometry and textile/wood surfaces are authored in code.</summary>
    public static class WorldBuilder {
        static Transform root;
        static Material oak,darkOak,parquet,cream,teal,trim,brass,black,burgundy,linen,glass,ceramic,iron,leather,tile,wallpaper,rugMat,water,screen,fire,green,bookRed,bookBlue,paper,lampGlass;
        static Dictionary<Material,Vector2> surfaceMeters;
        static System.Random random;
        static int propCount;
        static List<UnityEngine.Object> ownedAssets;
        public static int PhysicalPropCount {get{return propCount;}}
        public static Vector3 SpawnPoint(int index) {return new Vector3(index%2==0?-.72f:.72f,.18f,-8.4f+(index/2)*1.5f);}
        public static string RoomAt(Vector3 p) {
            if(Mathf.Abs(p.x)<2.05f)return "Галерея";
            if(p.x<0)return p.z<-2?"Гостиная":p.z<4?"Кухня":"Гараж";
            if(p.z<-4)return "Кабинет";
            if(p.z>4)return "Спальня Элеоноры";
            return p.x<7?"Спальня хозяина":"Ванная";
        }
        public static GameObject Build() {
            HauntedObject.ResetRegistry();DetailedObjects.Reset();propCount=0;random=new System.Random(1927);ownedAssets=new List<UnityEngine.Object>();surfaceMeters=new Dictionary<Material,Vector2>();
            root=new GameObject("VEIL HOUSE · Дом по ту сторону").transform;
            MakeMaterials();Atmosphere();Architecture();LivingRoom();Kitchen();Garage();Study();Bedroom();Bath();GuestBedroom();Hall();Garden();
            DetailedObjects.Apply(root);
            OptimizeGeometry();root.gameObject.AddComponent<ProceduralWorldAssets>().Owned=ownedAssets.ToArray();
            return root.gameObject;
        }
        static void MakeMaterials() {
            oak=Surface("SmokedOak",1,2);darkOak=Surface("SmokedOak",1,2,new Color(.57f,.49f,.40f));
            parquet=Surface("Parquet",1.2f,2.4f);
            cream=Surface("Plaster",1,1);teal=Surface("TealPaint",1,1);
            trim=Surface("Enamel",.5f,.5f);
            brass=Surface("Brass",.25f,.25f);
            black=Surface("Rubber",.5f,.5f);
            burgundy=Surface("BurgundyFabric",.25f,.25f);linen=Surface("Linen",.25f,.25f);
            leather=Surface("Leather",.5f,.5f);
            glass=Mat("Moonlit leaded glass",new Color(.15f,.28f,.34f),.86f,.22f);glass.EnableKeyword("_EMISSION");glass.SetColor("_EmissionColor",new Color(.13f,.24f,.31f)*.7f);
            ceramic=Surface("Porcelain",.5f,.5f);
            iron=Surface("Iron",.5f,.5f);
            tile=Surface("Tile",.8f,.8f);wallpaper=Surface("Wallpaper",.6f,1.2f);
            rugMat=Surface("PersianRug",3,5);surfaceMeters.Remove(rugMat); // Whole carpet layout, once per rug.
            water=Mat("Falling water",new Color(.3f,.62f,.67f),.9f,.2f);water.EnableKeyword("_EMISSION");water.SetColor("_EmissionColor",new Color(.04f,.12f,.16f));
            screen=Mat("Television phosphor",new Color(.58f,.72f,.63f),.35f);screen.mainTexture=Texture("screen",64);screen.EnableKeyword("_EMISSION");screen.SetColor("_EmissionColor",new Color(.35f,.52f,.40f)*1.5f);
            fire=Mat("Living embers",new Color(.9f,.3f,.07f),.2f);fire.EnableKeyword("_EMISSION");fire.SetColor("_EmissionColor",new Color(1,.18f,.025f)*2);
            lampGlass=Mat("Illuminated opal and silk",new Color(.94f,.83f,.61f),.18f);lampGlass.EnableKeyword("_EMISSION");lampGlass.SetColor("_EmissionColor",new Color(1,.73f,.38f)*1.05f);
            green=Mat("Broadleaf",new Color(.10f,.24f,.13f),.1f);
            bookRed=Mat("Clothbound sienna",new Color(.49f,.18f,.12f),.1f);bookBlue=Mat("Clothbound navy",new Color(.12f,.23f,.28f),.1f);paper=Mat("Old paper",new Color(.79f,.74f,.59f),.02f);
        }
        static Material Surface(string id,float width,float height,Color? tint=null) {
            var source=Resources.Load<Material>("Materials/"+id);
            if(!source)throw new InvalidOperationException("Missing surface "+id+". Run Veil House/Refresh surface materials.");
            var mat=new Material(source);mat.name=id;mat.color=tint??Color.white;
            ownedAssets.Add(mat);surfaceMeters.Add(mat,new Vector2(width,height));return mat;
        }
        static Material Mat(string name,Color color,float smooth=0,float metal=0) {var m=new Material(Shader.Find("Standard"));m.name=name;m.color=color;m.SetFloat("_Glossiness",smooth);m.SetFloat("_Metallic",metal);ownedAssets.Add(m);return m;}
        static Texture2D Texture(string kind,int size) {
            var t=new Texture2D(size,size,TextureFormat.RGB24,true);t.name="Authored "+kind;t.wrapMode=TextureWrapMode.Repeat;var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
                float u=(float)x/size,v=(float)y/size,n=Mathf.PerlinNoise(x*.13f+27,y*.13f+13);Color c=Color.white;
                if(kind=="wood") {float grain=.80f+.07f*Mathf.Sin(v*210+Mathf.Sin(u*13)*2)+.08f*Mathf.PerlinNoise(x*.02f,y*.34f);int row=(int)(u*8);float seam=Mathf.Repeat(u*8,1)<.024f?.5f:1;float end=Mathf.Repeat(v*3+(row%3)*.32f,1)<.014f?.6f:1;c=new Color(grain,grain*.97f,grain*.90f)*seam*end;}
                if(kind=="plaster")c=Color.white*(.86f+n*.14f);
                if(kind=="fabric")c=Color.white*(.72f+n*.16f+((x+y)%2)*.10f);
                if(kind=="screen") {float b=.45f+n*.3f+(y%4==0?.2f:0);c=new Color(b*.72f,b,b*.83f);}
                if(kind=="tile") {bool dark=((int)(u*8)+(int)(v*8))%2==0;c=dark?new Color(.28f,.38f,.36f):new Color(.72f,.72f,.63f);if(Mathf.Repeat(u*8,1)<.035||Mathf.Repeat(v*8,1)<.035)c=new Color(.31f,.32f,.28f);}
                if(kind=="wallpaper") {float stem=Mathf.Sin(v*22+Mathf.Sin(u*25))*Mathf.Sin(u*25);c=Color.white*(stem>.6f?.76f:.96f);}
                if(kind=="rug") {
                    float border=Mathf.Min(Mathf.Min(u,1-u),Mathf.Min(v,1-v));
                    float med=Mathf.Abs(u-.5f)+Mathf.Abs(v-.5f)*.65f;
                    c=new Color(.26f,.105f,.11f);
                    if(border<.045f)c=new Color(.61f,.40f,.22f);else if(border<.08f)c=new Color(.14f,.23f,.23f);else if(border<.13f)c=new Color(.62f,.43f,.27f)*(Mathf.Sin(u*190)*Mathf.Sin(v*190)>.2f?.65f:1);
                    else if(med<.23f)c=new Color(.54f,.35f,.21f);else if(med<.26f)c=new Color(.10f,.25f,.27f);
                    if(border>.15f&&Mathf.Sin(u*70)*Mathf.Sin(v*80)>.72f)c=new Color(.72f,.54f,.3f);
                    c*=.8f+n*.2f;
                }
                pixels[y*size+x]=c;
            }
            t.SetPixels(pixels);t.Apply();ownedAssets.Add(t);return t;
        }
        static GameObject Group(string name,Vector3 position,Transform parent=null,float yaw=0) {var o=new GameObject(name);o.transform.SetParent(parent?parent:root,false);o.transform.localPosition=position;o.transform.localRotation=Quaternion.Euler(0,yaw,0);return o;}
        static GameObject Part(string name,PrimitiveType shape,Vector3 position,Vector3 scale,Material mat,Transform parent=null,bool collider=false,Vector3? rotation=null) {
            var o=GameObject.CreatePrimitive(shape);o.name=name;o.transform.SetParent(parent?parent:root,false);o.transform.localPosition=position;o.transform.localScale=scale;if(rotation.HasValue)o.transform.localEulerAngles=rotation.Value;
            o.GetComponent<Renderer>().sharedMaterial=mat;
            if(shape==PrimitiveType.Cube&&surfaceMeters.TryGetValue(mat,out var meters)) {
                // Project UVs in metres before combining meshes; all PBR channels share the same UVs.
                var mesh=UnityEngine.Object.Instantiate(o.GetComponent<MeshFilter>().sharedMesh);
                mesh.name=name+" metric UV";var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
                for(int i=0;i<vertices.Length;i++) {
                    var p=Vector3.Scale(vertices[i],scale);var n=normals[i];Vector2 coord;
                    if(Mathf.Abs(n.y)>.5f)coord=new Vector2(p.x,p.z);
                    else if(Mathf.Abs(n.x)>.5f)coord=new Vector2(p.z,p.y);
                    else coord=new Vector2(p.x,p.y);
                    if((mat==oak||mat==darkOak)&&Mathf.Abs(n.y)<.5f&&((Mathf.Abs(n.x)>.5f?scale.z:scale.x)>scale.y))coord=new Vector2(coord.y,coord.x);
                    uv[i]=new Vector2(coord.x/meters.x,coord.y/meters.y);
                }
                mesh.uv=uv;mesh.RecalculateTangents();ownedAssets.Add(mesh);o.GetComponent<MeshFilter>().sharedMesh=mesh;
            }
            if(!collider){var c=o.GetComponent<Collider>();if(c){c.enabled=false;UnityEngine.Object.Destroy(c);}}
            return o;
        }
        static GameObject Box(string n,Vector3 p,Vector3 s,Material m,Transform parent=null,bool collide=false) {return Part(n,PrimitiveType.Cube,p,s,m,parent,collide);}
        static GameObject Ball(string n,Vector3 p,Vector3 s,Material m,Transform parent=null) {return Part(n,PrimitiveType.Sphere,p,s,m,parent);}
        static GameObject Cylinder(string n,Vector3 p,Vector3 s,Material m,Transform parent=null) {return Part(n,PrimitiveType.Cylinder,p,s,m,parent);}
        static void ColliderBox(Transform t,Vector3 center,Vector3 size) {var c=t.gameObject.AddComponent<BoxCollider>();c.center=center;c.size=size;}
        static Light Point(string name,Vector3 position,Color color,float intensity,float range,Transform parent=null) {
            var obj=Group(name,position,parent);var l=obj.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;l.renderMode=LightRenderMode.ForcePixel;return l;
        }
        static void Atmosphere() {
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.35f,.39f,.44f);RenderSettings.ambientEquatorColor=new Color(.25f,.28f,.29f);RenderSettings.ambientGroundColor=new Color(.15f,.14f,.12f);
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.065f,.10f,.135f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.012f;
            var obj=Group("Cold moonlight",Vector3.zero);obj.transform.rotation=Quaternion.Euler(45,-30,0);var sun=obj.AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(.47f,.62f,.78f);sun.intensity=.42f;sun.shadows=LightShadows.Soft;sun.shadowStrength=.7f;RenderSettings.sun=sun;
        }
        static void Architecture() {
            Box("Manor foundation",new Vector3(0,-.24f,0),new Vector3(24.5f,.45f,22.5f),darkOak,null,true);
            Floor("Gallery",0,0,4,22,parquet);Floor("Drawing room",-7,-6.5f,10,9,parquet);Floor("Kitchen",-7,1,10,6,tile);Floor("Motor house",-7,7.5f,10,7,cream);Floor("Study",7,-7.5f,10,7,parquet);Floor("Master bedroom",4.5f,0,5,8,parquet);Floor("Bathroom",9.5f,0,5,8,tile);Floor("Guest bedroom",7,7.5f,10,7,parquet);
            WallZ(-11,-12,12,cream);WallZ(11,-12,12,cream);WallX(-12,-11,11,cream);WallX(12,-11,11,cream);
            WallXDoors(-2,-11,11,new float[]{-6.5f,1,7.5f},teal);WallXDoors(2,-11,11,new float[]{-7.5f,0,7.5f},teal);
            WallZ(-2,-12,-2,wallpaper);WallZ(4,-12,-2,cream);WallZ(-4,2,12,wallpaper);WallZ(4,2,12,cream);WallXDoors(7,-4,4,new float[]{0},wallpaper);
            Box("Ceiling",new Vector3(0,3.6f,0),new Vector3(24.2f,.17f,22.2f),cream,null,true);
            for(int i=0;i<3;i++){Window(new Vector3(-11.86f,1.95f,-8+i*7.4f),90,2.6f);Window(new Vector3(11.86f,1.95f,-7.5f+i*7.4f),-90,2.6f);}
            Window(new Vector3(-6.6f,1.95f,-10.86f),0,3.5f);Window(new Vector3(6.6f,1.95f,-10.86f),0,3.2f);Window(new Vector3(7,1.95f,10.86f),180,3.2f);
            Door(new Vector3(-2,0,-6.5f),90,"Дверь гостиной",true);Door(new Vector3(-2,0,1),90,"Дверь кухни",true);Door(new Vector3(-2,0,7.5f),90,"Дверь гаража",true);
            Door(new Vector3(2,0,-7.5f),90,"Дверь кабинета",true);Door(new Vector3(2,0,0),90,"Дверь спальни",true);Door(new Vector3(2,0,7.5f),90,"Дверь спальни Элеоноры",true);Door(new Vector3(7,0,0),90,"Дверь ванной",true);
            // The front entrance is a distinctive double door, decorative and sealed for the investigation.
            var entry=Group("Oak entrance",new Vector3(0,0,-10.81f));Box("Double front door",new Vector3(0,1.4f,0),new Vector3(2.6f,2.8f,.13f),darkOak,entry.transform,true);
            foreach(float x in new float[]{-.64f,.64f}){Box("Recessed entrance panel",new Vector3(x,1.55f,.08f),new Vector3(1.0f,2.1f,.045f),oak,entry.transform);Cylinder("Brass pull",new Vector3(x*.24f,1.23f,.16f),new Vector3(.035f,.17f,.035f),brass,entry.transform);}
            for(int z=-9;z<=9;z+=6)CeilingLamp(new Vector3(0,3.38f,z),"Люстра галереи",6.7f,.72f);
        }
        static void Floor(string name,float x,float z,float w,float d,Material mat) {Box(name+" floor",new Vector3(x,.025f,z),new Vector3(w,.06f,d),mat,null,true);}
        static void WallX(float x,float a,float b,Material mat) {Wall(new Vector3(x,1.75f,(a+b)/2),new Vector3(.22f,3.5f,b-a),mat);}
        static void WallZ(float z,float a,float b,Material mat) {Wall(new Vector3((a+b)/2,1.75f,z),new Vector3(b-a,3.5f,.22f),mat);}
        static void Wall(Vector3 at,Vector3 size,Material mat) {
            Box("Plaster wall",at,size,mat,null,true);
            bool xwall=size.x<.3f;Vector3 railSize=new Vector3(xwall?.30f:size.x,.075f,xwall?size.z:.30f);
            Box("Picture rail",new Vector3(at.x,2.88f,at.z),railSize,trim);railSize.y=.18f;Box("Crown moulding",new Vector3(at.x,3.43f,at.z),railSize,trim);
            railSize.y=.15f;Box("Skirting board",new Vector3(at.x,.12f,at.z),railSize,darkOak);
            var panelSize=size;panelSize.y=.83f;if(xwall)panelSize.x=.25f;else panelSize.z=.25f;Box("Oak wainscoting",new Vector3(at.x,.56f,at.z),panelSize,darkOak);
            railSize.y=.055f;Box("Dado rail",new Vector3(at.x,.99f,at.z),railSize,oak);
            float len=xwall?size.z:size.x;for(float v=-len/2+.45f;v<len/2;v+=.9f) {
                Vector3 p=new Vector3(at.x,.56f,at.z);if(xwall)p.z+=v;else p.x+=v;
                Box("Wainscot stile",p,new Vector3(xwall?.27f:.035f,.72f,xwall?.035f:.27f),oak);
            }
        }
        static void WallXDoors(float x,float a,float b,float[] doors,Material mat) {
            float last=a;foreach(float z in doors) {if(z-.8f>last)WallX(x,last,z-.8f,mat);Box("Door lintel",new Vector3(x,3.09f,z),new Vector3(.22f,.82f,1.6f),mat,null,true);last=z+.8f;}
            if(last<b)WallX(x,last,b,mat);
        }
        static void Window(Vector3 p,float yaw,float width) {
            var g=Group("Moonlit sash window",p,null,yaw).transform;
            Box("Blue night beyond glass",Vector3.zero,new Vector3(width,1.78f,.04f),glass,g);
            foreach(float side in new float[]{-1,1})Box("Window jamb",new Vector3(side*(width/2+.06f),0,.08f),new Vector3(.13f,1.95f,.20f),trim,g);
            foreach(float v in new float[]{-.96f,.96f})Box("Window lintel",new Vector3(0,v,.07f),new Vector3(width+.26f,.13f,.20f),trim,g);
            for(int i=-1;i<=1;i++)Box("Leaded vertical bar",new Vector3(i*width/4,0,.075f),new Vector3(.042f,1.83f,.05f),darkOak,g);
            Box("Sash crossbar",new Vector3(0,0,.1f),new Vector3(width,.058f,.055f),darkOak,g);
            Box("Window sill",new Vector3(0,-.94f,.16f),new Vector3(width+.36f,.13f,.44f),trim,g);
            foreach(float side in new float[]{-1,1}) {
                for(int i=0;i<4;i++)Cylinder("Velvet curtain fold",new Vector3(side*(width/2+.15f)+i*.095f*side,-.02f,.29f),new Vector3(.19f,1.08f,.19f),burgundy,g);
                Cylinder("Curtain tie",new Vector3(side*(width/2+.29f),-.37f,.34f),new Vector3(.44f,.034f,.31f),brass,g);
            }
            var rod=Cylinder("Curtain pole",new Vector3(0,1.18f,.29f),new Vector3(.045f,(width+.9f)/2,.045f),brass,g);rod.transform.localRotation=Quaternion.Euler(0,0,90);
            Point("Window bounce",new Vector3(0,.1f,.8f),new Color(.42f,.60f,.79f),.52f,5.8f,g);
            // Frosted branch silhouettes, authored directly against the glass.
            for(int i=0;i<3;i++) {var b=Box("Distant tree branch",new Vector3(-width*.36f+i*width*.30f,-.07f,-.024f),new Vector3(.028f,1.4f,.015f),teal,g);b.transform.localRotation=Quaternion.Euler(0,0,-16+i*17);}
        }
        static void Door(Vector3 position,float yaw,string name,bool opened) {
            var g=Group(name,position,null,yaw).transform;
            foreach(float side in new float[]{-1,1})Box("Door casing",new Vector3(side*.83f,1.39f,0),new Vector3(.17f,2.78f,.35f),trim,g);
            Box("Door architrave",new Vector3(0,2.77f,0),new Vector3(1.86f,.15f,.36f),trim,g);
            var hinge=Group("Hinged oak leaf",new Vector3(-.73f,0,0),g).transform;
            Box("Solid oak door",new Vector3(.73f,1.32f,0),new Vector3(1.46f,2.64f,.095f),oak,hinge,true);
            for(int y=0;y<3;y++)foreach(float face in new float[]{-1,1})Box("Raised panel",new Vector3(.73f,.45f+y*.82f,face*.06f),new Vector3(1.17f,.64f,.04f),darkOak,hinge);
            foreach(float face in new float[]{-1,1}) {Box("Brass backplate",new Vector3(1.25f,1.15f,face*.078f),new Vector3(.10f,.27f,.035f),brass,hinge);Ball("Brass doorknob",new Vector3(1.25f,1.2f,face*.15f),new Vector3(.11f,.11f,.11f),brass,hinge);}
            var haunt=g.gameObject.AddComponent<HauntedObject>().Initialize(name,HauntKind.Door,opened);haunt.Hinge=hinge;haunt.OpenAngle=-98;hinge.localRotation=Quaternion.Euler(0,opened?-98:0,0);
        }
        static HauntedObject CeilingLamp(Vector3 p,string label,float range=7,float intensity=.85f) {
            var g=Group(label,p).transform;
            Cylinder("Ceiling rosette",Vector3.zero,new Vector3(.36f,.028f,.36f),trim,g);
            Cylinder("Pendant stem",new Vector3(0,-.22f,0),new Vector3(.032f,.22f,.032f),brass,g);
            var bulb=Ball("Opal globe",new Vector3(0,-.52f,0),new Vector3(.41f,.42f,.41f),lampGlass,g);bulb.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            Cylinder("Brass gallery",new Vector3(0,-.33f,0),new Vector3(.39f,.038f,.39f),brass,g);
            var l=Point("Warm pendant light",new Vector3(0,-.8f,0),new Color(1,.80f,.57f),intensity*1.32f,range,g);
            var h=g.gameObject.AddComponent<HauntedObject>().Initialize(label,HauntKind.Light,true);h.Lamps=new[]{l};h.EmissiveParts=new[]{g.GetChild(2).GetComponent<Renderer>()};ColliderBox(g,new Vector3(0,-.5f,0),new Vector3(.45f,.5f,.45f));
            return h;
        }
        static void TableLamp(Vector3 p,string label,Transform parent=null) {
            var g=Group(label,p,parent).transform;
            Cylinder("Lamp weighted foot",new Vector3(0,.035f,0),new Vector3(.33f,.035f,.33f),brass,g);
            Cylinder("Lamp stem",new Vector3(0,.22f,0),new Vector3(.048f,.19f,.048f),brass,g);
            var shade=LampShade(g,new Vector3(0,.48f,0));
            Cylinder("Shade bottom trim",new Vector3(0,.34f,0),new Vector3(.51f,.016f,.51f),brass,g);
            var l=Point("Amber reading pool",new Vector3(0,.45f,0),new Color(1,.72f,.39f),.85f,5,g);
            var h=g.gameObject.AddComponent<HauntedObject>().Initialize(label,HauntKind.Light,true);h.Lamps=new[]{l};h.EmissiveParts=new[]{shade.GetComponent<Renderer>()};ColliderBox(g,new Vector3(0,.25f,0),new Vector3(.5f,.55f,.5f));
        }
        static GameObject LampShade(Transform parent,Vector3 position) {
            const int sides=48;var vertices=new Vector3[sides*2];var uv=new Vector2[vertices.Length];var triangles=new int[sides*6];
            for(int i=0;i<sides;i++){float angle=i*Mathf.PI*2/sides;float fold=i%2==0?1:.95f;vertices[i*2]=new Vector3(Mathf.Cos(angle)*.245f*fold,-.14f,Mathf.Sin(angle)*.245f*fold);vertices[i*2+1]=new Vector3(Mathf.Cos(angle)*.145f*fold,.14f,Mathf.Sin(angle)*.145f*fold);uv[i*2]=new Vector2((float)i/sides,0);uv[i*2+1]=new Vector2((float)i/sides,1);int n=(i+1)%sides;int at=i*6;triangles[at]=i*2;triangles[at+1]=i*2+1;triangles[at+2]=n*2;triangles[at+3]=n*2;triangles[at+4]=i*2+1;triangles[at+5]=n*2+1;}
            var mesh=new Mesh{name="Authored pleated silk lampshade"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();ownedAssets.Add(mesh);
            var obj=Group("Pleated silk shade",position,parent);obj.AddComponent<MeshFilter>().sharedMesh=mesh;var r=obj.AddComponent<MeshRenderer>();r.sharedMaterial=lampGlass;r.shadowCastingMode=ShadowCastingMode.Off;return obj;
        }
        static void Rug(Vector3 p,float w,float d,float yaw=0) {var g=Box("Persian wool carpet",p+Vector3.up*.065f,new Vector3(w,.016f,d),rugMat);g.transform.localRotation=Quaternion.Euler(0,yaw,0);for(int end=-1;end<=1;end+=2)for(int i=0;i<(int)(w/.10f);i++)Box("Carpet fringe",p+new Vector3(-w/2+i*.1f,.068f,end*(d/2+.065f)),new Vector3(.025f,.009f,.17f),linen);}
        static void Furniture(string id,Transform parent) {
            var prefab=Resources.Load<GameObject>("Furniture/Prefabs/"+id);
            if(!prefab)throw new InvalidOperationException("Missing furniture prefab "+id);
            var model=UnityEngine.Object.Instantiate(prefab,parent,false);model.name="Authored furniture / "+id;
        }
        static Transform Table(Vector3 p,float w,float d,string name,float height=.78f,float yaw=0,Material material=null) {
            var g=Group(name,p,null,yaw).transform;
            string id="Table_"+Mathf.RoundToInt(w*1000)+"_"+Mathf.RoundToInt(d*1000)+"_"+Mathf.RoundToInt(height*1000);
            Furniture(id,g);
            ColliderBox(g,new Vector3(0,height,0),new Vector3(w,.095f,d));
            ColliderBox(g,new Vector3(0,height/2,0),new Vector3(w*.91f,height,d*.87f));return g;
        }
        static Transform Chair(Vector3 p,float yaw=0,Material cloth=null) {
            var g=Group("Upholstered dining chair",p,null,yaw).transform;Furniture("DiningChair",g);
            ColliderBox(g,new Vector3(0,.6f,0),new Vector3(.61f,1.2f,.61f));return g;
        }
        static void Sofa(Vector3 p,float yaw,bool single=false) {
            float w=single?1.15f:2.9f;var g=Group(single?"Club armchair":"Tailored parlor sofa",p,null,yaw).transform;
            Furniture(single?"Armchair":"Sofa",g);
            ColliderBox(g,new Vector3(0,.56f,0),new Vector3(w+.25f,1.12f,1.04f));
        }
        static Transform Cabinet(Vector3 p,string label,float width=1.5f,float height=1.2f,float yaw=0) {
            var g=Group(label,p,null,yaw).transform;
            Box("Cabinet carcass",new Vector3(0,height/2,0),new Vector3(width,height,.57f),darkOak,g,true);
            Box("Carved cornice",new Vector3(0,height+.04f,0),new Vector3(width+.12f,.10f,.67f),oak,g);
            foreach(float x in new[]{-width*.39f,width*.39f})Ball("Cabinet foot",new Vector3(x,.06f,0),new Vector3(.18f,.17f,.38f),oak,g);
            var hinge=Group("Cabinet hinge",new Vector3(-width/2,.12f,-.34f),g).transform;
            Box("Framed cabinet leaf",new Vector3(width/2,(height-.17f)/2,0),new Vector3(width-.04f,height-.17f,.065f),oak,hinge,true);
            Box("Inset walnut panel",new Vector3(width/2,(height-.17f)/2,-.043f),new Vector3(width-.30f,height-.4f,.025f),darkOak,hinge);
            Ball("Brass handle",new Vector3(width-.16f,height*.47f,-.10f),new Vector3(.075f,.075f,.07f),brass,hinge);
            var h=g.gameObject.AddComponent<HauntedObject>().Initialize(label,HauntKind.Cabinet);h.Hinge=hinge;h.OpenAngle=105;return g;
        }
        static void Bookcase(Vector3 p,float yaw=0,float width=2.4f) {
            var g=Group("Library bookcase",p,null,yaw).transform;
            Furniture("Bookcase_"+Mathf.RoundToInt(width*1000),g);
            ColliderBox(g,new Vector3(0,1.35f,0),new Vector3(width,2.7f,.5f));
        }
        static void Painting(Vector3 p,float width,float height,float yaw=0,bool portrait=false) {
            var g=Group(portrait?"Family portrait, 1924":"Framed landscape",p,null,yaw).transform;Material imageMat=Mat("Oil canvas",new Color(.26f,.33f,.29f),.04f);
            Texture2D portraitTexture=portrait?Resources.Load<Texture2D>("FamilyPortrait"):null;
            if(portraitTexture) {imageMat.color=Color.white;imageMat.mainTexture=portraitTexture;}
            Box("Painting backing",Vector3.zero,new Vector3(width,height,.05f),darkOak,g);
            var canvas=Group("Upright oil painting canvas",new Vector3(0,0,-.028f),g);var mesh=new Mesh{name="Front facing upright painting"};
            mesh.vertices=new[]{new Vector3(-width/2,-height/2,0),new Vector3(width/2,-height/2,0),new Vector3(width/2,height/2,0),new Vector3(-width/2,height/2,0)};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();ownedAssets.Add(mesh);canvas.AddComponent<MeshFilter>().sharedMesh=mesh;canvas.AddComponent<MeshRenderer>().sharedMaterial=imageMat;
            if(!portraitTexture){Ball("Painted moon",new Vector3(width*.22f,height*.21f,-.037f),new Vector3(height*.16f,height*.16f,.016f),linen,g);for(int i=0;i<7;i++) {var t=Box("Painted birch",new Vector3(-width*.39f+i*width*.13f,-.07f,-.035f),new Vector3(.02f,height*.61f,.016f),darkOak,g);t.transform.localRotation=Quaternion.Euler(0,0,i*7-20);}}
            foreach(float s in new[]{-1f,1f}){Box("Gilt vertical frame",new Vector3(s*(width/2+.035f),0,-.025f),new Vector3(.09f,height+.18f,.1f),brass,g);Box("Gilt horizontal frame",new Vector3(0,s*(height/2+.035f),-.025f),new Vector3(width+.1f,.09f,.1f),brass,g);}
        }
        static void Plant(Vector3 p,float scale=1) {
            var g=Group("Parlor palm",p).transform;Cylinder("Glazed pot",new Vector3(0,.24f,0),new Vector3(.48f,.24f,.48f),ceramic,g);Cylinder("Soil",new Vector3(0,.48f,0),new Vector3(.4f,.016f,.4f),darkOak,g);
            for(int i=0;i<8;i++){float angle=i*Mathf.PI*.25f;var leaf=Ball("Palm leaf",new Vector3(Mathf.Cos(angle)*.26f,.81f+Mathf.Sin(angle)*.13f,Mathf.Sin(angle)*.26f),new Vector3(.17f,.79f,.09f),green,g);leaf.transform.localRotation=Quaternion.Euler(20*Mathf.Sin(angle),-i*45,25*Mathf.Cos(angle));}g.localScale=Vector3.one*scale;ColliderBox(g,new Vector3(0,.25f,0),new Vector3(.48f,.5f,.48f));
        }
        static Transform Physical(Vector3 p,string name,Vector3 size,float mass=.5f,float yaw=0) {
            var g=Group(name,p,null,yaw).transform;ColliderBox(g,Vector3.zero,size);var rb=g.gameObject.AddComponent<Rigidbody>();rb.mass=mass;rb.linearDamping=.12f;rb.angularDamping=.22f;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.maxAngularVelocity=10;
            var h=g.gameObject.AddComponent<HauntedObject>().Initialize(name,HauntKind.Prop);h.Cost=2;propCount++;return g;
        }
        static void PropBook(Vector3 p,int style=0,float yaw=0) {
            var g=Physical(p,"Книга",new Vector3(.31f,.07f,.43f),.35f,yaw);Box("Cream page block",Vector3.zero,new Vector3(.285f,.049f,.405f),paper,g);foreach(float y in new[]{-.032f,.032f})Box("Cloth book cover",new Vector3(0,y,0),new Vector3(.32f,.014f,.44f),style%2==0?bookRed:bookBlue,g);Box("Book spine",new Vector3(-.156f,0,0),new Vector3(.022f,.067f,.44f),style%2==0?bookRed:bookBlue,g);Box("Gold title",new Vector3(.02f,.041f,-.02f),new Vector3(.17f,.006f,.025f),brass,g);
        }
        static void Mug(Vector3 p,string label="Чашка",bool tea=true) {
            var g=Physical(p,label,new Vector3(.23f,.16f,.18f),.25f);
            string[] variants={"MugClassic","TeacupFloral","MugEnamel"};
            DetailedObjects.Model(variants[propCount%3],g,new Bounds(Vector3.zero,new Vector3(.23f,.16f,.18f)),false);
        }
        static void Bottle(Vector3 p,string label="Бутылка",bool clear=false) {
            var g=Physical(p,label,new Vector3(.16f,.42f,.16f),.55f);Cylinder("Glass bottle body",new Vector3(0,-.065f,0),new Vector3(.15f,.13f,.15f),clear?glass:leather,g);Ball("Bottle shoulder",new Vector3(0,.068f,0),new Vector3(.15f,.14f,.15f),clear?glass:leather,g);Cylinder("Bottle neck",new Vector3(0,.145f,0),new Vector3(.057f,.075f,.057f),clear?glass:leather,g);Cylinder("Cork",new Vector3(0,.214f,0),new Vector3(.05f,.024f,.05f),oak,g);Box("Paper bottle label",new Vector3(0,-.04f,-.076f),new Vector3(.09f,.10f,.004f),paper,g);
        }
        static void Vase(Vector3 p,string label="Ваза") {
            var g=Physical(p,label,new Vector3(.27f,.40f,.27f),.75f);Ball("Porcelain vase body",new Vector3(0,-.04f,0),new Vector3(.28f,.34f,.28f),ceramic,g);Cylinder("Vase neck",new Vector3(0,.13f,0),new Vector3(.14f,.10f,.14f),ceramic,g);Cylinder("Vase opening",new Vector3(0,.224f,0),new Vector3(.10f,.004f,.10f),darkOak,g);
        }
        static void Candle(Vector3 p) {var g=Physical(p,"Подсвечник",new Vector3(.17f,.31f,.17f),.4f);Cylinder("Brass candlestick foot",new Vector3(0,-.13f,0),new Vector3(.17f,.021f,.17f),brass,g);Cylinder("Brass stem",new Vector3(0,-.04f,0),new Vector3(.047f,.08f,.047f),brass,g);Cylinder("Wax candle",new Vector3(0,.08f,0),new Vector3(.065f,.095f,.065f),linen,g);}
        static void Photo(Vector3 p) {var g=Physical(p,"Фотография в рамке",new Vector3(.26f,.32f,.07f),.3f);Box("Small gilt frame",Vector3.zero,new Vector3(.26f,.32f,.05f),brass,g);Box("Sepia photograph",new Vector3(0,0,-.03f),new Vector3(.22f,.28f,.01f),paper,g);Ball("Photographic silhouette",new Vector3(0,.03f,-.04f),new Vector3(.075f,.093f,.006f),darkOak,g);Box("Photo shoulders",new Vector3(0,-.06f,-.04f),new Vector3(.13f,.09f,.007f),darkOak,g);}
        static void Plate(Vector3 p) {var g=Physical(p,"Тарелка",new Vector3(.30f,.045f,.30f),.35f);Cylinder("Porcelain plate rim",Vector3.zero,new Vector3(.30f,.023f,.30f),ceramic,g);Cylinder("Glazed plate well",new Vector3(0,.024f,0),new Vector3(.23f,.005f,.23f),linen,g);}
        static void BoxProp(Vector3 p,string label,Vector3 size,Material material) {var g=Physical(p,label,size,.6f);Box(label,Vector3.zero,size,material,g);Box("Lid edge",new Vector3(0,size.y*.42f,0),new Vector3(size.x*1.015f,.024f,size.z*1.015f),brass,g);}
        static void LivingRoom() {
            Rug(new Vector3(-7,0,-6.3f),6.5f,5.7f);
            Sofa(new Vector3(-7.1f,0,-8.7f),180);Sofa(new Vector3(-4.4f,0,-5.05f),42,true);Sofa(new Vector3(-9.9f,0,-4.8f),-38,true);
            Table(new Vector3(-7,0,-6.25f),2.5f,1.24f,"Drawing-room coffee table",.49f);
            PropBook(new Vector3(-7.58f,.59f,-6.18f),0,14);PropBook(new Vector3(-7.58f,.67f,-6.18f),1,-7);Mug(new Vector3(-6.4f,.68f,-6.45f));Plate(new Vector3(-6.43f,.566f,-6.4f));
            Vase(new Vector3(-6.7f,.77f,-5.98f));BoxProp(new Vector3(-7.75f,.63f,-6.55f),"Спичечный коробок",new Vector3(.11f,.06f,.18f),bookRed);
            Table(new Vector3(-10.5f,0,-6.40f),.76f,.76f,"Side table",.71f);TableLamp(new Vector3(-10.5f,.77f,-6.40f),"Лампа у дивана");Photo(new Vector3(-10.74f,.98f,-6.35f));
            Table(new Vector3(-4.87f,0,-8.9f),.76f,.76f,"Side table",.71f);Candle(new Vector3(-4.87f,.93f,-8.9f));PropBook(new Vector3(-4.68f,.83f,-8.73f),1,20);
            Fireplace(new Vector3(-7,0,-2.41f));Painting(new Vector3(-7,2.25f,-2.27f),1.88f,1.25f,0,true);
            Bookcase(new Vector3(-10,0,-2.35f),0,2.6f);Bookcase(new Vector3(-4,0,-2.35f),0,2.6f);
            Television(new Vector3(-2.62f,0,-9.07f),90);
            Table(new Vector3(-11.35f,0,-7.0f),1.65f,.56f,"Gramophone console",.82f,90);Radio(new Vector3(-11.32f,.87f,-7.1f),90);
            Bottle(new Vector3(-11.35f,1.11f,-6.55f),"Графин");Mug(new Vector3(-11.38f,1.05f,-7.69f),"Стакан");Plant(new Vector3(-11.1f,0,-9.9f),1.3f);Plant(new Vector3(-2.9f,0,-3.0f),1.3f);
            var chandelier=CeilingLamp(new Vector3(-7,3.43f,-6.15f),"Люстра гостиной",9,.95f);
            var cg=Group("Drawing room chandelier branches",new Vector3(0,-.59f,0),chandelier.transform).transform;var bulbs=new List<Renderer>(chandelier.EmissiveParts);
            for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;var b=Cylinder("Chandelier curved arm",new Vector3(Mathf.Cos(a)*.3f,0,Mathf.Sin(a)*.3f),new Vector3(.028f,.35f,.028f),brass,cg);b.transform.localRotation=Quaternion.Euler(0,-i*72,90);var globe=Ball("Frosted tulip",new Vector3(Mathf.Cos(a)*.62f,.10f,Mathf.Sin(a)*.62f),new Vector3(.21f,.23f,.21f),lampGlass,cg).GetComponent<Renderer>();globe.shadowCastingMode=ShadowCastingMode.Off;bulbs.Add(globe);}chandelier.EmissiveParts=bulbs.ToArray();
            Photo(new Vector3(-8.03f,1.47f,-2.56f));Candle(new Vector3(-6.1f,1.42f,-2.61f));Candle(new Vector3(-6.4f,1.42f,-2.61f));
        }
        static void Fireplace(Vector3 p) {
            var g=Group("Limestone fireplace",p).transform;Box("Fireplace opening",new Vector3(0,.64f,0),new Vector3(2.2f,1.2f,.12f),black,g);
            Box("Stone hearth",new Vector3(0,.13f,-.29f),new Vector3(3.2f,.23f,1.15f),cream,g,true);
            foreach(float side in new[]{-1f,1f}) {Box("Carved fireplace pillar",new Vector3(side*1.22f,.74f,-.12f),new Vector3(.46f,1.27f,.5f),cream,g,true);for(int j=0;j<3;j++)Box("Pillar fluting",new Vector3(side*1.22f+(j-1)*.09f,.8f,-.392f),new Vector3(.025f,.82f,.025f),trim,g);}
            Box("Stone mantel",new Vector3(0,1.28f,-.12f),new Vector3(3.22f,.21f,.68f),trim,g,true);
            for(int i=0;i<4;i++){var log=Cylinder("Hearth log",new Vector3(-.55f+i*.35f,.31f,-.2f),new Vector3(.19f,.37f,.19f),darkOak,g);log.transform.localRotation=Quaternion.Euler(80,i*23,80);}
            for(int i=0;i<7;i++)Ball("Banked ember",new Vector3(-.65f+i*.21f,.27f,-.41f),new Vector3(.19f,.08f,.17f),fire,g);
            for(int i=0;i<6;i++)Cylinder("Fire basket bar",new Vector3(-.8f+i*.32f,.34f,-.61f),new Vector3(.025f,.20f,.025f),iron,g);
            Point("Ember glow",new Vector3(0,.43f,-.64f),new Color(1,.36f,.11f),.9f,4.8f,g);
        }
        static void Television(Vector3 p,float yaw) {
            var g=Group("Ранний телевизионный приёмник",p,null,yaw).transform;
            foreach(float x in new[]{-.53f,.53f})foreach(float z in new[]{-.2f,.2f})Cylinder("Receiver leg",new Vector3(x,.22f,z),new Vector3(.06f,.22f,.06f),darkOak,g);
            Box("Walnut receiver case",new Vector3(0,.92f,0),new Vector3(1.38f,1.04f,.62f),oak,g,true);
            Box("Receiver front face",new Vector3(0,.95f,-.331f),new Vector3(1.22f,.9f,.052f),darkOak,g);
            Ball("Convex glass tube",new Vector3(-.16f,1,-.37f),new Vector3(.88f,.66f,.12f),black,g);
            var active=Box("Noisy phosphor display",new Vector3(-.16f,1,-.437f),new Vector3(.74f,.5f,.012f),screen,g);
            foreach(float y in new[]{.84f,1.14f}) {var knob=Cylinder("Bakelite tuning knob",new Vector3(.47f,y,-.4f),new Vector3(.11f,.035f,.11f),black,g);knob.transform.localRotation=Quaternion.Euler(90,0,0);}
            for(int i=0;i<5;i++)Box("Speaker grille",new Vector3(-.17f,.62f+i*.025f,-.36f),new Vector3(.77f,.01f,.02f),black,g);
            foreach(float s in new[]{-1f,1f}) {var a=Cylinder("Rabbit-ear aerial",new Vector3(s*.28f,1.72f,0),new Vector3(.017f,.42f,.017f),brass,g);a.transform.localRotation=Quaternion.Euler(0,0,-s*30);}
            var h=g.gameObject.AddComponent<HauntedObject>().Initialize("Телевизор",HauntKind.Television,false);h.ActiveVisual=active;active.SetActive(false);h.Lamps=new[]{Point("TV green cast",new Vector3(0,1,-.65f),new Color(.45f,.75f,.58f),.7f,4,g)};
            Photo(new Vector3(p.x,.22f,p.z)+g.forward*-.2f);
        }
        static void Radio(Vector3 p,float yaw=0) {
            var g=Group("Ламповое радио",p,null,yaw).transform;
            Box("Bakelite radio",new Vector3(0,.25f,0),new Vector3(.8f,.49f,.31f),darkOak,g,true);Ball("Rounded radio crown",new Vector3(0,.46f,0),new Vector3(.8f,.3f,.32f),darkOak,g);
            Box("Woven speaker cloth",new Vector3(-.13f,.28f,-.17f),new Vector3(.40f,.29f,.022f),linen,g);
            for(int i=0;i<7;i++)Box("Radio grille",new Vector3(-.30f+i*.056f,.28f,-.19f),new Vector3(.016f,.3f,.025f),brass,g);
            Box("Tuning dial",new Vector3(.24f,.30f,-.178f),new Vector3(.15f,.08f,.02f),cream,g);
            var dial=Cylinder("Tuning knob",new Vector3(.24f,.15f,-.20f),new Vector3(.09f,.022f,.09f),black,g);dial.transform.localRotation=Quaternion.Euler(90,0,0);
            var glow=Ball("Dial lamp",new Vector3(.24f,.40f,-.19f),new Vector3(.023f,.023f,.013f),fire,g);
            var h=g.gameObject.AddComponent<HauntedObject>().Initialize("Радио",HauntKind.Radio);h.ActiveVisual=glow;glow.SetActive(false);
        }
        static void Kitchen() {
            CeilingLamp(new Vector3(-7,3.4f,.8f),"Кухонная лампа",8.5f,1.05f);
            for(int i=0;i<4;i++) {float x=-10.8f+i*2.0f;var c=Cabinet(new Vector3(x,0,3.57f),"Кухонный шкаф",1.88f,.87f);Box("Marble worktop",new Vector3(0,.99f,0),new Vector3(1.94f,.09f,.84f),cream,c);}
            Sink(new Vector3(-8.8f,1.04f,3.51f),"Кран на кухне");
            var oven=Group("Enamel range",new Vector3(-11.27f,0,1.8f),null,90).transform;Box("Cream stove",new Vector3(0,.52f,0),new Vector3(1.25f,1.02f,.75f),ceramic,oven,true);
            Box("Oven iron door",new Vector3(0,.43f,-.40f),new Vector3(.82f,.58f,.045f),iron,oven);Box("Oven viewing pane",new Vector3(0,.43f,-.432f),new Vector3(.57f,.31f,.01f),black,oven);Box("Oven handle",new Vector3(0,.75f,-.48f),new Vector3(.65f,.043f,.044f),brass,oven);
            foreach(float x in new[]{-.31f,.31f})foreach(float z in new[]{-.2f,.2f})Cylinder("Iron hob",new Vector3(x,1.05f,z),new Vector3(.30f,.02f,.30f),iron,oven);
            var kettle=Physical(new Vector3(-11.33f,1.27f,1.58f),"Чайник",new Vector3(.32f,.34f,.32f),.8f);Ball("Kettle body",Vector3.zero,new Vector3(.32f,.28f,.32f),brass,kettle);Cylinder("Kettle lid",new Vector3(0,.13f,0),new Vector3(.17f,.022f,.17f),brass,kettle);Ball("Kettle lid knob",new Vector3(0,.17f,0),new Vector3(.055f,.05f,.055f),black,kettle);var spout=Cylinder("Kettle spout",new Vector3(.18f,.03f,0),new Vector3(.067f,.13f,.067f),brass,kettle);spout.transform.localRotation=Quaternion.Euler(0,0,-45);
            Table(new Vector3(-6.7f,0,.25f),3.4f,1.65f,"Kitchen oak table",.81f);foreach(float x in new[]{-7.75f,-5.8f}) {Chair(new Vector3(x,0,-.9f),180);Chair(new Vector3(x,0,1.4f));}
            for(int i=0;i<4;i++){float x=-7.75f+(i%2)*1.85f;float z=-.25f+(i/2)*.75f;Plate(new Vector3(x,.90f,z));Mug(new Vector3(x+.38f,1.025f,z+.1f));}
            Bottle(new Vector3(-6.82f,1.12f,.34f),"Бутылка молока",true);PropBook(new Vector3(-6.25f,.91f,.35f),0,8);
            for(int i=0;i<4;i++)Bottle(new Vector3(-6.35f+i*.27f,1.25f,3.47f),"Банка специй",i%2==0);
            BoxProp(new Vector3(-10.84f,1.2f,3.4f),"Жестяная коробка чая",new Vector3(.26f,.28f,.26f),bookBlue);
            BoxProp(new Vector3(-10.25f,1.16f,3.4f),"Хлебница",new Vector3(.65f,.20f,.38f),oak);
            Painting(new Vector3(-4.1f,2.0f,3.84f),1.2f,.8f);Plant(new Vector3(-3,0,3.1f));
            // Copper pots and the tiled splashback give the kitchen a distinct silhouette.
            for(int i=0;i<5;i++) {float x=-10.7f+i*.7f;Cylinder("Hanging copper pan",new Vector3(x,1.87f,3.71f),new Vector3(.31f,.052f,.31f),brass,null).transform.localRotation=Quaternion.Euler(90,0,0);Box("Pan handle",new Vector3(x,2.12f,3.71f),new Vector3(.047f,.26f,.045f),iron);}
        }
        static void Sink(Vector3 p,string label,float yaw=0) {
            var g=Group(label,p,null,yaw).transform;
            Box("Porcelain sink lip",new Vector3(0,.005f,0),new Vector3(.96f,.07f,.61f),ceramic,g);Box("Deep sink well",new Vector3(0,.025f,0),new Vector3(.75f,.075f,.43f),darkOak,g);
            Box("Sink basin",new Vector3(0,.07f,0),new Vector3(.63f,.031f,.33f),ceramic,g);
            Cylinder("Brass tap upright",new Vector3(0,.27f,.22f),new Vector3(.039f,.22f,.039f),brass,g);var tap=Cylinder("Brass faucet spout",new Vector3(0,.47f,.09f),new Vector3(.040f,.15f,.040f),brass,g);tap.transform.localRotation=Quaternion.Euler(90,0,0);
            foreach(float x in new[]{-.24f,.24f}) {Cylinder("Tap cross spindle",new Vector3(x,.14f,.22f),new Vector3(.035f,.08f,.035f),brass,g);Box("Tap cross handle",new Vector3(x,.21f,.22f),new Vector3(.16f,.025f,.04f),ceramic,g);}
            var stream=Cylinder("Running water",new Vector3(0,.27f,-.045f),new Vector3(.027f,.16f,.027f),water,g);stream.SetActive(false);
            var h=g.gameObject.AddComponent<HauntedObject>().Initialize(label,HauntKind.Water);h.ActiveVisual=stream;
            ColliderBox(g,new Vector3(0,-.01f,0),new Vector3(.95f,.17f,.64f));
            ColliderBox(g,new Vector3(0,.30f,label=="Кран умывальника"?-.12f:.12f),new Vector3(.15f,.42f,.22f));
        }
        static void Study() {
            CeilingLamp(new Vector3(7.1f,3.4f,-7.5f),"Лампа кабинета",9,.85f);Rug(new Vector3(7,0,-7.4f),6.9f,4.4f);
            Bookcase(new Vector3(4.15f,0,-4.28f),0,3.2f);Bookcase(new Vector3(9.5f,0,-4.28f),0,3.7f);
            Table(new Vector3(7,0,-8.75f),3.2f,1.35f,"Partners writing desk",.82f);Chair(new Vector3(7,0,-9.9f),180);Chair(new Vector3(5.85f,0,-7.4f));Chair(new Vector3(8.4f,0,-7.4f));
            TableLamp(new Vector3(8.08f,.88f,-8.83f),"Банкирская лампа");
            for(int i=0;i<5;i++)PropBook(new Vector3(5.82f,.91f+i*.075f,-8.68f),i,i*9-10);
            PropBook(new Vector3(6.74f,.91f,-8.93f),1,7);Photo(new Vector3(7.60f,1.08f,-8.93f));Mug(new Vector3(7.38f,1.0f,-8.37f),"Кофейная чашка");
            var ink=Physical(new Vector3(6.99f,.96f,-8.4f),"Чернильница",new Vector3(.12f,.15f,.12f),.25f);Box("Ink bottle",Vector3.zero,new Vector3(.12f,.12f,.12f),glass,ink);Cylinder("Ink stopper",new Vector3(0,.08f,0),new Vector3(.07f,.02f,.07f),brass,ink);
            BoxProp(new Vector3(6.66f,.94f,-8.37f),"Письма",new Vector3(.25f,.027f,.18f),paper);
            var telephone=Physical(new Vector3(8.25f,1.06f,-8.35f),"Телефон",new Vector3(.40f,.31f,.29f),1);Ball("Telephone base",new Vector3(0,-.10f,0),new Vector3(.4f,.12f,.29f),black,telephone);Cylinder("Rotary dial",new Vector3(0,-.026f,-.04f),new Vector3(.18f,.018f,.18f),brass,telephone);Box("Receiver rests",new Vector3(0,.04f,.06f),new Vector3(.30f,.10f,.08f),brass,telephone);var receiver=Part("Telephone receiver",PrimitiveType.Capsule,new Vector3(0,.12f,.06f),new Vector3(.105f,.22f,.105f),black,telephone);receiver.transform.localRotation=Quaternion.Euler(0,0,90);
            Cabinet(new Vector3(11.45f,0,-5.0f),"Шкаф с документами",1.7f,1.2f,90);Plant(new Vector3(10.9f,0,-10.15f),1.3f);Sofa(new Vector3(3.05f,0,-9.8f),-90,true);
            Table(new Vector3(3.22f,0,-5.22f),.85f,.85f,"Globe stand",.78f);var globe=Physical(new Vector3(3.22f,1.13f,-5.22f),"Глобус",new Vector3(.47f,.57f,.47f),1.2f);Cylinder("Globe foot",new Vector3(0,-.25f,0),new Vector3(.35f,.04f,.35f),brass,globe);Ball("Old terrestrial globe",new Vector3(0,.04f,0),new Vector3(.45f,.45f,.45f),teal,globe);Cylinder("Globe axis",Vector3.zero,new Vector3(.028f,.3f,.028f),brass,globe);for(int i=0;i<5;i++)Ball("Globe continent",new Vector3(Mathf.Cos(i*1.7f)*.20f,Mathf.Sin(i*1.7f)*.17f,-.1f),new Vector3(.16f,.13f,.10f),oak,globe);
            Painting(new Vector3(11.85f,2.15f,-8.8f),2.0f,1.23f,90);Candle(new Vector3(11.40f,1.42f,-5.0f));Photo(new Vector3(11.47f,1.47f,-4.5f));
        }
        static void Bed(Vector3 p,float yaw,Material cover=null) {
            var g=Group("Brass double bed",p,null,yaw).transform;Material c=cover?cover:burgundy;
            Box("Bed sprung base",new Vector3(0,.38f,0),new Vector3(1.86f,.25f,2.5f),darkOak,g);
            Box("Linen mattress",new Vector3(0,.59f,0),new Vector3(1.83f,.25f,2.48f),linen,g);
            Box("Folded quilt",new Vector3(0,.755f,-.30f),new Vector3(1.86f,.10f,1.85f),c,g);
            foreach(float x in new[]{-.46f,.46f})Ball("Pillow",new Vector3(x,.80f,.86f),new Vector3(.79f,.19f,.47f),linen,g);
            foreach(float x in new[]{-.97f,.97f})foreach(float z in new[]{-1.30f,1.3f}) {float h=z>0?1.31f:.84f;Cylinder("Brass bed post",new Vector3(x,h/2,z),new Vector3(.045f,h/2,.045f),brass,g);Ball("Bed post finial",new Vector3(x,h,z),new Vector3(.105f,.105f,.105f),brass,g);}
            foreach(float z in new[]{-1.3f,1.3f}) {
                float h=z>0?1.2f:.75f;var bar=Cylinder("Brass cross rail",new Vector3(0,h,z),new Vector3(.04f,.97f,.04f),brass,g);bar.transform.localRotation=Quaternion.Euler(0,0,90);
                for(int i=-3;i<=3;i++)Cylinder("Brass bed spindle",new Vector3(i*.24f,(h+.38f)/2,z),new Vector3(.023f,(h-.38f)/2,.023f),brass,g);
            }
            ColliderBox(g,new Vector3(0,.44f,0),new Vector3(1.93f,.85f,2.62f));
        }
        static void Bedroom() {
            CeilingLamp(new Vector3(4.48f,3.4f,.6f),"Лампа спальни",6.7f,.76f);Rug(new Vector3(4.45f,0,-.5f),3.5f,5.4f);
            Bed(new Vector3(4.48f,0,1.60f),0);Cabinet(new Vector3(5.50f,0,-3.55f),"Платяной шкаф",2.0f,2.35f,0);
            Table(new Vector3(3.01f,0,2.62f),.70f,.70f,"Bedside table",.65f);TableLamp(new Vector3(3.01f,.71f,2.62f),"Лампа у кровати");
            Table(new Vector3(5.95f,0,2.62f),.70f,.70f,"Bedside table",.65f);PropBook(new Vector3(5.93f,.77f,2.59f),1,13);Mug(new Vector3(5.86f,.88f,2.77f));
            Photo(new Vector3(3.14f,.91f,2.51f));Vase(new Vector3(6.13f,.93f,2.57f));
            Table(new Vector3(2.46f,0,-2.54f),1.5f,.58f,"Dressing table",.84f,90);Chair(new Vector3(3.23f,0,-2.45f),90);
            Bottle(new Vector3(2.49f,1.11f,-2.74f),"Флакон духов",true);BoxProp(new Vector3(2.46f,1.02f,-2.14f),"Шкатулка",new Vector3(.37f,.22f,.27f),darkOak);Candle(new Vector3(2.53f,1.06f,-2.42f));
            Painting(new Vector3(4.40f,2.13f,3.85f),1.40f,.87f);Plant(new Vector3(6.38f,0,3.39f),.85f);
            BoxProp(new Vector3(3.3f,.25f,-1.65f),"Чемодан",new Vector3(.87f,.37f,.53f),leather);
        }
        static void OpenBathtub(Transform parent) {
            // Continuous vessel section, running up the outside, over the rolled rim,
            // down the inside and onto a recessed flat floor. The opening is actual geometry.
            const int segments=64;
            Vector3[] section={
                new Vector3(.79f,.27f,.29f),new Vector3(1.04f,.39f,.43f),
                new Vector3(1.15f,.70f,.49f),new Vector3(1.21f,.89f,.54f),
                new Vector3(1.20f,.94f,.54f),new Vector3(1.07f,.945f,.42f),
                new Vector3(1.04f,.83f,.39f),new Vector3(.88f,.45f,.29f),
                new Vector3(.72f,.40f,.22f)
            };
            var vertices=new Vector3[segments*section.Length+2];var uv=new Vector2[vertices.Length];
            var triangles=new List<int>((section.Length-1)*segments*6+segments*6);
            for(int ring=0;ring<section.Length;ring++)for(int i=0;i<segments;i++) {
                float a=i*Mathf.PI*2/segments;Vector3 s=section[ring];int at=ring*segments+i;
                vertices[at]=new Vector3(Mathf.Cos(a)*s.x,s.y,Mathf.Sin(a)*s.z);uv[at]=new Vector2((float)i/segments,(float)ring/(section.Length-1));
                if(ring<section.Length-1) {
                    int next=ring*segments+(i+1)%segments;
                    triangles.Add(at);triangles.Add(at+segments);triangles.Add(next);
                    triangles.Add(next);triangles.Add(at+segments);triangles.Add(next+segments);
                }
            }
            int insideCenter=vertices.Length-2,undersideCenter=vertices.Length-1;
            vertices[insideCenter]=new Vector3(0,.40f,0);vertices[undersideCenter]=new Vector3(0,.27f,0);
            for(int i=0;i<segments;i++) {
                int next=(i+1)%segments,inner=(section.Length-1)*segments;
                triangles.Add(inner+i);triangles.Add(insideCenter);triangles.Add(inner+next);
                triangles.Add(i);triangles.Add(next);triangles.Add(undersideCenter);
            }
            var mesh=new Mesh{name="Open enamel bath with rolled rim and recessed basin"};mesh.vertices=vertices;mesh.uv=uv;mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();ownedAssets.Add(mesh);
            var vessel=Group("Open porcelain bathtub",Vector3.zero,parent);vessel.AddComponent<MeshFilter>().sharedMesh=mesh;vessel.AddComponent<MeshRenderer>().sharedMaterial=ceramic;
            Cylinder("Basin brass drain",new Vector3(-.56f,.406f,0),new Vector3(.095f,.006f,.095f),brass,parent);
            foreach(float x in new[]{-.75f,.75f})foreach(float z in new[]{-.28f,.28f}) {
                Cylinder("Attached claw-foot stem",new Vector3(x,.26f,z),new Vector3(.09f,.15f,.09f),brass,parent);
                Ball("Claw-foot toe",new Vector3(x,.13f,z),new Vector3(.20f,.15f,.24f),brass,parent);
            }
        }
        static void Bath() {
            CeilingLamp(new Vector3(9.5f,3.4f,.6f),"Лампа ванной",6.8f,.9f);
            var bath=Group("Claw-foot bath",new Vector3(10.83f,0,1.63f),null,90).transform;
            OpenBathtub(bath);
            ColliderBox(bath,new Vector3(0,.5f,0),new Vector3(2.40f,.88f,1.04f));
            var faucet=Group("Кран ванны",new Vector3(10.82f,0,2.81f)).transform;
            Cylinder("Bath brass riser",new Vector3(0,.53f,0),new Vector3(.045f,.53f,.045f),brass,faucet);var f=Cylinder("Bath spout",new Vector3(0,1.04f,-.14f),new Vector3(.045f,.16f,.045f),brass,faucet);f.transform.localRotation=Quaternion.Euler(90,0,0);Box("Hot and cold cross",new Vector3(0,.90f,0),new Vector3(.35f,.04f,.045f),brass,faucet);
            var stream=Cylinder("Bath running water",new Vector3(0,.85f,-.28f),new Vector3(.027f,.16f,.027f),water,faucet);var h=faucet.gameObject.AddComponent<HauntedObject>().Initialize("Кран ванны",HauntKind.Water);h.ActiveVisual=stream;stream.SetActive(false);ColliderBox(faucet,new Vector3(0,.9f,-.1f),new Vector3(.42f,.5f,.5f));
            var basin=Group("Pedestal washbasin",new Vector3(9.30f,0,-3.32f)).transform;Cylinder("Porcelain pedestal",new Vector3(0,.47f,0),new Vector3(.32f,.47f,.32f),ceramic,basin);Sink(new Vector3(9.3f,.92f,-3.32f),"Кран умывальника");
            Box("Oxidized mirror",new Vector3(9.3f,2.02f,-3.84f),new Vector3(1.0f,1.16f,.04f),glass);foreach(float s in new[]{-1f,1f}) {Box("Mirror vertical frame",new Vector3(9.3f+s*.55f,2.02f,-3.80f),new Vector3(.1f,1.34f,.08f),brass);Box("Mirror horizontal frame",new Vector3(9.3f,2.02f+s*.62f,-3.80f),new Vector3(1.2f,.10f,.08f),brass);}
            Cabinet(new Vector3(7.57f,0,3.44f),"Бельевой шкаф",.82f,1.74f);
            var toilet=Group("Porcelain toilet",new Vector3(11.19f,0,-2.46f)).transform;Ball("Toilet bowl",new Vector3(0,.42f,0),new Vector3(.65f,.56f,.87f),ceramic,toilet);Cylinder("Porcelain pedestal",new Vector3(0,.19f,0),new Vector3(.35f,.19f,.40f),ceramic,toilet);Box("Cistern",new Vector3(0,.87f,.36f),new Vector3(.58f,.62f,.22f),ceramic,toilet);Ball("Black toilet seat",new Vector3(0,.67f,-.05f),new Vector3(.60f,.08f,.72f),black,toilet);ColliderBox(toilet,new Vector3(0,.56f,.1f),new Vector3(.7f,1.15f,.95f));
            Table(new Vector3(7.62f,0,-2.67f),.60f,.6f,"Bath stool",.47f);for(int i=0;i<3;i++)BoxProp(new Vector3(7.63f,.55f+i*.11f,-2.69f),"Сложенное полотенце",new Vector3(.47f,.09f,.40f),linen);
            Bottle(new Vector3(9.6f,1.21f,-3.30f),"Аптечный флакон",true);Bottle(new Vector3(9.0f,1.21f,-3.30f),"Аптечный флакон");Mug(new Vector3(7.62f,1.90f,3.43f),"Стакан для щёток",false);BoxProp(new Vector3(7.62f,1.89f,3.22f),"Мыло",new Vector3(.16f,.09f,.24f),linen);
            Box("Bath mat",new Vector3(9.59f,.071f,1.4f),new Vector3(.85f,.03f,2.13f),linen);
        }
        static void GuestBedroom() {
            CeilingLamp(new Vector3(7,3.4f,7.15f),"Люстра спальни Элеоноры",8.5f,.83f);Rug(new Vector3(7,0,7.2f),6.8f,4.5f);
            Bed(new Vector3(7.1f,0,9.20f),0,leather);
            Cabinet(new Vector3(3.8f,0,10.5f),"Гардероб",2.7f,2.5f);Table(new Vector3(5.54f,0,10.13f),.72f,.68f,"Bedside table",.67f);TableLamp(new Vector3(5.54f,.74f,10.13f),"Прикроватная лампа");
            Table(new Vector3(8.64f,0,10.13f),.72f,.68f,"Bedside table",.67f);Photo(new Vector3(8.72f,.97f,10.14f));PropBook(new Vector3(8.55f,.81f,10.09f),0,12);Mug(new Vector3(8.50f,.93f,9.98f));
            Table(new Vector3(10.80f,0,5.40f),2.30f,.8f,"Vanity writing table",.83f,90);Chair(new Vector3(9.82f,0,5.6f),90);TableLamp(new Vector3(10.8f,.89f,4.63f),"Лампа туалетного столика");
            Vase(new Vector3(10.78f,1.12f,6.12f));Photo(new Vector3(10.65f,1.10f,5.8f));Bottle(new Vector3(10.9f,1.12f,5.4f),"Флакон духов",true);BoxProp(new Vector3(10.71f,1.06f,5.09f),"Музыкальная шкатулка",new Vector3(.39f,.24f,.28f),oak);
            PropBook(new Vector3(10.67f,.94f,5.54f),1,90);Candle(new Vector3(8.73f,.94f,10.30f));
            Sofa(new Vector3(4,0,5.15f),-165,true);Table(new Vector3(3.04f,0,5.60f),.72f,.72f,"Small tea table",.62f);Mug(new Vector3(3.01f,.82f,5.54f));
            Plant(new Vector3(11.1f,0,10.24f),1.28f);Painting(new Vector3(7,2.1f,4.16f),2.05f,1.17f,180);BoxProp(new Vector3(9.3f,.3f,8.4f),"Кожаный чемодан",new Vector3(.93f,.46f,.55f),bookRed);
        }
        static void Garage() {
            CeilingLamp(new Vector3(-7,3.4f,7.35f),"Лампа гаража",9,.91f);
            var car=Group("1927 touring motorcar",new Vector3(-8.18f,0,7.53f)).transform;
            Box("Motorcar chassis",new Vector3(0,.46f,0),new Vector3(1.88f,.2f,3.75f),iron,car);
            Box("Touring body",new Vector3(0,.88f,.45f),new Vector3(1.72f,.65f,2.16f),leather,car);Box("Long bonnet",new Vector3(0,.91f,-1.03f),new Vector3(1.29f,.66f,1.35f),leather,car);
            foreach(float s in new[]{-1f,1f}) {
                Box("Side running board",new Vector3(s*1.03f,.49f,.1f),new Vector3(.34f,.10f,2.16f),black,car);
                foreach(float z in new[]{-1.20f,1.18f}) {
                    var tire=Cylinder("Period tire",new Vector3(s*1.01f,.51f,z),new Vector3(.83f,.14f,.83f),black,car);tire.transform.localRotation=Quaternion.Euler(0,0,90);
                    var hub=Cylinder("Spoked brass wheel",new Vector3(s*1.166f,.51f,z),new Vector3(.55f,.015f,.55f),brass,car);hub.transform.localRotation=Quaternion.Euler(0,0,90);
                    var cap=Cylinder("Wheel hub",new Vector3(s*1.2f,.51f,z),new Vector3(.16f,.025f,.16f),iron,car);cap.transform.localRotation=Quaternion.Euler(0,0,90);
                    Ball("Swept wheel arch",new Vector3(s*.95f,.83f,z),new Vector3(.37f,.27f,1.09f),leather,car);
                }
                Box("Windscreen pillar",new Vector3(s*.78f,1.55f,-.34f),new Vector3(.048f,.91f,.049f),brass,car);Box("Coach roof pillar",new Vector3(s*.80f,1.65f,1.30f),new Vector3(.06f,.70f,.06f),black,car);
                Ball("Headlamp casing",new Vector3(s*.78f,1.0f,-1.76f),new Vector3(.29f,.29f,.23f),brass,car);Ball("Headlamp lens",new Vector3(s*.78f,1,-1.90f),new Vector3(.24f,.24f,.035f),linen,car);
            }
            Box("Black canvas roof",new Vector3(0,2.10f,.5f),new Vector3(1.83f,.17f,2.15f),black,car);Box("Windscreen",new Vector3(0,1.66f,-.34f),new Vector3(1.53f,.76f,.025f),glass,car);Box("Windshield upper rail",new Vector3(0,2.03f,-.34f),new Vector3(1.62f,.055f,.055f),brass,car);
            Box("Radiator surround",new Vector3(0,.93f,-1.74f),new Vector3(1.1f,.8f,.08f),brass,car);Box("Radiator matrix",new Vector3(0,.93f,-1.8f),new Vector3(.92f,.66f,.025f),black,car);for(int i=0;i<11;i++)Box("Radiator slat",new Vector3(-.43f+i*.086f,.93f,-1.82f),new Vector3(.015f,.64f,.015f),brass,car);
            Box("Chromed bumper",new Vector3(0,.54f,-2.01f),new Vector3(1.83f,.08f,.095f),brass,car);Box("Motorcar leather seat",new Vector3(0,1.06f,.56f),new Vector3(1.45f,.20f,1.2f),burgundy,car);ColliderBox(car,new Vector3(0,1,0),new Vector3(2.2f,1.9f,4.12f));
            Table(new Vector3(-4.15f,0,10.35f),3.2f,.90f,"Garage workbench",.91f);Cabinet(new Vector3(-2.65f,0,8.6f),"Инструментальный шкаф",1.22f,1.6f,90);
            for(int i=0;i<5;i++)BoxProp(new Vector3(-5.25f+i*.44f,1.13f,10.3f),"Коробка инструментов",new Vector3(.31f,.27f,.40f),i%2==0?bookRed:iron);
            Bottle(new Vector3(-3.05f,1.20f,10.31f),"Маслёнка");Radio(new Vector3(-5.24f,.98f,10.51f));
            for(int i=0;i<3;i++) {
                var g=Physical(new Vector3(-4.42f+i*.38f,1.03f,10.05f),i%2==0?"Гаечный ключ":"Молоток",new Vector3(.12f,.08f,.36f),.7f,i*20);
                Box("Tool shaft",Vector3.zero,new Vector3(.04f,.045f,.31f),i%2==0?iron:oak,g);Box("Tool head",new Vector3(0,0,.135f),new Vector3(.14f,.075f,.065f),iron,g);
            }
            for(int i=0;i<3;i++) {var tire=Cylinder("Spare tire stack",new Vector3(-11.0f,.20f+i*.33f,9.86f),new Vector3(.9f,.16f,.9f),black);Cylinder("Spare wheel opening",new Vector3(-11,.37f+i*.33f,9.86f),new Vector3(.50f,.01f,.50f),iron);}
            BoxProp(new Vector3(-3.5f,.32f,5.03f),"Деревянный ящик",new Vector3(.82f,.55f,.65f),oak);BoxProp(new Vector3(-4.5f,.24f,5.0f),"Канистра",new Vector3(.36f,.42f,.25f),leather);
            Box("Garage roller door",new Vector3(-7,1.45f,10.81f),new Vector3(4.45f,2.75f,.08f),darkOak);for(int i=0;i<12;i++)Box("Garage door panel",new Vector3(-7,.18f+i*.23f,10.75f),new Vector3(4.31f,.19f,.035f),oak);
            for(int i=0;i<3;i++) {var x=Box("Workshop leaning board",new Vector3(-11.46f+i*.18f,1.10f,4.65f),new Vector3(.14f,2.12f,.10f),oak);x.transform.localRotation=Quaternion.Euler(11,0,-7);}
        }
        static void Hall() {
            Rug(new Vector3(0,0,0),2.9f,17.6f);
            for(int z=-4;z<=8;z+=6){Painting(new Vector3(-1.84f,2.13f,z),1.15f,.91f,-90);Painting(new Vector3(1.84f,2.13f,z-1.0f),1.15f,.91f,90);}
            var clock=Group("Grandfather clock",new Vector3(.97f,0,10.55f)).transform;
            Box("Clock walnut trunk",new Vector3(0,1.21f,0),new Vector3(.75f,2.38f,.44f),darkOak,clock,true);Box("Clock foot",new Vector3(0,.11f,0),new Vector3(.92f,.21f,.58f),oak,clock);Box("Clock cornice",new Vector3(0,2.48f,0),new Vector3(.90f,.15f,.56f),oak,clock);
            var dial=Cylinder("Ivory clock dial",new Vector3(0,2.02f,-.24f),new Vector3(.55f,.025f,.55f),linen,clock);dial.transform.localRotation=Quaternion.Euler(90,0,0);
            Box("Hour hand",new Vector3(.04f,2.06f,-.275f),new Vector3(.023f,.18f,.008f),black,clock).transform.localRotation=Quaternion.Euler(0,0,-35);Box("Minute hand",new Vector3(-.07f,2.05f,-.283f),new Vector3(.024f,.24f,.01f),black,clock).transform.localRotation=Quaternion.Euler(0,0,62);
            Box("Pendulum glass",new Vector3(0,.93f,-.245f),new Vector3(.43f,1.37f,.021f),glass,clock);Cylinder("Pendulum stem",new Vector3(0,1.05f,-.278f),new Vector3(.018f,.43f,.018f),brass,clock);Ball("Pendulum brass bob",new Vector3(0,.70f,-.29f),new Vector3(.28f,.28f,.047f),brass,clock);
            var clockHaunt=clock.gameObject.AddComponent<HauntedObject>().Initialize("Напольные часы",HauntKind.Radio,true);
            Table(new Vector3(-1.37f,0,-9.85f),1.20f,.54f,"Entrance console",.79f,90);Vase(new Vector3(-1.36f,1.06f,-9.99f));Photo(new Vector3(-1.37f,1.05f,-9.52f));
            for(int i=0;i<3;i++)PropBook(new Vector3(-1.33f,.89f+i*.075f,-10.17f),i,i*11);
            var umbrella=Physical(new Vector3(1.38f,.59f,-9.94f),"Зонт",new Vector3(.14f,1.00f,.14f),.55f);Cylinder("Folded umbrella",new Vector3(0,-.09f,0),new Vector3(.13f,.37f,.13f),bookBlue,umbrella);Cylinder("Umbrella brass handle",new Vector3(0,.35f,0),new Vector3(.032f,.16f,.032f),brass,umbrella);Ball("Umbrella crook",new Vector3(.043f,.50f,0),new Vector3(.12f,.10f,.041f),darkOak,umbrella);
            Plant(new Vector3(-1.05f,0,10.05f),1.38f);Candle(new Vector3(-1.39f,1.0f,-9.75f));
            // Small room plaques read naturally at eye height and guide all paths from the gallery.
            Plaque(new Vector3(-1.81f,1.78f,-5.20f),-90,"ГОСТИНАЯ");Plaque(new Vector3(-1.81f,1.78f,2.26f),-90,"КУХНЯ");Plaque(new Vector3(-1.81f,1.78f,8.80f),-90,"ГАРАЖ");Plaque(new Vector3(1.81f,1.78f,-6.20f),90,"КАБИНЕТ");Plaque(new Vector3(1.81f,1.78f,1.26f),90,"СПАЛЬНЯ");Plaque(new Vector3(1.81f,1.78f,8.80f),90,"ЭЛЕОНОРА");
        }
        static void Plaque(Vector3 position,float yaw,string label) {
            var g=Group("Room plaque "+label,position,null,yaw).transform;Box("Brass door plate",Vector3.zero,new Vector3(.85f,.17f,.03f),brass,g);
            // Geometry engraving respects wall depth; the HUD supplies the localized room name.
            for(int i=-2;i<=2;i++)Box("Engraved line",new Vector3(i*.12f,0,-.019f),new Vector3(.07f,.017f,.007f),darkOak,g);
        }
        static void Garden() {
            Box("Night garden",new Vector3(0,-.50f,0),new Vector3(150,.12f,150),green);
            var moonMat=Mat("Cold moon",new Color(.72f,.82f,.87f),.0f);moonMat.EnableKeyword("_EMISSION");moonMat.SetColor("_EmissionColor",new Color(.48f,.6f,.72f));Ball("Moon",new Vector3(-33,27,-43),new Vector3(5,5,5),moonMat);
            for(int i=0;i<30;i++) {
                float angle=i*Mathf.PI*2/30;float r=19+(i%5)*2.3f;var p=new Vector3(Mathf.Cos(angle)*r,0,Mathf.Sin(angle)*r);var g=Group("Garden cypress",p).transform;
                Cylinder("Cypress trunk",new Vector3(0,2.4f,0),new Vector3(.24f,2.4f,.24f),darkOak,g);Ball("Cypress silhouette",new Vector3(0,4.6f,0),new Vector3(2.3f,6.5f,2.3f),green,g);
            }
        }
        static void OptimizeGeometry() {
            // Keep every collider and animated hinge, but merge static mesh surfaces by material.
            // This turns thousands of trim/book/furniture parts into a few dozen draw calls.
            var staticMeshes=new List<MeshFilter>();
            foreach(var mf in root.GetComponentsInChildren<MeshFilter>())if(!mf.GetComponentInParent<HauntedObject>()&&!mf.GetComponent<PlanarMirror>())staticMeshes.Add(mf);
            Combine(root,staticMeshes,"Architectural surfaces");
            foreach(var h in HauntedObject.All.Values)if(h.Kind==HauntKind.Prop)Combine(h.transform,new List<MeshFilter>(h.GetComponentsInChildren<MeshFilter>()),"Prop surfaces");
        }
        static void Combine(Transform parent,List<MeshFilter> sources,string label) {
            var groups=new Dictionary<Material,List<CombineInstance>>();
            foreach(var source in sources) {
                var renderer=source.GetComponent<MeshRenderer>();if(!renderer||!renderer.enabled||!source.sharedMesh||!renderer.sharedMaterial)continue;
                var material=renderer.sharedMaterial;
                if(!groups.ContainsKey(material))groups[material]=new List<CombineInstance>();
                groups[material].Add(new CombineInstance{mesh=source.sharedMesh,transform=parent.worldToLocalMatrix*source.transform.localToWorldMatrix});renderer.enabled=false;
            }
            foreach(var pair in groups) {
                var obj=Group(label+" / "+pair.Key.name,Vector3.zero,parent);
                var mesh=new Mesh();mesh.name=label+" / "+pair.Key.name;mesh.indexFormat=IndexFormat.UInt32;mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();ownedAssets.Add(mesh);
                obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=pair.Key;
            }
        }
    }
    public sealed class ProceduralWorldAssets : MonoBehaviour {
        public UnityEngine.Object[] Owned;
        void OnDestroy() {if(Owned!=null)foreach(var asset in Owned)if(asset)UnityEngine.Object.Destroy(asset);}
    }
}
