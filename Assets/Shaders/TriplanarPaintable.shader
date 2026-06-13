Shader "TriplanarPaintable"
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

        _Invert("Day -> Night", Range(0,1)) = 0

        [Header(Painted Layers)]
        [NoScaleOffset] _Control("Splat Map", 2D) = "black" {}

        _Splat0("Painted Layer 0", 2D) = "white" {}
        _Splat0Tiling("Layer 0 Tiling", Float) = 1
        _Splat1("Painted Layer 1", 2D) = "white" {}
        _Splat1Tiling("Layer 1 Tiling", Float) = 1
        _Splat2("Painted Layer 2", 2D) = "white" {}
        _Splat2Tiling("Layer 2 Tiling", Float) = 1
        _Splat3("Painted Layer 3", 2D) = "white" {}
        _Splat3Tiling("Layer 3 Tiling", Float) = 1
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
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex      : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 samplePos   : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float2 uv          : TEXCOORD3;
            };

            sampler2D _DayTex, _NightTex;
            sampler2D _SteepDayTex, _SteepNightTex;
            sampler2D _Control;
            sampler2D _Splat0, _Splat1, _Splat2, _Splat3;

            float _Height1, _Height2, _HeightBlend;
            float _SlopeThreshold, _SlopeBlend;
            float _DayTiling, _NightTiling;
            float _SteepDayTiling, _SteepNightTiling;
            float _TriplanarBlend;
            float _BendAmount;
            float _Invert;
            float _Splat0Tiling, _Splat1Tiling, _Splat2Tiling, _Splat3Tiling;

            v2f vert(appdata v)
            {
                v2f o;

                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.samplePos = worldPos.xyz;

                float2 offset = worldPos.xz - _WorldSpaceCameraPos.xz;
                worldPos.y -= dot(offset, offset) * _BendAmount;

                o.vertex      = mul(UNITY_MATRIX_VP, worldPos);
                o.worldPos    = worldPos.xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv          = v.uv;

                return o;
            }

            fixed4 Triplanar(sampler2D tex, float3 p, float tiling, float3 blend)
            {
                return tex2D(tex, p.zy * tiling) * blend.x
                     + tex2D(tex, p.xz * tiling) * blend.y
                     + tex2D(tex, p.xy * tiling) * blend.z;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n     = normalize(i.worldNormal);
                float3 blend = pow(abs(n), _TriplanarBlend);
                blend /= (blend.x + blend.y + blend.z);

                float height = i.worldPos.y;
                float slope  = n.y;

                float flatW  = smoothstep(_SlopeThreshold - _SlopeBlend,
                                          _SlopeThreshold + _SlopeBlend, slope);
                float steepW = 1.0 - flatW;

                float h1 = 1.0 - smoothstep(_Height1 - _HeightBlend, _Height1, height);
                float h2 = smoothstep(_Height1 - _HeightBlend, _Height1, height)
                         - smoothstep(_Height2 - _HeightBlend, _Height2, height);
                float h3 = smoothstep(_Height2 - _HeightBlend, _Height2, height);
                float heightMask = h1 + h2 + h3;

                fixed4 dayFlat    = Triplanar(_DayTex,        i.samplePos, _DayTiling,        blend) * heightMask;
                fixed4 nightFlat  = Triplanar(_NightTex,      i.samplePos, _NightTiling,      blend) * heightMask;
                fixed4 daySteep   = Triplanar(_SteepDayTex,   i.samplePos, _SteepDayTiling,   blend);
                fixed4 nightSteep = Triplanar(_SteepNightTex, i.samplePos, _SteepNightTiling,  blend);

                fixed4 dayCol   = dayFlat   * flatW + daySteep   * steepW;
                fixed4 nightCol = nightFlat * flatW + nightSteep * steepW;

                fixed4 baseColor = lerp(dayCol, nightCol, _Invert);

                // Unity terrain auto-binds its alphamap to _Control and painted
                // textures to _Splat0-3. Use the paint tool normally — unpainted
                // areas (ctrl.rgb = 0) show the triplanar base unchanged.
                fixed4 ctrl = tex2D(_Control, i.uv);

                fixed4 p0 = Triplanar(_Splat0, i.samplePos, _Splat0Tiling, blend);
                fixed4 p1 = Triplanar(_Splat1, i.samplePos, _Splat1Tiling, blend);
                fixed4 p2 = Triplanar(_Splat2, i.samplePos, _Splat2Tiling, blend);
                fixed4 p3 = Triplanar(_Splat3, i.samplePos, _Splat3Tiling, blend);

                float  paintedWeight = saturate(ctrl.r + ctrl.g + ctrl.b);
                fixed4 paintedColor  = p0 * ctrl.r + p1 * ctrl.g + p2 * ctrl.b;

                return lerp(baseColor, paintedColor, paintedWeight);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
