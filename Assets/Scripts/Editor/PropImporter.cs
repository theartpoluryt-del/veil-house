using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace VeilHouse.Editor {
 public sealed class PropImporter : AssetPostprocessor {
  const string Root="Assets/Resources/Props/";
  void OnPreprocessModel(){
   if(!assetPath.StartsWith(Root,StringComparison.Ordinal))return;
   var i=(ModelImporter)assetImporter;i.isReadable=true;i.globalScale=1;i.useFileScale=true;
   i.importAnimation=false;i.addCollider=false;i.importBlendShapes=false;
   i.importNormals=ModelImporterNormals.Import;i.importTangents=ModelImporterTangents.CalculateMikk;
   i.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
  }
  public static void RefreshPrefabs(){
   AssetDatabase.Refresh();Directory.CreateDirectory(Root+"Prefabs");AssetDatabase.Refresh();
   ArtMaterial("ArtAtlas","ObjectArt/ArtAtlas");ArtMaterial("Decals","ObjectArt/Decals");ArtMaterial("FamilyPortrait","FamilyPortrait");
   Special("Glow","Linen",new Color(.9f,.65f,.3f));Special("Screen","GlassPatina",new Color(.22f,.43f,.31f));
   int count=0;
   foreach(var file in Directory.GetFiles(Root,"*.fbx",SearchOption.AllDirectories)){
    string path=file.Replace('\\','/');string id=Path.GetFileNameWithoutExtension(path);
    var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
    var o=(GameObject)PrefabUtility.InstantiatePrefab(source);o.name=id;
    o.transform.rotation=Quaternion.Euler(0,180,0)*o.transform.rotation;
    try{
     foreach(var r in o.GetComponentsInChildren<MeshRenderer>()){
      var slots=r.sharedMaterials;
      for(int k=0;k<slots.Length;k++){
       var name=slots[k].name.Replace(" (Instance)","");
       var mat=Resources.Load<Material>("Materials/"+name);
       if(!mat)throw new Exception("Missing prop material "+name+" for "+id);
       slots[k]=mat;
      }
      r.sharedMaterials=slots;
     }
     foreach(var mf in o.GetComponentsInChildren<MeshFilter>())if(!mf.sharedMesh||!mf.sharedMesh.isReadable||mf.sharedMesh.uv.Length==0)throw new Exception("Invalid prop mesh "+id);
     PrefabUtility.SaveAsPrefabAsset(o,Root+"Prefabs/"+id+".prefab");count++;
    }finally{UnityEngine.Object.DestroyImmediate(o);}
   }
   AssetDatabase.SaveAssets();Debug.Log("VH PROPS: "+count+" authored prefabs imported.");
  }
  static void ArtMaterial(string id,string texture){
   string path="Assets/Resources/Materials/"+id+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
   m.name=id;m.mainTexture=Resources.Load<Texture2D>(texture);m.color=Color.white;m.SetFloat("_Glossiness",.08f);EditorUtility.SetDirty(m);
  }
  static void Special(string id,string source,Color emission){
   string path="Assets/Resources/Materials/"+id+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   var original=Resources.Load<Material>("Materials/"+source);
   if(!m){m=new Material(original);AssetDatabase.CreateAsset(m,path);}else m.CopyPropertiesFromMaterial(original);
   m.name=id;m.SetColor("_EmissionColor",emission);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;m.EnableKeyword("_EMISSION");EditorUtility.SetDirty(m);
  }
 }
}
