Shader "Custom/TerrainWorldBend"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _BendAmount ("Bend Amount", Range(0, 0.01)) = 0.0005
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        float _BendAmount;

        struct Input
        {
            float2 uv_MainTex;
        };

        void vert(inout appdata_full v)
        {
            // Get world position
            float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
            
            // Calculate horizontal distance from camera
            float2 offset = worldPos.xz - _WorldSpaceCameraPos.xz;
            float distSq = dot(offset, offset);
            
            // Apply bend
            worldPos.y -= distSq * _BendAmount;
            
            // Convert back to object space
            v.vertex = mul(unity_WorldToObject, worldPos);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}