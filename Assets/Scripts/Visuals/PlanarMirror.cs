using UnityEngine;
namespace VeilHouse {
 [RequireComponent(typeof(MeshRenderer))]
 public sealed class PlanarMirror : MonoBehaviour {
  Camera reflection;RenderTexture texture;Material material;MeshRenderer surface;static bool rendering;int lastFrame=-1;
  public int RenderCount {get;private set;}
  public RenderTexture ReflectionTexture=>texture;
  void Awake(){
   surface=GetComponent<MeshRenderer>();var shader=Resources.Load<Shader>("VeilMirror");
   gameObject.layer=31;
   material=new Material(shader);surface.sharedMaterial=material;
   texture=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32){name="Bathroom mirror reflection"};texture.Create();material.mainTexture=texture;
   var o=new GameObject("Mirror reflection camera"){hideFlags=HideFlags.HideAndDontSave};reflection=o.AddComponent<Camera>();reflection.enabled=false;
  }
  void OnWillRenderObject(){
   var source=Camera.current;if(rendering||!source||!source.CompareTag("MainCamera")||lastFrame==Time.frameCount||!reflection)return;
   lastFrame=Time.frameCount;rendering=true;bool oldInvert=GL.invertCulling;
   try{
    reflection.CopyFrom(source);reflection.enabled=false;reflection.targetTexture=texture;reflection.allowHDR=false;reflection.useOcclusionCulling=false;
    reflection.cullingMask=source.cullingMask&~(1<<31);
    Vector3 n=Vector3.forward,p=surface.bounds.center;float d=-Vector3.Dot(n,p);
    Matrix4x4 r=Matrix4x4.identity;
    r.m00=1-2*n.x*n.x;r.m01=-2*n.x*n.y;r.m02=-2*n.x*n.z;r.m03=-2*d*n.x;
    r.m10=-2*n.y*n.x;r.m11=1-2*n.y*n.y;r.m12=-2*n.y*n.z;r.m13=-2*d*n.y;
    r.m20=-2*n.z*n.x;r.m21=-2*n.z*n.y;r.m22=1-2*n.z*n.z;r.m23=-2*d*n.z;
    reflection.transform.position=r.MultiplyPoint(source.transform.position);
    reflection.worldToCameraMatrix=source.worldToCameraMatrix*r;
    Vector3 cp=reflection.worldToCameraMatrix.MultiplyPoint(p+n*.015f),cn=reflection.worldToCameraMatrix.MultiplyVector(n).normalized;
    reflection.projectionMatrix=source.CalculateObliqueMatrix(new Vector4(cn.x,cn.y,cn.z,-Vector3.Dot(cp,cn)));
    GL.invertCulling=!oldInvert;reflection.Render();RenderCount++;
   }finally{GL.invertCulling=oldInvert;rendering=false;}
  }
  void OnDestroy(){if(reflection)Destroy(reflection.gameObject);if(texture){texture.Release();Destroy(texture);}if(material)Destroy(material);}
 }
}
