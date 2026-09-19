using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace VeilHouse.Editor {
 public static class BuildProject {
  [MenuItem("Veil House/Build Windows prototype")]
  public static void Build() {
   SurfaceMaterialImporter.RefreshMaterials();
   FurnitureImporter.RefreshPrefabs();
   PropImporter.RefreshPrefabs();
   if(Environment.GetCommandLineArgs().Contains("--rebake-probes"))LightProbeBaker.Bake();else LightProbeBaker.Ensure();
   PlayerSettings.companyName="Veil House Studio";PlayerSettings.productName="Дом по ту сторону";PlayerSettings.bundleVersion="0.4.0";
   PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
   PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;PlayerSettings.visibleInBackground=true;
   PlayerSettings.colorSpace=ColorSpace.Linear;
   PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
   PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone,ApiCompatibilityLevel.NET_Standard);
   PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
   PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
   // Procedural geometry has no serialized scene materials: explicitly retain its shader.
   var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
   var included=graphics.FindProperty("m_AlwaysIncludedShaders");var standard=Shader.Find("Standard");bool found=false;
   for(int i=0;i<included.arraySize;i++)if(included.GetArrayElementAtIndex(i).objectReferenceValue==standard)found=true;
   if(!found){int index=included.arraySize;included.InsertArrayElementAtIndex(index);included.GetArrayElementAtIndex(index).objectReferenceValue=standard;graphics.ApplyModifiedPropertiesWithoutUndo();}
   QualitySettings.SetQualityLevel(QualitySettings.names.Length-1,true);QualitySettings.pixelLightCount=12;QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadowDistance=30;QualitySettings.antiAliasing=4;QualitySettings.vSyncCount=0;
   RenderSettings.ambientMode=AmbientMode.Trilight;
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   LightProbeBaker.CleanupScratch();
   var marker=new GameObject("VEIL HOUSE — runtime composition");marker.AddComponent<RuntimeDirector>();
   EditorSceneManager.SaveScene(scene,"Assets/Scenes/VeilHouse.unity");
   EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/VeilHouse.unity",true)};
   var catalog=JsonUtility.FromJson<StoryCatalog>(Resources.Load<TextAsset>("storybook").text);
   if(catalog.categories.Length<1 || catalog.stories.Length<12)throw new Exception("Story catalog is incomplete");
   foreach(var s in catalog.stories){if(s.answers.Length!=catalog.categories.Length)throw new Exception("Missing answers "+s.id);foreach(var c in catalog.categories){var a=s.answers.Single(x=>x.categoryId==c.id);if(!c.options.Any(o=>o.id==a.optionId))throw new Exception("Unknown answer "+s.id);}}
   Debug.Log("VH VALIDATION: "+catalog.stories.Length+" complete stories; "+catalog.categories.Length+" valid categories.");
   AssetDatabase.SaveAssets();
   var path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Windows/VeilHouse.exe"));Directory.CreateDirectory(Path.GetDirectoryName(path));
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/VeilHouse.unity"},locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
   Debug.Log("VH BUILD SUCCESS: "+path+" | "+report.summary.totalSize+" bytes");
  }
 }
}

