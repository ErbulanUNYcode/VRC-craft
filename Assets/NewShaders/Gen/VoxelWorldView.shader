Shader "VRC_MINE/Debug/VoxelWorldLayer"
{
    Properties
    {
        _VoxelWorld ("Voxel World", 3D) = "black" {}
        _BlockAtlas ("Block Atlas", 2DArray) = "black" {}
        [IntRange] _Layer ("Layer Y", Range(0, 127)) = 0
        _Scale ("World Scale", Float) = 1
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
            float _Scale;
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
                float2 uv = frac(i.uv * _Scale);
                float2 voxelUV = uv * 512.0;
                uint x = min((uint)floor(voxelUV.x), 511u);
                uint z = min((uint)floor(voxelUV.y), 511u);
                z = 511u - z;
                uint block = _VoxelWorld.Load(int4(x, z, _Layer, 0));

                float2 blockUV = frac(voxelUV);
                uint atlasX = block & 15u;
                uint atlasY = block >> 4u;
                float2 atlasUV = (float2(atlasX, atlasY) + blockUV) / 16.0;

                // Match ChunkView: derivatives are measured in block space and
                // converted to atlas texels before selecting the mip level.
                float2 faceDx = ddx(voxelUV);
                float2 faceDy = ddy(voxelUV);
                float atlasTexelFootprint = max(length(faceDx), length(faceDy)) * 32.0;
                float mipLevel = clamp(log2(max(atlasTexelFootprint, 1.0)), 0.0, 5.0);

                // Preview the top face (WorldShow uses atlas slice 3 for +Y).
                fixed4 blockColor = _BlockAtlas.SampleLevel(
                    sampler_BlockAtlas,
                    float3(atlasUV, 3.0),
                    mipLevel
                );

                if (block == 0 || blockColor.a < 0.5)
                    return float4(0, 0, 0, 1);

                if (abs(blockColor.a - 0.5) < 0.01)
                {
                    float3 foliage = _BiomeLeafColor.rgb * blockColor.g;
                    foliage.r += blockColor.r;
                    foliage.b += blockColor.b;
                    blockColor.rgb = saturate(foliage);
                }

                blockColor.a = 1.0;
                return blockColor;
            }
            ENDHLSL
        }
    }
}
