Shader "Custom/SelectiveInvert"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Invert("Invert", Range(0,1)) = 0
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
    }
        SubShader
        {
            Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }
            LOD 200
            Cull Off
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                };

                sampler2D _MainTex;
                float _Invert;
                float _Cutoff;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    fixed4 col = tex2D(_MainTex, i.uv);

                // Discard fully transparent pixels
                if (col.a < _Cutoff) discard;

                // Smooth invert
                col.rgb = lerp(col.rgb, 1.0 - col.rgb, _Invert);

                return col;
            }
            ENDCG
        }
        }
}
