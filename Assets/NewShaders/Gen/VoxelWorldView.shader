Shader "VRC_MINE/Debug/VoxelWorldLayer"
{
    Properties
    {
        _VoxelWorld ("Voxel World", 3D) = "black" {}
        _BlockAtlas ("Block Atlas", 2DArray) = "black" {}
        [IntRange] _Layer ("Top Layer Y", Range(0, 127)) = 0
        [IntRange] _Depth ("Layers to Scan", Range(1, 20)) = 1
        _BiomeLeafColor ("Preview Biome Leaf Color", Color) = (0.45, 1.0, 0.45, 1.0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }

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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            Texture3D<uint> _VoxelWorld;
            UNITY_DECLARE_TEX2DARRAY(_BlockAtlas);
            int _Layer;
            int _Depth;
            fixed4 _BiomeLeafColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // The preview quad represents the complete 512x512 layer.
                // Do not rescale or tile the world inside the preview.
                float2 uv = saturate(i.uv);
                float2 voxelUV = uv * 512.0;
                uint x = min((uint)floor(voxelUV.x), 511u);
                uint z = min((uint)floor(voxelUV.y), 511u);
                z = 511u - z;
                float2 blockUV = frac(voxelUV);

                // Match ChunkView: derivatives are measured in block space and
                // converted to atlas texels before selecting the mip level.
                float2 faceDx = ddx(voxelUV);
                float2 faceDy = ddy(voxelUV);
                float atlasTexelFootprint = max(length(faceDx), length(faceDy)) * 32.0;
                float mipLevel = clamp(log2(max(atlasTexelFootprint, 1.0)), 0.0, 5.0);

                int top = clamp(_Layer, 0, 127);
                int bottom = max(0, top - clamp(_Depth, 1, 20) + 1);

                [loop]
                for (int layer = top; layer >= bottom; --layer)
                {
                    uint block = _VoxelWorld.Load(int4(x, z, layer, 0));
                    if (block == 0u) continue;

                    float2 atlasUV = (float2(block & 15u, block >> 4u) + blockUV) / 16.0;
                    fixed4 blockColor = _BlockAtlas.SampleLevel(
                        sampler_BlockAtlas, float3(atlasUV, 3.0), mipLevel);
                    if (blockColor.a < 0.5) continue;

                    if (abs(blockColor.a - 0.5) < 0.01)
                    {
                        float3 foliage = _BiomeLeafColor.rgb * blockColor.g;
                        foliage.r += blockColor.r;
                        foliage.b += blockColor.b;
                        blockColor.rgb = saturate(foliage);
                    }

                    float brightness = 1.0;
                    if (layer < top)
                    {
                        // Preview up is -Z, and preview right is +X.
                        if (z > 0u && _VoxelWorld.Load(int4(x, max(int(z) - 1, 0), layer + 1, 0)) != 0u)
                            brightness -= 0.2;
                        if (x < 511u && _VoxelWorld.Load(int4(min(x + 1u, 511u), z, layer + 1, 0)) != 0u)
                            brightness -= 0.1;
                    }

                    return float4(blockColor.rgb * brightness, 1.0);
                }

                return float4(0, 0, 0, 1);
            }
            ENDHLSL
        }
    }
}
