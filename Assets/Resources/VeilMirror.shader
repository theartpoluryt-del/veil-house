Shader "VeilHouse/PlanarMirror" {
 Properties { _MainTex ("Reflection", 2D) = "gray" {} }
 SubShader { Tags { "RenderType"="Opaque" } Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 struct v2f { float4 pos:SV_POSITION;float4 screen:TEXCOORD0; };
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.screen=ComputeScreenPos(o.pos);return o;}
 fixed4 frag(v2f i):SV_Target {float2 uv=i.screen.xy/i.screen.w;fixed4 c=tex2D(_MainTex,uv);return fixed4(c.rgb*.87+fixed3(.018,.025,.02),1);}
 ENDCG
 } }
}
