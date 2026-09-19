using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace VeilHouse {
 public static class HouseLayout {
  public const float PlanScale=.78f;
  public const float CeilingHeight=3.0f;
  public static Vector3 Map(Vector3 p)=>new Vector3(p.x*PlanScale,p.y,p.z*PlanScale);
  public static Vector3 Unmap(Vector3 p)=>new Vector3(p.x/PlanScale,p.y,p.z/PlanScale);
  public static void SettleProps(){
   Physics.SyncTransforms();
   foreach(var h in HauntedObject.All.Values.Where(h=>h.Kind==HauntKind.Prop)){
    var c=h.GetComponent<BoxCollider>();if(!c)continue;
    var hits=Physics.RaycastAll(h.transform.position+Vector3.up*.5f,Vector3.down,1.6f,~0,QueryTriggerInteraction.Ignore);
    var supports=hits.Where(hit=>hit.normal.y>.75f&&hit.point.y<=h.transform.position.y+.05f&&hit.collider.GetComponentInParent<HauntedObject>()?.Kind!=HauntKind.Prop).OrderByDescending(hit=>hit.point.y).ToArray();
    if(supports.Length==0)continue;float lift=supports[0].point.y+.006f-c.bounds.min.y;
    if(lift>.002f&&lift<.27f)h.transform.position+=Vector3.up*lift;
   }
   Physics.SyncTransforms();
  }
  public static void Apply(Transform root){
   var nodes=root.Cast<Transform>().ToArray();
   var original=nodes.ToDictionary(t=>t,t=>t.position);
   var tables=nodes.Where(t=>t.GetComponentsInChildren<Transform>().Any(m=>m.name.StartsWith("Authored furniture / Table_"))).ToArray();
   foreach(var t in nodes){
    var p=original[t];t.position=Map(p);
    var n=t.name;
    bool architecture=n=="Plaster wall"||n.EndsWith(" floor")||n=="Manor foundation"||n=="Ceiling"||n=="Picture rail"||n=="Crown moulding"||n=="Skirting board"||n=="Oak wainscoting"||n=="Dado rail"||n=="Wainscot stile";
    if(architecture){var s=t.localScale;s.x*=PlanScale;s.z*=PlanScale;t.localScale=s;}
    if(n=="Plaster wall"){var s=t.localScale;s.y=2.90f;t.localScale=s;t.position=new Vector3(t.position.x,1.45f,t.position.z);}
    if(n=="Ceiling")t.position=new Vector3(t.position.x,CeilingHeight,t.position.z);
    if(n=="Crown moulding")t.position=new Vector3(t.position.x,2.88f,t.position.z);
    if(n=="Picture rail")t.position=new Vector3(t.position.x,2.64f,t.position.z);
    if(n=="Door lintel"){t.position=new Vector3(t.position.x,2.84f,t.position.z);t.localScale=new Vector3(t.localScale.x*PlanScale,.32f,t.localScale.z);}
    if(n=="Moonlit sash window"){t.localScale=new Vector3(PlanScale,1,1);t.position+=Vector3.down*.22f;}
    if(n=="Library bookcase"||n=="Limestone fireplace"||t.GetComponent<HauntedObject>()?.Kind==HauntKind.Cabinet)t.localScale=new Vector3(PlanScale,1,1);
    if(n=="Persian wool carpet"||n=="Bath mat"||n=="Carpet fringe")t.localScale=Vector3.Scale(t.localScale,new Vector3(PlanScale,1,PlanScale));
    var h=t.GetComponent<HauntedObject>();
    if(h&&h.Kind==HauntKind.Light&&p.y>3)t.position+=Vector3.down*.60f;
   }
   // Keep table-top objects at their real size and offset from the supporting table.
   foreach(var t in nodes){
    var h=t.GetComponent<HauntedObject>();if(!h||(h.Kind!=HauntKind.Prop&&h.Kind!=HauntKind.Light))continue;
    var p=original[t];
    var table=tables.FirstOrDefault(a=>{
     var boxes=a.GetComponents<BoxCollider>();if(boxes.Length==0)return false;
     var q=Quaternion.Inverse(a.rotation)*(p-original[a]);var b=boxes[0];
     return Mathf.Abs(q.x)<b.size.x*.55f&&Mathf.Abs(q.z)<b.size.z*.65f&&q.y>b.center.y-.05f&&q.y<b.center.y+.95f;
    });
    if(table)t.position=table.position+(p-original[table]);
   }
   foreach(var table in tables.Where(t=>t.name=="Bedside table")){
    var bed=nodes.Where(t=>t.name=="Brass double bed").OrderBy(t=>(t.position-table.position).sqrMagnitude).First();
    var delta=Vector3.right*Mathf.Sign(table.position.x-bed.position.x)*.23f;var center=table.position;
    foreach(var t in nodes){var h=t.GetComponent<HauntedObject>();var d=t.position-center;if(t==table||(h&&Mathf.Abs(d.x)<.45f&&Mathf.Abs(d.z)<.45f&&d.y>.6f&&d.y<1.6f))t.position+=delta;}
   }
   // Preserve a standing-width route across the master bedroom after compressing the plan.
   var masterBed=nodes.First(t=>t.name=="Brass double bed"&&WorldBuilder.RoomAt(t.position)=="Спальня хозяина");
   masterBed.position+=Vector3.forward*.43f;
   foreach(var table in tables.Where(t=>t.name=="Bedside table"&&WorldBuilder.RoomAt(t.position)=="Спальня хозяина")){
    var center=table.position;foreach(var t in nodes){var h=t.GetComponent<HauntedObject>();var d=t.position-center;if(t==table||(h&&Mathf.Abs(d.x)<.45f&&Mathf.Abs(d.z)<.45f&&d.y>.6f&&d.y<1.6f))t.position+=Vector3.forward*.43f;}
   }
   var bedroomDoor=HauntedObject.All.Values.First(h=>h.DisplayName=="Дверь спальни");
   bedroomDoor.OpenAngle=-172;bedroomDoor.Hinge.localRotation=Quaternion.Euler(0,bedroomDoor.Active?-172:0,0);
   // Leave space for the radiator and the gathered curtain behind the bath.
   foreach(var t in nodes.Where(t=>t.name=="Claw-foot bath"||t.name=="Кран ванны"||t.name=="Bath mat"))t.position+=Vector3.left*.38f;
   foreach(var chair in nodes.Where(t=>t.name=="Upholstered dining chair")){
    var table=tables.OrderBy(t=>(t.position-chair.position).sqrMagnitude).First();var b=table.GetComponents<BoxCollider>()[0];
    var q=table.InverseTransformPoint(chair.position);float space=b.size.z*.5f+.40f;
    if(Mathf.Abs(q.x)<b.size.x*.5f+.3f&&Mathf.Abs(q.z)<space&&Mathf.Abs(q.z)>.3f){q.z=Mathf.Sign(q.z)*space;chair.position=table.TransformPoint(q);}
   }
   foreach(var h in HauntedObject.All.Values)h.ResetHomePose();
  }
 }
}
