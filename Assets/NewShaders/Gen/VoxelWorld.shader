Shader "VRC_MINE/Gen/VoxelWorld"
{
    Properties
    {
        [IntRange] _OffsetX ("Offset X", Range(0, 31)) = 1
        [IntRange] _OffsetY ("Offset Y", Range(0, 31)) = 1
        [Toggle] _GenChunk ("Generate Chunk", Float) = 1
        _WorldGeneratorTexture ("World Generator Texture", 2D) = "black" {}
        _TerrainTexture ("Terrain Texture", 2D) = "black" {}
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
            #include "UnityCustomRenderTexture.cginc"

            struct VoxelWorldV2F
            {
                float4 vertex : SV_POSITION;
                float3 localTexcoord : TEXCOORD0;
                float3 globalTexcoord : TEXCOORD1;
                nointerpolation uint primitiveID : TEXCOORD2;
            };

            int _OffsetX;
            int _OffsetY;
            int _GenChunk;
            int _ZonesCount;
            float4 _ClearChunks[4];
            int _ClearChunksCount;
            float _BlockX[64];
            float _BlockZ[64];
            float _BlockId[64];
            Texture2D<uint> _WorldGeneratorTexture;
            Texture2D<uint4> _TerrainTexture;

            int WrapChunk(int value)
            {
                return value & 31;
            }

            VoxelWorldV2F vert(appdata_customrendertexture v)
            {
                v2f_customrendertexture base = CustomRenderTextureVertexShader(v);
                VoxelWorldV2F o;
                o.vertex = base.vertex;
                o.localTexcoord = base.localTexcoord;
                o.globalTexcoord = base.globalTexcoord;
                o.primitiveID = base.primitiveID;

                if (o.primitiveID >= (uint)_ZonesCount)
                {
                    o.vertex.xy = float2(0.0, 0.0);
                    return o;
                }

                // The horizontal chunk plane is represented by texture X/Y.
                uint clearCount = (uint)_ClearChunksCount;
                if (o.primitiveID < clearCount)
                {
                    float4 region = _ClearChunks[o.primitiveID];
                    float2 chunkClipSize = 32.0 / float2(
                        _CustomRenderTextureWidth, _CustomRenderTextureHeight);
                    float2 origin = float2(-1.0, -1.0);
                    #if !UNITY_UV_STARTS_AT_TOP
                        origin.y = 1.0;
                        chunkClipSize.y = -chunkClipSize.y;
                    #endif
                    o.vertex.xy = origin + (o.vertex.xy - origin) * region.zw
                        + region.xy * chunkClipSize;
                }
                else if (o.primitiveID == clearCount && _GenChunk != 0)
                {
                    float2 textureSize = float2(
                        _CustomRenderTextureWidth,
                        _CustomRenderTextureHeight
                    );
                    float2 pixelOffset = float2(WrapChunk(_OffsetX), WrapChunk(_OffsetY)) * 16.0;
                    float2 clipOffset = pixelOffset * (2.0 / textureSize);
                    #if !UNITY_UV_STARTS_AT_TOP
                        clipOffset.y = -clipOffset.y;
                    #endif
                    o.vertex.xy += clipOffset;

                    // Rebuild UV from the final vertex position, as in the working
                    // world-generator shader. This keeps UV synchronized with the
                    // actually moved update zone.
                    o.globalTexcoord.xy = o.vertex.xy * 0.5 + 0.5;
                    #if UNITY_UV_STARTS_AT_TOP
                        o.globalTexcoord.y = 1.0 - o.globalTexcoord.y;
                    #endif
                }
				else if (o.primitiveID < (uint)_ZonesCount)
				{
					uint blockIndex = o.primitiveID;
					if (blockIndex < 64u)
					{
						int blockX = (int)round(_BlockX[blockIndex]);
						int blockZ = (int)round(_BlockZ[blockIndex]);
						float2 pixelOffset = float2(blockX & 511, blockZ & 511);
						float2 clipOffset = pixelOffset * (2.0 / float2(
							_CustomRenderTextureWidth,
							_CustomRenderTextureHeight
						));
						#if !UNITY_UV_STARTS_AT_TOP
							clipOffset.y = -clipOffset.y;
						#endif
						o.vertex.xy += clipOffset;
					}
				}

                return o;
            }

            uint frag(VoxelWorldV2F i) : SV_Target
            {
				if (i.primitiveID >= (uint)_ZonesCount)
					return 0u;

				uint clearCount = (uint)_ClearChunksCount;
				if (i.primitiveID < clearCount)
					return 0u;

				if (_GenChunk == 0 || i.primitiveID != clearCount)
				{
					uint blockIndex = i.primitiveID;
					return (uint)round(_BlockId[blockIndex]);
				}

				// The update zone represents one chunk: 16 x 16 x 130.
				int3 local = (int3)floor(saturate(i.localTexcoord) * float3(16.0, 16.0, 130.0));
				local = min(local, int3(15, 15, 129));

				// WorldGenerator stores a chunk as 16 horizontal columns packed
				// into X and 128 vertical values in Y.
				// The zone Y axis maps to world Z in the opposite direction.
				int sourceColumnZ = 15 - local.y;

				// The two upper layers store terrain metadata:
				// layer 128 = surface height (R), layer 129 = biome index (G).
				if (local.z >= 128)
				{
					uint4 terrainData = _TerrainTexture.Load(int3(local.x + 8, sourceColumnZ + 8, 0));
					uint terrainHeight = (terrainData.r & 127u);
					uint biomeId = terrainData.g & 31u;
					uint metadata = local.z == 128 ? terrainHeight : biomeId;
					return metadata;
				}

				int sourceX = local.x + (sourceColumnZ << 4);
				int sourceY = local.z;
				uint block = _WorldGeneratorTexture.Load(int3(sourceX, sourceY, 0));
				return block;
            }

            ENDHLSL
        }
    }
}
