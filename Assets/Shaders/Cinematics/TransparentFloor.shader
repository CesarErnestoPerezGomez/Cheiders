Shader "ACG/Transparent Floor"
{
    Properties
    {
        _Glass ("Glass tint and opacity", Color) = (0.045,0.16,0.21,0.34)
        [HDR] _Edge ("Panel lights", Color) = (0.12,1.65,2,1)
        _PanelSize ("Panel spacing (metres)", Range(2,12)) = 6
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-20" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Glass, _Edge;
                float _PanelSize;
            CBUFFER_END
            struct Attributes { float3 position : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 clip : SV_POSITION; float3 world : TEXCOORD0; half3 normal : TEXCOORD1; float2 uv : TEXCOORD2; };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.world = TransformObjectToWorld(v.position);
                o.clip = TransformWorldToHClip(o.world);
                o.normal = TransformObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half3 normal = normalize(i.normal);
                half3 view = GetWorldSpaceNormalizeViewDir(i.world);
                half fresnel = pow(1 - saturate(abs(dot(normal,view))), 3);
                half3 reflection = GlossyEnvironmentReflection(reflect(-view,normal), 0.10h, 1.0h);
                float2 panel = i.world.xz / _PanelSize;
                float2 dist = abs(frac(panel + 0.5) - 0.5);
                float2 aa = max(fwidth(panel), 0.0001);
                float2 seam = 1 - smoothstep(0.005, 0.005 + aa, dist);
                seam *= saturate(0.12 / aa);
                float2 uvDist = min(i.uv, 1-i.uv) / max(fwidth(i.uv),0.00001);
                float rim = 1 - smoothstep(1, 2.5, min(uvDist.x,uvDist.y));
                half glow = max(rim, 0.48 * max(seam.x,seam.y));
                half3 color = _Glass.rgb + reflection * (0.65 + fresnel) + _Edge.rgb * glow;
                return half4(color, saturate(_Glass.a + fresnel * 0.34 + glow * 0.65));
            }
            ENDHLSL
        }
    }
}
