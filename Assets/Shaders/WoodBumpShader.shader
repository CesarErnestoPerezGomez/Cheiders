Shader "ACG/WoodBump"
{
    Properties
    {
        _DarkColor ("Madera oscura", Color) = (0.12,0.045,0.015,1)
        _LightColor ("Madera clara", Color) = (0.5,0.25,0.09,1)
        _Scale ("Cantidad de vetas", Range(3,50)) = 22
        _Length ("Variacion vertical", Range(0.5,8)) = 3
        _Distortion ("Irregularidad", Range(0,3)) = 1.2
        _Bump ("Intensidad del bump", Range(0,3)) = 0.8
        _Smoothness ("Suavidad", Range(0,1)) = 0.25
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _DarkColor, _LightColor;
                float _Scale, _Length, _Distortion, _Bump, _Smoothness;
            CBUFFER_END
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float4 tangent : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };
            float hash(float2 p)
            {
                return frac(sin(dot(p,float2(127.1,311.7))) * 43758.5453);
            }
            float noise(float2 p, float period)
            {
                float2 cell = floor(p), f = frac(p);
                f = f*f*(3-2*f);
                cell.x -= floor(cell.x/period)*period;
                float nextX = fmod(cell.x+1,period);
                float a = lerp(hash(cell),hash(float2(nextX,cell.y)),f.x);
                float b = lerp(hash(cell+float2(0,1)),hash(float2(nextX,cell.y+1)),f.x);
                return lerp(a,b,f.y);
            }
            float wood(float2 uv)
            {
                float count = round(_Scale);
                float warp = noise(float2(uv.x*6,uv.y*_Length),6);
                float grain = sin((uv.x*count + warp*_Distortion*3)*6.28318);
                grain = smoothstep(0.15,0.85,grain*0.5+0.5);
                float detail = noise(uv*float2(count*3,_Length*8),count*3);
                return grain*0.85 + detail*0.15;
            }
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.normal = TransformObjectToWorldNormal(v.normal);
                o.tangent = float4(TransformObjectToWorldDir(v.tangent.xyz),
                    v.tangent.w * GetOddNegativeScale());
                o.uv = v.uv;
                return o;
            }
            half4 frag(v2f i) : SV_Target
            {
                float h = wood(i.uv);
                float hx = wood(i.uv + float2(0.001,0));
                float hy = wood(i.uv + float2(0,0.001));
                
                float3 bump = normalize(float3(float2(h-hx,h-hy)*_Bump*20,1));
                float3 n = normalize(i.normal);
                float3 t = normalize(i.tangent.xyz-n*dot(i.tangent.xyz,n));
                float3 b = cross(n,t)*i.tangent.w;
                n = normalize(t*bump.x + b*bump.y + n*bump.z);
                float3 color = lerp(_DarkColor.rgb,_LightColor.rgb,h);
                Light light = GetMainLight(TransformWorldToShadowCoord(i.worldPos));
                float diffuse = saturate(dot(n,light.direction));
                float3 viewDir = GetWorldSpaceNormalizeViewDir(i.worldPos);
                float3 halfDir = SafeNormalize(light.direction+viewDir);
                float specular = pow(saturate(dot(n,halfDir)),lerp(8,128,_Smoothness));
                specular *= _Smoothness*0.2;
                float3 direct = light.color*light.shadowAttenuation;
                return half4(color*SampleSH(n) + (color*diffuse+specular)*direct,1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection, _LightPosition;
            float4 shadowVert(float3 vertex : POSITION, float3 normal : NORMAL) : SV_POSITION
            {
                float3 p = TransformObjectToWorld(vertex);
                float3 n = TransformObjectToWorldNormal(normal);
                float3 lightDir = _LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    lightDir = normalize(_LightPosition-p);
                #endif
                float4 pos = TransformWorldToHClip(ApplyShadowBias(p,n,lightDir));
                return ApplyShadowClamping(pos);
            }
            half4 shadowFrag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
