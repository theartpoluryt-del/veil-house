using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VeilHouse.Editor {
 public sealed class FurnitureImporter : AssetPostprocessor {
  const string Root="Assets/Resources/Furniture/";
  void OnPreprocessModel() {
   if(!assetPath.StartsWith(Root,StringComparison.Ordinal))return;
   var importer=(ModelImporter)assetImporter;
   importer.globalScale=1;importer.useFileScale=true;importer.isReadable=true;
   importer.importAnimation=false;importer.importBlendShapes=false;importer.addCollider=false;
   importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
   importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
   importer.meshCompression=ModelImporterMeshCompression.Off;
  }
  public static void RefreshPrefabs() {
   AssetDatabase.Refresh();Directory.CreateDirectory(Root+"Prefabs");AssetDatabase.Refresh();
   var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+"manifest.json"));
   foreach(var item in manifest.models) {
    var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+item.id+".fbx");
    if(!source)throw new Exception("Furniture FBX missing: "+item.id);
    var instance=(GameObject)PrefabUtility.InstantiatePrefab(source);instance.name=item.id;
    // Blender -Y fronts export as Unity +Z; house placements use -Z fronts.
    instance.transform.rotation=Quaternion.Euler(0,180,0)*instance.transform.rotation;
    try {
     foreach(var r in instance.GetComponentsInChildren<MeshRenderer>()) {
      var slots=r.sharedMaterials;
      for(int i=0;i<slots.Length;i++) {
       string name=slots[i].name.Replace(" (Instance)","");
       var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/"+name+".mat");
       if(!material)throw new Exception("Unmapped furniture material "+name+" in "+item.id);
       slots[i]=material;
      }
      r.sharedMaterials=slots;
     }
     Bounds bounds=new Bounds();bool first=true;
     foreach(var r in instance.GetComponentsInChildren<MeshRenderer>()){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
     if(first||bounds.size.y<.25f||bounds.size.y>3.1f)throw new Exception("Invalid furniture scale: "+item.id+" "+bounds.size);
     PrefabUtility.SaveAsPrefabAsset(instance,Root+"Prefabs/"+item.id+".prefab");
     Debug.Log("VH FURNITURE: "+item.id+" bounds="+bounds.size+" triangles="+item.triangles);
    } finally {UnityEngine.Object.DestroyImmediate(instance);}
   }
   AssetDatabase.SaveAssets();
  }
  [Serializable] class Manifest {public Entry[] models;}
  [Serializable] class Entry {public string id;public int triangles;}
 }
}
