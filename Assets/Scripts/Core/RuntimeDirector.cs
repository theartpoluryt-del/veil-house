using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VeilHouse {
 public sealed class RuntimeDirector : MonoBehaviour {
  public static RuntimeDirector I;
  public Camera View; public FirstPersonController Controller; public GameUI UI;
  public string Toast=""; public float ToastUntil;
  GameObject house; GameSession session;
  readonly Dictionary<int,DetectiveAvatar> avatars=new Dictionary<int,DetectiveAvatar>();
  readonly Dictionary<int,float> interacting=new Dictionary<int,float>();
  float blackoutUntil; readonly List<int> darkLights=new List<int>();
  GamePhase previous=GamePhase.Menu;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Launch() {
   if (FindFirstObjectByType<RuntimeDirector>()!=null) return;
   var root=new GameObject("VEIL HOUSE · Runtime"); root.AddComponent<RuntimeDirector>();
  }
  void Awake() {
   I=this; Application.targetFrameRate=90; QualitySettings.vSyncCount=0;
   session=gameObject.AddComponent<GameSession>();
   var cameraObject=new GameObject("Investigation camera"); View=cameraObject.AddComponent<Camera>();
   View.fieldOfView=65; View.nearClipPlane=.045f; View.farClipPlane=140;
   View.allowHDR=true; View.backgroundColor=new Color(.025f,.045f,.065f);
   AtmosphereEffect.Attach(View);
   View.tag="MainCamera"; cameraObject.AddComponent<AudioListener>();
   Soundscape.Create(); HauntedObject.Sound=Soundscape.PlayAt;
   BuildHouse();
   Controller=gameObject.AddComponent<FirstPersonController>(); Controller.Initialize(View);
   UI=gameObject.AddComponent<GameUI>();
   session.MatchStarted+=OnMatch;
   session.MatchEnded+=()=> { Notify("Расследование завершено. Дом раскроет свою тайну."); Soundscape.PlayAt(View.transform.position,"pulse"); };
   session.ValidateInteraction=Validate;
   session.InteractionAccepted=Act;
   session.PlayerInteracted+=id=>interacting[id]=Time.time+.55f;
   session.CaptureWorld=()=>HauntedObject.All.Values.Select(o=>o.Capture()).ToArray();
   session.ApplyWorld=states=> { foreach(var st in states) if(HauntedObject.All.TryGetValue(st.id,out var o)) o.Apply(st); };
   session.WorldEvent+=(id,action)=> { if(session.IsHost)return; if(id<0){var ghost=session.Players.Find(p=>p.role==PlayerRole.Ghost);Soundscape.PlayAt(action=="blackout"||ghost==null?View.transform.position:ghost.Position,action=="blackout"?"blackout":"pulse");return;} if(HauntedObject.All.TryGetValue(id,out var obj)) Soundscape.PlayAt(obj.transform.position,SoundFor(obj,action)); };
   gameObject.AddComponent<RuntimeProbe>();
  }
  public void BuildHouse() {
   if(house!=null) { house.SetActive(false); Destroy(house); }
   HauntedObject.ResetRegistry(); house=WorldBuilder.Build();
   foreach(var obj in HauntedObject.All.Values) obj.SetAuthority(session.IsHost);
  }
  void OnMatch() {
   foreach(var a in avatars.Values) if(a!=null) Destroy(a.transform.parent.gameObject); avatars.Clear();
   BuildHouse(); Controller.Spawn(WorldBuilder.SpawnPoint(Mathf.Max(0,session.Players.FindIndex(p=>p.id==session.LocalId))));
   blackoutUntil=0; darkLights.Clear(); UI.ClosePanels();
   Notify(session.LocalPlayer!=null && session.LocalPlayer.role==PlayerRole.Ghost ? "Вы — Призрак. Откройте историю клавишей Tab и передавайте подсказки через предметы." : "Вы — Детектив. Наблюдайте за домом и отмечайте гипотезы в журнале: Tab.",8);
  }
  float Validate(InteractionRequest r) {
   var player=session.Players.Find(p=>p.id==r.playerId);
   if(player==null) return -1;
   if(r.action=="poltergeist") return player.role==PlayerRole.Ghost ? 35 : -1;
   if(r.action=="blackout") return player.role==PlayerRole.Ghost && blackoutUntil<=Time.time ? 28 : -1;
   return HauntedObject.Validate(r);
  }
  void Act(InteractionRequest r) {
   interacting[r.playerId]=Time.time+.55f;
   if(r.action=="poltergeist") {
    var p=session.Players.Find(x=>x.id==r.playerId); if(p==null)return;
    foreach(var o in HauntedObject.All.Values) if(o.Kind==HauntKind.Prop && Vector3.Distance(p.Position,o.transform.position)<5) {
     var shake=new InteractionRequest{playerId=r.playerId,objectId=o.Id,action="shake"}; shake.Direction=Vector3.up; o.ServerAct(shake);
    }
    Soundscape.PlayAt(p.Position,"pulse"); return;
   }
   if(r.action=="blackout") {
    darkLights.Clear(); foreach(var o in HauntedObject.All.Values) if(o.Kind==HauntKind.Light && o.Active) {darkLights.Add(o.Id);o.ServerAct(new InteractionRequest{playerId=r.playerId,objectId=o.Id,action="toggle"});}
    blackoutUntil=Time.time+4; Soundscape.PlayAt(View.transform.position,"blackout"); return;
   }
   if(HauntedObject.All.TryGetValue(r.objectId,out var obj)) obj.ServerAct(r);
  }
  string SoundFor(HauntedObject o,string action) {
   if(action=="hold")return "pickup";
   if(action=="throw")return "throw";
   if(action=="shake")return "rattle";
   if(o.Kind==HauntKind.Door||o.Kind==HauntKind.Cabinet)return "door";
   return o.Kind.ToString().ToLowerInvariant();
  }
  void Update() {
   if(session.IsHost)foreach(var o in HauntedObject.All.Values)if(o.Holder>=0&&!session.Players.Exists(p=>p.id==o.Holder))o.ReleasePlayer(o.Holder);
   if(session.IsHost && blackoutUntil>0 && Time.time>blackoutUntil) {
    foreach(int id in darkLights) if(HauntedObject.All.TryGetValue(id,out var o)&&!o.Active)o.ServerAct(new InteractionRequest{objectId=id,action="toggle"});
    darkLights.Clear();blackoutUntil=0;
   }
   if(session.Phase==GamePhase.Menu || session.Phase==GamePhase.Lobby) {
    float sway=Mathf.Sin(Time.time*.09f)*.14f;
    View.transform.position=HouseLayout.Map(new Vector3(-10.5f+sway,1.75f,-9.5f));
    View.transform.LookAt(HouseLayout.Map(new Vector3(-4.5f,1.5f,-3.2f)));
   }
   foreach(var p in session.Players) {
    if(p.role==PlayerRole.Ghost || session.Phase==GamePhase.Menu || session.Phase==GamePhase.Lobby) {
     if(avatars.TryGetValue(p.id,out var existing))existing.SetVisible(false); continue;
    }
    if(!avatars.TryGetValue(p.id,out var avatar)||avatar==null) {
     var pivot=new GameObject("Detective · "+p.name); avatar=DetectiveAvatar.Create(pivot.transform,p.id);
     avatars[p.id]=avatar;
    }
    var t=avatar.transform.parent;
    t.position=Vector3.Lerp(t.position,p.Position,1-Mathf.Exp(-14*Time.deltaTime));
    t.rotation=Quaternion.Slerp(t.rotation,Quaternion.Euler(0,p.yaw,0),1-Mathf.Exp(-14*Time.deltaTime));
    avatar.SetVisible(p.id!=session.LocalId);
    avatar.Animate(session.Phase==GamePhase.Investigation?p.speed:0,p.crouch,interacting.TryGetValue(p.id,out float until)&&until>Time.time);
   }
   foreach(int id in avatars.Keys.ToArray())if(!session.Players.Exists(p=>p.id==id)){Destroy(avatars[id].transform.parent.gameObject);avatars.Remove(id);}
   if(previous!=session.Phase) { previous=session.Phase; if(previous==GamePhase.Menu)UI.ClosePanels(); }
  }
  public void Notify(string text,float seconds=4){Toast=text;ToastUntil=Time.unscaledTime+seconds;}
 }
}
