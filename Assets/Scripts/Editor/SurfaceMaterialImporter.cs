using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VeilHouse.Editor {
 public sealed class SurfaceMaterialImporter : AssetPostprocessor {
  const string TextureRoot="Assets/Resources/SurfaceTextures/";
  void OnPreprocessTexture() {
   if(!assetPath.StartsWith(TextureRoot,StringComparison.Ordinal))return;
   var t=(TextureImporter)assetImporter;
   bool normal=assetPath.EndsWith("_Normal.png",StringComparison.Ordinal);
   t.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
   t.sRGBTexture=assetPath.EndsWith("_BaseColor.png",StringComparison.Ordinal);
   t.convertToNormalmap=false;t.mipmapEnabled=true;t.isReadable=false;
   t.wrapMode=TextureWrapMode.Repeat;t.filterMode=FilterMode.Trilinear;t.anisoLevel=8;
   t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.CompressedHQ;
   t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=false;
   var platform=t.GetPlatformTextureSettings("Standalone");
   platform.overridden=true;platform.maxTextureSize=2048;
   platform.format=normal?TextureImporterFormat.BC5:TextureImporterFormat.BC7;
   platform.compressionQuality=100;t.SetPlatformTextureSettings(platform);
  }
  [MenuItem("Veil House/Refresh surface materials")]
  public static void RefreshMaterials() {
   AssetDatabase.Refresh();
   string path=TextureRoot+"manifest.json";
   var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
   Directory.CreateDirectory("Assets/Resources/Materials");AssetDatabase.Refresh();
   foreach(var entry in manifest.materials) {
    var maps=new Texture2D[3];string[] suffixes={"BaseColor","Normal","MetallicSmoothness"};
    for(int i=0;i<maps.Length;i++) {
     maps[i]=AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot+entry.id+"_"+suffixes[i]+".png");
     if(maps[i]==null||maps[i].width!=2048||maps[i].height!=2048)throw new Exception("Missing 2K surface: "+entry.id+" "+suffixes[i]);
    }
    string target="Assets/Resources/Materials/"+entry.id+".mat";
    var mat=AssetDatabase.LoadAssetAtPath<Material>(target);
    if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,target);}
    mat.name=entry.id;mat.color=Color.white;
    mat.SetTexture("_MainTex",maps[0]);mat.SetTexture("_BumpMap",maps[1]);mat.SetTexture("_MetallicGlossMap",maps[2]);
    mat.SetFloat("_BumpScale",1.8f);mat.SetFloat("_GlossMapScale",.65f);mat.SetFloat("_Glossiness",.15f);mat.SetFloat("_SmoothnessTextureChannel",0);
    mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_METALLICGLOSSMAP");
    bool cloth=entry.id=="BurgundyFabric"||entry.id=="Linen"||entry.id=="PersianRug";
    if(cloth){mat.SetFloat("_GlossMapScale",.2f);mat.SetFloat("_Glossiness",.015f);}
    if(entry.id=="Brass")mat.SetFloat("_GlossMapScale",.85f);
    mat.SetFloat("_SpecularHighlights",cloth?0:1);mat.SetFloat("_GlossyReflections",cloth?0:1);
    if(cloth){mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");mat.EnableKeyword("_GLOSSYREFLECTIONS_OFF");}
    else{mat.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");mat.DisableKeyword("_GLOSSYREFLECTIONS_OFF");}
    mat.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");EditorUtility.SetDirty(mat);
   }
   Variant("Walnut","SmokedOak",new Color(.54f,.49f,.44f));
   Variant("FabricSeam","BurgundyFabric",new Color(.55f,.55f,.55f));
   Variant("BookRed","BurgundyFabric",new Color(.6f,.6f,.6f));
   Variant("BookBlue","TealPaint",new Color(.8f,.8f,.8f));
   AssetDatabase.SaveAssets();Debug.Log("VH MATERIALS: "+manifest.materials.Length+" PBR materials; "+manifest.materials.Length*3+" verified 2048px textures.");
  }
  static void Variant(string id,string source,Color tint) {
   string path="Assets/Resources/Materials/"+id+".mat";
   var original=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/"+source+".mat");
   var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!mat){mat=new Material(original);AssetDatabase.CreateAsset(mat,path);}else mat.CopyPropertiesFromMaterial(original);
   mat.name=id;mat.color=tint;EditorUtility.SetDirty(mat);
  }
  [Serializable] class Manifest {public Entry[] materials;}
  [Serializable] class Entry {public string id;public float width,height;}
 }
}
