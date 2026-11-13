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

            Texture3D<uint4> _MainTex;

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
                uint4 data = _MainTex.Load(int4(pixel, slice, 0));
                clip((float)(data.a & 1u) - 0.5);

                uint lowX = (data.r & 15u) | (((data.a >> 1) & 1u) << 4);
                uint highX = (data.r >> 4) | (((data.a >> 2) & 1u) << 4);
                uint lowZ = (data.g & 15u) | (((data.a >> 4) & 1u) << 4);
                uint highZ = (data.g >> 4) | (((data.a >> 3) & 1u) << 4);

                // R is world X. G is world Z.
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
                return float4((float3)data.rgb / 255.0, 1.0);
            }
            ENDHLSL
        }
    }
}
