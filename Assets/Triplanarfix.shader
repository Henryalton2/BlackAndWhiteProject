Shader "triplanarfix"
{
    Properties
    {
        _DayTex("Day Flat Texture", 2D) = "white" {}
        _NightTex("Night Flat Texture", 2D) = "black" {}

        _SteepDayTex("Day Steep Texture", 2D) = "gray" {}
        _SteepNightTex("Night Steep Texture", 2D) = "black" {}

        _Height1("Flat Height 1 End", Float) = 10
        _Height2("Flat Height 2 End", Float) = 20
        _HeightBlend("Height Blend", Range(0.1, 20)) = 2

        _SlopeThreshold("Slope Threshold", Range(0,1)) = 0.5
        _SlopeBlend("Slope Blend", Range(0.01,1)) = 0.1

        _DayTiling("Day Flat Tiling", Float) = 1
        _NightTiling("Night Flat Tiling", Float) = 1
        _SteepDayTiling("Day Steep Tiling", Float) = 1
        _SteepNightTiling("Night Steep Tiling", Float) = 1

        _TriplanarBlend("Triplanar Sharpness", Range(1,8)) = 4
        _BendAmount("World Bend", Range(0,0.01)) = 0

        _Invert("Day → Night", Range(0,1)) = 0
    }

        SubShader
        {
            Tags { "RenderType" = "Opaque" }
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
                };

                sampler2D _DayTex, _NightTex;
                sampler2D _SteepDayTex, _SteepNightTex;

                float _Height1, _Height2, _HeightBlend;
                float _SlopeThreshold, _SlopeBlend;
                float _DayTiling, _NightTiling;
                float _SteepDayTiling, _SteepNightTiling;
                float _TriplanarBlend;
                float _BendAmount;
                float _Invert;

                v2f vert(appdata v)
                {
                    v2f o;
                    float4 worldPos = mul(unity_ObjectToWorld, v.vertex);

                    float2 offset = worldPos.xz - _WorldSpaceCameraPos.xz;
                    worldPos.y -= dot(offset, offset) * _BendAmount;

                    o.vertex = mul(UNITY_MATRIX_VP, worldPos);
                    o.worldPos = worldPos.xyz;
                    o.worldNormal = UnityObjectToWorldNormal(v.normal);
                    return o;
                }

                fixed4 Triplanar(sampler2D tex, float3 p, float tiling, float3 blend)
                {
                    return tex2D(tex, p.zy * tiling) * blend.x +
                           tex2D(tex, p.xz * tiling) * blend.y +
                           tex2D(tex, p.xy * tiling) * blend.z;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    float3 n = normalize(i.worldNormal);
                    float3 blend = pow(abs(n), _TriplanarBlend);
                    blend /= (blend.x + blend.y + blend.z);

                    float height = i.worldPos.y;
                    float slope = n.y;

                    float flatW = smoothstep(_SlopeThreshold - _SlopeBlend, _SlopeThreshold + _SlopeBlend, slope);
                    float steepW = 1 - flatW;

                    float h1 = 1 - smoothstep(_Height1 - _HeightBlend, _Height1, height);
                    float h2 = smoothstep(_Height1 - _HeightBlend, _Height1, height)
                             - smoothstep(_Height2 - _HeightBlend, _Height2, height);
                    float h3 = smoothstep(_Height2 - _HeightBlend, _Height2, height);
                    float heightMask = h1 + h2 + h3;

                    fixed4 dayFlat = Triplanar(_DayTex, i.worldPos, _DayTiling, blend) * heightMask;
                    fixed4 nightFlat = Triplanar(_NightTex, i.worldPos, _NightTiling, blend) * heightMask;

                    fixed4 daySteep = Triplanar(_SteepDayTex, i.worldPos, _SteepDayTiling, blend);
                    fixed4 nightSteep = Triplanar(_SteepNightTex, i.worldPos, _SteepNightTiling, blend);

                    fixed4 dayCol = dayFlat * flatW + daySteep * steepW;
                    fixed4 nightCol = nightFlat * flatW + nightSteep * steepW;

                    return lerp(dayCol, nightCol, _Invert);
                }
                ENDCG
            }
        }
            FallBack "Diffuse"
}
