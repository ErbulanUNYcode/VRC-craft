Shader "VRC_MINE/Gen/VoxelWorld"
{
    Properties
    {
        [IntRange] _OffsetX ("Offset X", Range(0, 63)) = 1
        [IntRange] _OffsetY ("Offset Y", Range(0, 63)) = 1
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
            Texture2D<uint> _WorldGeneratorTexture;
            Texture2D<uint> _TerrainTexture;

            VoxelWorldV2F vert(appdata_customrendertexture v)
            {
                v2f_customrendertexture base = CustomRenderTextureVertexShader(v);
                VoxelWorldV2F o;
                o.vertex = base.vertex;
                o.localTexcoord = base.localTexcoord;
                o.globalTexcoord = base.globalTexcoord;
                o.primitiveID = base.primitiveID;

                // The horizontal chunk plane is represented by texture X/Y.
                if (o.primitiveID == 0)
                {
                    float2 textureSize = float2(
                        _CustomRenderTextureWidth,
                        _CustomRenderTextureHeight
                    );
                    float2 pixelOffset = float2(_OffsetX, _OffsetY) * 16.0;
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

                return o;
            }

            uint frag(VoxelWorldV2F i) : SV_Target
            {
                if (i.primitiveID != 0)
					return 0u;

				// The update zone represents one chunk: 16 x 16 x 258.
				int3 local = (int3)floor(saturate(i.localTexcoord) * float3(16.0, 16.0, 258.0));
				local = min(local, int3(15, 15, 257));

				// WorldGenerator stores a chunk as 16 horizontal columns packed
				// into X and 256 vertical values in Y.
				// The zone Y axis maps to world Z in the opposite direction.
				int sourceColumnZ = 15 - local.y;

				// The two upper layers store terrain metadata:
				// layer 256 = surface height (R), layer 257 = biome index (G).
				if (local.z >= 256)
				{
					uint4 terrainData = _TerrainTexture.Load(int3(local.x, sourceColumnZ, 0));
					uint terrainHeight = (terrainData.r & 127u) + 40u;
					uint biomeId = terrainData.g & 31u;
					uint metadata = local.z == 256 ? terrainHeight : biomeId;
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
