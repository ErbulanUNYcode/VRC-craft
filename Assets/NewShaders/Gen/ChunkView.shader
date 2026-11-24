Shader "VRC_MINE/Debug/ChunkView"
{
    Properties
    {
        _MainTex ("Chunk Data", 2D) = "black" {}
        _BlockAtlas ("Block Atlas", 2DArray) = "black" {}
        _LeafTint ("Leaf Mask Tint", Color) = (0.35, 1.0, 0.35, 1.0)
        _BiomeLeafColor ("Preview Biome Leaf Color", Color) = (0.45, 1.0, 0.45, 1.0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        ZWrite On

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Texture2D<uint> _MainTex;
            UNITY_DECLARE_TEX2DARRAY(_BlockAtlas);
            fixed4 _LeafTint;
            fixed4 _BiomeLeafColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // The chunk data is a 256x256 indexed image. Each texel stores
                // the block ID; the preview expands that texel to one 32x32
                // block texture from the Back slice of the atlas.
                float2 dataCoord = floor(i.uv * 256.0);
                uint blockID = _MainTex.Load(int3(dataCoord, 0));
                uint atlasX = blockID & 15u;
                uint atlasY = blockID >> 4u;
                float2 blockUV = frac(i.uv * 256.0);
                float2 atlasUV = (float2(atlasX, atlasY) + blockUV) / 16.0;
                // Use the unwrapped UV derivatives so the mip selector does
                // not interpret the local 0.99 -> 0.01 wrap as a huge screen
                // footprint.
                float2 faceDx = ddx(i.uv * 256.0);
                float2 faceDy = ddy(i.uv * 256.0);
                float atlasTexelFootprint = max(length(faceDx), length(faceDy)) * 32.0;
                float mipLevel = clamp(log2(max(atlasTexelFootprint, 1.0)), 0.0, 5.0);
                fixed4 blockColor = _BlockAtlas.SampleLevel(
                    sampler_BlockAtlas,
                    float3(atlasUV, 4.0),
                    mipLevel);
                // Transparent pixels represent empty space in the chunk preview.
                // Alpha 0.5 is reserved for active foliage-mask pixels.
                if (blockColor.a < 0.5)
                    blockColor = fixed4(0.22, 0.22, 0.58, 1.0);
                // Atlas alpha 0.5 marks pixels supplied by the foliage mask.
                // Apply the packed foliage format: G is biome strength,
                // while R and B are authored additive color channels.
                if (abs(blockColor.a - 0.5) < 0.01)
                {
                    float3 foliage = _BiomeLeafColor.rgb * blockColor.g;
                    foliage.r += blockColor.r;
                    foliage.b += blockColor.b;
                    blockColor.rgb = saturate(foliage);
                }
                float layerUV = frac(i.uv.x * 16.0);
                float borderDistance = min(layerUV, 1.0 - layerUV);
                float separatorWidth = 1.0 / 128.0;
                fixed separatorStrength = saturate(1.0 - borderDistance / separatorWidth) * 0.35;
                separatorStrength *= i.uv.x > 0.0 && i.uv.x < 1.0;
                blockColor.rgb = lerp(blockColor.rgb, 1.0.xxx, separatorStrength);
                blockColor.a = 1.0;
                return blockColor;
            }
            ENDHLSL
        }
    }
}
