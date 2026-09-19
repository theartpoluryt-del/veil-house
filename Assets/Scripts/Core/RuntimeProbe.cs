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
   if(mode=="lighting"){S.StartTraining(PlayerRole.Detective);yield return new WaitForSeconds(4);yield return LightingDetails();Finish();yield break;}
   if(mode=="gallery") {S.StartTraining(PlayerRole.Ghost);yield return new WaitForSeconds(3);yield return Gallery();Finish();yield break;}
   if(mode=="objects") {S.StartTraining(PlayerRole.Ghost);yield return new WaitForSeconds(3);yield return ObjectDetails();Finish();yield break;}
   if(mode=="walk") {S.StartTraining(PlayerRole.Detective);yield return new WaitForSeconds(2);yield return WalkHouse();Finish();yield break;}
   if(mode=="ghost"||mode=="detective"){
    S.StartTraining(mode=="ghost"?PlayerRole.Ghost:PlayerRole.Detective);yield return new WaitForSeconds(3);D.Controller.Teleport(HouseLayout.Map(new Vector3(-8,.15f,-7)),35);yield return new WaitForSeconds(1);yield return Capture(mode);D.UI.JournalOpen=true;yield return Capture(mode+"-journal");Check(S.Phase==GamePhase.Investigation,"Training started");Check(HauntedObject.All.Count>50,"Furnished interactive house");Finish();yield break;
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
    D.Controller.Teleport(HouseLayout.Map(new Vector3(-7,.15f,-6)),30);yield return new WaitForSeconds(1);
    var obj=HauntedObject.All.Values.FirstOrDefault(o=>o.Kind==HauntKind.Prop&&Vector3.Distance(o.transform.position,D.Controller.Position)<5);
    if(obj!=null){float before=S.LocalPlayer.energy;S.RequestInteraction(obj.Id,"hold",D.Controller.Position+new Vector3(0,1.8f,1),Vector3.forward);yield return new WaitForSeconds(.3f);S.RequestInteraction(obj.Id,"throw",obj.transform.position,Vector3.up+Vector3.right);yield return new WaitForSeconds(.2f);Check(S.LocalPlayer.energy<before,"Ghost spends energy on physics");}
    yield return new WaitForSeconds(1);S.RequestInteraction(-1,"blackout",D.Controller.Position,Vector3.up);yield return new WaitForSeconds(1);yield return Capture("network-ghost");
   }else{
    Check(S.KnownStory==null,"Detective cannot access private story before reveal");
    timeout=Time.realtimeSinceStartup+10;string f=Path.Combine(dir,"test-story.json");while(!File.Exists(f)&&Time.realtimeSinceStartup<timeout)yield return new WaitForSeconds(.2f);
    if(File.Exists(f)){var truth=JsonUtility.FromJson<DeathStory>(File.ReadAllText(f));foreach(var a in truth.answers){S.SubmitAnswer(a.categoryId,a.optionId);yield return new WaitForSeconds(.2f);}yield return new WaitForSeconds(.7f);Check(S.Journal.Count==truth.answers.Length,"Private journal stores every category");}
    float energy=S.LocalPlayer.energy;S.RequestInteraction(-1,"poltergeist",Vector3.zero,Vector3.up);yield return new WaitForSeconds(.5f);Check(Mathf.Approximately(S.LocalPlayer.energy,energy),"Detective ghost ability rejected");
    D.Controller.Teleport(HouseLayout.Map(new Vector3(-7+(mode=="client1"?1:-1),.15f,-7)),0);yield return new WaitForSeconds(1);D.UI.JournalOpen=true;yield return Capture("network-"+mode+"-journal");D.UI.ClosePanels();
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
   string[] surfaces={"SmokedOak","Parquet","Plaster","TealPaint","Wallpaper","Tile","BurgundyFabric","Linen","Leather","Brass","PersianRug","Porcelain","BlueGlaze","Enamel","Iron","Rubber","MotorPaint","Marble","Limestone","HearthBrick","Wax","Paper","Soil","Bark","Leaf","Towel","Speaker","GlassPatina","Copper","Chrome","MirrorSilver"};
   foreach(var id in surfaces){var m=Resources.Load<Material>("Materials/"+id);Check(m&&m.mainTexture&&m.mainTexture.width==2048&&m.GetTexture("_BumpMap")&&m.GetTexture("_MetallicGlossMap")&&m.GetTexture("_OcclusionMap")&&m.IsKeywordEnabled("_NORMALMAP"),"Packaged PBR surface "+id);}
   Check(HauntedObject.All.Count==129,"Material update preserves 129 interactive objects");
   string[] furniture={"Sofa","Armchair","DiningChair","Table_2500_1240_490","Bookcase_2600"};
   foreach(var id in furniture){var model=Resources.Load<GameObject>("Furniture/Prefabs/"+id);Check(model&&model.GetComponentsInChildren<MeshFilter>().Sum(x=>x.sharedMesh.vertexCount)>1000,"Detailed furniture mesh "+id);}
   var positions=new[]{new Vector3(-10.5f,1.95f,-9.5f),new Vector3(-10.5f,2,3),new Vector3(3.2f,1.9f,-9.5f),new Vector3(3.2f,1.9f,-1.5f),new Vector3(8,1.9f,-1.5f),new Vector3(-10,2,5),new Vector3(3,1.9f,5)};
   var targets=new[]{new Vector3(-4.5f,1.4f,-3.2f),new Vector3(-5,1.4f,-1),new Vector3(10,1.4f,-5),new Vector3(6.5f,1.2f,2),new Vector3(11,1.2f,2),new Vector3(-5,1.3f,9),new Vector3(10,1.3f,9)};
   string[] names={"living","kitchen","study","bedroom","bathroom","garage","guest-bedroom"};
   for(int i=0;i<positions.Length;i++){D.View.transform.position=HouseLayout.Map(positions[i]);D.View.transform.LookAt(HouseLayout.Map(targets[i]));yield return new WaitForSeconds(.5f);yield return Capture("room-"+names[i]);}
   var closePositions=new[]{new Vector3(-8.6f,1.9f,-4),new Vector3(-8.6f,.9f,-7.4f),new Vector3(-3.1f,1.5f,-8.3f),new Vector3(-8,1.3f,.5f)};
   var closeTargets=new[]{new Vector3(-8.5f,1.8f,-2),new Vector3(-7.7f,.55f,-8.5f),new Vector3(-2.4f,.1f,-7.7f),new Vector3(-7.2f,.05f,1.5f)};
   string[] closeNames={"wallpaper","upholstery","parquet","tile"};
   for(int i=0;i<closePositions.Length;i++){D.View.transform.position=HouseLayout.Map(closePositions[i]);D.View.transform.LookAt(HouseLayout.Map(closeTargets[i]));yield return new WaitForSeconds(.5f);yield return Capture("material-"+closeNames[i]);}
   var root=new GameObject("Avatar visual check");root.transform.position=new Vector3(0,0,-3);var a=DetectiveAvatar.Create(root.transform,1);Check(a.HasHumanoidRig,"Valid humanoid avatar");D.View.transform.position=new Vector3(0,1.15f,0);D.View.transform.LookAt(root.transform.position+Vector3.up*.95f);root.transform.rotation=Quaternion.Euler(0,0,0);a.Animate(0,false,false);yield return new WaitForSeconds(.5f);yield return Capture("detective-model");
  }
  IEnumerator ObjectDetails(){
   D.UI.enabled=false;D.Controller.enabled=false;Cursor.lockState=CursorLockMode.None;
   var prefabs=Resources.LoadAll<GameObject>("Props/Prefabs");
   Check(prefabs.Length==95,"All 95 authored object prefabs packaged");
   Check(DetailedObjects.ReplacedCount>=220,"More than 220 scene objects upgraded");
   Check(HauntedObject.All.Count==129,"129 interaction ids preserved");
   foreach(var p in prefabs){
    var meshes=p.GetComponentsInChildren<MeshFilter>();
    Check(meshes.Length>0&&meshes.All(m=>m.sharedMesh&&m.sharedMesh.isReadable&&m.sharedMesh.uv.Length>0),"Readable textured prefab "+p.name);
   }
   foreach(var h in HauntedObject.All.Values.Where(h=>h.Kind==HauntKind.Prop))Check(h.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("Detailed / ")),"Physical object model "+h.DisplayName+" #"+h.Id);
   var interactive=HauntedObject.All.Values.Where(h=>h.Kind!=HauntKind.Prop).ToArray();
   foreach(var h in interactive)h.ServerAct(new InteractionRequest{objectId=h.Id,action="open"});
   yield return new WaitForSeconds(2);
   foreach(var h in interactive){
    if(h.Hinge)Check(Quaternion.Angle(h.Hinge.localRotation,Quaternion.Euler(0,h.OpenAngle,0))<1,"Hinge opens "+h.DisplayName+" #"+h.Id);
    if(h.Kind==HauntKind.Light)Check(h.Lamps.All(l=>l.enabled)&&h.EmissiveParts.Length>0&&h.EmissiveParts.All(r=>EmissionState(r,true)),"Lamp emits and switches "+h.DisplayName+" #"+h.Id);
    if(h.ActiveVisual)Check(h.ActiveVisual.activeSelf&&h.ActiveVisual.GetComponent<Renderer>().enabled,"Active visual opens "+h.DisplayName+" #"+h.Id);
   }
   var pos=new[]{new Vector3(-5.9f,1.20f,-7.1f),new Vector3(-8.1f,1.85f,-4.25f),new Vector3(-9.2f,2.1f,1.8f),new Vector3(-10.1f,1.6f,1.1f),new Vector3(-5.6f,1.8f,6.8f),new Vector3(9.1f,1.8f,-.8f),new Vector3(8.5f,1.9f,-2),new Vector3(7,1.65f,-7.1f),new Vector3(0,1.8f,7.8f),new Vector3(-9,1.8f,-8.4f)};
   var target=new[]{new Vector3(-6.9f,.62f,-6.2f),new Vector3(-7,1.2f,-2.4f),new Vector3(-7.9f,1.3f,3.5f),new Vector3(-11.3f,.90f,1.8f),new Vector3(-8.1f,1.0f,7.5f),new Vector3(11.1f,.65f,-2.4f),new Vector3(9.3f,1.8f,-3.84f),new Vector3(7.5f,1,-8.7f),new Vector3(.9f,1.8f,10.5f),new Vector3(-11.8f,2,-8)};
   string[] names={"tableware","fireplace","kitchen-cabinets","stove","motorcar","toilet","mirror","desk","clock","window"};
   for(int i=0;i<pos.Length;i++){
    D.View.transform.position=HouseLayout.Map(pos[i]);D.View.transform.LookAt(HouseLayout.Map(target[i]));yield return new WaitForSeconds(.5f);yield return Capture("detail-"+names[i]);
    if(names[i]=="mirror"){
     var mirror=FindFirstObjectByType<PlanarMirror>();Check(mirror&&mirror.RenderCount>0,"Bathroom mirror renders room reflection");
     if(mirror){var previous=RenderTexture.active;RenderTexture.active=mirror.ReflectionTexture;var tex=new Texture2D(512,512,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,512,512),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(dir,"mirror-reflection.png"),tex.EncodeToPNG());Destroy(tex);RenderTexture.active=previous;}
    }
   }
   foreach(var h in interactive)h.ServerAct(new InteractionRequest{objectId=h.Id,action="close"});
   yield return new WaitForSeconds(2);
   foreach(var h in interactive){
    if(h.Hinge)Check(Quaternion.Angle(h.Hinge.localRotation,Quaternion.identity)<1,"Hinge closes "+h.DisplayName+" #"+h.Id);
    if(h.Lamps!=null)Check(h.Lamps.All(l=>!l.enabled)&&(h.EmissiveParts==null||h.EmissiveParts.All(r=>EmissionState(r,false))),"Light and emission off "+h.DisplayName+" #"+h.Id);
    if(h.ActiveVisual)Check(!h.ActiveVisual.activeSelf,"Active visual closes "+h.DisplayName+" #"+h.Id);
   }
  }
  static bool EmissionState(Renderer r,bool on){
   if(!r||!r.enabled||!r.sharedMaterial||!r.sharedMaterial.shader.isSupported)return false;
   if(r.sharedMaterial.shader.name!="VeilHouse/Translucent lamp"&&!r.sharedMaterial.IsKeywordEnabled("_EMISSION"))return false;
   var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);float value=block.GetColor("_EmissionColor").maxColorComponent;
   return on?value>.1f:value<.001f;
  }
  [Serializable]class LightSample {public string name;public float mean,center,darkFraction;}
  [Serializable]class LightSamples {public LightSample[] samples;public float averageFps;}
  readonly List<LightSample> lightSamples=new List<LightSample>();
  IEnumerator LightingShot(string name){
   yield return new WaitForSeconds(.6f);yield return new WaitForEndOfFrame();
   var t=ScreenCapture.CaptureScreenshotAsTexture();var pixels=t.GetPixels32();float sum=0,center=0;int dark=0,nc=0;
   for(int y=0;y<t.height;y+=4)for(int x=0;x<t.width;x+=4){var c=pixels[y*t.width+x];float l=(.2126f*c.r+.7152f*c.g+.0722f*c.b)/255;sum+=l;if(l<.08f)dark++;if(x>t.width*.35f&&x<t.width*.65f&&y>t.height*.3f&&y<t.height*.7f){center+=l;nc++;}}
   int n=((t.width+3)/4)*((t.height+3)/4);lightSamples.Add(new LightSample{name=name,mean=sum/n,center=center/Mathf.Max(1,nc),darkFraction=dark/(float)n});
   File.WriteAllBytes(Path.Combine(dir,name+".png"),t.EncodeToPNG());Destroy(t);
  }
  IEnumerator LightingDetails(){
   D.UI.enabled=false;D.Controller.enabled=false;D.Controller.Flashlight.enabled=false;Cursor.lockState=CursorLockMode.None;
   var effect=D.View.GetComponent<AtmosphereEffect>();var rig=FindFirstObjectByType<RoomLighting>();
   float deadline=Time.realtimeSinceStartup+30;while(rig.ReflectionUpdates<8&&Time.realtimeSinceStartup<deadline)yield return null;
   Check(effect&&effect.Ready&&D.View.allowHDR,"HDR tonemapping effect active");
   Check(RenderSettings.ambientSkyColor.maxColorComponent<.03f,"Very low environment lighting");
   Check(LightmapSettings.lightProbes&&LightmapSettings.lightProbes.count>=300,"Baked light probes loaded with positions and SH data");
   var darkProbes=Resources.Load<LightProbes>("Lighting/HouseProbes").bakedProbes;var litProbes=Resources.Load<LightProbes>("Lighting/HouseProbesLit").bakedProbes;
   float darkEnergy=0,litEnergy=0;for(int i=0;i<darkProbes.Length;i++){darkEnergy+=darkProbes[i][0,0]+darkProbes[i][1,0]+darkProbes[i][2,0];litEnergy+=litProbes[i][0,0]+litProbes[i][1,0]+litProbes[i][2,0];}
   Check(litEnergy>darkEnergy*1.5f&&litEnergy>1,"Baked lamp bounce has measurable energy above the night probe set");
   Check(FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None).Length==8&&rig.ReflectionUpdates>=8,"Eight local reflection probes rendered");
   Check(LivedInHouse.Details.Count==8&&LivedInHouse.Details.All(p=>p.Value>=10&&p.Value<=15),"10 to 15 logical household details in every room");
   Check(HauntedObject.All.Count==129&&WorldBuilder.PhysicalPropCount==90,"Network identities and physical prop population preserved");
   Check(FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.enabled).All(l=>l.shadows!=LightShadows.None),"Enabled local sources and flashlight have shadow maps");
   Check(Mathf.Abs(GameObject.Find("Ceiling").transform.position.y-3)<.01f,"Ceiling lowered to three metres");
   var oldPos=new Vector3(-10.25f,1.65f,-9.45f);var oldTarget=new Vector3(-6.8f,1.05f,-5.7f);
   D.View.transform.position=HouseLayout.Map(oldPos);D.View.transform.LookAt(HouseLayout.Map(oldTarget));
   yield return LightingShot("living-local-light");
   if(Arg("--vh-diagnose")=="yes"){
    effect.enabled=false;yield return LightingShot("diagnostic-no-post");effect.enabled=true;
    var allLights=FindObjectsByType<Light>(FindObjectsSortMode.None);foreach(var l in allLights)l.shadows=LightShadows.None;
    yield return LightingShot("diagnostic-no-shadows");foreach(var l in allLights)l.shadows=LightShadows.Soft;
    var report=new System.Text.StringBuilder();
    foreach(var h in HauntedObject.All.Values.Where(x=>x.Kind==HauntKind.Light))foreach(var r in h.GetComponentsInChildren<MeshRenderer>().Where(x=>x.enabled)){
     var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);report.AppendLine(h.name+" | "+r.name+" | "+string.Join(",",r.sharedMaterials.Select(m=>m.name+":"+m.GetColor("_EmissionColor")))+" override "+block.GetColor("_EmissionColor"));
    }
    File.WriteAllText(Path.Combine(dir,"lighting-diagnostic.txt"),report.ToString());
   }
   effect.AmbientOcclusion=false;effect.ContactShadows=false;yield return LightingShot("living-no-ao-reference");effect.AmbientOcclusion=true;effect.ContactShadows=true;
   var switchable=HauntedObject.All.Values.Where(h=>h.Kind==HauntKind.Light||h.Kind==HauntKind.Television).ToArray();
   foreach(var h in switchable)h.ServerAct(new InteractionRequest{objectId=h.Id,action="close"});
   yield return new WaitForSeconds(4);yield return LightingShot("living-lights-off");
   D.Controller.Flashlight.enabled=true;yield return LightingShot("living-flashlight");
   var warm=lightSamples[0];var off=lightSamples.First(x=>x.name=="living-lights-off");var torch=lightSamples.First(x=>x.name=="living-flashlight");
   Check(warm.mean>.10f&&warm.mean<.40f&&warm.darkFraction<.70f,"Lit room is readable without flat showroom brightness");
   Check(warm.mean>off.mean*1.25f,"Switching lights off visibly darkens the room");
   Check(torch.center>off.center*1.8f,"Player flashlight reveals the aimed area in a dark room");
   Check(off.darkFraction>.25f,"Unlit room retains genuinely dark areas");
   float start=Time.realtimeSinceStartup;int frames=0;while(Time.realtimeSinceStartup-start<3){frames++;yield return null;}float fps=frames/(Time.realtimeSinceStartup-start);
   D.Controller.Flashlight.enabled=false;foreach(var h in switchable.Where(h=>h.Kind==HauntKind.Light))h.ServerAct(new InteractionRequest{objectId=h.Id,action="open"});
   var pos=new[]{new Vector3(-9.7f,1.65f,2.9f),new Vector3(3.5f,1.65f,-9.1f),new Vector3(3.0f,1.65f,-1.7f),new Vector3(8.0f,1.65f,-1.35f),new Vector3(-10.1f,1.65f,5.2f),new Vector3(3.3f,1.65f,5.2f),new Vector3(0,1.65f,-7.5f)};
   var target=new[]{new Vector3(-5.3f,1.0f,-.2f),new Vector3(8,1.0f,-7),new Vector3(5,.9f,1.6f),new Vector3(10.5f,1.2f,1.8f),new Vector3(-5,1.0f,9),new Vector3(8.5f,1,9),new Vector3(0,1.2f,-10.5f)};
   string[] names={"kitchen","study","bedroom","bathroom","garage","guest-bedroom","entrance"};
   for(int i=0;i<pos.Length;i++){
    D.View.transform.position=HouseLayout.Map(pos[i]);D.View.transform.LookAt(HouseLayout.Map(target[i]));yield return LightingShot("mood-"+names[i]);
    foreach(var h in switchable)h.ServerAct(new InteractionRequest{objectId=h.Id,action="close"});D.Controller.Flashlight.enabled=true;yield return LightingShot("torch-"+names[i]);D.Controller.Flashlight.enabled=false;
    foreach(var h in switchable.Where(h=>h.Kind==HauntKind.Light))h.ServerAct(new InteractionRequest{objectId=h.Id,action="open"});
   }
   File.WriteAllText(Path.Combine(dir,"lighting-metrics.json"),JsonUtility.ToJson(new LightSamples{samples=lightSamples.ToArray(),averageFps=fps},true));
   var newspaper=GameObject.Find("Household / Newspaper");
   D.View.transform.position=newspaper.transform.position+new Vector3(0,.65f,-.25f);D.View.transform.LookAt(newspaper.transform.position);D.Controller.Flashlight.enabled=true;yield return Capture("household-newspaper");
   var paperReport=string.Join("\n",newspaper.GetComponentsInChildren<MeshRenderer>().Select(r=>r.name+" "+r.bounds+" material="+r.sharedMaterial.name+" texture="+r.sharedMaterial.mainTexture?.name));File.WriteAllText(Path.Combine(dir,"paper-diagnostic.txt"),paperReport);
  }
  IEnumerator WalkHouse(){
   D.UI.PauseOpen=true;
   foreach(var o in HauntedObject.All.Values)if(o.Kind==HauntKind.Door)o.ServerAct(new InteractionRequest{objectId=o.Id,action="open"});
   yield return new WaitForSeconds(2);
   Vector3[] stops={new Vector3(0,0,-6.5f),new Vector3(-3.4f,0,-6.5f),new Vector3(0,0,-6.5f),new Vector3(0,0,1),new Vector3(-3.4f,0,1),new Vector3(0,0,1),new Vector3(0,0,7.5f),new Vector3(-3.4f,0,7.5f),new Vector3(0,0,7.5f),new Vector3(3.4f,0,7.5f),new Vector3(0,0,7.5f),new Vector3(0,0,0),new Vector3(3.4f,0,0),new Vector3(5.9f,0,0),new Vector3(8.0f,0,0),new Vector3(5.9f,0,0),new Vector3(3.4f,0,0),new Vector3(0,0,0),new Vector3(0,0,-7.5f),new Vector3(3.4f,0,-7.5f)};
   for(int i=0;i<stops.Length;i++)stops[i]=HouseLayout.Map(stops[i]);
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
