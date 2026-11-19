Shader "VRC_MINE/Editor/LodView3D"
{
    Properties
    {
        _MainTex ("Texture", 3D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            Texture3D<uint> _MainTex;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o;
            }

            float4 frag(v2f i):SV_Target
            {
                float2 gridUV = saturate(i.uv) * 4.0;
                int2 tile = min((int2)floor(gridUV), 3);
                float2 sectionUV = frac(gridUV) * 64.0;
                int2 pixel = min((int2)floor(sectionUV), 63);
                int slice = tile.x + tile.y * 4;
                uint data = _MainTex.Load(int4(pixel, slice, 0));
                clip((float)(data & 1u) - 0.5);

                uint lowX = data >> 1 & 31u;
                uint highX = data >> 6 & 31u;
                uint lowZ = data >> 21 & 31u;
                uint highZ = data >> 26 & 31u;

                // Preview world X/Z bounds.
                // Narrow the preview pixel to the same X/Z bounds used by
                // the world geometry.
                float2 lower = float2(lowX / 16.0, 1.0 - highZ / 16.0);
                float2 upper = float2(highX / 16.0, 1.0 - lowZ / 16.0);
                float2 center = (lower + upper) * 0.5;
                float2 minimumHalfWidth = max(0.25 / 16.0,
                                              fwidth(gridUV) * 32.0);
                float2 halfWidth = max((upper - lower) * 0.5,
                                       minimumHalfWidth);
                lower = center - halfWidth;
                upper = center + halfWidth;
                float2 withinPixel = frac(sectionUV);
                clip(float4(withinPixel - lower, upper - withinPixel));
                return float4(float3(lowX, data >> 11 & 31u, lowZ) / 16.0, 1.0);
            }
            ENDHLSL
        }
    }
}
