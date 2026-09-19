using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace VeilHouse {
 public static class DetailedObjects {
  public static int ReplacedCount {get;private set;}
  static Bounds LocalBounds(Transform parent,IEnumerable<MeshFilter> meshes){
   var b=new Bounds();bool first=true;
   foreach(var mf in meshes){
    var mb=mf.sharedMesh.bounds;var m=parent.worldToLocalMatrix*mf.transform.localToWorldMatrix;
    for(int i=0;i<8;i++){
     var p=m.MultiplyPoint3x4(mb.center+Vector3.Scale(mb.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
     if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);
    }
   }
   return b;
  }
  public static GameObject Model(string id,Transform parent,Bounds target,bool stretch=true,float yaw=0){
   var prefab=Resources.Load<GameObject>("Props/Prefabs/"+id);
   if(!prefab)throw new InvalidOperationException("Missing authored prop "+id);
   var o=UnityEngine.Object.Instantiate(prefab,parent,false);o.name="Detailed / "+id;
   o.transform.localRotation=Quaternion.Euler(0,yaw,0)*o.transform.localRotation;
   var b=LocalBounds(parent,o.GetComponentsInChildren<MeshFilter>());
   var scale=new Vector3(target.size.x/Mathf.Max(.001f,b.size.x),target.size.y/Mathf.Max(.001f,b.size.y),target.size.z/Mathf.Max(.001f,b.size.z));
   if(!stretch)scale=Vector3.one*Mathf.Min(scale.x,scale.y,scale.z);
   o.transform.localScale=Vector3.Scale(o.transform.localScale,scale);
   b=LocalBounds(parent,o.GetComponentsInChildren<MeshFilter>());
   var position=target.center-b.center;position.y=target.min.y-b.min.y;
   o.transform.localPosition+=position;ReplacedCount++;return o;
  }
  public static GameObject Replace(Transform parent,string id,bool children=true,bool stretch=true,float yaw=0){
   var meshes=new List<MeshFilter>();
   var haunt=parent.GetComponent<HauntedObject>();
   foreach(var mf in parent.GetComponentsInChildren<MeshFilter>(true)){
    var r=mf.GetComponent<MeshRenderer>();
    if(mf.name=="Marble worktop")continue;
    if(haunt&&haunt.Kind==HauntKind.Water&&haunt.ActiveVisual&&mf.gameObject==haunt.ActiveVisual)continue;
    if(r&&r.enabled&&(children||mf.transform.parent==parent||mf.transform==parent))meshes.Add(mf);
   }
   if(meshes.Count==0)throw new InvalidOperationException("No source visual for "+parent.name);
   var bounds=LocalBounds(parent,meshes);foreach(var mf in meshes)mf.GetComponent<MeshRenderer>().enabled=false;
   var model=Model(id,parent,bounds,stretch,yaw);
   if(haunt){
    var renderers=model.GetComponentsInChildren<MeshRenderer>();
    if(haunt.Kind==HauntKind.Light)haunt.EmissiveParts=renderers.Where(r=>r.sharedMaterial.name=="Glow").Cast<Renderer>().ToArray();
    if(haunt.Kind==HauntKind.Television||haunt.Kind==HauntKind.Radio){
     var r=renderers.FirstOrDefault(x=>x.sharedMaterial.name==(haunt.Kind==HauntKind.Television?"Screen":"Glow"));
     if(r){if(haunt.ActiveVisual)haunt.ActiveVisual.SetActive(false);haunt.ActiveVisual=r.gameObject;r.gameObject.SetActive(haunt.Active);}
    }
   }
   return model;
  }
  public static void Apply(Transform root){
   int variant=0;
   // Snapshot before instantiating any models; only original composition nodes are visited.
   foreach(var t in root.GetComponentsInChildren<Transform>(true)){
    var h=t.GetComponent<HauntedObject>();string id=null;
    if(h){
     if(h.Kind==HauntKind.Door){Replace(t,"DoorCasing",false);Replace(h.Hinge,"PanelDoor");continue;}
     if(h.Kind==HauntKind.Cabinet){
      string style=h.DisplayName.Contains("Кухон")?"Kitchen":h.DisplayName.Contains("Инструмент")?"Tool":h.DisplayName.Contains("документ")?"Document":"Wardrobe";
      Replace(t,style+"Body",false);Replace(h.Hinge,style+"Leaf");continue;
     }
     if(h.Kind==HauntKind.Light){id=h.DisplayName=="Люстра гостиной"?"Chandelier":h.DisplayName=="Банкирская лампа"?"BankerLamp":t.Find("Lamp weighted foot")?"TableLamp":"Pendant";}
     else if(h.Kind==HauntKind.Television)id="Television";
     else if(h.Kind==HauntKind.Radio)id=h.DisplayName=="Напольные часы"?"GrandfatherClock":"Radio";
     else if(h.Kind==HauntKind.Water){
      if(h.DisplayName=="Кран умывальника"){Replace(t,"Sink",true,true,180);continue;}
      id=h.DisplayName=="Кран ванны"?"BathTap":"Sink";
     }
     else if(h.Kind==HauntKind.Prop){
      switch(h.DisplayName){
       case "Тарелка":id=(variant++%2==0)?"PlateIvory":"PlateBlue";break;
       case "Ваза":id=(variant++%2==0)?"VasePear":"VaseFluted";break;
       case "Подсвечник":id="Candlestick";break;
       case "Фотография в рамке":id=(variant++%2==0)?"PhotoCouple":"PhotoManor";break;
       case "Бутылка":case "Графин":id="Wine";break;
       case "Бутылка молока":id="Milk";break;
       case "Флакон духов":id="Perfume";break;
       case "Аптечный флакон":id="Medicine";break;
       case "Банка специй":id=(variant++%2==0)?"JarTea":"JarSpice";break;
       case "Маслёнка":id="Oil";break;
       case "Чайник":id="Kettle";break;
       case "Книга":id=(variant++%2==0)?"BookRedDetail":"BookBlueDetail";break;
       case "Телефон":id="Telephone";break;
       case "Глобус":id="Globe";break;
       case "Зонт":id="Umbrella";break;
       case "Чернильница":id="Inkpot";break;
       case "Сложенное полотенце":id="FoldedTowel";break;
       case "Мыло":id="Soap";break;
       case "Гаечный ключ":id="Wrench";break;
       case "Молоток":id="Hammer";break;
       case "Коробка инструментов":id="Toolbox";break;
       case "Деревянный ящик":id="WoodCrate";break;
       case "Канистра":id="Canister";break;
       case "Чемодан":case "Кожаный чемодан":id="Suitcase";break;
       case "Шкатулка":case "Музыкальная шкатулка":id="JewelryBox";break;
       case "Спичечный коробок":id="Matchbox";break;
       case "Хлебница":id="Breadbox";break;
       case "Жестяная коробка чая":id="JarTea";break;
       case "Письма":id="Letters";break;
      }
     }
    }else{
     switch(t.name){
      case "Moonlit sash window":Replace(t,"SashWindow",true,true,180);continue;
      case "Oak entrance":Replace(t,"EntranceDoors",true,true,180);continue;
      case "Garage roller door":id="GarageDoors";break;
      case "Garage door panel":t.GetComponent<MeshRenderer>().enabled=false;continue;
      case "Garden cypress":id="Cypress";break;
      case "Family portrait, 1924":id="PortraitFrame";break;
      case "Framed landscape":id=(variant++%2==0)?"Landscape":"StillLife";break;
      case "Parlor palm":id=(variant++%2==0)?"PalmPlanter":"FernPlanter";break;
      case "Limestone fireplace":id="Fireplace";break;
      case "Enamel range":Replace(t,"Stove",true,true,180);continue;
      case "Brass double bed":id=t.position.z>4?"GuestBed":"BrassBed";break;
      case "Claw-foot bath":id="Bathtub";break;
      case "Pedestal washbasin":id="Pedestal";break;
      case "Porcelain toilet":Replace(t,"Toilet",true,true,180);continue;
      case "1927 touring motorcar":id="Motorcar";break;
      case "Spare tire stack":id="SpareTire";break;
      case "Spare wheel opening":case "Mirror vertical frame":case "Mirror horizontal frame":case "Pan handle":
       t.GetComponent<MeshRenderer>().enabled=false;continue;
      case "Oxidized mirror":
       var mirror=Replace(t,"Mirror",true,true,180);
       foreach(var r in mirror.GetComponentsInChildren<MeshRenderer>())if(r.sharedMaterial.name=="MirrorSilver")r.gameObject.AddComponent<PlanarMirror>();
       continue;
      case "Bath mat":id="BathMat";break;
      case "Hanging copper pan":Replace(t,"CopperPan",true,true,180);continue;
     }
    }
    if(id!=null)Replace(t,id,true,true,h&&h.DisplayName=="Фотография в рамке"&&t.position.x>2&&t.position.z< -4?180:0);
   }
   // Add planters beside the entrance; they are scenery and do not change network ids.
   for(int s=-1;s<=1;s+=2){
    var g=new GameObject("Garden flower planter");g.transform.SetParent(root,false);g.transform.localPosition=new Vector3(s*3.5f,0,-11.65f);
    Model("Flowerbed",g.transform,new Bounds(new Vector3(0,.36f,0),new Vector3(1.8f,.70f,.65f)));
   }
   Debug.Log("VH DETAILED OBJECTS: "+ReplacedCount+" replacements; interactives="+HauntedObject.All.Count);
  }
  public static void Reset(){ReplacedCount=0;}
 }
}
