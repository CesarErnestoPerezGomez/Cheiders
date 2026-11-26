Shader "ACG/CinematicSurface"
{
 Properties
 {
  _Color("Color", Color) = (0,1,1,1)
  _Mode("Mode (glass, trail, death)", Float) = 0
  _Progress("Progress", Range(0,1)) = 0
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _Color;
   float _Mode;
   float _Progress;
   CBUFFER_END
   struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
   struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 world : TEXCOORD1; };
   float Hash(float3 p) { return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453); }
   Varyings Vert(Attributes input)
   {
    Varyings output;
    output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
    output.uv=input.uv;
    output.world=TransformObjectToWorld(input.positionOS.xyz);
    return output;
   }
   half4 Frag(Varyings input) : SV_Target
   {
    if (_Mode < .5)
    {
     float2 p=input.uv*12;
     float2 cell=floor(p);
     float nearest=100, second=100, id=0;
     for(int y=-1;y<=1;y++) for(int x=-1;x<=1;x++)
     {
      float2 neighbor=cell+float2(x,y);
      float2 seed=neighbor+float2(Hash(float3(neighbor,1)),Hash(float3(neighbor,2)));
      float distance=length(p-seed);
      if(distance<nearest) { second=nearest; nearest=distance; id=Hash(float3(neighbor,3)); }
      else second=min(second,distance);
     }
     float activation=saturate(_Progress*3-length(input.uv-.5));
     float crack=1-smoothstep(.015,.08,second-nearest);
     clip(id+.001-max(0,_Progress*1.4-.3));
     return half4(_Color.rgb+crack*activation*3,_Color.a+crack*activation*.6);
    }
    if (_Mode < 1.5)
    {
     float edge=pow(abs(input.uv.y*2-1),8);
     float pulse=.7+.3*sin(input.uv.x*8-_Time.y*10);
     return half4(_Color.rgb*(1+edge*3)*pulse,_Color.a);
    }
    // The stopped bike and rider share one grid, including differently scaled parts.
    float3 cell=floor(input.world*5);
    float random=Hash(cell);
    clip(random+.001-_Progress*1.002);
    float flash=1-smoothstep(0,.15,random-_Progress);
    float3 ice = _Color.rgb * lerp(.58, 1.0, random);
    return half4(lerp(ice, float3(1.35,1.55,1.8), flash),1);
   }
   ENDHLSL
  }
 }
}
