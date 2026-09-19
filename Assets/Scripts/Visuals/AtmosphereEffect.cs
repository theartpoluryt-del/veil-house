using UnityEngine;
namespace VeilHouse {
 [DisallowMultipleComponent,RequireComponent(typeof(Camera))]
 public sealed class AtmosphereEffect:MonoBehaviour {
  [Range(0,4)]public float Strength=2.8f;
  [Range(.1f,1.2f)]public float Radius=.62f;
  [Range(.2f,3)]public float Exposure=1.0f;
  [Range(0,1.3f)]public float Saturation=.93f;
  public bool AmbientOcclusion=true,ContactShadows=true,Bloom=true;
  public float BloomIntensity=.075f;
  public bool Ready=>effect!=null;
  Material effect;Camera targetCamera;Light[] lights;float nextLightSearch;
  public static AtmosphereEffect Attach(Camera camera){if(!camera)return null;var e=camera.GetComponent<AtmosphereEffect>();return e?e:camera.gameObject.AddComponent<AtmosphereEffect>();}
  void OnEnable(){targetCamera=GetComponent<Camera>();targetCamera.allowHDR=true;targetCamera.depthTextureMode|=DepthTextureMode.DepthNormals;var s=Resources.Load<Shader>("VeilAtmosphere");if(s&&s.isSupported)effect=new Material(s){hideFlags=HideFlags.HideAndDontSave};}
  void OnDisable(){if(effect){if(Application.isPlaying)Destroy(effect);else DestroyImmediate(effect);}effect=null;}
  Light DominantLight(){
   var torch=RuntimeDirector.I&&RuntimeDirector.I.Controller?RuntimeDirector.I.Controller.Flashlight:null;
   if(torch&&torch.enabled)return torch;
   if(lights==null||Time.unscaledTime>nextLightSearch){lights=FindObjectsByType<Light>(FindObjectsSortMode.None);nextLightSearch=Time.unscaledTime+2;}
   Light best=null;float score=0;
   foreach(var l in lights){if(!l||!l.enabled||l.type==LightType.Directional)continue;float d=(l.transform.position-targetCamera.transform.position).sqrMagnitude;float value=l.intensity/(.3f+d);if(d<l.range*l.range&&value>score){score=value;best=l;}}
   return best;
  }
  void OnRenderImage(RenderTexture source,RenderTexture destination){
   if(!effect){Graphics.Blit(source,destination);return;}
   var projection=targetCamera.projectionMatrix;
   effect.SetVector("_ProjectionScale",new Vector4(1/projection[0,0],1/projection[1,1],targetCamera.farClipPlane,0));
   effect.SetVector("_OcclusionSettings",new Vector4(Radius,AmbientOcclusion?Strength:0,.012f,.65f));
   effect.SetVector("_GradeSettings",new Vector4(Exposure,Saturation,Bloom?BloomIntensity:0,1.25f));
   var light=DominantLight();var p=light?targetCamera.worldToCameraMatrix.MultiplyPoint(light.transform.position):Vector3.zero;
   effect.SetVector("_ContactLight",new Vector4(p.x,p.y,p.z,light&&ContactShadows?1:0));
   var ao=RenderTexture.GetTemporary(Mathf.Max(1,source.width/2),Mathf.Max(1,source.height/2),0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);
   var bloom=RenderTexture.GetTemporary(Mathf.Max(1,source.width/4),Mathf.Max(1,source.height/4),0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);
   var blur=RenderTexture.GetTemporary(bloom.width,bloom.height,0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);
   ao.filterMode=bloom.filterMode=blur.filterMode=FilterMode.Bilinear;ao.wrapMode=bloom.wrapMode=blur.wrapMode=TextureWrapMode.Clamp;
   try{
    Graphics.Blit(source,ao,effect,0);Graphics.Blit(source,bloom,effect,1);
    effect.SetVector("_BlurDirection",new Vector4(1f/bloom.width,0,0,0));Graphics.Blit(bloom,blur,effect,2);
    effect.SetVector("_BlurDirection",new Vector4(0,1f/bloom.height,0,0));Graphics.Blit(blur,bloom,effect,2);
    effect.SetTexture("_AOTexture",ao);effect.SetTexture("_BloomTexture",bloom);Graphics.Blit(source,destination,effect,3);
   }finally{RenderTexture.ReleaseTemporary(ao);RenderTexture.ReleaseTemporary(bloom);RenderTexture.ReleaseTemporary(blur);}
  }
 }
}
