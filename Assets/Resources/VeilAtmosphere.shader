Shader "Hidden/VeilHouse/Atmosphere" {
 Properties { _MainTex("Scene",2D)="white"{} }
 SubShader {
 Cull Off ZWrite Off ZTest Always
 CGINCLUDE
 #include "UnityCG.cginc"
 sampler2D _MainTex,_CameraDepthNormalsTexture,_AOTexture,_BloomTexture;
 float4 _MainTex_TexelSize,_AOTexture_TexelSize,_ProjectionScale,_OcclusionSettings,_GradeSettings,_ContactLight,_BlurDirection;
 float2 DepthUV(float2 uv){
  #if UNITY_UV_STARTS_AT_TOP
  if(_MainTex_TexelSize.y<0)uv.y=1-uv.y;
  #endif
  return uv;
 }
 void Surface(float2 uv,out float depth,out float3 normal){DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture,DepthUV(uv)),depth,normal);depth*=_ProjectionScale.z;}
 float3 Position(float2 uv,float depth){return float3((uv*2-1)*_ProjectionScale.xy,-1)*depth;}
 float Contribution(float2 uv,float2 offset,float3 center,float3 normal){
  float2 q=uv+offset;if(any(q<0)||any(q>1))return 0;
  float d;float3 n;Surface(q,d,n);float3 delta=Position(q,d)-center;float dist2=dot(delta,delta);
  float falloff=saturate(1-dist2/(_OcclusionSettings.x*_OcclusionSettings.x));
  return max(0,dot(normal,delta)*rsqrt(max(dist2,.00001))-.08)*falloff*falloff*smoothstep(.012,.035,sqrt(dist2));
 }
 float Contact(float3 center,float3 normal){
  if(_ContactLight.w<.5)return 1;
  float3 delta=_ContactLight.xyz-center;float distance=length(delta);float3 direction=delta/max(distance,.01);
  if(dot(normal,direction)<.05)return 1;
  float travel=min(.38,distance*.2);float shade=0;
  [unroll]for(int k=1;k<=12;k++){
   float3 p=center+normal*.018+direction*(travel*k/12.0);if(p.z>-.04)continue;
   float2 uv=(p.xy/(-p.z)/_ProjectionScale.xy)*.5+.5;if(any(uv<0)||any(uv>1))continue;
   float d;float3 n;Surface(uv,d,n);float diff=-p.z-d;
   if(diff>.014&&diff<.095)shade=max(shade,(1-k/14.0)*.55);
  }
  return 1-shade;
 }
 half4 Occlusion(v2f_img i):SV_Target{
  float depth;float3 normal;Surface(i.uv,depth,normal);if(depth>=_ProjectionScale.z*.995||depth<.05)return 1;
  float3 center=Position(i.uv,depth);float2 r=(_OcclusionSettings.x*.5)/(max(depth,.2)*_ProjectionScale.xy);float sum=0;
  const float2 taps[16]={float2(.2,0),float2(-.2,0),float2(0,.2),float2(0,-.2),float2(.33,.33),float2(-.33,.33),float2(.33,-.33),float2(-.33,-.33),float2(.72,.12),float2(-.72,-.12),float2(-.12,.72),float2(.12,-.72),float2(.6,.6),float2(-.6,.6),float2(.6,-.6),float2(-.6,-.6)};
  [unroll]for(int k=0;k<16;k++)sum+=Contribution(i.uv,r*taps[k],center,normal);
  float ao=1-min(_OcclusionSettings.w,sum/16*_OcclusionSettings.y);
  return half4(ao,Contact(center,normal),0,1);
 }
 half4 ExtractBloom(v2f_img i):SV_Target{
  float3 c=tex2D(_MainTex,i.uv).rgb;float peak=max(c.r,max(c.g,c.b));return half4(c*max(0,peak-_GradeSettings.w)/max(peak,.0001),1);
 }
 half4 Blur(v2f_img i):SV_Target{
  float2 d=_BlurDirection.xy;return tex2D(_MainTex,i.uv)*.227027+(tex2D(_MainTex,i.uv+d*1.3846)+tex2D(_MainTex,i.uv-d*1.3846))*.316216+(tex2D(_MainTex,i.uv+d*3.23077)+tex2D(_MainTex,i.uv-d*3.23077))*.07027;
 }
 float Weight(float2 uv,float depth,float3 normal){float d;float3 n;Surface(uv,d,n);return exp2(-abs(d-depth)*32)*pow(saturate(dot(normal,n)),8);}
 float3 Tonemap(float3 c){return saturate((c*(2.51*c+.03))/(c*(2.43*c+.59)+.14));}
 half4 Composite(v2f_img i):SV_Target{
  float depth;float3 normal;Surface(i.uv,depth,normal);float2 a=tex2D(_AOTexture,i.uv).rg*2;float weight=2;
  const float2 offsets[4]={float2(1,0),float2(-1,0),float2(0,1),float2(0,-1)};
  [unroll]for(int k=0;k<4;k++){float2 q=i.uv+offsets[k]*_AOTexture_TexelSize.xy;float w=Weight(q,depth,normal);a+=tex2D(_AOTexture,q).rg*w;weight+=w;}
  a/=weight;float3 c=tex2D(_MainTex,i.uv).rgb*a.x*a.y+tex2D(_BloomTexture,i.uv).rgb*_GradeSettings.z;
  float l=dot(c,float3(.2126,.7152,.0722));c*=lerp(float3(.90,.96,1.055),float3(1.025,1,.97),smoothstep(.02,.35,l));
  c=Tonemap(c*_GradeSettings.x);l=dot(c,float3(.2126,.7152,.0722));c=lerp(l.xxx,c,_GradeSettings.y);
  return half4(c,1);
 }
 ENDCG
 Pass { CGPROGRAM
 #pragma target 3.0
 #pragma vertex vert_img
 #pragma fragment Occlusion
 ENDCG }
 Pass { CGPROGRAM
 #pragma target 3.0
 #pragma vertex vert_img
 #pragma fragment ExtractBloom
 ENDCG }
 Pass { CGPROGRAM
 #pragma target 3.0
 #pragma vertex vert_img
 #pragma fragment Blur
 ENDCG }
 Pass { CGPROGRAM
 #pragma target 3.0
 #pragma vertex vert_img
 #pragma fragment Composite
 ENDCG }
 }
 Fallback Off
}
