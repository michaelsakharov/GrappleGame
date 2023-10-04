// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/EditorBackground"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 pos : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.pos = mul(unity_ObjectToWorld, float4(v.vertex.xy, 0.0, 1.0));
                return o;
            }

            float GridPoints(float2 pos, float scale, float sMin, float sMax)
            {
                float x = pos.x * scale;
                float y = pos.y * scale;
                // Distance to whole number
                float dx = abs(abs(frac(x) - 0.5) - 0.5);
                float dy = abs(abs(frac(y) - 0.5) - 0.5);
                // 0.0 at whole number, 1.0 in between
                return smoothstep(sMin, sMax, dx * dx + dy * dy);
            }

            float Grid(float2 pos, float scale)
            {
                float x = pos.x * scale;
                float y = pos.y * scale;

                // Calculate grid intensity for X and Y edges
                float dx = abs(abs(frac(x) - 0.5) - 0.5);
                float dy = abs(abs(frac(y) - 0.5) - 0.5);

                // Threshold value for grid lines
                float threshold = 0.01;

                float smoothness = 0.03;
                float gridIntensityX = smoothstep(threshold - smoothness, threshold + smoothness, dx);
                float gridIntensityY = smoothstep(threshold - smoothness, threshold + smoothness, dy);

                return gridIntensityX * gridIntensityY;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float small = GridPoints(i.pos, 4.0f, -0.04, 0.04);
                float big = GridPoints(i.pos, 1.0f, -0.001, 0.0075);

                // 0.0 at whole number, 1.0 in between
                float gridPoints = max(0.6, min(small, big));

                float grid = max(0.8, Grid(i.pos, 1.0f));

				//return (1.0 - min(gridPoints, grid)) + 0.1;
				return min(gridPoints, grid) * 0.3;
            }
            ENDCG
        }
    }
}
