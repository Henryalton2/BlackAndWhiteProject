Shader "Custom/TriplanarSlopeHeight"
{
Properties
{
[Header(Flat Surface Textures by Height)]
_FlatTex1 ("Flat Layer 1 Low", 2D) = "white" {}
_FlatTex2 ("Flat Layer 2 Mid", 2D) = "white" {}
_FlatTex3 ("Flat Layer 3 High", 2D) = "white" {}

[Header(Steep Surface Texture)]
_SteepTex ("Steep Cliff Texture", 2D) = "gray" {}

[Header(Height Transitions for Flat Surfaces)]
_Height1 ("Flat Height 1 End", Float) = 10.0
_Height2 ("Flat Height 2 End", Float) = 20.0
_HeightBlend ("Height Gradient Smoothness", Range(0.1, 20)) = 2.0

[Header(Slope Settings)]
_SlopeThreshold ("Slope Steepness Threshold", Range(0, 1)) = 0.5
_SlopeBlend ("Slope Gradient Smoothness", Range(0.01, 1)) = 0.1

[Header(Triplanar Settings)]
_Tiling ("Texture Tiling", Float) = 1.0
_TriplanarBlend ("Triplanar Blend Sharpness", Range(1, 8)) = 4.0

[Header(World Bend)]
_BendAmount ("Bend Amount", Range(0, 0.01)) = 0.0

[Header(Night Mode)]
_Invert ("Invert Amount", Range(0,1)) = 0
}

SubShader
{
Tags { "RenderType"="Opaque" }
LOD 200

Pass
{
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

struct appdata
{
float4 vertex : POSITION;
float3 normal : NORMAL;
};

struct v2f
{
float4 vertex : SV_POSITION;
float3 worldPos : TEXCOORD0;
float3 worldNormal : TEXCOORD1;
float3 originalWorldPos : TEXCOORD2;
};

sampler2D _FlatTex1;
sampler2D _FlatTex2;
sampler2D _FlatTex3;
sampler2D _SteepTex;

float _Tiling;
float _BendAmount;
float _Height1;
float _Height2;
float _HeightBlend;
float _SlopeThreshold;
float _SlopeBlend;
float _TriplanarBlend;
float _Invert;

v2f vert (appdata v)
{
v2f o;

float4 originalWorldPos = mul(unity_ObjectToWorld, v.vertex);
o.originalWorldPos = originalWorldPos.xyz;

float4 worldPos = originalWorldPos;
float2 offset = worldPos.xz - _WorldSpaceCameraPos.xz;
float distSq = dot(offset, offset);
worldPos.y -= distSq * _BendAmount;

o.vertex = mul(UNITY_MATRIX_VP, worldPos);
o.worldPos = worldPos.xyz;
o.worldNormal = UnityObjectToWorldNormal(v.normal);

return o;
}

fixed4 TriplanarSample(sampler2D tex, float3 worldPos, float3 blendAxes)
{
float2 xUV = worldPos.zy * _Tiling;
float2 yUV = worldPos.xz * _Tiling;
float2 zUV = worldPos.xy * _Tiling;

fixed4 xSample = tex2D(tex, xUV);
fixed4 ySample = tex2D(tex, yUV);
fixed4 zSample = tex2D(tex, zUV);

return xSample * blendAxes.x +
ySample * blendAxes.y +
zSample * blendAxes.z;
}

fixed4 frag (v2f i) : SV_Target
{
float3 worldNormal = normalize(i.worldNormal);

float3 blendAxes = abs(worldNormal);
blendAxes = pow(blendAxes, _TriplanarBlend);
blendAxes /= (blendAxes.x + blendAxes.y + blendAxes.z);

float height = i.originalWorldPos.y;
float slope = worldNormal.y;

float flatWeight = smoothstep(
_SlopeThreshold - _SlopeBlend,
_SlopeThreshold + _SlopeBlend,
slope
);

float steepWeight = 1.0 - flatWeight;

fixed4 flatTex1 = TriplanarSample(_FlatTex1, i.originalWorldPos, blendAxes);
fixed4 flatTex2 = TriplanarSample(_FlatTex2, i.originalWorldPos, blendAxes);
fixed4 flatTex3 = TriplanarSample(_FlatTex3, i.originalWorldPos, blendAxes);

float h1Weight = 1.0 - smoothstep(_Height1 - _HeightBlend, _Height1, height);
float h2Weight =
smoothstep(_Height1 - _HeightBlend, _Height1, height) -
smoothstep(_Height2 - _HeightBlend, _Height2, height);
float h3Weight = smoothstep(_Height2 - _HeightBlend, _Height2, height);

fixed4 flatColor =
flatTex1 * h1Weight +
flatTex2 * h2Weight +
flatTex3 * h3Weight;

fixed4 steepColor = TriplanarSample(_SteepTex, i.originalWorldPos, blendAxes);

fixed4 finalColor =
flatColor * flatWeight +
steepColor * steepWeight;

// Selective invert (driven by script)
fixed3 inverted = 1.0 - finalColor.rgb;
finalColor.rgb = lerp(finalColor.rgb, inverted, _Invert);

return finalColor;
}
ENDCG
}
}

FallBack "Diffuse"
}    

