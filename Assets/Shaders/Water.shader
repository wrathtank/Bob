Shader "BobsPetroleum/Water"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.2, 0.6, 0.8, 0.8)
        _DeepColor ("Deep Color", Color) = (0.1, 0.2, 0.4, 0.95)
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)

        _MainTex ("Water Texture", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _FoamTex ("Foam Texture", 2D) = "white" {}

        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveHeight ("Wave Height", Float) = 0.5
        _WaveFrequency ("Wave Frequency", Float) = 1.0

        _Transparency ("Transparency", Range(0, 1)) = 0.7
        _Reflectivity ("Reflectivity", Range(0, 1)) = 0.5
        _FresnelPower ("Fresnel Power", Float) = 2.0

        _FoamThreshold ("Foam Threshold", Range(0, 2)) = 0.5
        _DistortionStrength ("Distortion", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                UNITY_FOG_COORDS(4)
            };

            sampler2D _MainTex;
            sampler2D _BumpMap;
            sampler2D _FoamTex;

            float4 _MainTex_ST;
            float4 _ShallowColor;
            float4 _DeepColor;
            float4 _FoamColor;

            float _WaveSpeed;
            float _WaveHeight;
            float _WaveFrequency;
            float _Transparency;
            float _Reflectivity;
            float _FresnelPower;
            float _FoamThreshold;
            float _DistortionStrength;

            // Wave function
            float GetWave(float3 pos, float time)
            {
                float wave1 = sin(pos.x * _WaveFrequency + time * _WaveSpeed) * _WaveHeight;
                float wave2 = sin(pos.z * _WaveFrequency * 0.8 + time * _WaveSpeed * 1.3) * _WaveHeight * 0.5;
                float wave3 = sin((pos.x + pos.z) * _WaveFrequency * 0.5 + time * _WaveSpeed * 0.7) * _WaveHeight * 0.25;
                return wave1 + wave2 + wave3;
            }

            v2f vert(appdata v)
            {
                v2f o;

                // Apply vertex waves
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float wave = GetWave(worldPos, _Time.y);
                v.vertex.y += wave;

                // Calculate wave normal
                float epsilon = 0.1;
                float waveL = GetWave(worldPos + float3(-epsilon, 0, 0), _Time.y);
                float waveR = GetWave(worldPos + float3(epsilon, 0, 0), _Time.y);
                float waveD = GetWave(worldPos + float3(0, 0, -epsilon), _Time.y);
                float waveU = GetWave(worldPos + float3(0, 0, epsilon), _Time.y);

                float3 waveNormal = normalize(float3(waveL - waveR, 2.0 * epsilon, waveD - waveU));

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = waveNormal;
                o.viewDir = normalize(_WorldSpaceCameraPos - o.worldPos);

                UNITY_TRANSFER_FOG(o, o.vertex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Animated UV for texture scrolling
                float2 uv1 = i.uv + float2(_Time.y * 0.05, _Time.y * 0.03);
                float2 uv2 = i.uv + float2(-_Time.y * 0.04, _Time.y * 0.02);

                // Sample textures
                fixed4 tex1 = tex2D(_MainTex, uv1);
                fixed4 tex2 = tex2D(_MainTex, uv2);
                fixed4 texColor = (tex1 + tex2) * 0.5;

                // Fresnel effect
                float fresnel = pow(1.0 - saturate(dot(i.worldNormal, i.viewDir)), _FresnelPower);

                // Blend shallow and deep colors based on view angle
                fixed4 waterColor = lerp(_ShallowColor, _DeepColor, fresnel * 0.5);
                waterColor *= texColor;

                // Add reflectivity
                waterColor.rgb += _Reflectivity * fresnel * 0.3;

                // Foam based on wave height
                float waveHeight = abs(GetWave(i.worldPos, _Time.y));
                float foam = smoothstep(_FoamThreshold - 0.1, _FoamThreshold + 0.1, waveHeight);

                fixed4 foamTex = tex2D(_FoamTex, i.uv * 2.0 + float2(_Time.y * 0.1, 0));
                waterColor.rgb = lerp(waterColor.rgb, _FoamColor.rgb * foamTex.rgb, foam * 0.5);

                // Set transparency
                waterColor.a = lerp(_Transparency, 1.0, fresnel * 0.3);

                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, waterColor);

                return waterColor;
            }
            ENDCG
        }
    }

    // Fallback for older hardware
    Fallback "Transparent/Diffuse"
}
