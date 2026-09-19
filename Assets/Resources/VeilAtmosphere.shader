Shader "Hidden/VeilHouse/Atmosphere" {
    Properties { _MainTex ("Scene", 2D) = "white" {} }
    SubShader {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
        sampler2D _CameraDepthNormalsTexture;
        sampler2D _AOTexture;
        float4 _AOTexture_TexelSize;
        float4 _ProjectionScale;
        float4 _OcclusionSettings; // radius in metres, strength, surface bias, max shade
        float4 _GradeSettings;

        float2 DepthUV(float2 uv) {
            #if UNITY_UV_STARTS_AT_TOP
            if(_MainTex_TexelSize.y<0)uv.y=1-uv.y;
            #endif
            return uv;
        }
        void Surface(float2 uv,out float depth,out float3 normal) {
            DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture,DepthUV(uv)),depth,normal);
            depth*= _ProjectionScale.z;
        }
        float3 ViewPosition(float2 uv,float eyeDepth) {
            // DepthNormals stores positive eye distance; Unity view-space forward is -Z.
            return float3((uv*2-1)*_ProjectionScale.xy,-1)*eyeDepth;
        }
        float Contribution(float2 uv,float2 offset,float3 center,float3 normal) {
            float2 neighborUV=uv+offset;
            if(any(neighborUV<0)||any(neighborUV>1))return 0;
            float neighborDepth;float3 neighborNormal;
            Surface(neighborUV,neighborDepth,neighborNormal);
            float3 delta=ViewPosition(neighborUV,neighborDepth)-center;
            float distanceSquared=dot(delta,delta);
            float radius=_OcclusionSettings.x;
            // Reject silhouettes crossing into distant rooms: only nearby geometry occludes.
            float falloff=saturate(1-distanceSquared/(radius*radius));
            float facing=max(0,dot(normal,delta)*rsqrt(max(distanceSquared,.0001))-.10);
            float bias=smoothstep(_OcclusionSettings.z,_OcclusionSettings.z*3,sqrt(distanceSquared));
            return facing*falloff*falloff*bias;
        }
        half4 Occlusion(v2f_img i):SV_Target {
            float depth;float3 normal;Surface(i.uv,depth,normal);
            if(depth>=_ProjectionScale.z*.995 || depth<.05)return 1;
            float3 center=ViewPosition(i.uv,depth);
            // Pixel footprint scales with perspective, preserving a fixed world-space radius.
            float2 radiusUV=(_OcclusionSettings.x*.5)/(max(depth,.2)*_ProjectionScale.xy);
            float sum=0;
            sum+=Contribution(i.uv,radiusUV*float2( .38, 0),center,normal);
            sum+=Contribution(i.uv,radiusUV*float2(-.38, 0),center,normal);
            sum+=Contribution(i.uv,radiusUV*float2(0, .38),center,normal);
            sum+=Contribution(i.uv,radiusUV*float2(0,-.38),center,normal);
            sum+=Contribution(i.uv,radiusUV*float2( .57, .57),center,normal);
            sum+=Contribution(i.uv,radiusUV*float2(-.57, .57),center,normal);
            sum+=Contribution(i.uv,radiusUV*float2( .57,-.57),center,normal);
            sum+=Contribution(i.uv,radiusUV*float2(-.57,-.57),center,normal);
            float shade=min(_OcclusionSettings.w,sum*.125*_OcclusionSettings.y);
            return half4(1-shade,1-shade,1-shade,1);
        }
        float FilterWeight(float2 uv,float depth,float3 normal) {
            float nearbyDepth;float3 nearbyNormal;Surface(uv,nearbyDepth,nearbyNormal);
            return exp2(-abs(nearbyDepth-depth)*35)*pow(saturate(dot(normal,nearbyNormal)),8);
        }
        half4 Composite(v2f_img i):SV_Target {
            half4 color=tex2D(_MainTex,i.uv);
            float depth;float3 normal;Surface(i.uv,depth,normal);
            float ao=tex2D(_AOTexture,i.uv).r*2;
            float weight=2;
            float2 px=_AOTexture_TexelSize.xy;
            float2 u=i.uv+float2(px.x,0);float w=FilterWeight(u,depth,normal);ao+=tex2D(_AOTexture,u).r*w;weight+=w;
            u=i.uv-float2(px.x,0);w=FilterWeight(u,depth,normal);ao+=tex2D(_AOTexture,u).r*w;weight+=w;
            u=i.uv+float2(0,px.y);w=FilterWeight(u,depth,normal);ao+=tex2D(_AOTexture,u).r*w;weight+=w;
            u=i.uv-float2(0,px.y);w=FilterWeight(u,depth,normal);ao+=tex2D(_AOTexture,u).r*w;weight+=w;
            color.rgb*=ao/weight;
            color.rgb*=_GradeSettings.x;
            float luminance=dot(color.rgb,float3(.2126,.7152,.0722));
            color.rgb=lerp(luminance.xxx,color.rgb,_GradeSettings.y);
            // No black-point crush, vignette, LUT tint or film grain: preserve investigation cues.
            return color;
        }
        ENDCG
        Pass {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Occlusion
            ENDCG
        }
        Pass {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Composite
            ENDCG
        }
    }
    Fallback Off
}
