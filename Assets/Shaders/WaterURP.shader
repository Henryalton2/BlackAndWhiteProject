Shader "Custom/WaterURP"
{
    Properties
    {
        [Header(Colors)]
        _ShallowColor       ("Shallow Color",         Color)   = (0.08, 0.58, 0.68, 0.75)
        _DeepColor          ("Deep Color",            Color)   = (0.01, 0.12, 0.35, 0.95)
        _FoamColor          ("Foam Color",            Color)   = (0.95, 0.97, 1.00, 1.00)

        [Header(Normal Maps)]
        _NormalMap          ("Normal Map A",          2D)      = "bump" {}
        _NormalMap2         ("Normal Map B",          2D)      = "bump" {}
        _NormalStrength     ("Normal Strength",       Range(0.0, 3.0))  = 1.2

        [Header(Waves)]
        _WaveHeight         ("Wave Height",           Range(0.0, 2.0))  = 0.15
        _WaveFrequency      ("Wave Frequency",        Range(0.1, 10.0)) = 1.5
        _WaveSpeed          ("Wave Speed",            Range(0.0, 5.0))  = 1.0
        _WaveSteepness      ("Wave Steepness",        Range(0.0, 1.0))  = 0.5

        [Header(Wind)]
        _WindSpeed          ("Wind Speed",            Range(0.0, 10.0)) = 2.0
        _WindDirection      ("Wind Direction (XZ)",   Vector)  = (1.0, 0.0, 0.0, 0.0)

        [Header(Foam)]
        _FoamScale          ("Foam Edge Scale",       Range(0.1, 5.0))  = 1.5
        _FoamCutoff         ("Foam Cutoff",           Range(0.0, 1.0))  = 0.55
        _FoamWindStreak     ("Foam Wind Streak",      Range(0.0, 1.0))  = 0.3

        [Header(Depth and Surface)]
        _DepthMaxDistance   ("Depth Fade Distance",   Float)   = 4.0
        _Smoothness         ("Smoothness",            Range(0.0, 1.0))  = 0.92
        _SpecularStrength   ("Specular Strength",     Range(0.0, 2.0))  = 1.0
        _FresnelPower       ("Fresnel Power",         Range(1.0, 10.0)) = 4.0
        _FresnelStrength    ("Fresnel Opacity Boost", Range(0.0, 1.0))  = 0.5

        [Header(Reflections)]
        _ReflectionTex      ("Planar Reflection Tex (auto)", 2D)      = "black" {}
        _ReflectionStrength ("Reflection Strength",  Range(0.0, 1.0))  = 0.7
        _ReflectionDistortion("Reflection Distortion",Range(0.0, 0.1)) = 0.02
        _PlanarBlend        ("Planar vs Probe Blend", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent-10"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 300
        Pass
        {
            Name "WaterForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex   WaterVert
            #pragma fragment WaterFrag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FoamColor;

                float4 _NormalMap_ST;
                float4 _NormalMap2_ST;
                float  _NormalStrength;

                float  _WaveHeight;
                float  _WaveFrequency;
                float  _WaveSpeed;
                float  _WaveSteepness;

                float  _WindSpeed;
                float4 _WindDirection;

                float  _FoamScale;
                float  _FoamCutoff;
                float  _FoamWindStreak;

                float  _DepthMaxDistance;
                float  _Smoothness;
                float  _SpecularStrength;
                float  _FresnelPower;
                float  _FresnelStrength;

                float  _ReflectionStrength;
                float  _ReflectionDistortion;
                float  _PlanarBlend;
            CBUFFER_END

            TEXTURE2D(_NormalMap);       SAMPLER(sampler_NormalMap);
            TEXTURE2D(_NormalMap2);      SAMPLER(sampler_NormalMap2);
            TEXTURE2D(_ReflectionTex);   SAMPLER(sampler_ReflectionTex);
            float2 Rotate2D(float2 v, float rad)
            {
                float s, c;
                sincos(rad, s, c);
                return float2(v.x * c - v.y * s, v.x * s + v.y * c);
            }

            float3 GerstnerDisplace(float2 dir, float amp, float steep,
                                    float freq, float spd,
                                    float3 posWS, float time)
            {
                dir = normalize(dir);
                float Q = steep / max(freq * amp * 4.0, 0.001);
                float theta = dot(dir, posWS.xz) * freq - time * spd;
                float s, c;
                sincos(theta, s, c);
                return float3(Q * amp * dir.x * c, amp * s, Q * amp * dir.y * c);
            }

            float3 GerstnerNormal(float2 dir, float amp, float steep,
                                  float freq, float spd,
                                  float3 posWS, float time)
            {
                dir = normalize(dir);
                float Q = steep / max(freq * amp * 4.0, 0.001);
                float theta = dot(dir, posWS.xz) * freq - time * spd;
                float s, c;
                sincos(theta, s, c);
                return float3(-(dir.x * freq * amp * c),
                              -(Q     * freq * amp * s),
                              -(dir.y * freq * amp * c));
            }
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 tangentWS    : TEXCOORD2;
                float3 bitangentWS  : TEXCOORD3;
                float2 uv           : TEXCOORD4;
                float4 screenPos    : TEXCOORD5;
                float  fogFactor    : TEXCOORD6;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings WaterVert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float  t     = _Time.y;
                float2 wDir  = normalize(_WindDirection.xz);
                float  ws    = max(_WindSpeed, 0.01);

                float3 disp = float3(0, 0, 0);
                disp += GerstnerDisplace(wDir,
                    _WaveHeight * ws * 0.06, _WaveSteepness,
                    _WaveFrequency, _WaveSpeed * ws * 0.5, posWS, t);
                disp += GerstnerDisplace(Rotate2D(wDir,  0.35),
                    _WaveHeight * ws * 0.04, _WaveSteepness * 0.7,
                    _WaveFrequency * 1.6, _WaveSpeed * ws * 0.45, posWS, t);
                disp += GerstnerDisplace(Rotate2D(wDir, -0.26),
                    _WaveHeight * ws * 0.025, _WaveSteepness * 0.5,
                    _WaveFrequency * 2.3, _WaveSpeed * ws * 0.4, posWS, t);
                disp += GerstnerDisplace(float2(-wDir.y, wDir.x),
                    _WaveHeight * ws * 0.015, _WaveSteepness * 0.3,
                    _WaveFrequency * 3.6, _WaveSpeed * ws * 0.3, posWS, t);
                posWS += disp;

                float3 nAcc = float3(0, 0, 0);
                nAcc += GerstnerNormal(wDir,
                    _WaveHeight * ws * 0.06, _WaveSteepness,
                    _WaveFrequency, _WaveSpeed * ws * 0.5, posWS, t);
                nAcc += GerstnerNormal(Rotate2D(wDir,  0.35),
                    _WaveHeight * ws * 0.04, _WaveSteepness * 0.7,
                    _WaveFrequency * 1.6, _WaveSpeed * ws * 0.45, posWS, t);
                nAcc += GerstnerNormal(Rotate2D(wDir, -0.26),
                    _WaveHeight * ws * 0.025, _WaveSteepness * 0.5,
                    _WaveFrequency * 2.3, _WaveSpeed * ws * 0.4, posWS, t);
                nAcc += GerstnerNormal(float2(-wDir.y, wDir.x),
                    _WaveHeight * ws * 0.015, _WaveSteepness * 0.3,
                    _WaveFrequency * 3.6, _WaveSpeed * ws * 0.3, posWS, t);

                float3 waveN = normalize(float3(nAcc.x, 1.0 - abs(nAcc.y), nAcc.z));
                float3 tangWS = normalize(float3(waveN.y + 0.001, -waveN.x, 0.0));
                float3 bitaWS = normalize(cross(waveN, tangWS));

                OUT.positionWS  = posWS;
                OUT.positionCS  = TransformWorldToHClip(posWS);
                OUT.normalWS    = waveN;
                OUT.tangentWS   = tangWS;
                OUT.bitangentWS = bitaWS;
                OUT.uv          = IN.uv;
                OUT.screenPos   = ComputeScreenPos(OUT.positionCS);
                OUT.fogFactor   = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

 
            half4 WaterFrag(Varyings IN) : SV_Target
            {
                float2 ssUV = IN.screenPos.xy / IN.screenPos.w;

 
                float rawDepth   = SampleSceneDepth(ssUV);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float depthDiff  = saturate((sceneDepth - IN.screenPos.w) / _DepthMaxDistance);

 
                half4 waterColor = lerp(_ShallowColor, _DeepColor, depthDiff);

 
                float  ws   = _WindSpeed;
                float2 wDir = normalize(_WindDirection.xz);
                float  t    = _Time.y;

                float2 uvA = IN.uv * _NormalMap_ST.xy  + _NormalMap_ST.zw
                           + wDir  * (t * ws * 0.04);
                float2 uvB = IN.uv * _NormalMap2_ST.xy + _NormalMap2_ST.zw
                           - wDir  * (t * ws * 0.025) + float2(0.31, 0.17);

                half3 nA = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap,  sampler_NormalMap,  uvA));
                half3 nB = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap2, sampler_NormalMap2, uvB));

   
                half3 blendN = normalize(half3(nA.xy + nB.xy, nA.z));
                blendN = lerp(half3(0, 0, 1), blendN, saturate(_NormalStrength));

         
                float3x3 TBN     = float3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
                float3   normalWS = normalize(mul(blendN, TBN));

          
                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float  NdotV   = saturate(dot(normalWS, viewDir));
                float  fresnel = pow(1.0 - NdotV, _FresnelPower);
                waterColor.a   = saturate(waterColor.a + fresnel * _FresnelStrength);
                float3 reflDir = reflect(-viewDir, normalWS);
                float  roughMip    = (1.0 - _Smoothness) * 6.0;
                half4  encodedProbe = SAMPLE_TEXTURECUBE_LOD(
                    unity_SpecCube0, samplerunity_SpecCube0, reflDir, roughMip);
                half3  probeRefl   = DecodeHDREnvironment(encodedProbe, unity_SpecCube0_HDR);
                float2 distOffset = blendN.xy * _ReflectionDistortion * (1.0 + ws * 0.05);
                float2 reflUV     = clamp(ssUV + distOffset, 0.001, 0.999);
                half4  planarSamp = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, reflUV);
                float  planarWeight = saturate(planarSamp.a * 100.0) * _PlanarBlend;
                half3  finalRefl    = lerp(probeRefl, planarSamp.rgb, planarWeight);
                float  foamMask  = 1.0 - saturate(depthDiff * _FoamScale);
                        foamMask  = step(_FoamCutoff, foamMask);
                float  streakN   = (nA.x * 0.5 + 0.5) * (nB.y * 0.5 + 0.5);
                        foamMask  = saturate(foamMask + streakN * _FoamWindStreak * saturate(ws * 0.2));

                float  reflMask  = fresnel * _ReflectionStrength * (1.0 - foamMask);
                waterColor.rgb   = lerp(waterColor.rgb, finalRefl, reflMask);
                waterColor.rgb   = lerp(waterColor.rgb, _FoamColor.rgb, foamMask);
                waterColor.a     = lerp(waterColor.a,   1.0,            foamMask * _FoamColor.a);
                Light  mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                float3 lightDir  = normalize(mainLight.direction);
                float3 halfDir   = normalize(lightDir + viewDir);

                float NdotL   = saturate(dot(normalWS, lightDir));
                float NdotH   = saturate(dot(normalWS, halfDir));
                float gloss   = exp2(_Smoothness * 10.0 + 1.0);

                half3 diffuse  = mainLight.color * mainLight.shadowAttenuation * NdotL;
                half3 specular = mainLight.color * mainLight.shadowAttenuation
                                 * pow(NdotH, gloss) * _SpecularStrength;
                half3 ambient  = SampleSH(normalWS) * 0.5;

                waterColor.rgb = waterColor.rgb * (diffuse + ambient) + specular;

                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < lightCount; ++i)
                {
                    Light al = GetAdditionalLight(i, IN.positionWS);
                    float aNdotL = saturate(dot(normalWS, al.direction));
                    waterColor.rgb += waterColor.rgb * al.color
                                    * al.distanceAttenuation
                                    * al.shadowAttenuation * aNdotL * 0.5;
                }
                #endif
                waterColor.rgb = MixFog(waterColor.rgb, IN.fogFactor);

                return waterColor;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   DepthVert
            #pragma fragment DepthFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _WindSpeed; float4 _WindDirection;
                float  _WaveHeight; float _WaveFrequency; float _WaveSpeed; float _WaveSteepness;
                float4 _ShallowColor; float4 _DeepColor; float4 _FoamColor;
                float4 _NormalMap_ST; float4 _NormalMap2_ST;
                float  _NormalStrength; float _FoamScale; float _FoamCutoff; float _FoamWindStreak;
                float  _DepthMaxDistance; float _Smoothness; float _SpecularStrength;
                float  _FresnelPower; float _FresnelStrength;
                float  _ReflectionStrength; float _ReflectionDistortion; float _PlanarBlend;
            CBUFFER_END

            struct DepthAttr { float4 positionOS : POSITION; };
            struct DepthVary { float4 positionCS : SV_POSITION; };

            float2 Rot2(float2 v, float r)
            {
                float s, c; sincos(r, s, c);
                return float2(v.x*c - v.y*s, v.x*s + v.y*c);
            }
            float3 GDisp(float2 d, float a, float st, float f, float sp, float3 p, float tm)
            {
                d = normalize(d);
                float Q = st / max(f * a * 4.0, 0.001);
                float th = dot(d, p.xz) * f - tm * sp;
                float s, c; sincos(th, s, c);
                return float3(Q*a*d.x*c, a*s, Q*a*d.y*c);
            }

            DepthVary DepthVert(DepthAttr IN)
            {
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float  t     = _Time.y;
                float2 wDir  = normalize(_WindDirection.xz);
                float  ws    = max(_WindSpeed, 0.01);

                posWS += GDisp(wDir, _WaveHeight*ws*0.06, _WaveSteepness,
                               _WaveFrequency, _WaveSpeed*ws*0.5, posWS, t);
                posWS += GDisp(Rot2(wDir, 0.35), _WaveHeight*ws*0.04, _WaveSteepness*0.7,
                               _WaveFrequency*1.6, _WaveSpeed*ws*0.45, posWS, t);

                DepthVary OUT;
                OUT.positionCS = TransformWorldToHClip(posWS);
                return OUT;
            }
            half DepthFrag(DepthVary IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
