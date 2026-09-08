// Love Game - procedural gradient skybox with sun disc, moon and stars.
// Pipeline-agnostic (renders in URP and Built-in). Driven by DayNightCycle:
//   _TopColor/_HorizonColor/_BottomColor  gradient
//   _SunDirection (points FROM camera TO sun), _NightFactor 0..1
Shader "LoveGame/GradientSkybox"
{
    Properties
    {
        _TopColor ("Top", Color) = (0.3, 0.7, 0.95, 1)
        _HorizonColor ("Horizon", Color) = (0.75, 0.9, 0.96, 1)
        _BottomColor ("Bottom", Color) = (0.9, 0.85, 0.7, 1)
        _SunColor ("Sun", Color) = (1, 0.95, 0.85, 1)
        _SunDirection ("Sun Direction", Vector) = (0, 0.6, -0.8, 0)
        _NightFactor ("Night Factor", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Back
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _TopColor, _HorizonColor, _BottomColor, _SunColor;
            float4 _SunDirection;
            float _NightFactor;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = mul(unity_ObjectToWorld, v.vertex).xyz - _WorldSpaceCameraPos;
                return o;
            }

            float starHash(float3 p)
            {
                float3 id = floor(p * 160.0);
                return frac(sin(dot(id, float3(12.9898, 78.233, 45.5433))) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float h = dir.y * 0.5 + 0.5;

                // three-band vertical gradient
                float3 col;
                if (h < 0.5)
                {
                    float t = smoothstep(0.32, 0.5, h);
                    col = lerp(_BottomColor.rgb, _HorizonColor.rgb, t);
                }
                else
                {
                    float t = smoothstep(0.5, 0.78, h);
                    col = lerp(_HorizonColor.rgb, _TopColor.rgb, t);
                }

                // sun disc + halo
                float3 sunDir = normalize(_SunDirection.xyz);
                float sd = dot(dir, sunDir);
                float sun = pow(max(sd, 0.0), 600.0) * 2.2 + pow(max(sd, 0.0), 24.0) * 0.12;
                col += _SunColor.rgb * sun * (1.0 - _NightFactor * 0.85);

                // moon opposite the sun
                float md = dot(dir, -sunDir);
                float moon = pow(max(md, 0.0), 1400.0) * 1.6;
                col += float3(0.9, 0.93, 1.0) * moon * _NightFactor;

                // procedural stars (only at night, above the horizon)
                float star = step(0.9965, starHash(dir)) * _NightFactor;
                star *= smoothstep(0.02, 0.2, dir.y);
                col += float3(star, star, star * (0.85 + 0.15 * starHash(dir * 3.1)));

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
