Shader "VRC_MINE/Gen/LodGen"
{
    Properties
    {
        _VoxelWorld ("Voxel World", 3D) = "black" {}
        [IntRange] _OffsetX ("Chunk X", Range(0, 63)) = 0
        [IntRange] _OffsetY ("Chunk Z", Range(0, 63)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCustomRenderTexture.cginc"

            int _OffsetX;
            int _OffsetY;
            Texture3D<uint> _VoxelWorld;
            static const int2 ZoneOffsets[5] = {
                int2(0, 0), int2(0, -1), int2(1, 0), int2(0, 1), int2(-1, 0)
            };

            uint ReadWorldVoxel(int3 worldPosition)
            {
                if (worldPosition.y < 0 || worldPosition.y >= 256)
                    return 0u;
                uint textureX = (uint)worldPosition.x & 1023u;
                uint textureY = 1023u - ((uint)worldPosition.z & 1023u);
                return _VoxelWorld.Load(int4((int)textureX,
                                             (int)textureY,
                                             (int)worldPosition.y, 0));
            }

            bool ShouldDrawFaceBidirectional(uint firstID, uint secondID)
            {
                if (firstID == 0u && secondID == 0u) return false; // air
                if (firstID > 8u && secondID > 8u) return false; // block
                if ((firstID >= 2u && firstID <= 8u) || (secondID >= 2u && secondID <= 8u)) return true; // leaf
                if (firstID == 1u && secondID == 1u) return false; // glass
                return true;
            }

            bool ShouldDrawFaceOneWay(uint firstID, uint secondID)
            {
                if (firstID == 0u) return secondID != 0u; // air
                if (firstID > 8u) return false; // block
                if (firstID >= 2u && firstID <= 8u) return true; // leaf
                if (firstID == 1u) return secondID != 1u; // glass
                return firstID == 0u;
            }

            struct V2F
            {
                float4 vertex : SV_POSITION;
                float3 globalTexcoord : TEXCOORD0;
                float3 localTexcoord : TEXCOORD1;
                nointerpolation uint primitiveID : TEXCOORD2;
            };

            V2F vert(appdata_customrendertexture v)
            {
                v2f_customrendertexture baseVertex = CustomRenderTextureVertexShader(v);
                V2F o;
                o.vertex = baseVertex.vertex;
                o.globalTexcoord = baseVertex.globalTexcoord;
                o.localTexcoord = baseVertex.localTexcoord;
                o.primitiveID = baseVertex.primitiveID;
                return o;
            }

            uint4 frag(V2F input) : SV_Target
            {
                uint2 pixel = (uint2)floor(saturate(input.localTexcoord.xy) * 16.0);
                pixel&=15u;
                uint zoneId = min(input.primitiveID, 4u);
                int2 zoneOffset = ZoneOffsets[zoneId];
                int sectionY = min((int)floor(saturate(input.globalTexcoord.z) * 16.0), 15);
                int3 worldSectionPosition = int3((_OffsetX+zoneOffset.x)&63, sectionY, (_OffsetY+zoneOffset.y)&63)<<4;
                uint3 minSideOffset = 16u;
                uint3 maxSideOffset = 0u;
                uint3 oldBlockID;

                uint i = pixel.x;
                uint j = pixel.y;

                oldBlockID.x = ReadWorldVoxel(worldSectionPosition + int3(-1, i, j));
                oldBlockID.y = ReadWorldVoxel(worldSectionPosition + int3(i, -1, j));
                oldBlockID.z = ReadWorldVoxel(worldSectionPosition + int3(i, j, -1));

                uint blockID = ReadWorldVoxel(worldSectionPosition + int3(0, i, j));
                if (ShouldDrawFaceOneWay(oldBlockID.x, blockID))
                {
                    minSideOffset.x = 0u;
                    minSideOffset.y = min(minSideOffset.y, (uint)i);
                    minSideOffset.z = min(minSideOffset.z, (uint)j);
                    maxSideOffset.y = max(maxSideOffset.y, (uint)(i + 1));
                    maxSideOffset.z = max(maxSideOffset.z, (uint)(j + 1));
                }
                oldBlockID.x = blockID;

                blockID = ReadWorldVoxel(worldSectionPosition + int3(i, 0, j));
                if (ShouldDrawFaceOneWay(oldBlockID.y, blockID))
                {
                    minSideOffset.y = 0u;
                    minSideOffset.x = min(minSideOffset.x, (uint)i);
                    minSideOffset.z = min(minSideOffset.z, (uint)j);
                    maxSideOffset.x = max(maxSideOffset.x, (uint)(i + 1));
                    maxSideOffset.z = max(maxSideOffset.z, (uint)(j + 1));
                }
                oldBlockID.y = blockID;

                blockID = ReadWorldVoxel(worldSectionPosition + int3(i, j, 0));
                if (ShouldDrawFaceOneWay(oldBlockID.z, blockID))
                {
                    minSideOffset.z = 0u;
                    minSideOffset.x = min(minSideOffset.x, (uint)i);
                    minSideOffset.y = min(minSideOffset.y, (uint)j);
                    maxSideOffset.x = max(maxSideOffset.x, (uint)(i + 1));
                    maxSideOffset.y = max(maxSideOffset.y, (uint)(j + 1));
                }
                oldBlockID.z = blockID;

                [loop] for (int k = 1; k < 16; k++)
                {
                    blockID = ReadWorldVoxel(worldSectionPosition + int3(k, i, j));
                    if (ShouldDrawFaceBidirectional(oldBlockID.x, blockID))
                    {
                        minSideOffset.x = min(minSideOffset.x, (uint)k);
                        maxSideOffset.x = max(maxSideOffset.x, (uint)k);
                        minSideOffset.y = min(minSideOffset.y, (uint)i);
                        minSideOffset.z = min(minSideOffset.z, (uint)j);
                        maxSideOffset.y = max(maxSideOffset.y, (uint)(i + 1));
                        maxSideOffset.z = max(maxSideOffset.z, (uint)(j + 1));
                    }
                    oldBlockID.x = blockID;

                    blockID = ReadWorldVoxel(worldSectionPosition + int3(i, k, j));
                    if (ShouldDrawFaceBidirectional(oldBlockID.y, blockID))
                    {
                        minSideOffset.y = min(minSideOffset.y, (uint)k);
                        maxSideOffset.y = max(maxSideOffset.y, (uint)k);
                        minSideOffset.x = min(minSideOffset.x, (uint)i);
                        minSideOffset.z = min(minSideOffset.z, (uint)j);
                        maxSideOffset.x = max(maxSideOffset.x, (uint)(i + 1));
                        maxSideOffset.z = max(maxSideOffset.z, (uint)(j + 1));
                    }
                    oldBlockID.y = blockID;

                    blockID = ReadWorldVoxel(worldSectionPosition + int3(i, j, k));
                    if (ShouldDrawFaceBidirectional(oldBlockID.z, blockID))
                    {
                        minSideOffset.z = min(minSideOffset.z, (uint)k);
                        maxSideOffset.z = max(maxSideOffset.z, (uint)k);
                        minSideOffset.x = min(minSideOffset.x, (uint)i);
                        minSideOffset.y = min(minSideOffset.y, (uint)j);
                        maxSideOffset.x = max(maxSideOffset.x, (uint)(i + 1));
                        maxSideOffset.y = max(maxSideOffset.y, (uint)(j + 1));
                    }
                    oldBlockID.z = blockID;
                }

                blockID = ReadWorldVoxel(worldSectionPosition + int3(16, i, j));
                if (ShouldDrawFaceOneWay(blockID, oldBlockID.x))
                {
                    maxSideOffset.x = 16u;
                    minSideOffset.y = min(minSideOffset.y, (uint)i);
                    minSideOffset.z = min(minSideOffset.z, (uint)j);
                    maxSideOffset.y = max(maxSideOffset.y, (uint)(i + 1));
                    maxSideOffset.z = max(maxSideOffset.z, (uint)(j + 1));
                }

                blockID = ReadWorldVoxel(worldSectionPosition + int3(i, 16, j));
                if (ShouldDrawFaceOneWay(blockID, oldBlockID.y))
                {
                    maxSideOffset.y = 16u;
                    minSideOffset.x = min(minSideOffset.x, (uint)i);
                    minSideOffset.z = min(minSideOffset.z, (uint)j);
                    maxSideOffset.x = max(maxSideOffset.x, (uint)(i + 1));
                    maxSideOffset.z = max(maxSideOffset.z, (uint)(j + 1));
                }

                blockID = ReadWorldVoxel(worldSectionPosition + int3(i, j, 16));
                if (ShouldDrawFaceOneWay(blockID, oldBlockID.z))
                {
                    maxSideOffset.z = 16u;
                    minSideOffset.x = min(minSideOffset.x, (uint)i);
                    minSideOffset.y = min(minSideOffset.y, (uint)j);
                    maxSideOffset.x = max(maxSideOffset.x, (uint)(i + 1));
                    maxSideOffset.y = max(maxSideOffset.y, (uint)(j + 1));
                }


                bool hasSurface = minSideOffset.x <= maxSideOffset.x &&
                                  minSideOffset.y <= maxSideOffset.y &&
                                  minSideOffset.z <= maxSideOffset.z;
                if (!hasSurface)
                    return uint4(0u, 0u, 0u, 0u);

                uint maxX = maxSideOffset.x;
                uint maxY = maxSideOffset.y;
                uint maxZ = maxSideOffset.z;
                uint red = (minSideOffset.x & 15u) | ((maxX & 15u) << 4);
                uint green = (minSideOffset.z & 15u) | ((maxZ & 15u) << 4);
                uint blue = (minSideOffset.y & 15u) | ((maxY & 15u) << 4);
                uint alpha = 1u |
                             ((minSideOffset.x >> 4) << 1) |
                             ((maxX >> 4) << 2) |
                             ((maxZ >> 4) << 3) |
                             ((minSideOffset.z >> 4) << 4) |
                             ((minSideOffset.y >> 4) << 5) |
                             ((maxY >> 4) << 6);
                return uint4(red, green, blue, alpha);
            }
            ENDHLSL
        }
    }
}
