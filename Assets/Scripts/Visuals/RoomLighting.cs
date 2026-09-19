using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace VeilHouse {
 public sealed class RoomLighting:MonoBehaviour {
  public static readonly Vector3[] Centers={new Vector3(-7,1.4f,-6.5f),new Vector3(-7,1.4f,1),new Vector3(-7,1.4f,7.5f),new Vector3(7,1.4f,-7.5f),new Vector3(4.5f,1.4f,0),new Vector3(9.5f,1.4f,0),new Vector3(7,1.4f,7.5f),new Vector3(0,1.4f,0)};
  public static readonly Vector3[] Sizes={new Vector3(9.7f,2.85f,8.7f),new Vector3(9.7f,2.85f,5.7f),new Vector3(9.7f,2.85f,6.7f),new Vector3(9.7f,2.85f,6.7f),new Vector3(4.7f,2.85f,7.7f),new Vector3(4.7f,2.85f,7.7f),new Vector3(9.7f,2.85f,6.7f),new Vector3(3.7f,2.85f,21.7f)};
  public int ReflectionUpdates {get;private set;}
  readonly List<ReflectionProbe> probes=new List<ReflectionProbe>();
  LightProbes ownedProbes;Light[] lights;SphericalHarmonicsL2[] darkSH,litSH;Vector3[] probePositions;
  int probeState;float nextProbeUpdate;
  void LateUpdate(){if(Time.unscaledTime<nextProbeUpdate)return;nextProbeUpdate=Time.unscaledTime+.12f;int state=State();if(state!=probeState){UpdateProbeLighting();probeState=state;}}
  public static void Install(Transform root){
   var rig=root.gameObject.AddComponent<RoomLighting>();rig.Configure();
  }
  void Configure(){
   RenderSettings.reflectionIntensity=.55f;RenderSettings.ambientIntensity=1;
   lights=GetComponentsInChildren<Light>();
   foreach(var l in lights){
    l.bounceIntensity=.65f;l.shadows=LightShadows.Soft;l.shadowStrength=1;l.shadowBias=.02f;l.shadowNormalBias=.10f;l.shadowNearPlane=.06f;
    l.shadowResolution=LightShadowResolution.Medium;l.renderMode=LightRenderMode.ForcePixel;
    if(l.name=="Warm pendant light"){
     l.type=LightType.Point;l.transform.localPosition=new Vector3(0,-.53f,0);l.intensity*=.65f;l.range*=1.20f;
     foreach(var r in l.transform.parent.GetComponentsInChildren<MeshRenderer>())r.shadowCastingMode=ShadowCastingMode.Off;
    }
    if(l.name=="Amber reading pool"){
     l.type=LightType.Point;l.intensity*=.60f;l.transform.localPosition=new Vector3(0,.48f,0);
     foreach(var r in l.transform.parent.GetComponentsInChildren<MeshRenderer>())r.shadowCastingMode=ShadowCastingMode.Off;
    }
    if(l.name=="Window bounce"){
     l.type=LightType.Spot;l.spotAngle=110;l.innerSpotAngle=75;l.transform.rotation=Quaternion.LookRotation(l.transform.parent.forward+Vector3.down*.28f);
    }
   }
   foreach(var r in GetComponentsInChildren<MeshRenderer>()){
    r.lightProbeUsage=LightProbeUsage.BlendProbes;r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;
    if(r.sharedMaterial&&r.sharedMaterial.name=="Glow")r.shadowCastingMode=ShadowCastingMode.Off;
   }
   if(!Application.isPlaying)return;
   var baked=Resources.Load<LightProbes>("Lighting/HouseProbes");
   if(baked){
    ownedProbes=Instantiate(baked);LightmapSettings.lightProbes=ownedProbes;LightProbes.Tetrahedralize();darkSH=baked.bakedProbes;probePositions=baked.positions;
    var lit=Resources.Load<LightProbes>("Lighting/HouseProbesLit");
    if(lit&&lit.count==baked.count){
     var lookup=new Dictionary<Vector3,SphericalHarmonicsL2>();var lp=lit.positions;var sh=lit.bakedProbes;for(int i=0;i<lp.Length;i++)lookup[lp[i]]=sh[i];
     litSH=new SphericalHarmonicsL2[baked.count];for(int i=0;i<litSH.Length;i++)litSH[i]=lookup.TryGetValue(probePositions[i],out var value)?value:darkSH[i];
    }
    UpdateProbeLighting();
   }
   for(int i=0;i<Centers.Length;i++){
    var o=new GameObject("Room reflection / "+i);o.transform.SetParent(transform,false);o.transform.position=HouseLayout.Map(Centers[i]);
    var p=o.AddComponent<ReflectionProbe>();p.mode=ReflectionProbeMode.Realtime;p.refreshMode=ReflectionProbeRefreshMode.ViaScripting;p.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;
    p.resolution=128;p.hdr=true;p.boxProjection=true;p.size=new Vector3(Sizes[i].x*HouseLayout.PlanScale,Sizes[i].y,Sizes[i].z*HouseLayout.PlanScale);p.blendDistance=.25f;p.importance=2;p.intensity=.65f;p.nearClipPlane=.08f;p.farClipPlane=24;p.clearFlags=ReflectionProbeClearFlags.SolidColor;p.backgroundColor=Color.black;
    probes.Add(p);
   }
   StartCoroutine(UpdateReflections());
  }
  IEnumerator UpdateReflections(){
   yield return null;var camera=Camera.main;
   foreach(var p in probes.OrderBy(p=>camera?(p.transform.position-camera.transform.position).sqrMagnitude:0)){
    int id=p.RenderProbe();while(!p.IsFinishedRendering(id))yield return null;ReflectionUpdates++;
   }
   int previous=State();UpdateProbeLighting();Vector3 oldPosition=camera?camera.transform.position:Vector3.zero;
   while(true){
    yield return new WaitForSeconds(.7f);if(!camera)continue;
    int now=State();bool changed=now!=previous;bool moved=(camera.transform.position-oldPosition).sqrMagnitude>9;
    if(changed)UpdateProbeLighting();
    if(!changed&&!moved)continue;
    foreach(var p in probes.OrderBy(p=>(p.transform.position-camera.transform.position).sqrMagnitude).Take(changed?8:2)){
     int id=p.RenderProbe();while(!p.IsFinishedRendering(id))yield return null;ReflectionUpdates++;
    }
    previous=now;oldPosition=camera.transform.position;
   }
  }
  int State(){unchecked{int s=17;foreach(var l in lights)if(l)s=s*31+(l.enabled?1:0);return s;}}
  void UpdateProbeLighting(){
   if(!ownedProbes||litSH==null)return;
   var values=new SphericalHarmonicsL2[darkSH.Length];
   for(int i=0;i<values.Length;i++){
    string room=WorldBuilder.RoomAt(probePositions[i]);float total=0,active=0;
    foreach(var l in lights){
     if(!l||l.type==LightType.Directional||l.name=="Window bounce"||l.name=="TV green cast"||l.name=="Ember glow"||WorldBuilder.RoomAt(l.transform.position)!=room)continue;
     float influence=l.intensity/(1+(l.transform.position-probePositions[i]).sqrMagnitude);total+=influence;if(l.enabled)active+=influence;
    }
    float blend=total>.001f?active/total:0;
    for(int channel=0;channel<3;channel++)for(int coefficient=0;coefficient<9;coefficient++)values[i][channel,coefficient]=Mathf.Lerp(darkSH[i][channel,coefficient],litSH[i][channel,coefficient],blend);
   }
   ownedProbes.bakedProbes=values;
  }
  void OnDestroy(){if(ownedProbes){if(LightmapSettings.lightProbes==ownedProbes)LightmapSettings.lightProbes=null;Destroy(ownedProbes);}}
 }
}
