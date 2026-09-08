// Love Game - vertex-colored biome terrain with matched exponential fog.
// Vertex colors are painted by RegionContentBuilder (sand/grass/rock/road blend).
Shader "LoveGame/TerrainVertexColor"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
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
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            float4 _Color;
            float4 _LGFogColor;
            float _LGFogDensity;
            float _LGFogStart;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR0;
                float3 world : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = i.color * _Color;
                // manual exponential fog, kept in sync with RenderSettings.fog by the world system
                float dist = distance(i.world, _WorldSpaceCameraPos);
                float fog = 1.0 - exp(-_LGFogDensity * max(0.0, dist - _LGFogStart));
                col.rgb = lerp(col.rgb, _LGFogColor.rgb, saturate(fog));
                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}
