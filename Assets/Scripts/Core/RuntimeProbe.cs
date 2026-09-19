using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace VeilHouse {
 // Explicit development-build probes. No automatic actions in an ordinary launch.
 public sealed class RuntimeProbe : MonoBehaviour {
  public static bool Running;
  string mode,dir;int port;GameSession S=>GameSession.I;RuntimeDirector D=>RuntimeDirector.I;
  List<string> checks=new List<string>(), errors=new List<string>();
  void OnEnable(){Application.logMessageReceived+=Log;}
  void OnDisable(){Application.logMessageReceived-=Log;}
  void Log(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error)errors.Add(message);}
  string Arg(string key,string fallback=""){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
  IEnumerator Start(){
   mode=Arg("--vh-probe");if(string.IsNullOrEmpty(mode))yield break;Running=true;
   dir=Arg("--vh-out",Application.persistentDataPath);Directory.CreateDirectory(dir);port=int.Parse(Arg("--vh-port","7788"));
   yield return new WaitForSeconds(2);
   if(mode=="menu"){yield return Capture("menu");Finish();yield break;}
   if(mode=="gallery") {S.StartTraining(PlayerRole.Ghost);yield return new WaitForSeconds(3);yield return Gallery();Finish();yield break;}
   if(mode=="walk") {S.StartTraining(PlayerRole.Detective);yield return new WaitForSeconds(2);yield return WalkHouse();Finish();yield break;}
   if(mode=="ghost"||mode=="detective"){
    S.StartTraining(mode=="ghost"?PlayerRole.Ghost:PlayerRole.Detective);yield return new WaitForSeconds(3);D.Controller.Teleport(new Vector3(-8,.15f,-7),35);yield return new WaitForSeconds(1);yield return Capture(mode);D.UI.JournalOpen=true;yield return Capture(mode+"-journal");Check(S.Phase==GamePhase.Investigation,"Training started");Check(HauntedObject.All.Count>50,"Furnished interactive house");Finish();yield break;
   }
   if(mode=="host") {S.Host("Призрак · host",port);yield return new WaitForSeconds(.5f);S.SetRole(PlayerRole.Ghost);S.MatchDuration=48;}
   else S.Join(mode=="client1"?"Агата":"Моррис","127.0.0.1",port);
   float timeout=Time.realtimeSinceStartup+50;
   while(S.Phase!=GamePhase.Investigation&&Time.realtimeSinceStartup<timeout){if(mode=="host"&&S.Players.Count>=3){S.StartMatch();}yield return new WaitForSeconds(.25f);}
   Check(S.Phase==GamePhase.Investigation,"Reached network investigation");
   if(S.Phase!=GamePhase.Investigation){Finish();yield break;}
   Check(S.Players.Count==3,"Three connected players");Check(S.Players.Count(p=>p.role==PlayerRole.Ghost)==1,"Exactly one ghost");
   Check(S.Catalog.stories==null||S.Catalog.stories.Length==0,"Public catalog excludes complete stories");
   D.UI.ClosePanels();
   if(mode=="host"){
    Check(S.KnownStory!=null,"Ghost receives private story");File.WriteAllText(Path.Combine(dir,"test-story.json"),JsonUtility.ToJson(S.KnownStory));
    D.Controller.Teleport(new Vector3(-7,.15f,-6),30);yield return new WaitForSeconds(1);
    var obj=HauntedObject.All.Values.FirstOrDefault(o=>o.Kind==HauntKind.Prop&&Vector3.Distance(o.transform.position,D.Controller.Position)<5);
    if(obj!=null){float before=S.LocalPlayer.energy;S.RequestInteraction(obj.Id,"hold",D.Controller.Position+new Vector3(0,1.8f,1),Vector3.forward);yield return new WaitForSeconds(.3f);S.RequestInteraction(obj.Id,"throw",obj.transform.position,Vector3.up+Vector3.right);yield return new WaitForSeconds(.2f);Check(S.LocalPlayer.energy<before,"Ghost spends energy on physics");}
    yield return new WaitForSeconds(1);S.RequestInteraction(-1,"blackout",D.Controller.Position,Vector3.up);yield return new WaitForSeconds(1);yield return Capture("network-ghost");
   }else{
    Check(S.KnownStory==null,"Detective cannot access private story before reveal");
    timeout=Time.realtimeSinceStartup+10;string f=Path.Combine(dir,"test-story.json");while(!File.Exists(f)&&Time.realtimeSinceStartup<timeout)yield return new WaitForSeconds(.2f);
    if(File.Exists(f)){var truth=JsonUtility.FromJson<DeathStory>(File.ReadAllText(f));foreach(var a in truth.answers){S.SubmitAnswer(a.categoryId,a.optionId);yield return new WaitForSeconds(.2f);}yield return new WaitForSeconds(.7f);Check(S.Journal.Count==truth.answers.Length,"Private journal stores every category");}
    float energy=S.LocalPlayer.energy;S.RequestInteraction(-1,"poltergeist",Vector3.zero,Vector3.up);yield return new WaitForSeconds(.5f);Check(Mathf.Approximately(S.LocalPlayer.energy,energy),"Detective ghost ability rejected");
    D.Controller.Teleport(new Vector3(-7+(mode=="client1"?1:-1),.15f,-7),0);yield return new WaitForSeconds(1);D.UI.JournalOpen=true;yield return Capture("network-"+mode+"-journal");D.UI.ClosePanels();
   }
   yield return new WaitForSeconds(3);
   Check(HauntedObject.All.Values.Count(o=>o.Body!=null)>25,"Physical prop population");
   File.WriteAllText(Path.Combine(dir,mode+"-objects.json"),JsonUtility.ToJson(new ObjectDump{objects=HauntedObject.All.Values.Select(o=>o.Capture()).ToArray()},true));
   timeout=Time.realtimeSinceStartup+55;
   while(S.Phase==GamePhase.Investigation&&Time.realtimeSinceStartup<timeout)yield return new WaitForSeconds(.5f);
   Check(S.Phase==GamePhase.Results,"Timer reaches results");Check(S.KnownStory!=null,"All players receive revealed story");Check(S.Scores.Count==2,"Two detective score rows");Check(S.Scores.All(x=>x.correct==5),"Five correct answers score five points");
   yield return Capture("results-"+mode);
   File.WriteAllText(Path.Combine(dir,mode+"-scores.json"),JsonUtility.ToJson(new ScoreDump{scores=S.Scores.ToArray()},true));
   if(mode=="host"){
    yield return new WaitForSeconds(3);S.ReturnToLobby();yield return new WaitForSeconds(1);Check(S.Phase==GamePhase.Lobby,"Return to lobby works");S.StartMatch();yield return new WaitForSeconds(1);Check(S.Phase==GamePhase.Investigation,"Second match starts");
    timeout=Time.realtimeSinceStartup+12;while((!File.Exists(Path.Combine(dir,"client1-second-ready"))||!File.Exists(Path.Combine(dir,"client2-second-ready")))&&Time.realtimeSinceStartup<timeout)yield return new WaitForSeconds(.25f);
    Check(File.Exists(Path.Combine(dir,"client1-second-ready"))&&File.Exists(Path.Combine(dir,"client2-second-ready")),"Both clients entered second match");S.FinishMatch();yield return new WaitForSeconds(1);
   }
   else {
    bool sawLobby=false,sawSecond=false;timeout=Time.realtimeSinceStartup+16;
    while(Time.realtimeSinceStartup<timeout){if(S.Phase==GamePhase.Lobby)sawLobby=true;if(sawLobby&&S.Phase==GamePhase.Investigation&&S.Players.Count>=2){sawSecond=true;break;}yield return new WaitForSeconds(.1f);}
    Check(sawLobby&&sawSecond,"Connected lobby to second match transition");Check(sawSecond&&S.Journal.Count==0,"Second match resets private journal while connected");
    File.WriteAllText(Path.Combine(dir,mode+"-second-ready"),sawSecond?"PASS":"FAIL");yield return new WaitForSeconds(3);
   }
   Finish();
  }
  IEnumerator Gallery(){
   D.UI.enabled=false;D.Controller.enabled=false;Cursor.lockState=CursorLockMode.None;
   string[] surfaces={"SmokedOak","Parquet","Plaster","TealPaint","Wallpaper","Tile","BurgundyFabric","Linen","Leather","Brass","PersianRug"};
   foreach(var id in surfaces){var m=Resources.Load<Material>("Materials/"+id);Check(m&&m.mainTexture&&m.mainTexture.width==2048&&m.GetTexture("_BumpMap")&&m.GetTexture("_MetallicGlossMap")&&m.IsKeywordEnabled("_NORMALMAP"),"Packaged PBR surface "+id);}
   Check(HauntedObject.All.Count==129,"Material update preserves 129 interactive objects");
   string[] furniture={"Sofa","Armchair","DiningChair","Table_2500_1240_490","Bookcase_2600"};
   foreach(var id in furniture){var model=Resources.Load<GameObject>("Furniture/Prefabs/"+id);Check(model&&model.GetComponentsInChildren<MeshFilter>().Sum(x=>x.sharedMesh.vertexCount)>1000,"Detailed furniture mesh "+id);}
   var positions=new[]{new Vector3(-10.5f,1.95f,-9.5f),new Vector3(-10.5f,2,3),new Vector3(3.2f,1.9f,-9.5f),new Vector3(3.2f,1.9f,-1.5f),new Vector3(8,1.9f,-1.5f),new Vector3(-10,2,5),new Vector3(3,1.9f,5)};
   var targets=new[]{new Vector3(-4.5f,1.4f,-3.2f),new Vector3(-5,1.4f,-1),new Vector3(10,1.4f,-5),new Vector3(6.5f,1.2f,2),new Vector3(11,1.2f,2),new Vector3(-5,1.3f,9),new Vector3(10,1.3f,9)};
   string[] names={"living","kitchen","study","bedroom","bathroom","garage","guest-bedroom"};
   for(int i=0;i<positions.Length;i++){D.View.transform.position=positions[i];D.View.transform.LookAt(targets[i]);yield return new WaitForSeconds(.5f);yield return Capture("room-"+names[i]);}
   var closePositions=new[]{new Vector3(-8.6f,1.9f,-4),new Vector3(-8.6f,.9f,-7.4f),new Vector3(-3.1f,1.5f,-8.3f),new Vector3(-8,1.3f,.5f)};
   var closeTargets=new[]{new Vector3(-8.5f,1.8f,-2),new Vector3(-7.7f,.55f,-8.5f),new Vector3(-2.4f,.1f,-7.7f),new Vector3(-7.2f,.05f,1.5f)};
   string[] closeNames={"wallpaper","upholstery","parquet","tile"};
   for(int i=0;i<closePositions.Length;i++){D.View.transform.position=closePositions[i];D.View.transform.LookAt(closeTargets[i]);yield return new WaitForSeconds(.5f);yield return Capture("material-"+closeNames[i]);}
   var root=new GameObject("Avatar visual check");root.transform.position=new Vector3(0,0,-3);var a=DetectiveAvatar.Create(root.transform,1);Check(a.HasHumanoidRig,"Valid humanoid avatar");D.View.transform.position=new Vector3(0,1.15f,0);D.View.transform.LookAt(root.transform.position+Vector3.up*.95f);root.transform.rotation=Quaternion.Euler(0,0,0);a.Animate(0,false,false);yield return new WaitForSeconds(.5f);yield return Capture("detective-model");
  }
  IEnumerator WalkHouse(){
   D.UI.PauseOpen=true;
   foreach(var o in HauntedObject.All.Values)if(o.Kind==HauntKind.Door)o.ServerAct(new InteractionRequest{objectId=o.Id,action="open"});
   yield return new WaitForSeconds(2);
   Vector3[] stops={new Vector3(0,0,-6.5f),new Vector3(-3.4f,0,-6.5f),new Vector3(0,0,-6.5f),new Vector3(0,0,1),new Vector3(-3.4f,0,1),new Vector3(0,0,1),new Vector3(0,0,7.5f),new Vector3(-3.4f,0,7.5f),new Vector3(0,0,7.5f),new Vector3(3.4f,0,7.5f),new Vector3(0,0,7.5f),new Vector3(0,0,0),new Vector3(3.4f,0,0),new Vector3(5.9f,0,0),new Vector3(8.0f,0,0),new Vector3(5.9f,0,0),new Vector3(3.4f,0,0),new Vector3(0,0,0),new Vector3(0,0,-7.5f),new Vector3(3.4f,0,-7.5f)};
   for(int i=0;i<stops.Length;i++){
    float until=Time.realtimeSinceStartup+5;float distance=100;
    while(Time.realtimeSinceStartup<until){D.Controller.ProbeStep(stops[i]);var delta=D.Controller.Position-stops[i];delta.y=0;distance=delta.magnitude;if(distance<.25f)break;yield return null;}
    Check(distance<.3f,"Walk waypoint "+i+" "+WorldBuilder.RoomAt(stops[i])+" distance="+distance.ToString("F2"));
   }
   D.UI.PauseOpen=false;yield return Capture("walk-finish");
  }
  IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(dir,name+".png"));yield return new WaitForSeconds(.5f);}
  void Check(bool okay,string name){checks.Add((okay?"PASS ":"FAIL ")+name);if(!okay)Debug.LogWarning("VH PROBE: "+name);}
  void Finish(){File.WriteAllText(Path.Combine(dir,mode+"-report.json"),JsonUtility.ToJson(new Report{mode=mode,checks=checks.ToArray(),errors=errors.ToArray(),objects=HauntedObject.All.Count},true));Debug.Log("VH PROBE COMPLETE "+mode);Application.Quit(errors.Count>0||checks.Any(x=>x.StartsWith("FAIL"))?1:0);}
  [Serializable]class Report{public string mode;public string[] checks,errors;public int objects;}
  [Serializable]class ObjectDump{public ObjectState[] objects;}
  [Serializable]class ScoreDump{public ScoreRow[] scores;}
 }
}
