using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace VeilHouse {
 public static class LivedInHouse {
  public static readonly Dictionary<string,int> Details=new Dictionary<string,int>();
  static Transform root;static List<Mesh> meshes;
  static Transform Find(string name)=>root.Cast<Transform>().First(t=>t.name==name);
  static GameObject Place(string id,Vector3 p,float yaw=0,float scale=1,bool count=true){
   var prefab=Resources.Load<GameObject>("Props/Prefabs/"+id);if(!prefab)throw new System.InvalidOperationException("Missing household model "+id);
   var o=Object.Instantiate(prefab,root,false);o.name="Household / "+id;o.transform.localPosition=p;o.transform.localRotation=Quaternion.Euler(0,yaw,0)*o.transform.localRotation;o.transform.localScale*=scale;
   if(count){var room=WorldBuilder.RoomAt(p);Details[room]=Details.TryGetValue(room,out int n)?n+1:1;}
   return o;
  }
  static GameObject At(string id,float x,float y,float z,float yaw=0,float scale=1)=>Place(id,HouseLayout.Map(new Vector3(x,y,z)),yaw,scale);
  static GameObject On(string id,string table,Vector3 offset,float yaw=0)=>Place(id,Find(table).TransformPoint(offset),yaw);
  public static void Apply(Transform world){
   root=world;meshes=new List<Mesh>();Details.Clear();
   var chairs=root.Cast<Transform>().Where(t=>t.name=="Club armchair").ToArray();
   chairs[0].Rotate(0,7,0);chairs[0].position+=new Vector3(.18f,0,.12f);
   var painting=root.Cast<Transform>().First(t=>t.name=="Framed landscape");painting.Rotate(0,0,-2.3f,Space.Self);
   var mug=HauntedObject.All.Values.First(h=>h.DisplayName=="Чашка"&&WorldBuilder.RoomAt(h.transform.position)=="Гостиная");mug.transform.position+=new Vector3(.13f,0,-.07f);
   // Living room: things left behind after reading, not random floor debris.
   On("Remote","Drawing-room coffee table",new Vector3(.72f,.55f,.28f),17);
   On("Newspaper","Drawing-room coffee table",new Vector3(.18f,.558f,-.30f),-8);
   On("Napkins","Drawing-room coffee table",new Vector3(.16f,.55f,.26f),7);
   On("Tumbler","Drawing-room coffee table",new Vector3(.81f,.55f,-.26f));
   var sofa=Find("Tailored parlor sofa");Place("Throw",sofa.TransformPoint(new Vector3(.76f,.68f,.04f)),180);
   Place("CrumpledPillow",sofa.TransformPoint(new Vector3(-.45f,.64f,.06f)),163);
   At("PowerStrip",-2.78f,.09f,-9.85f,90);At("Socket",-2.18f,.32f,-9.85f,90);
   var tv=Find("Ранний телевизионный приёмник");Cord("TV power lead",new[]{tv.position+tv.forward*.21f+Vector3.up*.53f,tv.position+tv.forward*.26f+Vector3.up*.13f,HouseLayout.Map(new Vector3(-2.78f,.09f,-9.85f)),HouseLayout.Map(new Vector3(-2.18f,.32f,-9.85f))});
   At("FloorLamp",-9.90f,.07f,-3.95f,15);At("WallSwitch",-2.17f,1.20f,-5.13f,90);
   // Kitchen table and counter.
   On("Napkins","Kitchen oak table",new Vector3(-1.1f,.87f,.32f),12);On("Newspaper","Kitchen oak table",new Vector3(.28f,.87f,-.1f),24);
   On("Tumbler","Kitchen oak table",new Vector3(1.10f,.87f,.28f));On("Tumbler","Kitchen oak table",new Vector3(-1.13f,.87f,-.40f));
   On("Pencil","Kitchen oak table",new Vector3(.54f,.89f,-.29f),33);At("CardboardBox",-4.35f,.075f,3.15f,17,.8f);
   At("Socket",-9.30f,1.29f,3.83f);At("Socket",-4.45f,1.29f,3.83f);At("WallSwitch",-2.17f,1.20f,2.35f,90);
   At("Charger",-5.30f,1.055f,3.44f,12);At("Vent",-11.81f,2.43f,2.2f,-90);
   // Study desk, floor lamp and paperwork.
   On("Newspaper","Partners writing desk",new Vector3(-.90f,.88f,-.13f),12);On("Pencil","Partners writing desk",new Vector3(-.48f,.9f,-.13f),57);
   On("Napkins","Partners writing desk",new Vector3(.76f,.88f,-.27f));On("Tumbler","Partners writing desk",new Vector3(1.10f,.88f,-.22f));
   On("Charger","Partners writing desk",new Vector3(1.17f,.88f,.35f),10);At("CardboardBox",10.55f,.07f,-5.14f,-12);
   At("Letters",10.6f,.09f,-5.0f,15,.7f);At("FloorLamp",3.3f,.07f,-8.3f);At("PowerStrip",8.75f,.08f,-10.1f,90);
   At("Socket",8.75f,.32f,-10.81f,180);At("WallSwitch",2.17f,1.2f,-6.15f,-90);
   // Bedroom and guest room.
   foreach(var guest in new[]{false,true}){
    float x=guest?7.1f:4.48f,z=guest?9.2f:1.6f;
    var bed=root.Cast<Transform>().Where(t=>t.name=="Brass double bed").OrderBy(t=>(t.position-HouseLayout.Map(new Vector3(x,0,z))).sqrMagnitude).First();
    Place("Throw",bed.position+new Vector3(.30f,.735f,-.96f),180,1.15f);
    Place("CrumpledPillow",bed.position+new Vector3(-.28f,.74f,.48f),163);
    At("Shoes",x+.85f,.07f,z-1.23f,24);At("CardboardBox",guest?10.95f:6.1f,.07f,guest?9.2f:-2.75f,18,.65f);
    string table=guest?"Vanity writing table":"Dressing table";
    On("Napkins",table,new Vector3(.42f,guest?.89f:.90f,.09f));On("Charger",table,new Vector3(-.46f,guest?.89f:.90f,.06f),16);
    On("Pencil",table,new Vector3(.1f,guest?.90f:.91f,.08f),38);
    At("Socket",guest?11.81f:2.17f,.33f,guest?5.35f:-2.5f,guest?90:-90);
    At("WallSwitch",2.17f,1.2f,guest?8.86f:1.35f,-90);
    At("PowerStrip",guest?10.9f:2.6f,.08f,guest?5.0f:-2.0f,31);
    var bedside=root.Cast<Transform>().Where(t=>t.name=="Bedside table"&&t.position.x<bed.position.x).OrderBy(t=>(t.position-bed.position).sqrMagnitude).First();
    Place("Newspaper",bedside.position+new Vector3(.04f,guest?.715f:.705f,-.12f),-7,.7f);
   }
   // Bathroom: small items kept beside the washbasin and linen cupboard.
   At("Tumbler",9.55f,1.03f,-3.22f);At("Napkins",8.8f,1.04f,-3.3f,9,.7f);
   At("Charger",7.66f,1.81f,3.39f);At("CardboardBox",7.78f,.075f,2.60f,0,.6f);
   At("FoldedTowel",7.80f,.11f,2.6f,0,.65f);At("Soap",9.18f,1.05f,-3.13f);
   At("Socket",7.18f,1.32f,2.62f,-90);At("WallSwitch",7.18f,1.2f,1.35f,-90);At("Vent",9.9f,2.53f,3.81f);
   At("Shoes",8.0f,.07f,-1.95f,27,.9f);At("Napkins",7.77f,1.81f,3.28f,22,.7f);
   // Garage: workbench clutter and a loose extension lead.
   On("Newspaper","Garage workbench",new Vector3(-.91f,.97f,.03f),-11);On("Pencil","Garage workbench",new Vector3(-.65f,1,.23f),41);
   On("Charger","Garage workbench",new Vector3(.27f,.97f,-.15f),19);On("Napkins","Garage workbench",new Vector3(1.15f,.97f,.04f),12);
   At("CardboardBox",-10.8f,.07f,10.0f,13);At("CardboardBox",-10.0f,.07f,10.1f,-8,.65f);At("Shoes",-3.6f,.07f,5.0f,42);
   At("PowerStrip",-5.05f,.09f,9.7f,48);At("Socket",-5.05f,1.30f,10.8f);At("WallSwitch",-2.17f,1.2f,8.85f,90);At("Vent",-11.8f,2.48f,6.5f,-90);
   // Entrance and gallery: shoes, mail and utilities outside the circulation path.
   At("Shoes",.97f,.07f,-9.35f,12);At("Shoes",1.15f,.07f,-8.90f,-18,.88f);At("CardboardBox",-1.32f,.07f,-8.55f,-5,.7f);
   On("Letters","Entrance console",new Vector3(.27f,.85f,0),12);
   On("Newspaper","Entrance console",new Vector3(-.30f,.85f,.03f),8);On("Pencil","Entrance console",new Vector3(.10f,.88f,-.07f),37);
   At("Socket",1.81f,.33f,-9.3f,90);At("WallSwitch",1.81f,1.20f,-9.4f,90);At("WallSwitch",-1.81f,1.20f,9.5f,-90);
   At("Vent",1.8f,2.40f,3.0f,90);At("Napkins",-1.32f,.08f,-8.5f,15,.65f);
   Architecture();
   HouseLayout.SettleProps();
   foreach(var h in HauntedObject.All.Values)h.ResetHomePose();
   root.gameObject.AddComponent<HouseholdAssets>().Meshes=meshes.ToArray();
   Debug.Log("VH HOUSEHOLD: "+string.Join("; ",Details.Select(p=>p.Key+"="+p.Value)));
  }
  static void Architecture(){
   foreach(var h in HauntedObject.All.Values.Where(h=>h.Kind==HauntKind.Door).ToArray())Place("Threshold",h.transform.position+Vector3.up*.075f,h.transform.eulerAngles.y,1,false);
   foreach(var window in root.Cast<Transform>().Where(t=>t.name=="Moonlit sash window").ToArray()){
    var p=window.position+window.forward*.43f;p.y=.075f;Place("Radiator",p,window.eulerAngles.y+180);
   }
   foreach(var t in root.Cast<Transform>().Where(t=>t.name=="Skirting board").ToArray()){
    var s=t.localScale;bool alongX=s.x>s.z;float length=alongX?s.x:s.z;
    t.GetComponent<Renderer>().enabled=false;
    for(int side=-1;side<=1;side+=2){var p=t.position+(alongX?Vector3.forward:Vector3.right)*side*.14f;p.y=.07f;var o=Place("TrimSegment",p,(alongX?0:90)+(side>0?180:0),1,false);o.transform.localScale=Vector3.Scale(o.transform.localScale,new Vector3(length,1,1));}
   }
   foreach(var lamp in root.Cast<Transform>().Where(t=>t.name=="Household / FloorLamp").ToArray()){
    var glow=lamp.GetComponentsInChildren<MeshRenderer>().First(r=>r.sharedMaterial.name=="Glow");glow.shadowCastingMode=ShadowCastingMode.Off;
    var light=new GameObject("Floor lamp reading pool").AddComponent<Light>();light.transform.SetParent(lamp,false);light.transform.localPosition=new Vector3(0,1.19f,0);light.type=LightType.Spot;light.transform.localRotation=Quaternion.Euler(90,0,0);light.spotAngle=130;light.innerSpotAngle=75;light.intensity=.95f;light.range=3.1f;light.color=new Color(1,.74f,.48f);
    var nearest=HauntedObject.All.Values.Where(h=>h.Kind==HauntKind.Light&&WorldBuilder.RoomAt(h.transform.position)==WorldBuilder.RoomAt(lamp.position)).OrderBy(h=>(h.transform.position-lamp.position).sqrMagnitude).First();
    nearest.Lamps=nearest.Lamps.Concat(new[]{light}).ToArray();nearest.EmissiveParts=nearest.EmissiveParts.Concat(new Renderer[]{glow}).ToArray();
    lamp.SetParent(nearest.transform,true);
    Cord("Floor lamp power lead",new[]{lamp.position+Vector3.up*.08f,lamp.position+new Vector3(.18f,.085f,.23f),lamp.position+new Vector3(.27f,.08f,.35f)});
   }
  }
  static void Cord(string name,Vector3[] points){
   var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
   for(int j=0;j<points.Length;j++){
    Vector3 tangent=(points[Mathf.Min(j+1,points.Length-1)]-points[Mathf.Max(j-1,0)]).normalized;
    Vector3 a=Vector3.Cross(tangent,Vector3.up).normalized;if(a.sqrMagnitude<.1f)a=Vector3.right;Vector3 b=Vector3.Cross(tangent,a);
    for(int i=0;i<8;i++){float angle=i*Mathf.PI/4;v.Add(points[j]+(a*Mathf.Cos(angle)+b*Mathf.Sin(angle))*.0055f);uv.Add(new Vector2(i/8f,j*.3f));if(j>0){int q=j*8+i,r=j*8+(i+1)%8;tri.AddRange(new[]{q-8,r-8,q,q,r-8,r});}}
   }
   var m=new Mesh{name=name};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tri,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();meshes.Add(m);
   var o=new GameObject(name);o.transform.SetParent(root,false);o.AddComponent<MeshFilter>().sharedMesh=m;o.AddComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>("Materials/Rubber");
  }
 }
 public sealed class HouseholdAssets:MonoBehaviour {public Mesh[] Meshes;void OnDestroy(){if(Meshes!=null)foreach(var m in Meshes)if(m){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}}}
}
