Shader "ACG/Damascus"
{
    Properties
    {
        _ColorA ("Turquesa", Color) = (0.03,0.85,0.95,1)
        _ColorB ("Verde", Color) = (0.08,0.75,0.35,1)
        _ColorC ("Azul violeta", Color) = (0.12,0.06,0.65,1)
        _EdgeColor ("Magenta", Color) = (0.85,0.05,0.65,1)
        _LineColor ("Coral", Color) = (1,0.24,0.25,1)
        _Scale ("Escala", Range(1,15)) = 4
        _Distortion ("Distorsion", Range(0,3)) = 1.4
        _Layers ("Cantidad de vetas", Range(5,35)) = 18
        _LineWidth ("Grosor de vetas", Range(0.02,0.12)) = 0.055
        _Metallic ("Metalico", Range(0,1)) = 0.35
        _Smoothness ("Suavidad", Range(0,1)) = 0.7
        _ColorBoost ("Emision suave", Range(0,1)) = 0.35
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA, _ColorB, _ColorC, _EdgeColor, _LineColor;
                float _Scale, _Distortion, _Layers, _LineWidth;
                float _Metallic, _Smoothness, _ColorBoost;
            CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            float hash(float2 p)
            {
                return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);
            }
            float noise(float2 p)
            {
                float2 cell = floor(p), f = frac(p);
                f = f*f*(3-2*f);
                float a = lerp(hash(cell),hash(cell+float2(1,0)),f.x);
                float b = lerp(hash(cell+float2(0,1)),hash(cell+float2(1,1)),f.x);
                return lerp(a,b,f.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.normal = TransformObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv*_Scale;
                float2 warp = float2(noise(p),noise(p+float2(5.2,1.3)));
                p += (warp-0.5)*_Distortion*2;
                // Varias escalas de ruido crean curvas y anillos irregulares.
                float field = noise(p*0.7)*0.6;
                field += noise(p*1.7)*0.28 + noise(p*3.4)*0.12;
                float bands = field*_Layers;
                float d = abs(frac(bands)-0.5);
                float aa = max(fwidth(bands),0.001);
                float outer = 1-smoothstep(_LineWidth*2.7,_LineWidth*2.7+aa,d);
                float edge = 1-smoothstep(_LineWidth*1.7,_LineWidth*1.7+aa,d);
                float lineMask = 1-smoothstep(_LineWidth,_LineWidth+aa,d);

                float baseMix = smoothstep(0.25,0.75,noise(p*0.5+10));
                float3 color = lerp(_ColorA.rgb,_ColorB.rgb,baseMix);
                color = lerp(color,_ColorC.rgb,outer);
                color = lerp(color,_EdgeColor.rgb,edge);
                color = lerp(color,_LineColor.rgb,lineMask);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = color;
                surface.metallic = _Metallic;
                surface.smoothness = _Smoothness;
                surface.occlusion = 1;
                surface.alpha = 1;
                surface.emission = color*_ColorBoost;

                InputData data = (InputData)0;
                data.positionWS = i.worldPos;
                data.normalWS = normalize(i.normal);
                data.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.worldPos);
                data.shadowCoord = TransformWorldToShadowCoord(i.worldPos);
                data.bakedGI = SampleSH(data.normalWS);
                data.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.pos);
                data.shadowMask = half4(1,1,1,1);
                return UniversalFragmentPBR(data,surface);
            }
            ENDHLSL
        }
    }
}