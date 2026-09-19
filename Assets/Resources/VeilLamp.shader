Shader "VeilHouse/Translucent lamp" {
 Properties {
  _MainTex("Fabric or opal",2D)="white"{}
  _Color("Tint",Color)=(1,1,1,1)
  [HDR]_EmissionColor("Bulb radiance",Color)=(1,0.72,0.4,1)
  _BumpMap("Surface",2D)="bump"{}
  _BumpScale("Normal strength",Float)=0.5
 }
 SubShader {
  Tags {"RenderType"="Opaque"} LOD 200
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows addshadow
  #pragma target 3.0
  sampler2D _MainTex,_BumpMap;fixed4 _Color;half4 _EmissionColor;half _BumpScale;
  struct Input {float2 uv_MainTex;float2 uv_BumpMap;};
  void surf(Input IN,inout SurfaceOutputStandard o){
   half3 cloth=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;
   o.Albedo=cloth;o.Normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_BumpMap),_BumpScale);
   o.Smoothness=.12;o.Metallic=0;o.Occlusion=1;o.Alpha=1;
   o.Emission=cloth*_EmissionColor.rgb;
  }
  ENDCG
 }
 Fallback "Diffuse"
}
