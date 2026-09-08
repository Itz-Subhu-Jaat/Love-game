// Love Game - stylized animated water: scrolling waves, fresnel sheen, matched fog.
// Mobile-friendly: no depth texture, no reflections - fresnel + horizon tint + fog.
Shader "LoveGame/StylizedWater"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.18, 0.77, 0.71, 0.82)
        _HorizonColor ("Horizon Tint", Color) = (0.55, 0.85, 0.95, 1)
        _WaveHeight ("Wave Height", Range(0, 0.5)) = 0.12
        _WaveSpeed ("Wave Speed", Range(0, 4)) = 1.2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color, _HorizonColor;
            float _WaveHeight, _WaveSpeed;
            float4 _LGFogColor;
            float _LGFogDensity;
            float _LGFogStart;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
                float wave : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                // two crossing wave trains
                float w = sin(world.x * 0.08 + _Time.y * _WaveSpeed)
                        + cos(world.z * 0.06 + _Time.y * _WaveSpeed * 0.7);
                world.y += w * _WaveHeight * 0.5;
                o.world = world;
                o.wave = w;
                o.pos = UnityWorldToClipPos(world);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.world);
                float fresnel = pow(1.0 - saturate(viewDir.y), 3.0);

                // base color brightened by wave crests
                float crest = saturate(i.wave * 0.25 + 0.5);
                float3 col = lerp(_Color.rgb * 0.85, _Color.rgb * 1.15, crest);
                col = lerp(col, _HorizonColor.rgb, fresnel * 0.6);

                float alpha = lerp(_Color.a, 0.95, fresnel * 0.5);

                // matched manual fog
                float dist = distance(i.world, _WorldSpaceCameraPos);
                float fog = 1.0 - exp(-_LGFogDensity * max(0.0, dist - _LGFogStart));
                col = lerp(col, _LGFogColor.rgb, saturate(fog));

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
