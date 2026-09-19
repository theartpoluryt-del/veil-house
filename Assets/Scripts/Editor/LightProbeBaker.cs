using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace VeilHouse.Editor {
 public static class LightProbeBaker {
  const string Path="Assets/Resources/Lighting/HouseProbes.asset";
  [MenuItem("Veil House/Bake room light probes")]
  public static void Bake(){
   if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new Exception("Probe baking needs a graphics device for material extraction. Omit -nographics when using --rebake-probes.");
   Directory.CreateDirectory("Assets/Resources/Lighting");AssetDatabase.Refresh();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var house=WorldBuilder.Build();
   foreach(var l in house.GetComponentsInChildren<Light>()){
    l.lightmapBakeType=LightmapBakeType.Baked;
    // First bake: permanent moon/window contribution. Second bake: switchable indirect light.
    l.enabled=l.name=="Window bounce"||l.type==LightType.Directional;
   }
   foreach(var r in house.GetComponentsInChildren<MeshRenderer>()){
    if(!r.enabled)continue;
    GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.ContributeGI);
    r.receiveGI=ReceiveGI.Lightmaps;r.scaleInLightmap=1;
    if(r.sharedMaterial&&r.sharedMaterial.HasProperty("_EmissionColor")&&r.sharedMaterial.GetColor("_EmissionColor").maxColorComponent>0){
     var m=new Material(r.sharedMaterial);m.SetColor("_EmissionColor",Color.black);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;r.sharedMaterial=m;
    }
   }
   var positions=new List<Vector3>();
   for(int room=0;room<RoomLighting.Centers.Length;room++){
    var center=HouseLayout.Map(RoomLighting.Centers[room]);var size=HouseLayout.Map(RoomLighting.Sizes[room]);
    int nx=Mathf.Max(2,Mathf.CeilToInt(size.x/1.8f)),nz=Mathf.Max(2,Mathf.CeilToInt(size.z/1.8f));
    for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)foreach(float y in new[]{.30f,1.35f,2.60f})
     positions.Add(new Vector3(center.x+Mathf.Lerp(-size.x*.43f,size.x*.43f,x/(float)(nx-1)),y,center.z+Mathf.Lerp(-size.z*.43f,size.z*.43f,z/(float)(nz-1))));
   }
   var group=new GameObject("Room probe volumes").AddComponent<LightProbeGroup>();group.probePositions=positions.ToArray();
   var settings=new LightingSettings();settings.name="House probe bake";settings.realtimeGI=false;settings.bakedGI=true;settings.lightmapper=LightingSettings.Lightmapper.ProgressiveCPU;
   settings.lightmapResolution=2;settings.lightmapMaxSize=512;settings.directSampleCount=32;settings.indirectSampleCount=64;settings.environmentSampleCount=64;settings.maxBounces=2;
   Lightmapping.lightingSettings=settings;
   EditorSceneManager.SaveScene(scene,"Assets/Scenes/ProbeBake.unity");
   Debug.Log("VH PROBE BAKE START: "+positions.Count+" positions");
   if(!Lightmapping.Bake())throw new Exception("Light probe bake failed");
   var source=LightmapSettings.lightProbes;if(!source||source.count<positions.Count)throw new Exception("No baked light probe data");
   Save(source,Path);
   settings.mixedBakeMode=MixedLightingMode.IndirectOnly;
   foreach(var l in house.GetComponentsInChildren<Light>()){
    l.enabled=l.name!="TV green cast";
    if(l.name!="Window bounce"&&l.type!=LightType.Directional)l.lightmapBakeType=LightmapBakeType.Mixed;
   }
   Debug.Log("VH INDIRECT BAKE START");
   if(!Lightmapping.Bake())throw new Exception("Indirect probe bake failed");
   Save(LightmapSettings.lightProbes,"Assets/Resources/Lighting/HouseProbesLit.asset");
   AssetDatabase.SaveAssets();Debug.Log("VH PROBE BAKE PASS: "+positions.Count+" dark and lit baked probes");
  }
  static void Save(LightProbes source,string path){
   var copy=UnityEngine.Object.Instantiate(source);copy.name=System.IO.Path.GetFileNameWithoutExtension(path);
   var existing=AssetDatabase.LoadAssetAtPath<LightProbes>(path);
   if(existing){EditorUtility.CopySerialized(copy,existing);UnityEngine.Object.DestroyImmediate(copy);}else AssetDatabase.CreateAsset(copy,path);
  }
  public static void Ensure(){if(!AssetDatabase.LoadAssetAtPath<LightProbes>(Path)||!AssetDatabase.LoadAssetAtPath<LightProbes>("Assets/Resources/Lighting/HouseProbesLit.asset"))Bake();}
  public static void CleanupScratch(){
   // The portable LightProbes asset holds positions and SH coefficients on its own.
   // Do not ship the temporary 250 MB scene containing generated mesh instances.
   AssetDatabase.DeleteAsset("Assets/Scenes/ProbeBake.unity");
   AssetDatabase.DeleteAsset("Assets/Scenes/ProbeBake");
  }
 }
}
