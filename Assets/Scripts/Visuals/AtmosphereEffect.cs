using UnityEngine;

namespace VeilHouse {
    /// <summary>Small-radius screen-space contact shading for the built-in renderer.
    /// Eight geometry samples, depth-aware upsampling, and restrained neutral grading.
    /// UI is rendered afterwards by IMGUI and is therefore never shaded by this effect.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class AtmosphereEffect : MonoBehaviour {
        [Range(0,2)] public float Strength=1.4f;
        [Range(.1f,1.2f)] public float Radius=.56f;
        [Range(.9f,1.2f)] public float Exposure=1.035f;
        [Range(.8f,1.2f)] public float Saturation=.98f;
        Material effect;
        Camera targetCamera;
        static readonly int AoTexture=Shader.PropertyToID("_AOTexture");
        static readonly int ProjectionScale=Shader.PropertyToID("_ProjectionScale");
        static readonly int OcclusionSettings=Shader.PropertyToID("_OcclusionSettings");
        static readonly int GradeSettings=Shader.PropertyToID("_GradeSettings");

        public static AtmosphereEffect Attach(Camera camera) {
            if(camera==null)return null;
            var component=camera.GetComponent<AtmosphereEffect>();
            return component!=null?component:camera.gameObject.AddComponent<AtmosphereEffect>();
        }
        void OnEnable() {
            targetCamera=GetComponent<Camera>();
            targetCamera.depthTextureMode|=DepthTextureMode.DepthNormals;
            var shader=Resources.Load<Shader>("VeilAtmosphere");
            if(shader!=null && shader.isSupported)effect=new Material(shader) {hideFlags=HideFlags.HideAndDontSave};
        }
        void OnDisable() {
            if(effect!=null) {if(Application.isPlaying)Destroy(effect);else DestroyImmediate(effect);}
            effect=null;
        }
        void OnRenderImage(RenderTexture source,RenderTexture destination) {
            if(effect==null || targetCamera==null) {Graphics.Blit(source,destination);return;}
            var projection=targetCamera.projectionMatrix;
            effect.SetVector(ProjectionScale,new Vector4(1f/projection[0,0],1f/projection[1,1],targetCamera.farClipPlane,0));
            // Max reduction is capped at 28%; open surfaces keep their original lighting.
            effect.SetVector(OcclusionSettings,new Vector4(Radius,Strength,.025f,.28f));
            effect.SetVector(GradeSettings,new Vector4(Exposure,Saturation,0,0));
            var format=SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RHalf)?RenderTextureFormat.RHalf:RenderTextureFormat.ARGB32;
            var ao=RenderTexture.GetTemporary(Mathf.Max(1,source.width/2),Mathf.Max(1,source.height/2),0,format,RenderTextureReadWrite.Linear);
            ao.filterMode=FilterMode.Bilinear;ao.wrapMode=TextureWrapMode.Clamp;
            try {
                Graphics.Blit(source,ao,effect,0);
                effect.SetTexture(AoTexture,ao);
                Graphics.Blit(source,destination,effect,1);
            } finally {RenderTexture.ReleaseTemporary(ao);}
        }
    }
}
