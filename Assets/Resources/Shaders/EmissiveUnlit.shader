// Love Game - flat emissive color for crystals, beacons, neon, lamps.
// Bloom-friendly unlit output; soft distance fade keeps glow cohesive in fog.
Shader "LoveGame/EmissiveUnlit"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float4 _LGFogColor;
            float _LGFogDensity;
            float _LGFogStart;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // half-strength fog: glow stays readable through haze
                float dist = distance(i.world, _WorldSpaceCameraPos);
                float fog = 1.0 - exp(-_LGFogDensity * max(0.0, dist - _LGFogStart));
                fog = saturate(fog) * 0.5;
                float3 col = lerp(_Color.rgb, _LGFogColor.rgb * 1.2, fog);
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
