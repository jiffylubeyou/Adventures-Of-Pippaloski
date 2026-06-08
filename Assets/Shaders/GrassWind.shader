// URP port of Nicrom LPW_Vegetation (Built-in) shader.
// Preserves the original wind math exactly; only the pipeline boilerplate changes.
Shader "Pippaloski/GrassWind"
{
    Properties
    {
        _Color              ("Color",                   Color)          = (1,1,1,1)
        [NoScaleOffset]
        _MainTex            ("Main Tex",                2D)             = "white" {}
        _Metallic           ("Metallic",                Range(0,1))     = 0
        _Smoothness         ("Smoothness",              Range(0,1))     = 0

        [Header(Main Bending)]
        _MBDefaultBending   ("MB Default Bending",      Float)          = 0
        _MBAmplitude        ("MB Amplitude",            Float)          = 1.5
        _MBAmplitudeOffset  ("MB Amplitude Offset",     Float)          = 2
        _MBFrequency        ("MB Frequency",            Float)          = 1.11
        _MBFrequencyOffset  ("MB Frequency Offset",     Float)          = 0
        _MBPhase            ("MB Phase",                Float)          = 1
        _MBWindDir          ("MB Wind Dir",             Range(0,360))   = 0
        _MBWindDirOffset    ("MB Wind Dir Offset",      Range(0,180))   = 20
        _MBMaxHeight        ("MB Max Height",           Float)          = 10

        [Header(World Space Noise)]
        [NoScaleOffset]
        _NoiseTexture       ("Noise Texture",           2D)             = "bump" {}
        _NoiseTextureTilling("Noise Tilling (Static XY, Anim ZW)", Vector) = (1,1,1,1)
        _NoisePannerSpeed   ("Noise Panner Speed",      Vector)         = (0.05,0.03,0,0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull Off   // show both sides like grass

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4   _Color;
                float4  _NoiseTextureTilling;
                float2  _NoisePannerSpeed;
                float   _MBWindDir;
                float   _MBWindDirOffset;
                float   _MBAmplitude;
                float   _MBAmplitudeOffset;
                float   _MBFrequency;
                float   _MBFrequencyOffset;
                float   _MBPhase;
                float   _MBDefaultBending;
                float   _MBMaxHeight;
                float   _Metallic;
                float   _Smoothness;
            CBUFFER_END

            TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTexture); SAMPLER(sampler_NoiseTexture);

            // Identical to the original RotateAroundAxis helper
            float3 RotateAroundAxis(float3 center, float3 original, float3 u, float angle)
            {
                original -= center;
                float C = cos(angle), S = sin(angle), t = 1.0 - C;
                float3x3 m = float3x3(
                    t*u.x*u.x + C,      t*u.x*u.y - S*u.z, t*u.x*u.z + S*u.y,
                    t*u.x*u.y + S*u.z,  t*u.y*u.y + C,     t*u.y*u.z - S*u.x,
                    t*u.x*u.z - S*u.y,  t*u.y*u.z + S*u.x, t*u.z*u.z + C
                );
                return mul(m, original) + center;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 posOS = IN.positionOS.xyz;

                // --- Wind vertex displacement (ported 1:1 from original) ---

                // Pivot in world space
                float3 pivotWS = TransformObjectToWorld(float3(0,0,0));

                // World-space XZ used as noise UVs
                float2 wsXZ = float2(pivotWS.x, pivotWS.z);

                // Animated noise sample
                float2 animTiling = _NoiseTextureTilling.zw;
                float2 panner     = 0.1 * _Time.y * _NoisePannerSpeed;
                float4 animNoise  = SAMPLE_TEXTURE2D_LOD(_NoiseTexture, sampler_NoiseTexture,
                                        wsXZ * animTiling + panner, 0);

                // Wind direction with per-object noise offset
                float windAngle = radians(
                    (_MBWindDir + _MBWindDirOffset * (-1.0 + animNoise.r * 2.0)) * -1.0
                );
                float3 windDirWS  = float3(cos(windAngle), 0.0, sin(windAngle));

                // Convert wind axis into object space
                float3 windOS     = normalize(
                    TransformWorldToObject(windDirWS) - TransformWorldToObject(float3(0,0,0))
                );

                // Static noise for per-object amplitude / frequency variation
                float2 staticTiling = _NoiseTextureTilling.xy;
                float4 staticNoise  = SAMPLE_TEXTURE2D_LOD(_NoiseTexture, sampler_NoiseTexture,
                                          wsXZ * staticTiling, 0);

                float amplitude  = _MBAmplitude + _MBAmplitudeOffset * staticNoise.r;
                float frequency  = _MBFrequency + _MBFrequencyOffset * staticNoise.r;
                float sineInput  = ((pivotWS.x + pivotWS.z) + _Time.y * frequency) * _MBPhase;
                float rotAngle   = radians(
                    (amplitude * sin(sineInput) + _MBDefaultBending) * (posOS.y / _MBMaxHeight)
                );

                // Rotate (only vertices above the base — same step(0.01,y) guard)
                float3 pivot3 = float3(0.0, posOS.y, 0.0);
                float3 rot1   = RotateAroundAxis(pivot3, posOS,  windOS, rotAngle);
                float3 rot2   = RotateAroundAxis(float3(0,0,0), rot1, windOS, rotAngle);
                posOS        += (rot2 - posOS) * step(0.01, posOS.y);

                // --- Standard URP outputs ---
                float3 posWS   = TransformObjectToWorld(posOS);
                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.positionWS  = posWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = IN.uv;
                OUT.fogFactor   = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half3 albedo   = texColor.rgb * _Color.rgb;

                float3 normalWS = normalize(IN.normalWS);

                InputData lightingInput     = (InputData)0;
                lightingInput.positionWS    = IN.positionWS;
                lightingInput.normalWS      = normalWS;
                lightingInput.viewDirectionWS = normalize(GetWorldSpaceViewDir(IN.positionWS));
                lightingInput.shadowCoord   = TransformWorldToShadowCoord(IN.positionWS);
                lightingInput.fogCoord      = IN.fogFactor;
                lightingInput.bakedGI       = SampleSH(normalWS);

                SurfaceData surface   = (SurfaceData)0;
                surface.albedo        = albedo;
                surface.metallic      = _Metallic;
                surface.smoothness    = _Smoothness;
                surface.alpha         = 1;
                surface.occlusion     = 1;

                half4 color = UniversalFragmentPBR(lightingInput, surface);
                color.rgb   = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   vertShadow
            #pragma fragment fragShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4   _Color;
                float4  _NoiseTextureTilling;
                float2  _NoisePannerSpeed;
                float   _MBWindDir;
                float   _MBWindDirOffset;
                float   _MBAmplitude;
                float   _MBAmplitudeOffset;
                float   _MBFrequency;
                float   _MBFrequencyOffset;
                float   _MBPhase;
                float   _MBDefaultBending;
                float   _MBMaxHeight;
                float   _Metallic;
                float   _Smoothness;
            CBUFFER_END

            TEXTURE2D(_NoiseTexture); SAMPLER(sampler_NoiseTexture);

            float3 RotateAroundAxis(float3 center, float3 original, float3 u, float angle)
            {
                original -= center;
                float C = cos(angle), S = sin(angle), t = 1.0 - C;
                float3x3 m = float3x3(
                    t*u.x*u.x + C,      t*u.x*u.y - S*u.z, t*u.x*u.z + S*u.y,
                    t*u.x*u.y + S*u.z,  t*u.y*u.y + C,     t*u.y*u.z - S*u.x,
                    t*u.x*u.z - S*u.y,  t*u.y*u.z + S*u.x, t*u.z*u.z + C
                );
                return mul(m, original) + center;
            }

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings   { float4 positionHCS : SV_POSITION; };

            Varyings vertShadow(Attributes IN)
            {
                Varyings OUT;
                float3 posOS   = IN.positionOS.xyz;
                float3 pivotWS = TransformObjectToWorld(float3(0,0,0));
                float2 wsXZ    = float2(pivotWS.x, pivotWS.z);

                float2 animTiling = _NoiseTextureTilling.zw;
                float2 panner     = 0.1 * _Time.y * _NoisePannerSpeed;
                float4 animNoise  = SAMPLE_TEXTURE2D_LOD(_NoiseTexture, sampler_NoiseTexture,
                                        wsXZ * animTiling + panner, 0);

                float windAngle  = radians((_MBWindDir + _MBWindDirOffset * (-1.0 + animNoise.r * 2.0)) * -1.0);
                float3 windDirWS = float3(cos(windAngle), 0.0, sin(windAngle));
                float3 windOS    = normalize(TransformWorldToObject(windDirWS) - TransformWorldToObject(float3(0,0,0)));

                float2 staticTiling = _NoiseTextureTilling.xy;
                float4 staticNoise  = SAMPLE_TEXTURE2D_LOD(_NoiseTexture, sampler_NoiseTexture, wsXZ * staticTiling, 0);

                float amplitude = _MBAmplitude + _MBAmplitudeOffset * staticNoise.r;
                float frequency = _MBFrequency + _MBFrequencyOffset * staticNoise.r;
                float sineInput = ((pivotWS.x + pivotWS.z) + _Time.y * frequency) * _MBPhase;
                float rotAngle  = radians((amplitude * sin(sineInput) + _MBDefaultBending) * (posOS.y / _MBMaxHeight));

                float3 pivot3 = float3(0.0, posOS.y, 0.0);
                float3 rot1   = RotateAroundAxis(pivot3, posOS,  windOS, rotAngle);
                float3 rot2   = RotateAroundAxis(float3(0,0,0), rot1, windOS, rotAngle);
                posOS        += (rot2 - posOS) * step(0.01, posOS.y);

                float3 posWS   = TransformObjectToWorld(posOS);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionHCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, _MainLightPosition.xyz));
                return OUT;
            }

            half4 fragShadow(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
